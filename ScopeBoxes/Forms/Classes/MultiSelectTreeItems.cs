using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
namespace RevitAddinTesting.Forms
{
    public class MultiSelectTreeView : TreeView
    {
        protected override DependencyObject GetContainerForItemOverride()
        {
            return new MultiSelectTreeViewItem();
        }
    }

    public class MultiSelectTreeViewItem : TreeViewItem
    {
        private static MultiSelectTreeViewItem _lastSelectedItem;

        public MultiSelectTreeViewItem()
        {
            Selected += MultiSelectTreeViewItem_Selected;
        }

        protected override void OnSelected(RoutedEventArgs e)
        {
            base.OnSelected(e);
        }

        private void MultiSelectTreeViewItem_Selected(object sender, RoutedEventArgs e)
        {
            if (Keyboard.Modifiers == ModifierKeys.Control)
            {
                IsSelected = !IsSelected;
                e.Handled = true;
            }
            else if (Keyboard.Modifiers == ModifierKeys.Shift && _lastSelectedItem != null)
            {
                SelectRange(_lastSelectedItem, this);
                e.Handled = true;
            }
            else
            {
                _lastSelectedItem = this;
            }
        }

        private void SelectRange(MultiSelectTreeViewItem startItem, MultiSelectTreeViewItem endItem)
        {
            var parentTreeView = ItemsControl.ItemsControlFromItemContainer(startItem) as TreeView;
            if (parentTreeView == null)
            {
                return;
            }

            bool isInRange = false;
            foreach (var item in parentTreeView.Items)
            {
                var container = parentTreeView.ItemContainerGenerator.ContainerFromItem(item) as MultiSelectTreeViewItem;
                if (container == null)
                {
                    continue;
                }

                if (container == startItem || container == endItem)
                {
                    isInRange = !isInRange;
                    container.IsSelected = true;
                }

                if (isInRange)
                {
                    container.IsSelected = true;
                }
            }
        }

        protected override void OnExpanded(RoutedEventArgs e)
        {
            base.OnExpanded(e);
            ExpandAllChildren(this);
        }

        private void ExpandAllChildren(TreeViewItem item)
        {
            foreach (var subItem in item.Items)
            {
                if (item.ItemContainerGenerator.ContainerFromItem(subItem) is TreeViewItem childItem)
                {
                    childItem.IsExpanded = true;
                    ExpandAllChildren(childItem);
                }
            }
        }
    }
}

//using System;
//using System.Windows;
//using System.Windows.Controls;
//using System.Windows.Input;

//namespace RevitAddinTesting.Forms
//{
//    public class MultiSelectTreeView : TreeView
//    {
//        protected override DependencyObject GetContainerForItemOverride()
//        {
//            return new MultiSelectTreeViewItem();
//        }
//    }

//    public class MultiSelectTreeViewItem : TreeViewItem
//    {
//        protected override void OnSelected(RoutedEventArgs e)
//        {
//            if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
//            {
//                IsSelected = !IsSelected;
//                e.Handled = true;
//            }
//            else if (Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
//            {
//                TreeViewItem first = null;
//                TreeViewItem last = null;
//                foreach (var item in ((TreeView)Parent).Items)
//                {
//                    var container = ((TreeView)Parent).ItemContainerGenerator.ContainerFromItem(item) as TreeViewItem;
//                    if (container != null && container.IsSelected)
//                    {
//                        if (first == null) first = container;
//                        last = container;
//                    }
//                }
//                if (first != null && last != null)
//                {
//                    var start = ((TreeView)Parent).ItemContainerGenerator.IndexFromContainer(first);
//                    var end = ((TreeView)Parent).ItemContainerGenerator.IndexFromContainer(last);
//                    for (int i = Math.Min(start, end); i <= Math.Max(start, end); i++)
//                    {
//                        var container = ((TreeView)Parent).ItemContainerGenerator.ContainerFromIndex(i) as TreeViewItem;
//                        if (container != null)
//                            container.IsSelected = true;
//                    }
//                }
//            }
//            else
//            {
//                base.OnSelected(e);
//            }
//        }
//    }
//}
