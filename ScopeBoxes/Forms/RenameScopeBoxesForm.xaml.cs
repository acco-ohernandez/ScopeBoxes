using System.Collections.Generic;
using System.Drawing;
using System.Linq;
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
            lbOriginalNames.ItemsSource = ElementsList.Cast<Element>().Select(x => x.Name);
            lbNewNames.ItemsSource = ElementsList.Cast<Element>().Select(x => x.Name);


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
                // Handle empty or null input as needed
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
                // Handle empty or null input as needed
                return input;
            }

            // Split the input string into an array of words
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
        private void txbSuffix_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            var suffixText = txbSuffix.Text;
            BtnRenameScopeBoxes.IsEnabled = true;

            SuffixList.Clear();
            StringBuilder input = new StringBuilder(suffixText);
            SuffixList.Add($"{NewName}{input}");
            var listOfNames = lbOriginalNames.Items;
            if (suffixText != null)
            {
                for (int i = 1; i < listOfNames.Count; i++)
                {
                    IncrementLastCharacter(input);
                    SuffixList.Add($"{NewName}{input}");
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

        private static void IncrementLastCharacter(StringBuilder str)
        {
            // Check if the StringBuilder is empty
            if (str.Length == 0)
            {
                return; // do nothing
            }

            // Get the last character in the StringBuilder
            char lastChar = str[str.Length - 1];

            // If the last character is a digit, increment the numeric value
            if (char.IsDigit(lastChar))
            {
                // Convert the StringBuilder content to an integer
                int.TryParse(str.ToString(), out int result);

                // Increment the numeric value
                result += 1;

                // Clear the StringBuilder and append the new numeric value
                str.Clear();
                str.Append(result);
            }
            // If the last character is 'z', wrap around to 'a'
            else if (lastChar == 'z')
            {
                str[str.Length - 1] = 'a';
            }
            // If the last character is 'Z', wrap around to 'A'
            else if (lastChar == 'Z')
            {
                str[str.Length - 1] = 'A';
            }
            // If the last character is a letter, increment to the next letter
            else if (char.IsLetter(lastChar))
            {
                str[str.Length - 1] = (char)(lastChar + 1);
            }
            // Add any additional conditions or handling as needed
        }


    }
}
