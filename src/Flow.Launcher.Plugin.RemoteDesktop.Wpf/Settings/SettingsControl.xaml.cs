using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Flow.Launcher.Plugin.RemoteDesktop.Settings;

public partial class SettingsControl
{
    private Point _dragStartPoint;

    public SettingsControl()
    {
        InitializeComponent();
    }

    private void OverrideList_MouseDoubleClickItem(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is not SettingsViewModel viewModel)
        {
            return;
        }

        if (viewModel.EditOverrideCommand.CanExecute(null))
        {
            viewModel.EditOverrideCommand.Execute(null);
        }
    }

    private void OverrideList_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _dragStartPoint = e.GetPosition(null);
    }

    private void OverrideList_PreviewMouseMove(object sender, MouseEventArgs e)
    {
        Point mousePos = e.GetPosition(null);
        Vector diff = _dragStartPoint - mousePos;

        if (e.LeftButton != MouseButtonState.Pressed
            || (!(Math.Abs(diff.X) > SystemParameters.MinimumHorizontalDragDistance)
                && !(Math.Abs(diff.Y) > SystemParameters.MinimumVerticalDragDistance)))
        {
            return;
        }

        var listView = (ListView)sender;
        var listViewItem = FindAncestor<ListViewItem>((DependencyObject)e.OriginalSource);

        if (listViewItem == null)
        {
            return;
        }

        var item = (UserOverrideViewModel)listView.ItemContainerGenerator.ItemFromContainer(listViewItem);

        if (item == null)
        {
            return;
        }

        DragDrop.DoDragDrop(listViewItem, item, DragDropEffects.Move);
    }

    private void OverrideList_Drop(object sender, DragEventArgs e)
    {
        if (!e.Data.GetDataPresent(typeof(UserOverrideViewModel)))
        {
            return;
        }

        if (e.Data.GetData(typeof(UserOverrideViewModel)) is not UserOverrideViewModel droppedData)
        {
            return;
        }

        var listView = (ListView)sender;
        ListViewItem? target = GetNearestContainer(e.OriginalSource);

        if (target == null)
        {
            return;
        }

        var targetData = (UserOverrideViewModel)listView.ItemContainerGenerator.ItemFromContainer(target);

        if (targetData == null)
        {
            return;
        }

        if (DataContext is not SettingsViewModel settings)
        {
            return;
        }

        ObservableCollection<UserOverrideViewModel> items = settings.UserOverrides;
        int removedIdx = items.IndexOf(droppedData);
        int targetIdx = items.IndexOf(targetData);

        if (removedIdx == targetIdx)
        {
            return;
        }

        items.Move(removedIdx, targetIdx);
    }

    private static ListViewItem? GetNearestContainer(object source)
    {
        var element = source as UIElement;

        while (element != null && element is not ListViewItem)
        {
            element = VisualTreeHelper.GetParent(element) as UIElement;
        }

        return element as ListViewItem;
    }

    private void OverrideList_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (sender is not ListView listView)
        {
            return;
        }

        if (listView.View is not GridView gView)
        {
            return;
        }

        // take into account vertical scrollbar
        double workingWidth = listView.ActualWidth - SystemParameters.VerticalScrollBarWidth;

        if (workingWidth <= 0)
        {
            return;
        }

        gView.Columns[0].Width = workingWidth * 0.5;
        gView.Columns[1].Width = workingWidth * 0.5;
    }

    private static T? FindAncestor<T>(DependencyObject current) where T : DependencyObject
    {
        DependencyObject? iterator = current;

        while (iterator != null)
        {
            if (iterator is T dependencyObject)
            {
                return dependencyObject;
            }

            iterator = VisualTreeHelper.GetParent(iterator);
        }

        return null;
    }
}