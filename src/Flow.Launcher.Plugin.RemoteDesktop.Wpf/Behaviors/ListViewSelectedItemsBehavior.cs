using System.Collections;
using System.Windows;
using System.Windows.Controls;

namespace Flow.Launcher.Plugin.RemoteDesktop.Behaviors;

internal static class ListViewSelectedItemsBehavior
{
    public static readonly DependencyProperty BindableSelectedItemsProperty = DependencyProperty.RegisterAttached(
        "BindableSelectedItems",
        typeof(IList),
        typeof(ListViewSelectedItemsBehavior),
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
        var view = (ListView)sender;
        IList? selectedItems = GetBindableSelectedItems(view);

        if (selectedItems == null)
        {
            return;
        }

        selectedItems.Clear();

        foreach (object? item in view.SelectedItems)
        {
            selectedItems.Add(item);
        }
    }

    private static void OnBindableSelectedItemsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not ListView view)
        {
            return;
        }

        view.SelectionChanged -= Grid_SelectionChanged;
        view.SelectionChanged += Grid_SelectionChanged;
    }
}