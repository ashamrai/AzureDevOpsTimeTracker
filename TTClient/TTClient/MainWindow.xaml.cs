using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
//using TFSRestApiHelper;

namespace TTClient
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        enum Mode { STARTED, PAUSED, STOPED };
        const int SLEEP_TIME = 10;

        private KeyboardInput keyboard;
        private MouseInput mouse;
        private int LastTimeKeyPressed = 0;
        ActiveWIList ActiveWorkItems = new ActiveWIList();
        WorkItem CurrentWi = null;
        WorkItemType CurrentWiType = null;
        WorkItemType ActWiType = null;
        private DateTime StartTime = DateTime.MinValue;
        private Mode CurrentState = Mode.STOPED;
        private int CurrentMins = 0;
        DispatcherTimer TTimer = new DispatcherTimer();
        DispatcherTimer PressHookTimer = new DispatcherTimer();
        DispatcherTimer ExceprionsTimer = new DispatcherTimer();
        List<String> ActivityTypes = new List<string>();
        private bool ClosaApp = false;


        public MainWindow()
        {
            InitializeComponent();

            foreach (string _atitem in Properties.Settings.Default.ActivityTypes) cmbActType.Items.Add(_atitem);
            if (Properties.Settings.Default.LastActivityType != "") cmbActType.Text = Properties.Settings.Default.LastActivityType;

            LoadSettings();
            HideToTray.Enable(this);
            TTimer.Tick += TTimer_Tick;
            TTimer.Interval = new TimeSpan(0, 1, 0);
            ExceprionsTimer.Tick += ExceprionsTimer_Tick;
            ExceprionsTimer.Interval = new TimeSpan(0, 0, 3);
            ExceprionsTimer.Start();
            CurrentState = Mode.STOPED;
            UpdateButtonsState();
            StartPressHook();
        }

        private void ExceprionsTimer_Tick(object sender, EventArgs e)
        {
            if (RestApiHelper.Exceptions == "") return;

            string _exceptions = RestApiHelper.Exceptions;
            RestApiHelper.Exceptions = "";

            string[] _exclines = _exceptions.Split(new char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (string _excline in _exclines)
                lstLogs.Items.Add(_excline);
        }

        private void StartPressHook()
        {
            keyboard = new KeyboardInput();
            keyboard.KeyBoardKeyPressed += Keyboard_KeyBoardKeyPressed;
            mouse = new MouseInput();
            mouse.MouseMoved += Mouse_MouseMoved;

            PressHookTimer.Tick += PressHookTimer_Tick;
            PressHookTimer.Interval = new TimeSpan(0, 1, 0);
            PressHookTimer.Start();
        }

        private void PressHookTimer_Tick(object sender, EventArgs e)
        {
            LastTimeKeyPressed += 1;

            if (LastTimeKeyPressed > SLEEP_TIME)
            {
                PauseCount();
            }
        }

        private void Mouse_MouseMoved(object sender, EventArgs e)
        {
            LastTimeKeyPressed = 0;
        }

        private void Keyboard_KeyBoardKeyPressed(object sender, EventArgs e)
        {
            LastTimeKeyPressed = 0;
        }

        private void TTimer_Tick(object sender, EventArgs e)
        {
            CurrentMins++;

            if (CurrentMins > 0)
            {
                if (CurrentMins < 60) txtCurrentTime.Text = String.Format("Time: {0} m", CurrentMins);
                else txtCurrentTime.Text = String.Format("Time: {0} h {1} m", (CurrentMins / 60), CurrentMins - (CurrentMins / 60) * 60 );
            }            
        }

        private void btnStart_Click(object sender, RoutedEventArgs e)
        {
            StartCount();
        }

        private void ActiveWorkItems_Click(object sender, RoutedEventArgs e)
        {
            ActiveWorkItems = RestApiHelper.GetActiveWorkItems();

            UpdateWorkitemMenuList((MenuItem) sender, ActiveWorkItems);
        }

        /// <summary>
        /// Create menu for projects if they more 1
        /// </summary>
        /// <param name="pActiveItemMenu"></param>
        /// <param name="pWorkItems"></param>
        private void UpdateWorkitemMenuList(MenuItem pActiveItemMenu, ActiveWIList pWorkItems)
        {
            if (pWorkItems == null) return;

            if (pActiveItemMenu.HasItems) pActiveItemMenu.Items.Clear();

            IEnumerable<String> _projects = (from _workitems in pWorkItems.value select _workitems.TeamProject).Distinct();

            if (_projects.Count() > 1)
                foreach (string _projectname in _projects)
                {
                    MenuItem _projectmenu = new MenuItem();
                    _projectmenu.Header = _projectname;
                    pActiveItemMenu.Items.Add(_projectmenu);
                    UpdateProjectMenu(_projectmenu, pWorkItems, _projectname);
                }
            else if (_projects.Count() > 0)
                UpdateProjectMenu(pActiveItemMenu, pWorkItems, _projects.ElementAt(0));
        }

        /// <summary>
        /// Create menu for work item types if they more 1
        /// </summary>
        /// <param name="pParentMenu"></param>
        /// <param name="pWorkItems"></param>
        /// <param name="pProjectName"></param>
        private void UpdateProjectMenu(MenuItem pParentMenu, ActiveWIList pWorkItems, string pProjectName)
        {
            IEnumerable<String> _witypes = 
                (from _workitems in pWorkItems.value where _workitems.TeamProject == pProjectName
                 select _workitems.WorkItemType).Distinct();

            if (_witypes.Count() > 1)
                foreach (string _witype in _witypes)
                {
                    MenuItem _witypemenu = new MenuItem();
                    _witypemenu.Header = _witype;
                    pParentMenu.Items.Add(_witypemenu);
                    UpdateWITypeMenu(_witypemenu, pWorkItems, pProjectName, _witype);
                }
            else
                UpdateWITypeMenu(pParentMenu, pWorkItems, pProjectName, _witypes.ElementAt(0));

        }

        /// <summary>
        /// Craete menu item for workitems and assign event
        /// </summary>
        /// <param name="pParentMenu"></param>
        /// <param name="pWorkItems"></param>
        /// <param name="pProjectName"></param>
        /// <param name="pWiType"></param>
        private void UpdateWITypeMenu(MenuItem pParentMenu, ActiveWIList pWorkItems, string pProjectName, string pWiType)
        {
            IEnumerable<ActiveWIList.ActiveWiListValues> _wis =
                (from _workitems in pWorkItems.value
                 where _workitems.TeamProject == pProjectName && _workitems.WorkItemType == pWiType
                 select _workitems).Distinct();

            foreach (ActiveWIList.ActiveWiListValues _wi in _wis)
            {
                MenuItem _wimenu = new MenuItem();
                string _menuheader = _wi.Id + ": " + _wi.Title;
                _wimenu.Header = _menuheader.Length > 30 ? (_menuheader.Substring(0,27) + "...") : _menuheader ;
                pParentMenu.Items.Add(_wimenu);
                _wimenu.Tag = _wi.Id;
                _wimenu.ToolTip = _wi.Title;
                _wimenu.Click += _wimenu_Click;
            }
        }

        /// <summary>
        /// Perfom count process for selected workitem
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _wimenu_Click(object sender, RoutedEventArgs e)
        {
            MenuStartCount(sender);
        }

        private void MenuStartCount(object sender)
        {      
            MenuItem _wimenu = (MenuItem)sender;
            int _wiid = 0;

            if (!int.TryParse(_wimenu.Tag.ToString(), out _wiid)) return;

            if (SetCurrentWI(_wiid)) StartCount();
        }

        private bool SetCurrentWI(int _wiid)
        {
            CurrentWi = RestApiHelper.GetWorkItem(_wiid);

            if (CurrentWi == null) return false;

            CurrentWiType = RestApiHelper.GetWorkItemType(CurrentWi.Fields[WIConsts.TeamProject].ToString(), CurrentWi.Fields[WIConsts.WorkItemType].ToString());
            ActWiType = RestApiHelper.GetWorkItemType(CurrentWi.Fields[WIConsts.WorkItemType].ToString(), Properties.Settings.Default.WIActivity);

            txtWiId.Text = CurrentWi.Id.ToString();
            lbWiTitle.Content = CurrentWi.Fields[WIConsts.Title].ToString();
            txtActTitle.Text = CurrentWi.Fields[WIConsts.Title].ToString();

            return true;
        }

        /// <summary>
        /// start count process for active workitem
        /// </summary>
        private void StartCount()
        {
            if (CurrentWi == null) return;

            if (CurrentState == Mode.PAUSED)
            {
                CurrentState = Mode.STARTED;
                UpdateButtonsState();
                TTimer.Start();
            }
            else
            {
                CurrentState = Mode.STARTED;                
                StartTime = DateTime.Now;
                CurrentMins = 0;                
            }

            UpdateButtonsState();
            TTimer.Start();
            HideToTray.Start();
        }

        /// <summary>
        /// update config for TFSRestApiHelper
        /// </summary>
        public static void LoadSettings()
        {
            RestApiHelper.ServiceUrl = Properties.Settings.Default.TFSUri;
            RestApiHelper.CollectionName = Properties.Settings.Default.TFSCollection;

            if (!Properties.Settings.Default.UseDefCreds)
            {
                RestApiHelper.UserName = Properties.Settings.Default.User;
                RestApiHelper.Password = Utils.Decrypt(Properties.Settings.Default.Password);
                RestApiHelper.UserDomain = Properties.Settings.Default.Domain;
            }
        }

        private void btnSettings_Click(object sender, RoutedEventArgs e)
        {
            Settings _st = new Settings();
            _st.ShowDialog();

            if (_st.DialogResult == true)
            {
                LoadSettings();
                if (_st.ActTypesChanged)
                {
                    cmbActType.Items.Clear();
                    foreach (string _atitem in Properties.Settings.Default.ActivityTypes) cmbActType.Items.Add(_atitem);
                }
            }
        }

        private void btnStop_Click(object sender, RoutedEventArgs e)
        {
            StopCount();
        }

        private void StopCount()
        {
            if (txtActTitle.Text == "")
            {
                MessageBox.Show("Update Activity Type", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            double _completedhours = Math.Round(CurrentMins / 60.0, 2);

            CurrentWi = RestApiHelper.GetWorkItem(CurrentWi.Id.Value);
            CreateNewActivity(_completedhours);
            UpdateParentWorkItem(_completedhours);

            CurrentState = Mode.STOPED;
            UpdateButtonsState();
            TTimer.Stop();
            HideToTray.InActive();
        }

        private void UpdateParentWorkItem(double _completedhours)
        {
            Dictionary<string, object> _fields = new Dictionary<string, object>();

            if (CurrentWi.Fields.Keys.Contains(WIConsts.RemainingWork))
            {
                double _remwork = 0;
                if (double.TryParse(CurrentWi.Fields[WIConsts.RemainingWork].ToString(), out _remwork))
                {
                    if (_remwork <= _completedhours) _remwork = 0;
                    else _remwork -= _completedhours;
                }
                else _remwork = 0;

                _fields.Add(WIConsts.RemainingWork, _remwork);
            }

            if (CurrentWi.Fields.Keys.Contains(WIConsts.CompletedWork))
            {
                double _cmpwork = 0;

                if (double.TryParse(CurrentWi.Fields[WIConsts.CompletedWork].ToString(), out _cmpwork)) _cmpwork += _completedhours;
                else _cmpwork = _completedhours;

                _fields.Add(WIConsts.CompletedWork, _cmpwork);
            }
            else
            {
                var _cmworkfield = from _finst in CurrentWiType.FieldInstances where _finst.ReferenceName == WIConsts.CompletedWork select _finst;

                if (_cmworkfield != null && _cmworkfield.Count() > 0) _fields.Add(WIConsts.CompletedWork, _completedhours);
            }

            if (_fields.Count > 0) RestApiHelper.SubmitWorkItem(_fields, CurrentWi.Id.Value);
        }

        private void CreateNewActivity(double _completedhours)
        {   
            Dictionary<string, object> _fields = new Dictionary<string, object>();

            _fields.Add(WIConsts.Title, txtActTitle.Text);
            _fields.Add(WIConsts.CompletedWork, _completedhours);

            if (cmbActType.Text != Properties.Settings.Default.LastActivityType)
            {
                Properties.Settings.Default.LastActivityType = cmbActType.Text;
                Properties.Settings.Default.Save();
            }

            if (cmbActType.Text != "")
            {
                var _typefield = from _finst in CurrentWiType.FieldInstances where _finst.ReferenceName == WIConsts.Discipline select _finst;
                if (_typefield != null && _typefield.Count() > 0) _fields.Add(WIConsts.Discipline, cmbActType.Text);
            }

            
            var _startfield = from _finst in CurrentWiType.FieldInstances where _finst.ReferenceName == WIConsts.StartDate select _finst;
            if (_startfield != null && _startfield.Count() > 0) _fields.Add(WIConsts.StartDate, StartTime);
            //if (_startfield != null && _startfield.Count() > 0) _fields.Add("Microsoft.VSTS.Scheduling.StartDate", StartTime.ToString("s") + "Z");
            var _finishfield = from _finst in CurrentWiType.FieldInstances where _finst.ReferenceName == WIConsts.FinishDate select _finst;
            //if (_finishfield != null && _finishfield.Count() > 0) _fields.Add("Microsoft.VSTS.Scheduling.FinishDate", DateTime.Now.ToString("s") + "Z");
            if (_finishfield != null && _finishfield.Count() > 0) _fields.Add(WIConsts.FinishDate, DateTime.Now);
            RestApiHelper.CreateChildWorkItem(CurrentWi.Fields[WIConsts.TeamProject].ToString(), Properties.Settings.Default.WIActivity, _fields, CurrentWi.Url);
        }

        private void UpdateButtonsState()
        {
            if (CurrentState == Mode.STOPED)
            {
                btnStart.IsEnabled = true;
                btnSettings.IsEnabled = true;
                btnPause.IsEnabled = false;
                btnStop.IsEnabled = false;
                btnAssign.IsEnabled = true;

                txtCurrentTime.Text = String.Format("Time: {0} m", 0);
            }

            if (CurrentState == Mode.STARTED)
            {
                btnStart.IsEnabled = false;
                btnSettings.IsEnabled = false;
                btnPause.IsEnabled = true;
                btnStop.IsEnabled = true;
                btnAssign.IsEnabled = false;

                txtCurrentTime.Text = String.Format("Time: {0} m", CurrentMins);
            }

            if (CurrentState == Mode.PAUSED)
            {
                btnStart.IsEnabled = true;
                btnSettings.IsEnabled = false;
                btnPause.IsEnabled = false;
                btnStop.IsEnabled = true;
                btnAssign.IsEnabled = false;                
            }
        }

        private void btnPause_Click(object sender, RoutedEventArgs e)
        {
            PauseCount();
        }

        private void PauseCount()
        {
            CurrentState = Mode.PAUSED;
            UpdateButtonsState();
            TTimer.Stop();
            HideToTray.Pause();
        }

        private void Start_Click(object sender, RoutedEventArgs e)
        {
            StartCount();
        }

        private void Pause_Click(object sender, RoutedEventArgs e)
        {
            PauseCount();
        }

        private void Stop_Click(object sender, RoutedEventArgs e)
        {
            StopCount();            
        }

        private void btnAssign_Click(object sender, RoutedEventArgs e)
        {
            AssingWorkItem();
        }

        private void AssingWorkItem()
        {
            int _wiid = 0;

            if (!int.TryParse(txtWiId.Text, out _wiid)) return;

            SetCurrentWI(_wiid);
        }

        private void TTClient_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (ClosaApp == false)
            {
                e.Cancel = true;
                this.Hide();
            }
            else
                HideToTray.RemoveIcon();
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Do you want to exit?", "Question", MessageBoxButton.OKCancel, MessageBoxImage.Question) == MessageBoxResult.OK)
            {
                ClosaApp = true;
                this.Close();
            }
        }

        private void expLogs_Expanded(object sender, RoutedEventArgs e)
        {
            this.Height = 285;
        }

        private void expLogs_Collapsed(object sender, RoutedEventArgs e)
        {
            this.Height = 200;
        }

        private void lbWiTitle_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (CurrentWi != null)
                System.Diagnostics.Process.Start(RestApiHelper.ServiceUrl + "/" + CurrentWi.Fields[WIConsts.TeamProject].ToString() + "/_workitems/edit/" + CurrentWi.Id.ToString());
        }
    }
}
