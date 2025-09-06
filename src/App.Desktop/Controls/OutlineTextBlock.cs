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

    public static readonly DependencyProperty FillProperty =
        DependencyProperty.Register(
            nameof(Fill), typeof(Brush), typeof(OutlineTextBlock),
            new FrameworkPropertyMetadata(Brushes.Black, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty StrokeProperty =
        DependencyProperty.Register(
            nameof(Stroke), typeof(Brush), typeof(OutlineTextBlock),
            new FrameworkPropertyMetadata(Brushes.White, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty StrokeThicknessProperty =
        DependencyProperty.Register(
            nameof(StrokeThickness), typeof(double), typeof(OutlineTextBlock),
            new FrameworkPropertyMetadata(3.0d, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty FontFamilyProperty =
        TextElement.FontFamilyProperty.AddOwner(
            typeof(OutlineTextBlock),
            new FrameworkPropertyMetadata(new FontFamily("Segoe UI"), FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty FontSizeProperty =
        TextElement.FontSizeProperty.AddOwner(
            typeof(OutlineTextBlock),
            new FrameworkPropertyMetadata(14.0d, FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty FontWeightProperty =
        TextElement.FontWeightProperty.AddOwner(
            typeof(OutlineTextBlock),
            new FrameworkPropertyMetadata(FontWeights.SemiBold, FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty FontStyleProperty =
        TextElement.FontStyleProperty.AddOwner(
            typeof(OutlineTextBlock),
            new FrameworkPropertyMetadata(FontStyles.Normal, FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty FontStretchProperty =
        TextElement.FontStretchProperty.AddOwner(
            typeof(OutlineTextBlock),
            new FrameworkPropertyMetadata(FontStretches.Normal, FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty TextAlignmentProperty =
        DependencyProperty.Register(
            nameof(TextAlignment), typeof(TextAlignment), typeof(OutlineTextBlock),
            new FrameworkPropertyMetadata(TextAlignment.Center, FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty TextWrappingProperty =
        DependencyProperty.Register(
            nameof(TextWrapping), typeof(TextWrapping), typeof(OutlineTextBlock),
            new FrameworkPropertyMetadata(TextWrapping.NoWrap, FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender));

    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public Brush Fill
    {
        get => (Brush)GetValue(FillProperty);
        set => SetValue(FillProperty, value);
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

    public TextAlignment TextAlignment
    {
        get => (TextAlignment)GetValue(TextAlignmentProperty);
        set => SetValue(TextAlignmentProperty, value);
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        var ft = CreateFormattedText();
        return new Size(ft.WidthIncludingTrailingWhitespace, ft.Height);
    }

    protected override void OnRender(DrawingContext dc)
    {
        if (string.IsNullOrEmpty(Text)) return;
        var ft = CreateFormattedText();
        var origin = new Point(0, 0);
        var geo = ft.BuildGeometry(origin);

        if (Stroke != null && StrokeThickness > 0)
        {
            var pen = new Pen(Stroke, StrokeThickness) { LineJoin = PenLineJoin.Round };
            dc.DrawGeometry(null, pen, geo);
        }
        if (Fill != null) dc.DrawGeometry(Fill, null, geo);
    }

    private FormattedText CreateFormattedText()
    {
        var dpi = VisualTreeHelper.GetDpi(this).PixelsPerDip;
        var tf = new Typeface(FontFamily, FontStyle, FontWeight, FontStretch);
        var ft = new FormattedText(Text ?? string.Empty, CultureInfo.CurrentUICulture, FlowDirection.LeftToRight,
                                   tf, FontSize, Brushes.Black, dpi)
        {
            TextAlignment = this.TextAlignment
        };
        return ft;
    }
}
