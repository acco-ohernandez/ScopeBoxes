using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Autodesk.Revit.UI;

namespace RevitAddinTesting.Utils
{
    public class TaskDialogExample : IExternalEventHandler
    {
        private TaskDialog td;

        public void Execute(UIApplication app)
        {
            ShowTaskDialog(app);
        }

        public string GetName()
        {
            return "TaskDialogExampleHandler";
        }

        public void ShowTaskDialog(UIApplication app)
        {
            // Initialize the TaskDialog properties
            string m_title = "Sample TaskDialog";
            string m_mainInstruction = "Main Instruction Text";
            string m_id = "TaskDialogID";
            TaskDialogIcon m_mainIcon = TaskDialogIcon.TaskDialogIconInformation;
            string m_mainContent = "This is the main content of the TaskDialog.";
            string m_expandedContent = "This is the expanded content of the TaskDialog.";
            string m_verificationText = "Verification text.";
            string m_extraCheckBoxText = "Extra checkbox text.";
            bool? m_verificationChecked = false;
            bool? m_extraCheckBoxChecked = true;
            string m_footerText = "Footer text.";
            TaskDialogCommonButtons m_commonButtons = TaskDialogCommonButtons.Ok | TaskDialogCommonButtons.Cancel;
            TaskDialogResult m_defaultButton = TaskDialogResult.Ok;
            bool m_allowCancellation = true;
            bool m_titleAutoPrefix = true;
            bool m_enableMarqueeProgressBar = true; // Enable the MarqueeProgressBar

            // Create command links
            SortedDictionary<int, string> m_commandLinks = new SortedDictionary<int, string>
        {
            { 1, "Command Link 1" },
            { 2, "Command Link 2" }
        };

            // Initialize and configure the TaskDialog
            td = new TaskDialog(m_title)
            {
                MainInstruction = m_mainInstruction,
                Id = m_id,
                MainIcon = m_mainIcon,
                MainContent = m_mainContent,
                ExpandedContent = m_expandedContent,
                VerificationText = m_verificationText,
                FooterText = m_footerText,
                CommonButtons = m_commonButtons,
                DefaultButton = m_defaultButton,
                AllowCancellation = m_allowCancellation,
                TitleAutoPrefix = m_titleAutoPrefix,
                EnableMarqueeProgressBar = m_enableMarqueeProgressBar
            };

            // Add extra checkbox text
            td.AddCommandLink(TaskDialogCommandLinkId.CommandLink1, m_extraCheckBoxText);

            // Add command links
            foreach (var commandLink in m_commandLinks)
            {
                td.AddCommandLink((TaskDialogCommandLinkId)commandLink.Key, commandLink.Value);
            }

            // Show the TaskDialog in a separate task
            ExternalEvent externalEvent = ExternalEvent.Create(new TaskDialogExample());
            Task.Run(() =>
            {
                // Simulate a process that takes 10 seconds
                for (int i = 0; i < 10; i++)
                {
                    Thread.Sleep(1000); // Sleep for 1 second
                }

                // Trigger the external event to close the dialog
                externalEvent.Raise();
            });

            // Show the TaskDialog
            TaskDialogResult result = td.Show();

            // Handle the result
            if (result == TaskDialogResult.CommandLink1)
            {
                // Handle Command Link 1
            }
            else if (result == TaskDialogResult.CommandLink2)
            {
                // Handle Command Link 2
            }
            else if (result == TaskDialogResult.Ok)
            {
                // Handle Ok button
            }
            else if (result == TaskDialogResult.Cancel)
            {
                // Handle Cancel button
            }
        }

        public void CloseDialog()
        {
            // Custom logic to close the dialog
            td.WasVerificationChecked();
        }
    }
}