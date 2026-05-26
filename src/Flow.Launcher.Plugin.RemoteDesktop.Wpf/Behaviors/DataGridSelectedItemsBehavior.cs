using System.Collections;
using System.Windows;
using System.Windows.Controls;

namespace Flow.Launcher.Plugin.RemoteDesktop.Behaviors;

internal static class DataGridSelectedItemsBehavior
{
    public static readonly DependencyProperty BindableSelectedItemsProperty = DependencyProperty.RegisterAttached(
        "BindableSelectedItems",
        typeof(IList),
        typeof(DataGridSelectedItemsBehavior),
        new PropertyMetadata(null, OnBindableSelectedItemsChanged)
    );

    public static IList? GetBindableSelectedItems(DependencyObject element)
    {
        return (IList?)element.GetValue(BindableSelectedItemsProperty);
    }

    public static void SetBindableSelectedItems(DependencyObject element, IList value)
    {
        element.SetValue(BindableSelectedItemsProperty, value);
    }

    private static void Grid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var grid = (DataGrid)sender;
        IList? selectedItems = GetBindableSelectedItems(grid);

        if (selectedItems == null)
        {
            return;
        }

        selectedItems.Clear();

        foreach (object? item in grid.SelectedItems)
        {
            selectedItems.Add(item);
        }
    }

    private static void OnBindableSelectedItemsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not DataGrid grid)
        {
            return;
        }

        grid.SelectionChanged -= Grid_SelectionChanged;
        grid.SelectionChanged += Grid_SelectionChanged;
    }
}