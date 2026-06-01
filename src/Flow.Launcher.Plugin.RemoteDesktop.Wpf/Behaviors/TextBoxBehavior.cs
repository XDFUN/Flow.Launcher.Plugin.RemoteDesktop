using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace Flow.Launcher.Plugin.RemoteDesktop.Behaviors;

public class TextBoxBehavior
{
    public static readonly DependencyProperty PlaceholderProperty = DependencyProperty.RegisterAttached(
        "Placeholder",
        typeof(string),
        typeof(TextBoxBehavior),
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

    private static bool GetOrCreateAdorner(TextBox textBoxControl, [NotNullWhen(true)] out PlaceholderAdorner? adorner)
    {
        var layer = AdornerLayer.GetAdornerLayer(textBoxControl);

        if (layer == null)
        {
            adorner = null;

            return false;
        }

        adorner = layer.GetAdorners(textBoxControl)
                       ?.OfType<PlaceholderAdorner>()
                       .FirstOrDefault(x => x.AdornedElement == textBoxControl);

        if (adorner != null)
        {
            return true;
        }

        adorner = new PlaceholderAdorner(textBoxControl);
        layer.Add(adorner);

        UpdateAdornerVisibility(textBoxControl, adorner);

        return true;
    }

    private static void OnPlaceholderChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not TextBox textBoxControl)
        {
            return;
        }

        if (!textBoxControl.IsLoaded)
        {
            textBoxControl.Loaded -= TextBoxControl_Loaded;
            textBoxControl.Loaded += TextBoxControl_Loaded;
        }

        textBoxControl.TextChanged -= TextBoxControl_TextChanged;
        textBoxControl.TextChanged += TextBoxControl_TextChanged;

        if (GetOrCreateAdorner(textBoxControl, out PlaceholderAdorner? adorner))
        {
            adorner.InvalidateVisual();
        }
    }

    private static void TextBoxControl_Loaded(object sender, RoutedEventArgs e)
    {
        if (sender is not TextBox textBoxControl)
        {
            return;
        }

        textBoxControl.Loaded -= TextBoxControl_Loaded;
        GetOrCreateAdorner(textBoxControl, out _);
    }

    private static void TextBoxControl_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (sender is not TextBox textBoxControl
            || !GetOrCreateAdorner(textBoxControl, out PlaceholderAdorner? adorner))
        {
            return;
        }

        UpdateAdornerVisibility(textBoxControl, adorner);
    }

    private static void UpdateAdornerVisibility(TextBox textBoxControl, PlaceholderAdorner adorner)
    {
        adorner.Visibility = textBoxControl.Text.Length > 0 ? Visibility.Hidden : Visibility.Visible;
    }

    private class PlaceholderAdorner : Adorner
    {
        public PlaceholderAdorner(TextBox textBox) : base(textBox)
        {
            IsHitTestVisible = false;
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            var textBoxControl = (TextBox)AdornedElement;

            string placeholderValue = GetPlaceholder(textBoxControl);

            if (string.IsNullOrEmpty(placeholderValue))
            {
                return;
            }

            var text = new FormattedText(
                placeholderValue,
                CultureInfo.CurrentCulture,
                textBoxControl.FlowDirection,
                new Typeface(
                    textBoxControl.FontFamily,
                    textBoxControl.FontStyle,
                    textBoxControl.FontWeight,
                    textBoxControl.FontStretch
                ),
                textBoxControl.FontSize,
                SystemColors.InactiveCaptionBrush,
                VisualTreeHelper.GetDpi(textBoxControl).PixelsPerDip
            )
            {
                MaxTextWidth = Math.Max(
                    textBoxControl.ActualWidth - textBoxControl.Padding.Left - textBoxControl.Padding.Right,
                    10
                ),
                MaxTextHeight = Math.Max(textBoxControl.ActualHeight, 10),
            };

            var renderingOffset = new Point(textBoxControl.Padding.Left, textBoxControl.Padding.Top);

            if (textBoxControl.Template.FindName("PART_ContentHost", textBoxControl) is FrameworkElement part)
            {
                Point partPosition = part.TransformToAncestor(textBoxControl).Transform(new Point(0, 0));
                renderingOffset.X += partPosition.X;
                renderingOffset.Y += partPosition.Y;

                text.MaxTextWidth = Math.Max(part.ActualWidth - renderingOffset.X, 10);
                text.MaxTextHeight = Math.Max(part.ActualHeight, 10);
            }

            drawingContext.DrawText(text, renderingOffset);
        }
    }
}