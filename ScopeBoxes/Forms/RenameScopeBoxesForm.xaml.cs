using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Windows;

using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace ScopeBoxes.Forms
{
    public partial class RenameScopeBoxesForm : Window
    {
        public List<Element> ElementsList { get; set; }

        public RenameScopeBoxesForm(List<Element> elements)
        {
            InitializeComponent();
            ElementsList = elements;

            SuffixList = new List<string>();

            // Call the method to bind ElementsList to lbOriginalNames ListBox
            BindElementsListToOriginalNamesListBox();
        }

        private void BindElementsListToOriginalNamesListBox()
        {
            // Set the ItemsSource of lbOriginalNames ListBox to ElementsList
            //lbOriginalNames.ItemsSource = ElementsList.Cast<Element>().Select(x => x.Name);
            lbOriginalNames.ItemsSource = ElementsList;


            var scopeBoxesSelected = ElementsList.Cast<Element>().Select(x => x.Name);
            lbNewNames.ItemsSource = lbOriginalNames.Items;



            // update the New Name text box
            txbNewName.Text = RemoveLastWord(ElementsList.Cast<Element>().Select(x => x.Name).FirstOrDefault());
            NewName = txbNewName.Text;
            txbSuffix.Text = GetLastWord(ElementsList.Cast<Element>().Select(x => x.Name).FirstOrDefault());

            // Optionally, set the DisplayMemberPath if you want to display a specific property of Element in the ListBox
            // lbOriginalNames.DisplayMemberPath = "Name";
        }
        private string RemoveLastWord(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                // Handle empty or null sb_Suffix as needed
                return input;
            }

            // Find the last space in the string
            int lastSpaceIndex = input.LastIndexOf(' ');

            // If a space is found, extract the substring up to that point
            if (lastSpaceIndex >= 0)
            {
                return input.Substring(0, lastSpaceIndex);
            }
            else
            {
                // If no space is found, return the original string
                return input;
            }
        }
        private string GetLastWord(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                // Handle empty or null sb_Suffix as needed
                return input;
            }

            // Split the sb_Suffix string into an array of words
            string[] words = input.Split(' ');

            // If there is at least one word, return the last one
            if (words.Length > 0)
            {
                return words[words.Length - 1];
            }
            else
            {
                // If no words are found, return the original string
                return input;
            }
        }


        private void BtnRenameScopeBoxes_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
            this.Close();
        }

        public string NewName { get; set; }
        private void txbNewName_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            NewName = txbNewName.Text;
            txbSuffix_TextChanged(sender, e);
        }

        public List<string> SuffixList { get; set; }
        public static StringBuilder OriginalSuffix { get; set; }
        private void txbSuffix_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            var suffixText = txbSuffix.Text;
            if (!string.IsNullOrEmpty(suffixText))
                BtnRenameScopeBoxes.IsEnabled = true;  // If the Suffix field has text, enable the BtnRenameScopeBoxes Button

            SuffixList.Clear();
            StringBuilder sb_Suffix = new StringBuilder(suffixText);
            //if (sb_Suffix[sb_Suffix.Length - 1] == 'z' || sb_Suffix[sb_Suffix.Length - 1] == 'Z')
            OriginalSuffix = sb_Suffix; // update the global variable

            SuffixList.Add($"{NewName}{sb_Suffix}"); // Add the first name to the SuffixList
            var listOfNames = lbOriginalNames.Items;
            if (suffixText != null)
            {
                for (int i = 1; i < listOfNames.Count; i++)
                {
                    IncrementLastCharacter(sb_Suffix);
                    SuffixList.Add($"{NewName}{sb_Suffix}"); // Add the new name to the SuffixList
                }

                // Update lbNewNames.ItemsSource with the updated SuffixList
                lbNewNames.ItemsSource = SuffixList;

                // Refresh the lbNewNames ListBox to reflect changes
                lbNewNames.Items.Refresh();
            }

            if (suffixText == string.Empty)
            {
                BtnRenameScopeBoxes.IsEnabled = false;
            }
        }
        private static void IncrementLastCharacter(StringBuilder inputString)
        {
            // Check if the StringBuilder is empty
            if (inputString.Length == 0)
            {
                return; // do nothing
            }

            // Get the last character in the StringBuilder
            char lastChar = inputString[inputString.Length - 1];

            // If the last character is a digit, increment the numeric value
            if (char.IsDigit(lastChar))
            {
                // Convert the StringBuilder content to an integer
                int.TryParse(inputString.ToString(), out int result);

                // Increment the numeric value
                result += 1;

                // Clear the StringBuilder and append the new numeric value
                inputString.Clear();
                inputString.Append(result);
            }
            // If the last character is 'z', wrap around to 'a'
            else if (lastChar == 'z')
            {
                OriginalSuffix.Append('a');
                inputString = OriginalSuffix;
                OriginalSuffix = inputString;
                //inputString[inputString.Length - 1] = 'a';
            }
            // If the last character is 'Z', wrap around to 'A'
            else if (lastChar == 'Z')
            {
                OriginalSuffix.Append('A');
                inputString = OriginalSuffix;
                OriginalSuffix = inputString;
                //inputString[inputString.Length - 1] = 'A';
            }
            // If the last character is a letter, increment to the next letter
            else if (char.IsLetter(lastChar))
            {
                inputString[inputString.Length - 1] = (char)(lastChar + 1);
            }
            // Add any additional conditions or handling as needed
        }

        private void btnUp_Click(object sender, RoutedEventArgs e)
        {
            MoveSelectedItem(-1);
        }

        private void btnDown_Click(object sender, RoutedEventArgs e)
        {
            MoveSelectedItem(1);
        }

        private void MoveSelectedItem(int direction)
        {
            var selectedIndex = lbOriginalNames.SelectedIndex;

            // Check if an item is selected and it's not the first or last item
            if (selectedIndex >= 0 && selectedIndex + direction >= 0 && selectedIndex + direction < ElementsList.Count)
            {
                // Swap the selected item with the item in the desired direction
                var temp = ElementsList[selectedIndex];
                ElementsList[selectedIndex] = ElementsList[selectedIndex + direction];
                ElementsList[selectedIndex + direction] = temp;

                // Update the ListBox and reselect the moved item
                lbOriginalNames.Items.Refresh();
                lbOriginalNames.SelectedIndex = selectedIndex + direction;
            }
        }

    }
}
