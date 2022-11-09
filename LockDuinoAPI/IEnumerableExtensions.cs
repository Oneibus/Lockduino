// --------------------------------------------------------------------------------------------------------------------
// <copyright file="IEnumerableExtensions.cs" company="Microsoft">
//   Copyright Microsoft Corporation. All rights reserved.
// </copyright>
// <summary>
//   Contains extension methods for the IEnumerable interface.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace LockDuinoAPI
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    ///     Contains extension methods for the IEnumerable interface.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    public static class IEnumerableExtensions
    {
        #region Public Methods and Operators

        /// <summary>
        /// Culls outliers by reducing outliers to current average value then averaging resultant range data
        /// </summary>
        /// <param name="collection">
        /// The range data array.
        /// </param>
        /// <returns>
        /// The double value representing the new culled average
        /// </returns>
        public static double CulledOutliersAverage(this IEnumerable<int> collection)
        {
            int[] array = collection.ToArray();

            double avg = array.Average();
            double sdv = Math.Sqrt(array.Select(v => (v - avg) * (v - avg)).Sum() / array.Length);

            double smoothedAvg = array.Where(v => Math.Abs(v - avg) <= sdv).Average();

            return smoothedAvg;
        }

        /// <summary>
        /// Means the specified collection.
        /// </summary>
        /// <param name="collection">
        /// The collection.
        /// </param>
        /// <returns>
        /// The mean value of the collection.
        /// </returns>
        public static int Mean(this IEnumerable<int> collection)
        {
            int[] array = collection.ToArray();

            Array.Sort(array, (left, right) => left.CompareTo(right));

            var newArray = new int[array.Length - 2];

            Array.Copy(array, 1, newArray, 0, array.Length - 2);

            return (int)newArray.Average();
        }

        /// <summary>
        /// Calculates StandardDeviation for the array values.
        /// </summary>
        /// <param name="collection">
        /// The range data array.
        /// </param>
        /// <returns>
        /// The double value representing the standard deviation
        /// </returns>
        public static double StandardDeviation(this IEnumerable<int> collection)
        {
            int[] array = collection.ToArray();

            double avg = array.Average();
            double sdv = Math.Sqrt(array.Select(v => (v - avg) * (v - avg)).Sum() / array.Length);

            return sdv;
        }

        #endregion
    }
}