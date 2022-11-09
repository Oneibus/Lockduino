// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Program.cs" company="Microsoft Corporation">
//   Copyright Microsoft Corporation. All rights reserved.
// </copyright>
// <summary>
//   The main program module.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace LockDuinoSystemTray
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Threading;
    using System.Windows.Forms;

    using LockDuinoAPI;

    /// <summary>
    ///     The main program class.
    /// </summary>
    public static class Program
    {
        #region Static Fields

        /// <summary>
        ///     The stop event.
        /// </summary>
        private static readonly AutoResetEvent StopEvent = new AutoResetEvent(false);

        /// <summary>
        ///     Gets or sets the system tray UI component.
        /// </summary>
        public static SystemTray SystemTray { get; set; }

        /// <summary>
        /// Gets the main lockduino API instance.
        /// </summary>
        [SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1650:ElementDocumentationMustBeSpelledCorrectly", Justification = "Reviewed. Suppression is OK here.")]
        public static LockDuino MainLockDuino { get; private set; }

        /// <summary>
        /// Gets the main lock duino worker.
        /// </summary>
        [SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1650:ElementDocumentationMustBeSpelledCorrectly", Justification = "Reviewed. Suppression is OK here.")]
        public static Thread MainLockDuinoWorker { get; private set; }

        #endregion

        #region Methods

        /// <summary>
        /// Stops the lockduino worker thread.
        /// </summary>
        [SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1650:ElementDocumentationMustBeSpelledCorrectly", Justification = "Reviewed. Suppression is OK here.")]
        public static void StopLockDuinoWorker()
        {
            StopEvent.Set();
            MainLockDuinoWorker.Join();
        }

        /// <summary>
        /// Starts the lockduino worker thread.
        /// </summary>
        [SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1650:ElementDocumentationMustBeSpelledCorrectly", Justification = "Reviewed. Suppression is OK here.")]
        public static void StartLockDuinoWorker()
        {
            MainLockDuinoWorker = MainLockDuino.StartGuardWorker(StopEvent);
        }

        /// <summary>
        ///     The main entry point for the application.
        /// </summary>
        [STAThread]
        private static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            SystemTray = new SystemTray();
            SystemTray.CreateSystemTrayMenu();

            using (var lockDuino = LockDuino.GetLockDuino())
            {
                MainLockDuino = lockDuino;
                lockDuino.Interval = 100;
                lockDuino.SampleSize = 32;
                lockDuino.Threshold = Properties.Settings.Default.Seated;
                lockDuino.RangeRead += LockDuinoRangeRead;
                lockDuino.Abort += LockDuinoAbort;

                MainLockDuinoWorker = lockDuino.StartGuardWorker(StopEvent);

                Application.Run();

                StopEvent.Set();
                MainLockDuinoWorker.Join();
            }

            SystemTray.Dispose();
        }

        /// <summary>
        /// The lockduino abort event callback.
        /// </summary>
        /// <param name="sender">
        /// The sender.
        /// </param>
        /// <param name="e">
        /// The event arguments.
        /// </param>
        [SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1650:ElementDocumentationMustBeSpelledCorrectly", Justification = "Reviewed. Suppression is OK here.")]
        private static void LockDuinoAbort(object sender, EventArgs e)
        {
            Thread.Sleep(1000);
            Application.Exit();
        }

        /// <summary>
        /// The lockduino range read event callback.
        /// </summary>
        /// <param name="sender">
        /// The sender.
        /// </param>
        /// <param name="e">
        /// The event arguments.
        /// </param>
        [SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1650:ElementDocumentationMustBeSpelledCorrectly", Justification = "Reviewed. Suppression is OK here.")]
        private static void LockDuinoRangeRead(object sender, EventArgs<int> e)
        {
            //if (SystemTray.PlayRangeSounds)
            //{
            //    Console.Beep(Math.Min(e.Data * 50, 32767), 100);
            //}
        }

        #endregion
    }
}