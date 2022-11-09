// --------------------------------------------------------------------------------------------------------------------
// <copyright file="LockDuinoSettings.cs" company="Microsoft">
//   Copyright Microsoft Corporation. All rights reserved.
// </copyright>
// <summary>
//   The Lockduino device settings.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace LockDuinoAPI
{
    using System;
    using System.Diagnostics.CodeAnalysis;

    /// <summary>
    /// The lockduino device settings.
    /// </summary>
    [SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1650:ElementDocumentationMustBeSpelledCorrectly", Justification = "Reviewed. Suppression is OK here."),Serializable]
    public class LockDuinoSettings
    {
        #region Public Properties

        /// <summary>
        ///     Gets or sets the average distance when absent.
        /// </summary>
        /// <value>
        ///     The average distance when absent.
        /// </value>
        public double AverageDistanceWhenAbsent { get; set; }

        /// <summary>
        ///     Gets or sets the average distance when present.
        /// </summary>
        /// <value>
        ///     The average distance when present.
        /// </value>
        public double AverageDistanceWhenPresent { get; set; }

        #endregion
    }
}