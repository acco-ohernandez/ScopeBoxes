using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;

using Autodesk.Revit.DB;

namespace RevitAddinTesting.Forms
{
    public partial class CreateBIMSetupView : Window, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private Level _selectedLevel;
        private int _selectedScale;
        private ObservableCollection<Level> _levels;
        private Dictionary<int, string> _scales;

        public CreateBIMSetupView(List<Level> levels, Dictionary<int, string> scales)
        {
            InitializeComponent();
            Levels = new ObservableCollection<Level>(levels);
            Scales = scales;
            SelectedScale = scales.First(s => s.Value == "1/4\" = 1'-0\"").Key;

            // Set the first level as the selected level by default
            if (Levels.Count > 0)
            {
                SelectedLevel = Levels[0];
            }

            DataContext = this;
        }

        public ObservableCollection<Level> Levels
        {
            get => _levels;
            set
            {
                _levels = value;
                OnPropertyChanged(nameof(Levels));
            }
        }

        public Dictionary<int, string> Scales
        {
            get => _scales;
            set
            {
                _scales = value;
                OnPropertyChanged(nameof(Scales));
            }
        }

        public Level SelectedLevel
        {
            get => _selectedLevel;
            set
            {
                _selectedLevel = value;
                OnPropertyChanged(nameof(SelectedLevel));
            }
        }

        public int SelectedScale
        {
            get => _selectedScale;
            set
            {
                _selectedScale = value;
                OnPropertyChanged(nameof(SelectedScale));
            }
        }

        private void btn_OK_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void btn_Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
