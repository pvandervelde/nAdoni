//-----------------------------------------------------------------------
// <copyright company="NAdoni">
//     Copyright 2013 NAdoni. Licensed under the Microsoft Public License (MS-PL).
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net;
using System.Security;
using System.Security.Cryptography;
using System.Security.Cryptography.Xml;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;
using NAdoni.Properties;

namespace NAdoni
{
    /// <summary>
    /// Defines methods for finding and gathering update packages for a given application.
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
    public sealed class Updater : ICheckForUpdates, IDownloadUpdates
    {
        private static string HttpGetString(Uri url)
        {
            {
                Debug.Assert(url != null, "The URL should not be a null reference.");
            }

            try
            {
                var request = WebRequest.Create(url);
                var resp = request.GetResponse();

                var responseStream = resp.GetResponseStream();
                if (responseStream == null)
                {
                    return string.Empty;
                }

                using (var sr = new StreamReader(responseStream))
                {
                    return sr.ReadToEnd().Trim();
                }
            }
            catch (NotSupportedException)
            {
                return string.Empty;
            }
            catch (ArgumentException)
            {
                return string.Empty;
            }
            catch (SecurityException)
            {
                return string.Empty;
            }
        }

        private static bool VerifyXml(XmlDocument manifest, AsymmetricAlgorithm algorithm)
        {
            var signedXml = new SignedXml(manifest);
            var nodeList = manifest.GetElementsByTagName("Signature");
            if (nodeList.Count <= 0)
            {
                return false;
            }

            // Though it is possible to have multiple signatures on 
            // an XML document, this app only supports one signature for
            // the entire XML document.  Throw an exception 
            // if more than one signature was found.
            if (nodeList.Count > 1)
            {
                return false;
            }

            // Load the first <signature> node.  
            signedXml.LoadXml((XmlElement)nodeList[0]);

            // Check the signature and return the result.
            return signedXml.CheckSignature(algorithm);
        }

        private static int GetContentLength(string url)
        {
            WebHeaderCollection headers;
            HttpWebResponse response = null;
            try
            {
                var request = WebRequest.Create(url) as HttpWebRequest;
                request.Method = "HEAD";
                response = request.GetResponse() as HttpWebResponse;
                headers = response.Headers;
            }
            finally
            {
                if (response != null)
                {
                    response.Close();
                }
            }

            if (headers.Get("Content-Length") != null)
            {
                return int.Parse(headers.Get("Content-Length"), CultureInfo.InvariantCulture);
            }

            return -1;
        }

        /// <summary>
        /// Returns a value indicating if a new update is available.
        /// </summary>
        /// <param name="url">The URL from which the update manifest can be downloaded.</param>
        /// <param name="xmlPublicKey">The public key formatted as an XML document.</param>
        /// <param name="currentVersion">The current version of the product.</param>
        /// <returns>
        /// <see langword="true" /> if a new version of the product is available; otherwise, <see langword="false" />.
        /// </returns>
        public UpdateInformation MostRecentUpdateOnRemoteServer(Uri url, string xmlPublicKey, Version currentVersion)
        {
            {
                Lokad.Enforce.Argument(() => url);
                Lokad.Enforce.Argument(() => xmlPublicKey);
                Lokad.Enforce.Argument(() => currentVersion);
            }

            UpdateInformation info;
            var manifestXml = HttpGetString(url);
            if (string.IsNullOrWhiteSpace(manifestXml))
            {
                info = new UpdateInformation
                    {
                        LatestAvailableVersion = "0.0.0.0",
                        UpdateIsAvailableAndValid = false
                    };

                return info;
            }

            // Create a new XML document.
            var xmlDoc = new XmlDocument
                {
                    PreserveWhitespace = true
                };

            // Load an XML file into the XmlDocument object.
            xmlDoc.LoadXml(manifestXml);

            // Verify the signature of the signed XML.
            var cryptoProvider = new RSACryptoServiceProvider();
            cryptoProvider.FromXmlString(xmlPublicKey);
            if (!VerifyXml(xmlDoc, cryptoProvider))
            {
                info = new UpdateInformation
                    {
                        LatestAvailableVersion = "0.0.0.0",
                        UpdateIsAvailableAndValid = false
                    };

                return info;
            }
            
            var s = new XmlSerializer(typeof(UpdateInformation));
            using (var sr = new StringReader(manifestXml))
            {
                info = (UpdateInformation)s.Deserialize(new XmlTextReader(sr));
                info.UpdateIsAvailableAndValid = new Version(info.LatestAvailableVersion) > currentVersion;
            }

            return info;
        }

        /// <summary>
        /// Starts the download of the application update.
        /// </summary>
        /// <param name="information">The object that provides all the information about the new update.</param>
        /// <returns>A task that handles the download process and returns a file object pointing to the newly downloaded file.</returns>
        public Task<FileInfo> StartDownloadAsync(UpdateInformation information)
        {
            return Task<FileInfo>.Factory.StartNew(StartDownload, information, TaskCreationOptions.LongRunning);
        }

        private FileInfo StartDownload(object info)
        {
            var information = info as UpdateInformation;
            var contentLength = GetContentLength(information.DownloadLocation);

            var req = WebRequest.Create(information.DownloadLocation);
            var resp = req.GetResponse();
            var totalBytesTransferred = 0;

            var path = Path.GetTempFileName();
            using (var fileStream = new FileStream(path, FileMode.Create))
            {
                using (var responseStream = resp.GetResponseStream())
                {
                    var buf = new byte[2048];
                    int n;

                    do
                    {
                        n = responseStream.Read(buf, 0, buf.Length);
                        totalBytesTransferred += n;
                        if (n > 0)
                        {
                            fileStream.Write(buf, 0, n);
                        }

                        var progress = (int)(totalBytesTransferred * 100.0 / contentLength);
                        RaiseOnDownloadProgress(
                            progress, 
                            string.Format(
                                CultureInfo.InvariantCulture,
                                Resources.Progress_BytesTransferredDuringDownload_WithTotalAndExpected,
                                totalBytesTransferred,
                                contentLength));
                    } 
                    while (n > 0);
                }
            }

            return new FileInfo(path);
        }

        /// <summary>
        /// An event raised when there is progress in the download.
        /// </summary>
        public event EventHandler<ProgressEventArgs> OnDownloadProgress;

        private void RaiseOnDownloadProgress(int progress, string description)
        {
            var local = OnDownloadProgress;
            if (local != null)
            {
                local(this, new ProgressEventArgs(progress, description));
            }
        }
    }
}
