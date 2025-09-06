using System;
using System.Globalization;
using System.Windows;
using System.Windows.Media;
using System.Windows.Documents;

namespace Lazarus.Desktop.Controls;

// Vector text renderer with crisp stroke + fill
public class OutlineTextBlock : FrameworkElement
{
    public static readonly DependencyProperty TextProperty =
        DependencyProperty.Register(
            nameof(Text), typeof(string), typeof(OutlineTextBlock),
            new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty ForegroundProperty =
        DependencyProperty.Register(
            nameof(Foreground), typeof(Brush), typeof(OutlineTextBlock),
            new FrameworkPropertyMetadata(Brushes.Black, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty StrokeProperty =
        DependencyProperty.Register(
            nameof(Stroke), typeof(Brush), typeof(OutlineTextBlock),
            new FrameworkPropertyMetadata(Brushes.White, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty StrokeThicknessProperty =
        DependencyProperty.Register(
            nameof(StrokeThickness), typeof(double), typeof(OutlineTextBlock),
            new FrameworkPropertyMetadata(1.4d, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty FontFamilyProperty =
        TextElement.FontFamilyProperty.AddOwner(typeof(OutlineTextBlock), new FrameworkPropertyMetadata(SystemFonts.MessageFontFamily, FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty FontSizeProperty =
        TextElement.FontSizeProperty.AddOwner(typeof(OutlineTextBlock), new FrameworkPropertyMetadata(SystemFonts.MessageFontSize, FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty FontWeightProperty =
        TextElement.FontWeightProperty.AddOwner(typeof(OutlineTextBlock), new FrameworkPropertyMetadata(SystemFonts.MessageFontWeight, FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty FontStyleProperty =
        TextElement.FontStyleProperty.AddOwner(typeof(OutlineTextBlock), new FrameworkPropertyMetadata(SystemFonts.MessageFontStyle, FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty FontStretchProperty =
        TextElement.FontStretchProperty.AddOwner(typeof(OutlineTextBlock), new FrameworkPropertyMetadata(FontStretches.Normal, FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty TextWrappingProperty =
        DependencyProperty.Register(
            nameof(TextWrapping), typeof(TextWrapping), typeof(OutlineTextBlock),
            new FrameworkPropertyMetadata(TextWrapping.NoWrap, FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender));

    public string? Text
    {
        get => (string?)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public Brush Foreground
    {
        get => (Brush)GetValue(ForegroundProperty);
        set => SetValue(ForegroundProperty, value);
    }

    public Brush Stroke
    {
        get => (Brush)GetValue(StrokeProperty);
        set => SetValue(StrokeProperty, value);
    }

    public double StrokeThickness
    {
        get => (double)GetValue(StrokeThicknessProperty);
        set => SetValue(StrokeThicknessProperty, value);
    }

    public FontFamily FontFamily
    {
        get => (FontFamily)GetValue(FontFamilyProperty);
        set => SetValue(FontFamilyProperty, value);
    }

    public double FontSize
    {
        get => (double)GetValue(FontSizeProperty);
        set => SetValue(FontSizeProperty, value);
    }

    public FontWeight FontWeight
    {
        get => (FontWeight)GetValue(FontWeightProperty);
        set => SetValue(FontWeightProperty, value);
    }

    public FontStyle FontStyle
    {
        get => (FontStyle)GetValue(FontStyleProperty);
        set => SetValue(FontStyleProperty, value);
    }

    public FontStretch FontStretch
    {
        get => (FontStretch)GetValue(FontStretchProperty);
        set => SetValue(FontStretchProperty, value);
    }

    public TextWrapping TextWrapping
    {
        get => (TextWrapping)GetValue(TextWrappingProperty);
        set => SetValue(TextWrappingProperty, value);
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        var ft = CreateFormattedText();

        if (TextWrapping == TextWrapping.Wrap && !double.IsInfinity(availableSize.Width))
        {
            ft.MaxTextWidth = Math.Max(0, availableSize.Width);
        }

        // Include stroke thickness margins to avoid clipping
        var width = Math.Ceiling(ft.WidthIncludingTrailingWhitespace + StrokeThickness);
        var height = Math.Ceiling(ft.Height + StrokeThickness);
        return new Size(width, height);
    }

    protected override void OnRender(DrawingContext drawingContext)
    {
        base.OnRender(drawingContext);
        var text = Text ?? string.Empty;
        if (string.IsNullOrEmpty(text)) return;

        var ft = CreateFormattedText();
        if (TextWrapping == TextWrapping.Wrap)
        {
            ft.MaxTextWidth = Math.Max(0, ActualWidth);
        }

        // Build geometry at origin; layout handles alignment externally
        var geo = ft.BuildGeometry(new Point(0, 0));

        // Draw stroke first for crisp edge, then fill
        var pen = new Pen(Stroke, StrokeThickness)
        {
            LineJoin = PenLineJoin.Round,
            MiterLimit = 2
        };

        drawingContext.DrawGeometry(null, pen, geo);
        drawingContext.DrawGeometry(Foreground, null, geo);
    }

    private FormattedText CreateFormattedText()
    {
        var dpi = VisualTreeHelper.GetDpi(this);
        return new FormattedText(
            Text ?? string.Empty,
            CultureInfo.CurrentUICulture,
            FlowDirection,
            new Typeface(FontFamily, FontStyle, FontWeight, FontStretch),
            FontSize,
            Foreground,
            dpi.PixelsPerDip);
    }
}
