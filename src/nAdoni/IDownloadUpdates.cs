//-----------------------------------------------------------------------
// <copyright company="NAdoni">
//     Copyright 2013 NAdoni. Licensed under the Microsoft Public License (MS-PL).
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.IO;
using System.Threading.Tasks;

namespace NAdoni
{
    /// <summary>
    /// Defines the interface for objects that handle downloading of updates.
    /// </summary>
    [Lokad.Quality.UsedImplicitly]
    public interface IDownloadUpdates
    {
        /// <summary>
        /// Starts the download of the application update.
        /// </summary>
        /// <param name="information">The object that provides all the information about the new update.</param>
        /// <returns>A task that handles the download process and returns a file object pointing to the newly downloaded file.</returns>
        Task<FileInfo> StartDownloadAsync(UpdateInformation information);

        /// <summary>
        /// An event raised when there is progress in the download.
        /// </summary>
        event EventHandler<ProgressEventArgs> OnDownloadProgress;
    }
}
