using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace Flow.Launcher.Plugin.RemoteDesktop.Behaviors;

public class TextBlockBehavior
{
    public static readonly DependencyProperty PlaceholderProperty = DependencyProperty.RegisterAttached(
        "Placeholder",
        typeof(string),
        typeof(TextBlockBehavior),
        new FrameworkPropertyMetadata(null, OnPlaceholderChanged)
    );

    public static string GetPlaceholder(DependencyObject obj)
    {
        return (string)obj.GetValue(PlaceholderProperty);
    }

    public static void SetPlaceholder(DependencyObject obj, string value)
    {
        obj.SetValue(PlaceholderProperty, value);
    }

    private static bool GetOrCreateAdorner(
        TextBlock textBlockControl,
        [NotNullWhen(true)] out PlaceholderAdorner? adorner
    )
    {
        var layer = AdornerLayer.GetAdornerLayer(textBlockControl);

        if (layer == null)
        {
            adorner = null;

            return false;
        }

        adorner = layer.GetAdorners(textBlockControl)?.OfType<PlaceholderAdorner>().FirstOrDefault();

        if (adorner != null)
        {
            return true;
        }

        adorner = new PlaceholderAdorner(textBlockControl);
        layer.Add(adorner);

        return true;
    }

    private static void OnPlaceholderChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not TextBlock textBlockControl)
        {
            return;
        }

        if (!textBlockControl.IsLoaded)
        {
            // Ensure that the events are not added multiple times
            textBlockControl.Loaded -= TextBlockControl_Loaded;
            textBlockControl.Loaded += TextBlockControl_Loaded;
        }

        DependencyPropertyDescriptor? descriptor
            = DependencyPropertyDescriptor.FromProperty(TextBlock.TextProperty, typeof(TextBlock));

        descriptor.RemoveValueChanged(textBlockControl, TextBlockControl_TextChanged);
        descriptor.AddValueChanged(textBlockControl, TextBlockControl_TextChanged);

        // If the adorner exists, invalidate it to draw the current text
        if (GetOrCreateAdorner(textBlockControl, out PlaceholderAdorner? adorner))
        {
            adorner.Visibility = string.IsNullOrEmpty(textBlockControl.Text) ? Visibility.Visible : Visibility.Hidden;
            adorner.InvalidateVisual();
        }
    }

    private static void TextBlockControl_Loaded(object sender, RoutedEventArgs e)
    {
        if (sender is not TextBlock textBlockControl)
        {
            return;
        }

        textBlockControl.Loaded -= TextBlockControl_Loaded;

        if (GetOrCreateAdorner(textBlockControl, out PlaceholderAdorner? adorner))
        {
            adorner.Visibility = string.IsNullOrEmpty(textBlockControl.Text) ? Visibility.Visible : Visibility.Hidden;
        }
    }

    private static void TextBlockControl_TextChanged(object? sender, EventArgs e)
    {
        if (sender is not TextBlock textBlockControl
            || !GetOrCreateAdorner(textBlockControl, out PlaceholderAdorner? adorner))
        {
            return;
        }

        adorner.Visibility = string.IsNullOrEmpty(textBlockControl.Text) ? Visibility.Visible : Visibility.Hidden;
    }

    private class PlaceholderAdorner(TextBlock textBlock) : Adorner(textBlock)
    {
        protected override void OnRender(DrawingContext drawingContext)
        {
            var textBlockControl = (TextBlock)AdornedElement;

            string placeholderValue = GetPlaceholder(textBlockControl);

            if (string.IsNullOrEmpty(placeholderValue))
            {
                return;
            }

            var text = new FormattedText(
                placeholderValue,
                CultureInfo.CurrentCulture,
                textBlockControl.FlowDirection,
                new Typeface(
                    textBlockControl.FontFamily,
                    textBlockControl.FontStyle,
                    textBlockControl.FontWeight,
                    textBlockControl.FontStretch
                ),
                textBlockControl.FontSize,
                SystemColors.InactiveCaptionBrush,
                VisualTreeHelper.GetDpi(textBlockControl).PixelsPerDip
            )
            {
                MaxTextWidth
                    = Math.Max(
                        textBlockControl.ActualWidth - textBlockControl.Padding.Left - textBlockControl.Padding.Right,
                        10
                    ),
                MaxTextHeight = Math.Max(
                    textBlockControl.ActualHeight - textBlockControl.Padding.Top - textBlockControl.Padding.Bottom,
                    10
                ),
                TextAlignment = textBlockControl.TextAlignment,
                Trimming = textBlockControl.TextTrimming,
            };

            var renderingOffset = new Point(textBlockControl.Padding.Left, textBlockControl.Padding.Top);

            drawingContext.DrawText(text, renderingOffset);
        }
    }
}