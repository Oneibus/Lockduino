// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CalibrationForm.cs" company="">
//   
// </copyright>
// <summary>
//   The calibration form.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace LockDuinoSystemTray
{
    using System;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Globalization;
    using System.Threading;
    using System.Windows.Forms;

    using LockDuinoAPI;

    using LockDuinoSystemTray.Properties;

    using Timer = System.Windows.Forms.Timer;

    /// <summary>
    /// The calibration form.
    /// </summary>
    public partial class CalibrationForm : Form
    {
        #region Constants

        /// <summary>
        /// The rest time milliseconds.
        /// </summary>
        private const int restTimeMilliseconds = 250;

        /// <summary>
        /// The sample size.
        /// </summary>
        private const int sampleSize = 16;

        /// <summary>
        /// The wait time milliseconds.
        /// </summary>
        private const int waitTimeMilliseconds = 10000;

        #endregion

        #region Fields

        /// <summary>
        /// The lock duino settings.
        /// </summary>
        private readonly LockDuinoSettings lockDuinoSettings;

        /// <summary>
        /// The saved sounds setting.
        /// </summary>
        private readonly bool savedSoundsSetting;

        /// <summary>
        /// The state timer.
        /// </summary>
        private readonly Timer stateTimer;

        /// <summary>
        /// The calibration state.
        /// </summary>
        private int calibrationState;

        #endregion

        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="CalibrationForm"/> class.
        /// </summary>
        public CalibrationForm()
        {
            this.InitializeComponent();

            this.savedSoundsSetting = Program.SystemTray.PlayRangeSounds;
            this.textBoxSeated.Text = (Settings.Default.Seated - 50).ToString(CultureInfo.InvariantCulture);
            this.textBoxAway.Text = Settings.Default.Away.ToString(CultureInfo.InvariantCulture);

            Program.StopLockDuinoWorker();

            this.lockDuinoSettings = new LockDuinoSettings();
            this.stateTimer = new Timer();
            this.stateTimer.Tick += this.stateTimer_Tick;
            this.calibrationState = 0;
            this.SetUIState();
        }

        #endregion

        #region Methods

        /// <summary>
        /// The on closing.
        /// </summary>
        /// <param name="e">
        /// The e.
        /// </param>
        protected override void OnClosing(CancelEventArgs e)
        {
            if (this.calibrationState == 3)
            {
                Settings.Default.Seated = this.lockDuinoSettings.AverageDistanceWhenPresent + 50;
                Settings.Default.Away = this.lockDuinoSettings.AverageDistanceWhenAbsent;
                Settings.Default.Save();
            }

            Program.SystemTray.PlayRangeSounds = this.savedSoundsSetting;
            Program.StartLockDuinoWorker();
            base.OnClosing(e);
        }

        /// <summary>
        /// The calibrate away.
        /// </summary>
        private void CalibrateAway()
        {
            var sampleBuffer = new int[sampleSize];

            Program.SystemTray.PlayRangeSounds = true;

            double averageDistance = 0.0;
            for (int i = 0; i < sampleSize; i++)
            {
                sampleBuffer[i] = Program.MainLockDuino.ReadRange();

                Debug.WriteLine("Reading sample {0}", sampleBuffer[i]);
                Thread.Sleep(restTimeMilliseconds);
            }

            averageDistance = sampleBuffer.CulledOutliersAverage();
            this.textBoxAway.Text = averageDistance.ToString(CultureInfo.InvariantCulture) + " cm";

            this.lockDuinoSettings.AverageDistanceWhenAbsent = Math.Max(
                averageDistance, 
                this.lockDuinoSettings.AverageDistanceWhenPresent + 100);
            this.calibrationState = 3;
            this.SetUIState();

            Console.Beep(1000, 500);
        }

        /// <summary>
        /// The calibrate seated.
        /// </summary>
        private void CalibrateSeated()
        {
            var sampleBuffer = new int[sampleSize];

            Program.SystemTray.PlayRangeSounds = true;

            double averageDistance = 0.0;

            for (int i = 0; i < sampleSize; i++)
            {
                sampleBuffer[i] = Program.MainLockDuino.ReadRange();
                Debug.WriteLine("Reading sample {0}", sampleBuffer[i]);

                Thread.Sleep(restTimeMilliseconds);
            }

            averageDistance = sampleBuffer.CulledOutliersAverage();
            this.textBoxSeated.Text = averageDistance.ToString(CultureInfo.InvariantCulture) + " cm";

            this.lockDuinoSettings.AverageDistanceWhenPresent = averageDistance;

            this.calibrationState = 1;
            this.SetUIState();
        }

        /// <summary>
        /// The calibrate wait to away.
        /// </summary>
        private void CalibrateWaitToAway()
        {
            this.calibrationState = 2;
            this.SetUIState();
            this.stateTimer.Interval = waitTimeMilliseconds;
            this.stateTimer.Start();
        }

        /// <summary>
        /// The calibration form_ load.
        /// </summary>
        /// <param name="sender">
        /// The sender.
        /// </param>
        /// <param name="e">
        /// The e.
        /// </param>
        private void CalibrationForm_Load(object sender, EventArgs e)
        {
        }

        /// <summary>
        /// The set ui state.
        /// </summary>
        private void SetUIState()
        {
            switch (this.calibrationState)
            {
                case 0:
                    this.textBoxPrompt.Text =
                        "Now calibrating the Lockduino.  Please sit (or stand) at your workstation as you normally would."
                        + "Click GO to continue, then please remain at your workstation until the next prompt appears.";

                    break;

                case 1:
                    this.textBoxPrompt.Text =
                        "Lockduino now needs to get an average reading of the distance when you are absent from your desk. "
                        + "Click GO now and step away from your desktop.  After 10 seconds, LockDuino will measure the distance."
                        + "Wait until you hear the beep and then return to your desk to complete calibration.";

                    break;

                case 2:
                    this.textBoxPrompt.Text = "Lockduino preparing to calibrate away distance.";
                    break;

                case 3:
                    this.textBoxPrompt.Text = "Lockduino has completed calibration";
                    break;
            }
        }

        /// <summary>
        /// The button close_ click.
        /// </summary>
        /// <param name="sender">
        /// The sender.
        /// </param>
        /// <param name="e">
        /// The e.
        /// </param>
        private void buttonClose_Click(object sender, EventArgs e)
        {
            Program.MainLockDuino.Threshold = this.lockDuinoSettings.AverageDistanceWhenPresent + 50;
            Program.MainLockDuino.MaxTolerance = this.lockDuinoSettings.AverageDistanceWhenAbsent;
            this.Close();
        }

        /// <summary>
        /// The button go_ click.
        /// </summary>
        /// <param name="sender">
        /// The sender.
        /// </param>
        /// <param name="e">
        /// The e.
        /// </param>
        private void buttonGo_Click(object sender, EventArgs e)
        {
            switch (this.calibrationState)
            {
                case 0:
                    this.textBoxSeated.Text = "0.0";
                    this.textBoxAway.Text = "0.0";
                    this.CalibrateSeated();
                    break;
                case 1:
                    this.CalibrateWaitToAway();
                    break;
                case 2:
                    break;
                case 3:
                    break;
            }
        }

        /// <summary>
        /// The state timer_ tick.
        /// </summary>
        /// <param name="sender">
        /// The sender.
        /// </param>
        /// <param name="e">
        /// The e.
        /// </param>
        private void stateTimer_Tick(object sender, EventArgs e)
        {
            this.stateTimer.Stop();

            if (this.calibrationState == 2)
            {
                this.calibrationState = 3;
                this.SetUIState();
                this.CalibrateAway();
            }
        }

        #endregion
    }
}