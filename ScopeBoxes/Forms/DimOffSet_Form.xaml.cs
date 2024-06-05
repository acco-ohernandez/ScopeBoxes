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
    /// Interaction logic for DimOffSet_Form.xaml
    /// </summary>
    public partial class DimOffSet_Form : Window
    {
        public DimOffSet_Form()
        {
            InitializeComponent();
        }

        private void btn_Ok_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
            this.Close();
        }

        private void RadioButton_Checked(object sender, RoutedEventArgs e)
        {
            if (!(sender is RadioButton radioButton)) return;

            // Check if tb_OffSetFeet is initialized
            if (tb_OffSetFeet == null) return;

            switch (radioButton.Content.ToString())
            {
                case "1/8 Scale":
                    tb_OffSetFeet.Text = "4.0";
                    tb_OffSetFeet.IsEnabled = false;
                    break;
                case "1/4 Scale":
                    tb_OffSetFeet.Text = "2.0";
                    tb_OffSetFeet.IsEnabled = false;
                    break;
                case "Custom Scale":
                    tb_OffSetFeet.IsEnabled = true;
                    tb_OffSetFeet.Text = "";
                    tb_OffSetFeet.Focus();
                    break;
            }
        }


    }
}
