using System;
using System.Collections.Generic;
using System.DirectoryServices.AccountManagement;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace TTClient
{
    /// <summary>
    /// Interaction logic for Settings.xaml
    /// </summary>
    public partial class Settings : Window
    {
        public bool ActTypesChanged = false;

        public Settings()
        {
            InitializeComponent();
            LoadParams();
        }

        private void LoadParams()
        {
            txtTFSUri.Text = Properties.Settings.Default.TFSUri;
            txtTFSCollection.Text = Properties.Settings.Default.TFSCollection;
            chkUseDefaultCreds.IsChecked = Properties.Settings.Default.UseDefCreds;
            if (!Properties.Settings.Default.UseDefCreds)
            {
                txtTFSUser.Text = Properties.Settings.Default.User;
                if (Properties.Settings.Default.Password != null) txtTFSPwd.Password = Utils.Decrypt(Properties.Settings.Default.Password);
                txtTFSDomain.Text = Properties.Settings.Default.Domain;
            }
            txtActiveState.Text = Properties.Settings.Default.ActiveState;
            txtTFSActivity.Text = Properties.Settings.Default.WIActivity;
            foreach (string _atitem in Properties.Settings.Default.ActivityTypes) lstActvityTypes.Items.Add(_atitem);
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            if (SaveParams()) this.DialogResult = true;
        }

        private bool SaveParams()
        {
            if (Properties.Settings.Default.TFSUri != txtTFSUri.Text) Properties.Settings.Default.TFSUri = txtTFSUri.Text;
            if (Properties.Settings.Default.TFSCollection != txtTFSCollection.Text) Properties.Settings.Default.TFSCollection = txtTFSCollection.Text;
            if (Properties.Settings.Default.ActiveState != txtActiveState.Text) Properties.Settings.Default.ActiveState = txtActiveState.Text;
            if (Properties.Settings.Default.WIActivity != txtTFSActivity.Text) Properties.Settings.Default.WIActivity = txtTFSActivity.Text;
            if (Properties.Settings.Default.UseDefCreds != chkUseDefaultCreds.IsChecked) Properties.Settings.Default.UseDefCreds = (bool)chkUseDefaultCreds.IsChecked;
            if (!Properties.Settings.Default.UseDefCreds)
            {
                Properties.Settings.Default.User = txtTFSUser.Text;
                if (txtTFSPwd.Password != null) Properties.Settings.Default.Password = Utils.Encrypt(txtTFSPwd.Password);
                Properties.Settings.Default.Domain = txtTFSDomain.Text;
            }
            else { Properties.Settings.Default.User = Properties.Settings.Default.Password = Properties.Settings.Default.Domain = ""; }

            if (ActTypesChanged)
            {
                Properties.Settings.Default.ActivityTypes.Clear();

                foreach (string _atitem in lstActvityTypes.Items) Properties.Settings.Default.ActivityTypes.Add(_atitem);
            }

            Properties.Settings.Default.Save();

            return true;
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }

        private void chkUseDefaultCreds_Checked(object sender, RoutedEventArgs e)
        {
            txtTFSUser.IsEnabled = false;
            txtTFSPwd.IsEnabled = false;
            txtTFSDomain.IsEnabled = false;
        }

        private void chkUseDefaultCreds_Unchecked(object sender, RoutedEventArgs e)
        {
            txtTFSUser.IsEnabled = true;
            txtTFSPwd.IsEnabled = true;
            txtTFSDomain.IsEnabled = true;
        }

        private void actAdd_Click(object sender, RoutedEventArgs e)
        {
            EditStringSettings _wndEdtSet = new EditStringSettings();
            if (_wndEdtSet.ShowDialog() == true)
            {
                if (_wndEdtSet.ItemText != "") lstActvityTypes.Items.Add(_wndEdtSet.ItemText);
                ActTypesChanged = true;
            }
        }

        private void actEdit_Click(object sender, RoutedEventArgs e)
        {
            if (lstActvityTypes.SelectedIndex >= 0)
            {
                EditStringSettings _wndEdtSet = new EditStringSettings();
                _wndEdtSet.SetItemText(lstActvityTypes.Items[lstActvityTypes.SelectedIndex].ToString());
                if (_wndEdtSet.ShowDialog() == true)
                {
                    if (_wndEdtSet.ItemText != "")
                        if (lstActvityTypes.Items[lstActvityTypes.SelectedIndex].ToString() != _wndEdtSet.ItemText)
                            lstActvityTypes.Items[lstActvityTypes.SelectedIndex] = _wndEdtSet.ItemText;
                    ActTypesChanged = true;
                }
            }
        }

        private void actDel_Click(object sender, RoutedEventArgs e)
        {
            if (lstActvityTypes.SelectedIndex >= 0)
            {
                ActTypesChanged = true;
                lstActvityTypes.Items.RemoveAt(lstActvityTypes.SelectedIndex);
            }
        }    
    }
}
