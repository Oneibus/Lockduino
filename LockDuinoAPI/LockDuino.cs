// --------------------------------------------------------------------------------------------------------------------
// <copyright file="LockDuino.cs" company="Microsoft Corporation">
//   Copyright Microsoft Corporation. All rights reserved.
// </copyright>
// <summary>
//   The lock duino.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace LockDuinoAPI
{
    using System;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.IO;
    using System.IO.Ports;
    using System.Runtime.InteropServices;
    using System.Threading;
    using System.Windows.Interop;

    /// <summary>
    ///     The lockduino API class.
    /// </summary>
    [SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1650:ElementDocumentationMustBeSpelledCorrectly", Justification = "Reviewed. Suppression is OK here.")]
    public sealed class LockDuino : IDisposable
    {
        #region Constants

        /// <summary>
        ///     The default interval.
        /// </summary>
        public const int DefaultInterval = 250;

        /// <summary>
        ///     The default max tolerance range value.
        /// </summary>
        public const int DefaultMaxTolerance = 1000;

        /// <summary>
        ///     The default sample size.
        /// </summary>
        public const int DefaultSampleSize = 12;

        /// <summary>
        ///     The default standard deviation epsilon.
        /// </summary>
        public const double DefaultStdEpsilon = .66;

        /// <summary>
        ///     The default threshold.
        /// </summary>
        private const int DefaultThreshold = 180;

        /// <summary>
        ///     The device identification response.
        /// </summary>
        private const string DeviceIdentificationResponse = "I0LockDuinoV1";

        /// <summary>
        ///     The device identify command.
        /// </summary>
        private const string DeviceIdentifyCommand = "I0";

        /// <summary>
        ///     The device write configuration command.
        /// </summary>
        private const string DeviceWriteConfigurationCommand = "C0";

        /// <summary>
        ///     The device lock sound command.
        /// </summary>
        private const string DeviceLockSoundCommand = "P0";

        /// <summary>
        ///     The device read range command.
        /// </summary>
        private const string DeviceReadRangeCommand = "R0";

        /// <summary>
        ///     The device unlock sound command.
        /// </summary>
        private const string DeviceUnlockSoundCommand = "P1";

        /// <summary>
        ///     The device unlock sound command.
        /// </summary>
        private const string DeviceWarningSoundCommand = "P2";

        /// <summary>
        ///     The Windows message for session change.
        /// </summary>
        private const int WmWtssessionChange = 0x2b1;

        /// <summary>
        ///     The Windows message notification code for session locked.
        /// </summary>
        private const int WtsSessionLock = 0x7;

        /// <summary>
        ///     The Windows message notification code for session unlocked.
        /// </summary>
        private const int WtsSessionUnlock = 0x8;

        #endregion

        #region Fields

        /// <summary>
        ///     Indicates whether or not the desktop is currently locked.
        /// </summary>
        private bool desktopLocked;

        /// <summary>
        ///     The HWND source
        /// </summary>
        private HwndSource hwndSource;

        /// <summary>
        ///     The serial port.
        /// </summary>
        private SerialPort serialPort;

        /// <summary>
        ///     The smart card tripped.
        /// </summary>
        private int smartCardTripped;

        /// <summary>
        ///     The timer.
        /// </summary>
        private Timer timer;

        #endregion

        #region Constructors and Destructors

        /// <summary>
        ///     Prevents a default instance of the <see cref="LockDuino" /> class from being created.
        /// </summary>
        private LockDuino()
        {
            this.hwndSource = new HwndSource(new HwndSourceParameters { HwndSourceHook = this.WndProc });
        }

        /// <summary>
        ///     Finalizes an instance of the <see cref="LockDuino" /> class.
        /// </summary>
        ~LockDuino()
        {
            this.desktopLocked = false;
            this.Dispose(false);
        }

        #endregion

        #region Public Events

        /// <summary>
        /// The abort.
        /// </summary>
        public event EventHandler Abort;

        /// <summary>
        ///     Occurs when the workstation is locked.
        /// </summary>
        public event EventHandler Locked;

        /// <summary>
        /// The range read.
        /// </summary>
        public event EventHandler<EventArgs<int>> RangeRead;

        /// <summary>
        ///     Occurs when the workstation is unlocked.
        /// </summary>
        public event EventHandler Unlocked;

        #endregion

        #region Public Properties

        /// <summary>
        ///     Gets or sets the interval.
        /// </summary>
        /// <value>
        ///     The interval.
        /// </value>
        public int Interval { get; set; }

        /// <summary>
        ///     Gets or sets the max tolerance.
        /// </summary>
        public double MaxTolerance { get; set; }

        /// <summary>
        ///     Gets or sets the size of the sample.
        /// </summary>
        /// <value>
        ///     The size of the sample.
        /// </value>
        public int SampleSize { get; set; }

        /// <summary>
        ///     Gets or sets the standard deviation epsilon value.
        /// </summary>
        /// <value>
        ///     The episilon value used to determin stable buffer state.
        /// </value>
        public double StdEpsilon { get; set; }

        /// <summary>
        ///     Gets or sets the threshold.
        /// </summary>
        /// <value>
        ///     The threshold.
        /// </value>
        public double Threshold { get; set; }

        #endregion

        #region Public Methods and Operators

        /// <summary>
        ///     Gets the LockDuino instance.
        /// </summary>
        /// <returns>
        ///     The <see cref="LockDuino" />.
        /// </returns>
        public static LockDuino GetLockDuino()
        {
            var lockDuino = new LockDuino
            {
                serialPort = FindLockDuinoSerialPort(),
                MaxTolerance = DefaultMaxTolerance,
                Interval = DefaultInterval,
                Threshold = DefaultThreshold,
                StdEpsilon = DefaultStdEpsilon,
                SampleSize = DefaultSampleSize
            };

            if (lockDuino.serialPort == null)
            {
                throw new InvalidOperationException("LockDuino USB device not found!");
            }

            lockDuino.ClearBuffer();

            return lockDuino;
        }

        /// <summary>
        ///     Clears the Serial Port buffer for the LockDuino.
        /// </summary>
        public void ClearBuffer()
        {
            this.serialPort.DiscardInBuffer();
            this.serialPort.DiscardOutBuffer();
        }

        /// <summary>
        ///     Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Initiates the Lockduino Guard Mode.
        /// </summary>
        /// <param name="stopEvent">
        /// The stop Event.
        /// </param>
        public void GuardWorkstation(AutoResetEvent stopEvent)
        {
            var rangeSamples = new int[this.SampleSize];
            int position = 0;

            while (!stopEvent.WaitOne(this.Interval))
            {
                if (!this.desktopLocked)
                {
                    int range = 0;
                    try
                    {
                        range = this.ReadRange();
                    }
                    catch (InvalidOperationException ex)
                    {
                        if (ex.Message.Equals("The port is closed.")) // BUG: This isn't i18n-friendly
                        {
                            this.Lock();
                            this.OnAbort(new EventArgs());
                            return;
                        }
                    }

                    if (range > 0 && range < this.MaxTolerance)
                    {
                        rangeSamples[position++ % this.SampleSize] = range;
                        double culledRange = rangeSamples.CulledOutliersAverage();
                        double stddev = rangeSamples.StandardDeviation();

                        Trace.TraceInformation(
                                "Lockduino read range: {0} cm, culled avg: {1} cm, stddev: {2}",
                                range,
                                culledRange,
                                stddev);

                        // if stable signal buffer
                        if (stddev < this.StdEpsilon && culledRange > this.Threshold)
                        {
                            // got enough samples to confirm locked distance state
                            if (this.Lock())
                            {
                                rangeSamples = new int[this.SampleSize];
                                position = 0;
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        ///     Locks the workstation.
        /// </summary>
        /// <returns>
        ///     <c>true</c> if the workstation is successfully locked, otherwise false.
        /// </returns>
        public bool Lock()
        {
            if (SmartCardDetector.HasBadge() && this.smartCardTripped++ < 3)
            {
                this.SendWarningSoundCmd();
                Trace.TraceInformation("Not locking due to forgotten smartcard.");
                return false;
            }

            Trace.TraceInformation("Locking Desktop.");
            return this.desktopLocked = LockWorkStation();
        }

        /// <summary>
        ///     Reads distance, in whole centimeters, between the Lockduino sensor and the nearest solid object
        ///     (within the sensor's cone of detection).
        /// </summary>
        /// <returns>
        ///     The distance, in whole centimeters, to the nearest solid object within the sensor's cone of detection.
        /// </returns>
        public int ReadRange()
        {
            this.serialPort.WriteLine(DeviceReadRangeCommand);
            Thread.SpinWait(100);
            string deviceInput = this.serialPort.ReadExisting();

            if (deviceInput.Length <= DeviceReadRangeCommand.Length)
            {
                return -1;
            }

            int newLinePos = deviceInput.IndexOf(Environment.NewLine, StringComparison.InvariantCulture);
            int length = newLinePos - DeviceReadRangeCommand.Length;

            int range = int.Parse(deviceInput.Substring(DeviceReadRangeCommand.Length, length));

            this.OnRangeRead(new EventArgs<int>(range));

            return range;
        }

        /// <summary>
        ///     Writes the lockduino settings buffer to the device.
        /// </summary>
        public void SaveSettings(string settings)
        {
            var command = string.Format("{0}{1}", DeviceWriteConfigurationCommand, settings);
            this.serialPort.WriteLine(command);

            Thread.SpinWait(100);
            this.serialPort.ReadExisting();
        }

        /// <summary>
        /// Starts the guard worker
        /// </summary>
        /// <param name="stopEvent">
        /// The stop event.
        /// </param>
        /// <returns>
        /// The <see cref="Thread"/>.
        /// </returns>
        public Thread StartGuardWorker(AutoResetEvent stopEvent)
        {
            var t = new Thread(o => this.GuardWorkstation((AutoResetEvent)o));
            t.Start(stopEvent);
            return t;
        }

        #endregion

        #region Methods

        /// <summary>
        ///     The finds the Lockduino Serial Port
        /// </summary>
        /// <returns>
        ///     The SerialPort on which the Lockduino device resides.
        /// </returns>
        private static SerialPort FindLockDuinoSerialPort()
        {
            const int BaudRate = 57600;
            string[] portNames = SerialPort.GetPortNames();
            Trace.TraceInformation("Available COM ports {0}.", portNames.ToString());

            foreach (string comPort in portNames)
            {
                Trace.TraceInformation("Testing COM port {0}.", comPort);
                var serialPort = new SerialPort(comPort, BaudRate);

                // If a serial port is non-responsive prevent waiting forever for anything
                serialPort.WriteTimeout = 1000;

                try
                {
                    serialPort.Open();
                    Trace.TraceInformation("Opened COM port {0}.", comPort);
                }
                catch (IOException)
                {
                    continue;
                }
                catch (UnauthorizedAccessException)
                {
                    continue;
                }
                catch (InvalidOperationException)
                {
                    continue;
                }

                int i = 0;
                while (i++ < 2)
                {
                    if (i == 1)
                    {
                        serialPort.RtsEnable = true; // This is necessary for ATmega32U4-based Arduinos but will not work on other models
                    }

                    try
                    {
                        serialPort.WriteLine(DeviceIdentifyCommand);
                        Thread.Sleep(250);

                        string rsp = serialPort.ReadExisting();
                        if (rsp.StartsWith(DeviceIdentificationResponse))
                        {
                            Trace.TraceInformation("Found Lockduino on COM port {0}.", comPort);
                            return serialPort;
                        }
                    }
                    catch (TimeoutException)
                    {
                        break;
                    }
                }

                serialPort.Close();
                serialPort.Dispose();
            }

            Trace.TraceInformation("Failed to find Lockduino COM port!");
            return null;
        }

        /// <summary>
        ///     Locks the workstation
        /// </summary>
        /// <returns>
        ///     <c>true</c> if the workstation is successfully locked, otherwise <c>false</c>.
        /// </returns>
        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool LockWorkStation();

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="disposing">
        /// <c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.
        /// </param>
        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (this.timer != null)
                {
                    this.timer.Dispose();
                    this.timer = null;
                }

                if (this.serialPort != null)
                {
                    this.serialPort.Dispose();
                    this.serialPort = null;
                }

                if (this.hwndSource != null)
                {
                    this.hwndSource.Dispose();
                    this.hwndSource = null;
                }
            }
        }

        /// <summary>
        /// The on abort.
        /// </summary>
        /// <param name="e">
        /// The e.
        /// </param>
        private void OnAbort(EventArgs e)
        {
            if (this.Abort != null)
            {
                this.Abort(this, e);
            }
        }

        /// <summary>
        /// Invoked when the workstation is locked
        /// </summary>
        /// <param name="e">
        /// The event args.
        /// </param>
        private void OnLocked(EventArgs e)
        {
            Debug.WriteLine("Lockduino :: Workstation Locked.");

            this.desktopLocked = true;
            this.smartCardTripped = 0;
            this.SendLockSoundCmd();

            if (this.Locked != null)
            {
                this.Locked(this, e);
            }
        }

        /// <summary>
        /// The on range read.
        /// </summary>
        /// <param name="e">
        /// The e.
        /// </param>
        private void OnRangeRead(EventArgs<int> e)
        {
            if (this.RangeRead != null)
            {
                this.RangeRead(this, e);
            }
        }

        /// <summary>
        /// Invoked when the workstation is unlocked.
        /// </summary>
        /// <param name="e">
        /// The event args.
        /// </param>
        private void OnUnlocked(EventArgs e)
        {
            Debug.WriteLine("Lockduino :: Workstation Unlocked.");

            this.desktopLocked = false;
            this.smartCardTripped = 0;
            this.SendUnlockSoundCmd();

            if (this.Unlocked != null)
            {
                this.Unlocked(this, e);
            }
        }

        /// <summary>
        /// The send lock sound cmd.
        /// </summary>
        private void SendLockSoundCmd()
        {
            try
            {
                this.serialPort.WriteLine(DeviceLockSoundCommand);
            }
            // ReSharper disable once EmptyGeneralCatchClause
            catch
            {
            }

            Thread.SpinWait(100);
        }

        /// <summary>
        /// The send unlock sound cmd.
        /// </summary>
        private void SendUnlockSoundCmd()
        {
            try
            {
                this.serialPort.WriteLine(DeviceUnlockSoundCommand);
            }
            // ReSharper disable once EmptyGeneralCatchClause
            catch
            {
            }

            Thread.SpinWait(100);
        }

        /// <summary>
        /// The send warning sound cmd.
        /// </summary>
        private void SendWarningSoundCmd()
        {
            try
            {
                this.serialPort.WriteLine(DeviceWarningSoundCommand);
            }
            // ReSharper disable once EmptyGeneralCatchClause
            catch
            {
            }

            Thread.SpinWait(100);
        }

        /// <summary>
        /// The window procedure for session change notifications.
        /// </summary>
        /// <param name="windowHandle">
        /// The window handle.
        /// </param>
        /// <param name="message">
        /// The window message.
        /// </param>
        /// <param name="wordParam">
        /// The word param.
        /// </param>
        /// <param name="longParam">
        /// The long param.
        /// </param>
        /// <param name="isHandled">
        /// The is handled.
        /// </param>
        /// <returns>
        /// The <see cref="IntPtr"/>.
        /// </returns>
        private IntPtr WndProc(IntPtr windowHandle, int message, IntPtr wordParam, IntPtr longParam, ref bool isHandled)
        {
            isHandled = false;

            if (message != WmWtssessionChange)
            {
                return IntPtr.Zero;
            }

            switch (wordParam.ToInt32())
            {
                case WtsSessionLock:
                    this.OnLocked(new EventArgs());
                    break;
                case WtsSessionUnlock:
                    this.OnUnlocked(new EventArgs());
                    break;
            }

            return IntPtr.Zero;
        }

        #endregion
    }
}