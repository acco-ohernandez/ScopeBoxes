using System.Collections.Generic;
using System.ComponentModel;

using Autodesk.Revit.DB;

//namespace RevitAddinTesting.Forms.Classes

namespace RevitAddinTesting.Forms
{
    public class TreeNode : INotifyPropertyChanged
    {
        private bool _isSelected;
        private bool _isChecked;
        private bool _isEnabled = true;
        private bool _isExpanded = true;

        public string Header { get; set; }
        public List<TreeNode> Children { get; set; } = new List<TreeNode>();
        public ElementId ViewId { get; set; }

        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (_isSelected != value)
                {
                    _isSelected = value;
                    OnPropertyChanged(nameof(IsSelected));
                }
            }
        }

        public bool IsChecked
        {
            get => _isChecked;
            set
            {
                if (_isChecked != value)
                {
                    _isChecked = value;
                    OnPropertyChanged(nameof(IsChecked));
                    foreach (var child in Children)
                    {
                        child.IsChecked = value;
                    }
                }
            }
        }

        public bool IsEnabled
        {
            get => _isEnabled;
            set
            {
                if (_isEnabled != value)
                {
                    _isEnabled = value;
                    OnPropertyChanged(nameof(IsEnabled));
                }
            }
        }

        public bool IsExpanded
        {
            get => _isExpanded;
            set
            {
                if (_isExpanded != value)
                {
                    _isExpanded = value;
                    OnPropertyChanged(nameof(IsExpanded));
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class ViewsTreeNode : TreeNode
    {
        public ViewsTreeNode(string viewType, List<View> views)
        {
            Header = viewType;
            foreach (var view in views)
            {
                var viewNode = new TreeNode
                {
                    Header = view.Name,
                    ViewId = view.Id
                };
                Children.Add(viewNode);
            }
        }
    }
}
