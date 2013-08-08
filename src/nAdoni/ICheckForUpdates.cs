//-----------------------------------------------------------------------
// <copyright company="NAdoni">
//     Copyright 2013 NAdoni. Licensed under the Microsoft Public License (MS-PL).
// </copyright>
//-----------------------------------------------------------------------

using System;

namespace NAdoni
{
    /// <summary>
    /// Defines the interface for objects that perform checks for updates.
    /// </summary>
    public interface ICheckForUpdates
    {
        /// <summary>
        /// Returns an object providing information about the most recent update available on the remote server.
        /// </summary>
        /// <param name="url">The URL from which the update manifest can be downloaded.</param>
        /// <param name="xmlPublicKey">The public key formatted as an XML document.</param>
        /// <param name="currentVersion">The current version of the product.</param>
        /// <returns>
        /// An object that contains the update information for the most recent update available on the 
        /// remote server.
        /// </returns>
        UpdateInformation MostRecentUpdateOnRemoteServer(
            Uri url, 
            string xmlPublicKey, 
            Version currentVersion);
    }
}
