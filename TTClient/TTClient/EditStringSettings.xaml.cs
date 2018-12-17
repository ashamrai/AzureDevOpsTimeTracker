using System;
using System.Collections.Generic;
using System.Linq;
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
    /// Interaction logic for EditStringSettings.xaml
    /// </summary>
    public partial class EditStringSettings : Window
    {
        public string ItemText = "";

        public EditStringSettings()
        {
            InitializeComponent();
        }

        public void SetItemText(string pText)
        {
            if (pText != "") txtSetting.Text = pText;
        }

        private void btnOk_Click(object sender, RoutedEventArgs e)
        {
            ItemText = txtSetting.Text;
            this.DialogResult = true;
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }
    }
}
