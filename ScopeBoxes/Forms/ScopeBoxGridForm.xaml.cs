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

namespace RevitAddinTesting.Forms
{
    /// <summary>
    /// Interaction logic for ScopeBoxGridForm.xaml
    /// </summary>
    public partial class ScopeBoxGridForm : Window
    {
        public ScopeBoxGridForm()
        {
            InitializeComponent();
            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            // Center the window on the screen
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
        }

        private void BtnCreateScopeBoxGrid_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
