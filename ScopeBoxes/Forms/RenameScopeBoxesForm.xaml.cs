using System.Collections.Generic;
using System.Linq;
using System.Windows;

using Autodesk.Revit.DB;

namespace ScopeBoxes.Forms
{
    public partial class RenameScopeBoxesForm : Window
    {
        public List<Element> ElementsList { get; set; }

        public RenameScopeBoxesForm(List<Element> elements)
        {
            InitializeComponent();
            ElementsList = elements;

            // Call the method to bind ElementsList to lbOriginalNames ListBox
            BindElementsListToOriginalNamesListBox();
        }

        private void BindElementsListToOriginalNamesListBox()
        {
            // Set the ItemsSource of lbOriginalNames ListBox to ElementsList
            lbOriginalNames.ItemsSource = ElementsList.Cast<Element>().Select(x => x.Name);
            lbNewNames.ItemsSource = ElementsList.Cast<Element>().Select(x => x.Name);
            txtNewName.Text = ElementsList.Cast<Element>().Select(x => x.Name).First();
            // Optionally, set the DisplayMemberPath if you want to display a specific property of Element in the ListBox
            // lbOriginalNames.DisplayMemberPath = "Name";
        }

        private void BtnRenameScopeBoxes_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
            this.Close();
        }

        private void txtNewName_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {

        }
    }
}
