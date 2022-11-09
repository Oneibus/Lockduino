// --------------------------------------------------------------------------------------------------------------------
// <copyright file="SystemTray.cs" company="Microsoft">
//   Copyright Microsoft Corporation. All rights reserved.
// </copyright>
// <summary>
//   The system tray UI component.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace LockDuinoSystemTray
{
    using System;
    using System.Windows.Forms;

    using LockDuinoSystemTray.Properties;

    /// <summary>
    /// The system tray UI component.
    /// </summary>
    public class SystemTray : IDisposable
    {
        #region Static Fields

        /// <summary>
        /// A boolean value that determines if we are showing about box
        /// </summary>
        private static bool aboutShown;

        /// <summary>
        /// The settings shown.
        /// </summary>
        private static bool settingsShown;

        #endregion

        #region Fields

        /// <summary>
        /// The NotifyIcon object.
        /// </summary>
        private readonly NotifyIcon trayIcon;

        #endregion

        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="SystemTray" /> class.
        /// </summary>
        public SystemTray()
        {
            // Instantiate the NotifyIcon object.
            this.trayIcon = new NotifyIcon();
        }

        #endregion

        #region Public Properties

        /// <summary>
        /// Gets or sets a value indicating whether play range sounds.
        /// </summary>
        public bool PlayRangeSounds { get; set; }

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// Displays the system tray icon.
        /// </summary>
        public void CreateSystemTrayMenu()
        {
            this.trayIcon.Icon = Resources.SystemTrayApp;
            this.trayIcon.Text = Resources.SystemTray_CreateSystemTrayMenu_LockDuino_Desktop_Locker;
            this.trayIcon.Visible = true;

            // wire up mouse clicks
            this.trayIcon.MouseClick += OnMouseClick;

            // Attach a context menu.
            var menu = new ContextMenuStrip();

            // Settings option.
            var item = new ToolStripMenuItem { Text = Resources.SystemTray_CreateSystemTrayMenu_Settings };
            item.Click += OnSettingsClick;
            menu.Items.Add(item);

            // Range sounds fun option.
            item = new ToolStripMenuItem { Text = Resources.SystemTray_CreateSystemTrayMenu_Range_Sounds, CheckOnClick = true };
            item.CheckedChanged += this.RangeSoundsCheckedChanged;
            menu.Items.Add(item);

            var sep = new ToolStripSeparator();
            menu.Items.Add(sep);

            // About opition.
            item = new ToolStripMenuItem { Text = Resources.SystemTray_CreateSystemTrayMenu_About_LockDuino_Desktop_Locker };
            item.Click += OnAboutClick;
            item.Image = Resources.About;
            menu.Items.Add(item);

            sep = new ToolStripSeparator();
            menu.Items.Add(sep);

            // Exit option.
            item = new ToolStripMenuItem { Text = Resources.SystemTray_CreateSystemTrayMenu_Exit };
            item.Click += OnExitClick;
            item.Image = Resources.Exit;
            menu.Items.Add(item);

            this.trayIcon.ContextMenuStrip = menu;
        }

        /// <summary>
        /// Releases resources
        /// </summary>
        public void Dispose()
        {
            this.trayIcon.Dispose();
        }

        #endregion

        #region Methods

        /// <summary>
        /// Handles the Click event of the About control.
        /// </summary>
        /// <param name="sender">
        /// The source of the event.
        /// </param>
        /// <param name="evt">
        /// The event arguments.
        /// </param>
        private static void OnAboutClick(object sender, EventArgs evt)
        {
            if (aboutShown)
            {
                return;
            }

            aboutShown = true;
            
            var aboutBox = new AboutBox();
            aboutBox.ShowDialog();

            aboutShown = false;
        }

        /// <summary>
        /// Processes a menu item.
        /// </summary>
        /// <param name="sender">
        /// The sender.
        /// </param>
        /// <param name="evt">
        /// The event arguments.
        /// </param>
        private static void OnExitClick(object sender, EventArgs evt)
        {
            Application.Exit();
        }

        /// <summary>
        /// Handles the Click event of the settings control.
        /// </summary>
        /// <param name="sender">
        /// The source of the event.
        /// </param>
        /// <param name="evt">
        /// The event arguments.
        /// </param>
        private static void OnSettingsClick(object sender, EventArgs evt)
        {
            if (settingsShown)
            {
                return;
            }

            settingsShown = true;

            var calibrationBox = new CalibrationForm();
            calibrationBox.ShowDialog();

            settingsShown = false;
        }

        /// <summary>
        /// Handles the MouseClick event of the tray icon control.
        /// </summary>
        /// <param name="sender">
        /// The source of the event.
        /// </param>
        /// <param name="evt">
        /// The event arguments.
        /// </param>
        private static void OnMouseClick(object sender, MouseEventArgs evt)
        {
            // Handle mouse button clicks.
            if (evt.Button == MouseButtons.Left)
            {
                OnAboutClick(sender, evt);
            }
        }

        /// <summary>
        /// The range sounds checked changed.
        /// </summary>
        /// <param name="sender">
        /// The sender.
        /// </param>
        /// <param name="evt">
        /// The event arguments.
        /// </param>
        private void RangeSoundsCheckedChanged(object sender, EventArgs evt)
        {
            var tmi = sender as ToolStripMenuItem;
            if (tmi != null)
            {
                this.PlayRangeSounds = tmi.Checked;
                Program.StopLockDuinoWorker();
                Program.MainLockDuino.SaveSettings(this.PlayRangeSounds ? "1" : "0");
                Program.StartLockDuinoWorker();
            }
        }

        #endregion
    }
}