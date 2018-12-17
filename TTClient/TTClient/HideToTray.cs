using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Controls;

namespace TTClient
{
    public static class HideToTray
    {
        public static MinimizeToTrayInstance st;
        /// <summary>
        /// Enables "minimize to tray" behavior for the specified Window.
        /// </summary>
        /// <param name="window">Window to enable the behavior for.</param>
        public static void Enable(MainWindow window)
        {
            // No need to track this instance; its event handlers will keep it alive
            st =  new MinimizeToTrayInstance(window);
        }

        public static void Start()
        {
            st.StartIcon();
        }

        public static void Pause()
        {
            st.PauseIcon();
        }

        public static void InActive()
        {
            st.InActiveIcon();
        }

        public static void RemoveIcon()
        {
            st.RemoveIcon();
        }

        /// <summary>
        /// Class implementing "minimize to tray" functionality for a Window instance.
        /// </summary>
        public class MinimizeToTrayInstance
        {
            private MainWindow _window;
            private NotifyIcon _notifyIcon;
            //private bool _balloonShown;
            public System.Windows.Controls.ContextMenu cmenu = null;

            /// <summary>
            /// Initializes a new instance of the MinimizeToTrayInstance class.
            /// </summary>
            /// <param name="window">Window instance to attach to.</param>
            public MinimizeToTrayInstance(MainWindow window)
            {
                Debug.Assert(window != null, "window parameter is null.");
                _window = window;
                //_window.StateChanged += new EventHandler(HandleStateChanged);
                HandleStateChanged(null, null);
                cmenu = (System.Windows.Controls.ContextMenu)_window.FindResource("NotifierContextMenu");
            }

            /// <summary>
            /// Handles the Window's StateChanged event.
            /// </summary>
            /// <param name="sender">Event source.</param>
            /// <param name="e">Event arguments.</param>
            private void HandleStateChanged(object sender, EventArgs e)
            {
                if (_notifyIcon == null)
                {
                    // Initialize NotifyIcon instance "on demand"
                    _notifyIcon = new NotifyIcon();                    
                    _notifyIcon.Icon = (Icon) TTClient.Properties.Resources.ResourceManager.GetObject("clock_ico");
                    _notifyIcon.MouseClick += new MouseEventHandler(HandleNotifyIconOrBalloonClicked);
                    _notifyIcon.BalloonTipClicked += new EventHandler(HandleNotifyIconOrBalloonClicked);
                }
                // Update copy of Window Title in case it has changed
                _notifyIcon.Text = _window.Title;

                // Show/hide Window and NotifyIcon
                //var minimized = (_window.WindowState == WindowState.Minimized);
                //_window.ShowInTaskbar = !minimized;
                _notifyIcon.Visible = true;
                SetPosition();
                _window.Hide();
                //if (minimized && !_balloonShown)
                //{
                //    // Если это первый запуск, то показываем сообшщение для пользователя
                    _notifyIcon.ShowBalloonTip(1000, null, "Start Task", ToolTipIcon.None);
                //    _balloonShown = true;
                //}
            }

            public void StartIcon()
            {
                if (_notifyIcon != null)
                {                    
                    _notifyIcon.Icon = (Icon)TTClient.Properties.Resources.ResourceManager.GetObject("clock_run_ico");                    
                }
            }

            public void PauseIcon()
            {
                if (_notifyIcon != null)
                {
                    _notifyIcon.Icon = (Icon)TTClient.Properties.Resources.ResourceManager.GetObject("clock_pause_ico");
                }
            }

            public void InActiveIcon()
            {
                if (_notifyIcon != null)
                {
                    _notifyIcon.Icon = (Icon)TTClient.Properties.Resources.ResourceManager.GetObject("clock_ico");
                }
            }

            public void RemoveIcon()
            {
                if (_notifyIcon != null)
                {
                    _notifyIcon.Dispose();
                }
            }

            /// <summary>
            /// Обрабатываем нажатие на иконке.
            /// </summary>
            /// <param name="sender">Event source.</param>
            /// <param name="e">Event arguments.</param>
            private void HandleNotifyIconOrBalloonClicked(object sender, EventArgs e)
            {
                try
                {
                    // Восстанавливаем окно
                    MouseEventArgs mev = (MouseEventArgs)e;

                    if (cmenu == null)
                        cmenu = (System.Windows.Controls.ContextMenu)_window.FindResource("NotifierContextMenu");

                    if (mev.Button == MouseButtons.Left)
                    {
                        //закоментировано для отображения одновлременно и окна и меню
                        //if (cmenu.IsOpen == false)
                        //{
                        if (_window.Visibility == Visibility.Visible) HideWindow();
                        else { ShowWindow(); SetPosition(); }
                        //}
                    }

                    if (mev.Button == MouseButtons.Right)
                    {
                        //закоментировано для отображения одновлременно и окна и меню
                        //if (_window.Visibility == Visibility.Hidden)
                        //{
                        if (cmenu.IsOpen == false)
                        {
                            cmenu.IsOpen = true;
                            //_window.mouse.MouseMenuOff += mouse_MouseMenuOff;
                        }
                        else
                        {
                            cmenu.IsOpen = false;
                            System.Windows.Controls.MenuItem cfgmenu = (System.Windows.Controls.MenuItem)cmenu.Items[cmenu.Items.Count - 1];
                            cfgmenu.Items.Clear();
                            //_window.mouse.MouseMenuOff -= mouse_MouseMenuOff;
                        }
                        //}

                    }
                }
                catch (Exception)
                { }
            }

            void mouse_MouseMenuOff(object sender, EventArgs e)
            {
                if (cmenu != null && cmenu.IsOpen == true)
                {
                    cmenu.IsOpen = false;
                    System.Windows.Controls.MenuItem cfgmenu = (System.Windows.Controls.MenuItem)cmenu.Items[cmenu.Items.Count - 1];
                    cfgmenu.Items.Clear();
                    //_window.mouse.MouseMenuOff -= mouse_MouseMenuOff;
                }
            }

            private void ShowWindow()
            {
                _window.Show();
                //_window.WindowState = WindowState.Normal;
            }

            private void HideWindow()
            {
                //_window.WindowState = WindowState.Minimized;
                _window.Hide();
            }

            private void SetPosition()
            {
                _window.Left = System.Windows.Forms.Screen.PrimaryScreen.WorkingArea.Right - _window.Width;
                _window.Top = System.Windows.Forms.Screen.PrimaryScreen.WorkingArea.Bottom - _window.Height;                
            }
        }

    }
    
}
