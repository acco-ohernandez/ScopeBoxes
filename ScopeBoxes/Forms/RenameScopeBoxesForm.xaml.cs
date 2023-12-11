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

            UniqueCharList = new List<string>();

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
            txbFirstUnique.Text = GetLastWord(ElementsList.Cast<Element>().Select(x => x.Name).FirstOrDefault());

            // Optionally, set the DisplayMemberPath if you want to display a specific property of Element in the ListBox
            // lbOriginalNames.DisplayMemberPath = "Name";
        }
        private string RemoveLastWord(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                // Handle empty or null sb_UniqueChar as needed
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
                // Handle empty or null sb_UniqueChar as needed
                return input;
            }

            // Split the sb_UniqueChar string into an array of words
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
            //txbSuffix_TextChanged(sender, e);
            txbFirstUnique_TextChanged(sender, e);

        }

        public List<string> UniqueCharList { get; set; }
        public static StringBuilder OriginalSuffix { get; set; }

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

        private void txbFirstUnique_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            // Get the text from the first unique TextBox and trim any leading or trailing spaces
            var FirstUniqueText = txbFirstUnique.Text;
            FirstUniqueText = FirstUniqueText.Trim(); // Corrected: Assign the trimmed value back to the variable

            // Enable the Rename button if the first unique text is not empty
            if (!string.IsNullOrEmpty(FirstUniqueText))
                BtnRenameScopeBoxes.IsEnabled = true;

            // Get the text from the suffix TextBox and trim any leading or trailing spaces
            var suffixText = txbSuffix.Text;
            suffixText = suffixText.Trim(); // Corrected: Assign the trimmed value back to the variable

            // Clear the list of unique characters
            UniqueCharList.Clear();

            // Create a StringBuilder with the first unique text
            StringBuilder sb_UniqueChar = new StringBuilder(FirstUniqueText);

            // Update the global variable with the first unique text
            OriginalSuffix = sb_UniqueChar;

            // Add the first item to the UniqueCharList based on the presence of a suffix
            if (string.IsNullOrEmpty(suffixText))
            {
                UniqueCharList.Add($"{NewName} {sb_UniqueChar}");
            }
            else
            {
                UniqueCharList.Add($"{NewName} {sb_UniqueChar} {suffixText}");
            }

            // Get the list of original names from the ListBox
            var listOfNames = lbOriginalNames.Items;

            // Process additional items in the list
            if (FirstUniqueText != null)
            {
                for (int i = 1; i < listOfNames.Count; i++)
                {
                    // Increment the last character in the StringBuilder
                    IncrementLastCharacter(sb_UniqueChar);

                    // Add items to the UniqueCharList based on the presence of a suffix
                    if (string.IsNullOrEmpty(suffixText))
                    {
                        UniqueCharList.Add($"{NewName} {sb_UniqueChar}");
                    }
                    else
                    {
                        UniqueCharList.Add($"{NewName} {sb_UniqueChar} {suffixText}");
                    }
                }

                // Update lbNewNames.ItemsSource with the updated UniqueCharList
                lbNewNames.ItemsSource = UniqueCharList;

                // Refresh the lbNewNames ListBox to reflect changes
                lbNewNames.Items.Refresh();
            }

            // Disable the Rename button if the first unique text is empty
            if (FirstUniqueText == string.Empty)
            {
                BtnRenameScopeBoxes.IsEnabled = false;
            }
        }

        private void txbSuffix_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            txbFirstUnique_TextChanged(sender, e);
        }
    }
}
