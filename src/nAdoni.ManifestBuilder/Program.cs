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
using System.Security.Cryptography.Xml;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;
using Autofac;
using Mono.Options;
using NAdoni.ManifestBuilder.Properties;
using Nuclei;
using Nuclei.Diagnostics;
using Nuclei.Diagnostics.Logging;

namespace NAdoni.ManifestBuilder
{
    /// <summary>
    /// Defines the main entry point for the console application that is used to create Auto-update manifest files.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This code is heavily based on the code presented in the following article: 
    /// http://blogs.msdn.com/b/dotnetinterop/archive/2008/03/28/simple-auto-update-for-wpf-apps.aspx
    /// </para>
    /// <para>
    /// The code in the article is licensed with the Ms-PL license
    /// </para>
    /// </remarks>
    [SuppressMessage("Microsoft.StyleCop.CSharp.MaintainabilityRules", "SA1400:AccessModifierMustBeDeclared",
            Justification = "Access modifiers should not be declared on the entry point for a command line application. See FxCop.")]
    static class Program
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
        /// The dependency injection container.
        /// </summary>
        private static IContainer s_Container;

        /// <summary>
        /// The object that provides the diagnostics methods for the application.
        /// </summary>
        private static SystemDiagnostics s_Diagnostics;

        /// <summary>
        /// A flag indicating if the help information for the application should be displayed.
        /// </summary>
        private static bool s_ShouldDisplayHelp;

        /// <summary>
        /// The version of the application for which the manifest is being generated.
        /// </summary>
        private static Version s_Version;

        /// <summary>
        /// The product name of the application for which the manifest is being generated.
        /// </summary>
        private static string s_ProductName;

        /// <summary>
        /// The full path to the package file that contains the application binaries for purposes
        /// of generating the hashes.
        /// </summary>
        private static string s_FilePath;

        /// <summary>
        /// The URL which points to the package file.
        /// </summary>
        private static string s_DownloadUrl;

        /// <summary>
        /// The name of the key container that contains the key that will be used to sign the manifest.
        /// </summary>
        private static string s_KeyContainer;

        /// <summary>
        /// The name of the file that contains the key that will be used to sign the manifest.
        /// </summary>
        private static string s_KeyFile;

        /// <summary>
        /// The full path to which the generated manifest file should be written.
        /// </summary>
        private static string s_OutputPath;

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

        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes",
            Justification = "We're just catching and then exiting the app.")]
        private static int RunApplication(IEnumerable<string> args)
        {
            try
            {
                ShowHeader();
                LoadContainer();
                LogStartup();

                var options = CreateOptionSet();
                try
                {
                    options.Parse(args);
                }
                catch (OptionException e)
                {
                    s_Diagnostics.Log(
                            LevelToLog.Fatal,
                            string.Format(
                                CultureInfo.InvariantCulture,
                                Resources.Log_Error_InvalidInputParameters_WithException,
                                e));

                    WriteErrorToConsole(Resources.Output_Error_InvalidInput);
                    return InvalidCommandLineParametersExitCode;
                }

                if (s_ShouldDisplayHelp)
                {
                    ShowHelp(options);
                    return HelpShownExitCode;
                }
                
                if (string.IsNullOrWhiteSpace(s_ProductName)
                    || string.IsNullOrEmpty(s_DownloadUrl)
                    || string.IsNullOrEmpty(s_FilePath)
                    || string.IsNullOrEmpty(s_OutputPath)
                    || (string.IsNullOrEmpty(s_KeyContainer) && string.IsNullOrEmpty(s_KeyFile)))
                {
                    s_Diagnostics.Log(LevelToLog.Fatal, Resources.Output_Error_MissingValues);
                    WriteErrorToConsole(Resources.Output_Error_MissingValues);
                    ShowHelp(options);
                    return InvalidCommandLineParametersExitCode;
                }

                WriteInputParametersToLog(
                    s_Version, 
                    s_ProductName, 
                    s_FilePath, 
                    s_DownloadUrl, 
                    s_KeyContainer,
                    s_KeyFile,
                    s_OutputPath);

                return GenerateManifest();
            }
            finally
            {
                if (s_Container != null)
                {
                    s_Container.Dispose();
                }
            }
        }

        private static void LoadContainer()
        {
            s_Container = DependencyInjection.CreateContainer();
            s_Diagnostics = s_Container.Resolve<SystemDiagnostics>();
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

        private static void WriteErrorToConsole(string errorText)
        {
            try
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(errorText);
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
                @"NAdoni.ManifestBuilder.Properties.licenses.xml");
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

        private static void LogStartup()
        {
            s_Diagnostics.Log(
                LevelToLog.Info,
                string.Format(
                    CultureInfo.CurrentCulture,
                    Resources.Log_Information_ApplicationAndVersion,
                    GetVersion()));
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
                        Resources.CommandLine_Options_Version_Key,
                        Resources.CommandLine_Options_Version_Description,
                        v => s_Version = Version.Parse(v)
                    },
                    {
                        Resources.CommandLine_Options_ProductName_Key,
                        Resources.CommandLine_Options_ProductName_Description,
                        v => s_ProductName = v
                    },
                    {
                        Resources.CommandLine_Options_FilePath_Key,
                        Resources.CommandLine_Options_FilePath_Description,
                        v => s_FilePath = v
                    },
                    {
                        Resources.CommandLine_Options_DownloadUrl_Key,
                        Resources.CommandLine_Options_DownloadUrl_Description,
                        v => s_DownloadUrl = v
                    },
                    {
                        Resources.CommandLine_Options_KeyContainer_Key,
                        Resources.CommandLine_Options_KeyContainer_Description,
                        v => s_KeyContainer = v
                    },
                    {
                        Resources.CommandLine_Options_KeyFile_Key,
                        Resources.CommandLine_Options_KeyFile_Description,
                        v => s_KeyFile = v
                    },
                    {
                        Resources.CommandLine_Options_OutputPath_Key,
                        Resources.CommandLine_Options_OutputPath_Description,
                        v => s_OutputPath = v
                    },
                };
            return options;
        }

        private static void WriteInputParametersToLog(
            Version version,
            string applicationName,
            string filePath,
            string downloadUrl,
            string keyContainer,
            string keyFile,
            string outputPath)
        {
            s_Diagnostics.Log(
                LevelToLog.Trace,
                string.Format(
                    CultureInfo.InvariantCulture,
                    Resources.Log_Information_InputParameterApplicationName,
                    applicationName));

            s_Diagnostics.Log(
                LevelToLog.Trace,
                string.Format(
                    CultureInfo.InvariantCulture,
                    Resources.Log_Information_InputParameterApplicationVersion,
                    version));

            s_Diagnostics.Log(
                LevelToLog.Trace,
                string.Format(
                    CultureInfo.InvariantCulture,
                    Resources.Log_Information_InputParameterFilePath,
                    filePath));

            s_Diagnostics.Log(
                LevelToLog.Trace,
                string.Format(
                    CultureInfo.InvariantCulture,
                    Resources.Log_Information_InputParameterDownloadUri,
                    downloadUrl));

            s_Diagnostics.Log(
                LevelToLog.Trace,
                string.Format(
                    CultureInfo.InvariantCulture,
                    Resources.Log_Information_InputParameterKeyContainer,
                    keyContainer));

            s_Diagnostics.Log(
                LevelToLog.Trace,
                string.Format(
                    CultureInfo.InvariantCulture,
                    Resources.Log_Information_InputParameterKeyFile,
                    keyFile));

            s_Diagnostics.Log(
                LevelToLog.Trace,
                string.Format(
                    CultureInfo.InvariantCulture,
                    Resources.Log_Information_InputParameterOutputPath,
                    outputPath));
        }

        private static int GenerateManifest()
        {
            WriteToConsole(Resources.Output_Information_WritingManifest);
            s_Diagnostics.Log(LevelToLog.Info, Resources.Log_Information_WritingManifest);

            var hash = ComputeHash(s_FilePath);
            var info = new UpdateInformation
                {
                    ProductName = s_ProductName,
                    LatestAvailableVersion = s_Version.ToString(4),
                    Hash = hash,
                    HashType = GetHashAlgorithm().GetType().FullName,
                    DownloadLocation = s_DownloadUrl,
                };

            var s = new XmlSerializer(typeof(UpdateInformation));
            string manifest = SerializeToString(s, info, "urn:Nuclei.AutoUpdate");

            WriteToConsole(Resources.Output_Information_SigningManifest);
            s_Diagnostics.Log(LevelToLog.Info, Resources.Log_Information_SigningManifest);
            using (var rsa = CreateCryptoServiceProvider())
            {
                var signedManifestDoc = SignManifest(manifest, rsa);
                using (var stream = new FileStream(s_OutputPath, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    signedManifestDoc.Save(stream);
                }
            }

            return NormalApplicationExitCode;
        }

        private static byte[] ComputeHash(string filePath)
        {
            var algorithm = GetHashAlgorithm();
            using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                return algorithm.ComputeHash(fileStream);
            }
        }

        private static HashAlgorithm GetHashAlgorithm()
        {
            return new SHA256CryptoServiceProvider();
        }

        private static string SerializeToString(XmlSerializer serializer, UpdateInformation info, string xmlNamespace)
        {
            var n = new XmlSerializerNamespaces();
            n.Add(string.Empty, xmlNamespace);
            var builder = new StringBuilder();
            var settings = new XmlWriterSettings { OmitXmlDeclaration = true, Indent = true };

            using (var writer = XmlWriter.Create(builder, settings))
            {
                serializer.Serialize(writer, info, n);
            }

            return builder.ToString();
        }

        private static RSACryptoServiceProvider CreateCryptoServiceProvider()
        {
            if (!string.IsNullOrEmpty(s_KeyContainer))
            {
                var parameters = new CspParameters
                {
                    KeyContainerName = s_KeyContainer,
                };
                return new RSACryptoServiceProvider(parameters);
            }

            using (var reader = new StreamReader(new FileStream(s_KeyFile, FileMode.Open, FileAccess.Read)))
            {
                var xmlString = reader.ReadToEnd();
                var rsa = new RSACryptoServiceProvider();
                rsa.FromXmlString(xmlString);

                return rsa;
            }
        }

        private static XmlDocument SignManifest(string manifest, AsymmetricAlgorithm key)
        {
            var doc = new XmlDocument
                {
                    PreserveWhitespace = true
                };
            doc.LoadXml(manifest);

            var signedDocument = new SignedXml(doc)
                {
                    SigningKey = key
                };

            // Create a reference to be signed.
            var reference = new Reference
                {
                    Uri = string.Empty
                };

            // Add an enveloped transformation to the reference.
            var env = new XmlDsigEnvelopedSignatureTransform();
            reference.AddTransform(env);

            // Add the reference to the SignedXml object.
            signedDocument.AddReference(reference);

            // Compute the signature.
            signedDocument.ComputeSignature();

            // Get the XML representation of the signature and save
            // it to an XmlElement object.
            XmlElement xmlDigitalSignature = signedDocument.GetXml();

            // Append the element to the XML document.
            doc.DocumentElement.AppendChild(doc.ImportNode(xmlDigitalSignature, true));

            return doc;
        }
    }
}
