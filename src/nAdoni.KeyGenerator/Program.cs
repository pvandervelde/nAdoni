//-----------------------------------------------------------------------
// <copyright company="NAdoni">
//     Copyright 2013 NAdoni. Licensed under the Microsoft Public License (MS-PL).
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Xml.Linq;
using Mono.Options;
using NAdoni.KeyGenerator.Properties;
using Nuclei;

namespace NAdoni.KeyGenerator
{
    /// <summary>
    /// Defines the main entry point for the console application that is used to create key files.
    /// </summary>
    [SuppressMessage("Microsoft.StyleCop.CSharp.MaintainabilityRules", "SA1400:AccessModifierMustBeDeclared",
            Justification = "Access modifiers should not be declared on the entry point for a command line application. See FxCop.")]
    class Program
    {
        /// <summary>
        /// The exit code used when the application exits normally.
        /// </summary>
        private const int NormalApplicationExitCode = 0;

        /// <summary>
        /// The exit code used when the application experiences an unhandled exception.
        /// </summary>
        private const int UnhandledExceptionExitCode = 1;

        /// <summary>
        /// The exit code used when the application has been provided with one or more invalid
        /// command line parameters.
        /// </summary>
        private const int InvalidCommandLineParametersExitCode = 2;

        /// <summary>
        /// The exit code used when the application has shown the help information.
        /// </summary>
        private const int HelpShownExitCode = 3;

        /// <summary>
        /// The size of the key.
        /// </summary>
        private static int s_KeySize = 2048;

        /// <summary>
        /// The path to the file that will hold the public key.
        /// </summary>
        private static string s_PublicKeyFile;

        /// <summary>
        /// The path to the file that will contain the private and public key.
        /// </summary>
        private static string s_PrivateKeyFile;

        /// <summary>
        /// A flag indicating if the help information for the application should be displayed.
        /// </summary>
        private static bool s_ShouldDisplayHelp;

        [STAThread]
        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes",
            Justification = "We're just catching and then exiting the app.")]
        static int Main(string[] args)
        {
            try
            {
                return RunApplication(args);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return UnhandledExceptionExitCode;
            }
        }

        private static int RunApplication(IEnumerable<string> args)
        {
            ShowHeader();

            var options = CreateOptionSet();
            try
            {
                options.Parse(args);
            }
            catch (OptionException)
            {
                WriteErrorToConsole(Resources.Output_Error_InvalidInput);
                return InvalidCommandLineParametersExitCode;
            }

            if (s_ShouldDisplayHelp)
            {
                ShowHelp(options);
                return HelpShownExitCode;
            }

            if (string.IsNullOrWhiteSpace(s_PrivateKeyFile)
                || string.IsNullOrEmpty(s_PublicKeyFile)
                || (s_KeySize <= 0))
            {
                WriteErrorToConsole(Resources.Output_Error_MissingValues);
                ShowHelp(options);
                return InvalidCommandLineParametersExitCode;
            }

            return CreateKeyFile();
        }

        private static void ShowHeader()
        {
            Console.WriteLine(Resources.Header_ApplicationAndVersion, GetVersion());
            Console.WriteLine(GetCopyright());
            Console.WriteLine(GetLibraryLicenses());
        }

        private static void ShowHelp(OptionSet argProcessor)
        {
            Console.WriteLine(Resources.Help_Usage_Intro);
            Console.WriteLine();
            argProcessor.WriteOptionDescriptions(Console.Out);
        }

        private static string GetVersion()
        {
            var attribute = Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyFileVersionAttribute), false);
            Debug.Assert(attribute.Length == 1, "There should be a copyright attribute.");

            return (attribute[0] as AssemblyFileVersionAttribute).Version;
        }

        private static string GetCopyright()
        {
            var attribute = Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyCopyrightAttribute), false);
            Debug.Assert(attribute.Length == 1, "There should be a copyright attribute.");

            return (attribute[0] as AssemblyCopyrightAttribute).Copyright;
        }

        private static string GetLibraryLicenses()
        {
            var licenseXml = EmbeddedResourceExtracter.LoadEmbeddedStream(
                Assembly.GetExecutingAssembly(),
                @"NAdoni.KeyGenerator.Properties.licenses.xml");
            var doc = XDocument.Load(licenseXml);
            var licenses = from element in doc.Descendants("package")
                           select new
                           {
                               Id = element.Element("id").Value,
                               Version = element.Element("version").Value,
                               Source = element.Element("url").Value,
                               License = element.Element("licenseurl").Value,
                           };

            var builder = new StringBuilder();
            builder.AppendLine(Resources.Header_OtherPackages_Intro);
            foreach (var license in licenses)
            {
                builder.AppendLine(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        Resources.Header_OtherPackages_IdAndLicense,
                        license.Id,
                        license.Version,
                        license.Source));
            }

            return builder.ToString();
        }

        private static void WriteErrorToConsole(string errorText)
        {
            try
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Error.WriteLine(errorText);
            }
            finally
            {
                Console.ResetColor();
            }
        }

        private static void WriteToConsole(string text)
        {
            Console.WriteLine(text);
        }

        private static OptionSet CreateOptionSet()
        {
            var options = new OptionSet 
                {
                    { 
                        Resources.CommandLine_Options_Help_Key, 
                        Resources.CommandLine_Options_Help_Description, 
                        v => s_ShouldDisplayHelp = v != null
                    },
                    {
                        Resources.CommandLine_Options_PrivatePath_Key,
                        Resources.CommandLine_Options_PrivatePath_Description,
                        v => s_PrivateKeyFile = v
                    },
                    {
                        Resources.CommandLine_Options_PublicPath_Key,
                        Resources.CommandLine_Options_PublicPath_Description,
                        v => s_PublicKeyFile = v
                    },
                    {
                        Resources.CommandLine_Options_KeySize_Key,
                        Resources.CommandLine_Options_KeySize_Description,
                        (int v) => s_KeySize = v
                    },
                };
            return options;
        }

        private static int CreateKeyFile()
        {
            using (var rsa = new RSACryptoServiceProvider(s_KeySize))
            {
                try
                {
                    // Write the public key file
                    using (var writer = new StreamWriter(new FileStream(s_PublicKeyFile, FileMode.Create, FileAccess.Write, FileShare.None)))
                    {
                        var publicKeyXml = rsa.ToXmlString(false);
                        writer.Write(publicKeyXml);

                        Console.WriteLine(
                            string.Format(
                                CultureInfo.InvariantCulture,
                                Resources.Output_Information_WrotePublicKeyFile_WithFilePath,
                                s_PublicKeyFile));
                    }

                    // Write the privat key file
                    using (var writer = new StreamWriter(new FileStream(s_PrivateKeyFile, FileMode.Create, FileAccess.Write, FileShare.None)))
                    {
                        var privateKeyXml = rsa.ToXmlString(true);
                        writer.Write(privateKeyXml);

                        Console.WriteLine(
                            string.Format(
                                CultureInfo.InvariantCulture,
                                Resources.Output_Information_WrotePrivateKeyFile_WithFilePath,
                                s_PrivateKeyFile));
                    }
                }
                finally
                {
                    // Make sure we're not storing this key in the machine container.
                    rsa.PersistKeyInCsp = false;
                }
            }

            return NormalApplicationExitCode;
        }
    }
}
