//-----------------------------------------------------------------------
// <copyright company="NAdoni">
//     Copyright 2013 NAdoni. Licensed under the Microsoft Public License (MS-PL).
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Diagnostics.CodeAnalysis;
using System.Xml.Serialization;

namespace NAdoni
{
    /// <summary>
    /// Defines a data structure for information describing an update to an application.
    /// </summary>
    [XmlRoot]
    public sealed class UpdateInformation
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="UpdateInformation"/> class.
        /// </summary>
        public UpdateInformation()
        {
            Timestamp = DateTime.Now;
        }

        /// <summary>
        /// Gets or sets the latest available version of the application.
        /// </summary>
        [XmlElement]
        public string LatestAvailableVersion
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the product name of the application.
        /// </summary>
        [XmlElement]
        public string ProductName
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the time at which the current update information object was created.
        /// </summary>
        [XmlElement]
        public DateTime Timestamp
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the hash for the update package file.
        /// </summary>
        [XmlElement(DataType = "hexBinary")]
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays",
            Justification = "The hash is stored as an array of bytes.")]
        public byte[] Hash
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the fully qualified name of the hash provider that was used to create the file hash for the 
        /// update package.
        /// </summary>
        [XmlElement]
        public string HashType
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the URL from which the update package can be downloaded.
        /// </summary>
        [XmlElement]
        public string DownloadLocation
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the XML element that contains the cryptographic signature of the update XML file.
        /// </summary>
        [XmlAnyElement]
        public System.Xml.XmlElement Signature
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a value indicating whether the update information is available and valid.
        /// </summary>
        [XmlIgnore]
        public bool UpdateIsAvailableAndValid
        {
            get;
            set;
        }
    }
}
