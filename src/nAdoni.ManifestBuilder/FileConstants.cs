//-----------------------------------------------------------------------
// <copyright company="NAdoni">
//     Copyright 2013 NAdoni. Licensed under the Microsoft Public License (MS-PL).
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.IO;

namespace NAdoni.ManifestBuilder
{
    /// <summary>
    /// Defines a set of values related to files and file paths.
    /// </summary>
    [Serializable]
    internal sealed class FileConstants
    {
        /// <summary>
        /// The object that stores constant values for the application.
        /// </summary>
        private readonly ApplicationConstants m_Constants;

        /// <summary>
        /// Initializes a new instance of the <see cref="FileConstants"/> class.
        /// </summary>
        /// <param name="constants">The object that stores constant values for the application.</param>
        public FileConstants(ApplicationConstants constants)
        {
            {
                Lokad.Enforce.Argument(() => constants);
            }

            m_Constants = constants;
        }

        /// <summary>
        /// Gets the extension for an assembly file.
        /// </summary>
        /// <value>The extension for an assembly file.</value>
        public string AssemblyExtension
        {
            get
            {
                return "dll";
            }
        }

        /// <summary>
        /// Gets the extension for a log file.
        /// </summary>
        /// <value>The extension for a log file.</value>
        public string LogExtension
        {
            get
            {
                return "log";
            }
        }

        /// <summary>
        /// Gets the extension for a feedback file.
        /// </summary>
        public string FeedbackReportExtension
        {
            get
            {
                return "nsdump";
            }
        }

        /// <summary>
        /// Returns the path for the directory in the AppData directory which contains
        /// all the product directories for the current company.
        /// </summary>
        /// <returns>
        /// The full path for the AppData directory for the current company.
        /// </returns>
        public string CompanyCommonPath()
        {
            var appDataDir = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
            var companyDirectory = Path.Combine(appDataDir, m_Constants.CompanyName);

            return companyDirectory;
        }

        /// <summary>
        /// Returns the path for the directory in the user specific AppData directory which contains
        /// all the product directories for the current company.
        /// </summary>
        /// <returns>
        /// The full path for the AppData directory for the current company.
        /// </returns>
        public string CompanyUserPath()
        {
            var appDataDir = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var companyDirectory = Path.Combine(appDataDir, m_Constants.CompanyName);

            return companyDirectory;
        }

        /// <summary>
        /// Returns the path for the directory where the global 
        /// settings for the product are written to.
        /// </summary>
        /// <returns>
        /// The full path for the directory where the global settings
        /// for the product are written to.
        /// </returns>
        public string ProductSettingsCommonPath()
        {
            var companyDirectory = CompanyCommonPath();
            var productDirectory = Path.Combine(companyDirectory, m_Constants.ApplicationName);
            var versionDirectory = Path.Combine(productDirectory, m_Constants.ApplicationCompatibilityVersion.ToString(2));

            return versionDirectory;
        }

        /// <summary>
        /// Returns the path for the directory where the global 
        /// settings for the product are written to.
        /// </summary>
        /// <returns>
        /// The full path for the directory where the global settings
        /// for the product are written to.
        /// </returns>
        public string ProductSettingsUserPath()
        {
            var companyDirectory = CompanyUserPath();
            var productDirectory = Path.Combine(companyDirectory, m_Constants.ApplicationName);
            var versionDirectory = Path.Combine(productDirectory, m_Constants.ApplicationCompatibilityVersion.ToString(2));

            return versionDirectory;
        }

        /// <summary>
        /// Returns the path for the directory where the log files are
        /// written to.
        /// </summary>
        /// <returns>
        /// The full path for the directory where the log files are written to.
        /// </returns>
        public string LogPath()
        {
            var versionDirectory = ProductSettingsCommonPath();
            var logDirectory = Path.Combine(versionDirectory, "logs");

            return logDirectory;
        }
    }
}
