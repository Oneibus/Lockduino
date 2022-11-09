// --------------------------------------------------------------------------------------------------------------------
// <copyright file="EventArgs.cs"  company="Microsoft">
//   Copyright Microsoft Corporation. All rights reserved.
// </copyright>
// <summary>
//   An Event Args class with a generic type parameter for additional data.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace LockDuinoAPI
{
    using System;

    /// <summary>
    /// An Event Args class with a generic type parameter for additional data.
    /// </summary>
    /// <typeparam name="T">
    /// The type of the additional data.
    /// </typeparam>
    public class EventArgs<T>
        : EventArgs
    {
        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="EventArgs{T}"/> class.
        /// </summary>
        /// <param name="data">The data.</param>
        public EventArgs(T data)
        {
            this.Data = data;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets the data.
        /// </summary>
        /// <value>
        /// The data.
        /// </value>
        public T Data
        {
            get; private set;
        }

        #endregion
    }
}