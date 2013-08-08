//-----------------------------------------------------------------------
// <copyright company="NAdoni">
//     Copyright 2013 NAdoni. Licensed under the Microsoft Public License (MS-PL).
// </copyright>
//-----------------------------------------------------------------------

using System;
using NAdoni.Properties;

namespace NAdoni
{
    /// <summary>
    /// Event arguments used for startup progress events.
    /// </summary>
    [Serializable]
    public sealed class ProgressEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ProgressEventArgs"/> class.
        /// </summary>
        /// <param name="progress">The progress percentage, ranging from 0 to 100.</param>
        /// <param name="description">The description for the current progress point.</param>
        public ProgressEventArgs(int progress, string description)
        {
            {
                Lokad.Enforce.With<ArgumentOutOfRangeException>(progress >= 0, Resources.Exceptions_Messages_ProgressToSmall, progress);
                Lokad.Enforce.With<ArgumentOutOfRangeException>(progress <= 100, Resources.Exceptions_Messages_ProgressToLarge, progress);
            }

            Progress = progress;
            Description = description;
        }

        /// <summary>
        /// Gets the progress percentage, ranging from 0 to 100.
        /// </summary>
        public int Progress
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the description for the current progress point.
        /// </summary>
        public string Description
        {
            get;
            private set;
        }
    }
}
