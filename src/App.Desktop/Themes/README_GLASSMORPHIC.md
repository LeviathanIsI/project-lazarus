# Finexa Glassmorphic Design System

A comprehensive WPF design system implementing glassmorphic aesthetics with dark theme foundation, gradient accents, and smooth animations.

## Overview

The Finexa Glassmorphic Design System provides a complete visual language for the Lazarus WPF application, featuring:

- **Unified Dark Theme**: Near-black (#0A0A0F) background with glass-effect overlays
- **Purple-Pink Gradient**: Primary gradient from #8B5CF6 to #EC4899
- **Glass Morphism**: Semi-transparent cards with blur simulation and gradient borders
- **Smooth Animations**: Hover, press, and entrance effects using native WPF Storyboards
- **Typography Hierarchy**: Six levels from H1 (32px) to Small (12px) with Cascadia Code mono

## File Structure

```
src/App.Desktop/Themes/
├── Glassmorphic.xaml              # Core theme: colors, typography, base styles
├── GlassmorphicControls.xaml      # Specialized controls and templates
└── GlassmorphicExamples.xaml      # Usage examples and implementation guide
```

## Core Color Palette

| Color | Hex Code | Usage |
|-------|----------|-------|
| Background | #0A0A0F | Main app background |
| Card Background | #0A0A0F | Glass card base |
| Card Overlay | #1A1A1A | Glass effect overlay (10% opacity) |
| Primary Gradient Start | #8B5CF6 | Purple accent |
| Primary Gradient End | #EC4899 | Pink accent |
| Text Primary | #FFFFFF | Main text color |
| Text Secondary | #A0A0A0 | Muted text |
| Glass Border | #19FFFFFF | Semi-transparent borders |

## Typography System

### Font Families
- **System Font**: Segoe UI (primary interface font)
- **Mono Font**: Cascadia Code, Consolas, Courier New (code/data display)

### Size Hierarchy
- **H1**: 32px Bold - Main page headers
- **H2**: 24px SemiBold - Section headers
- **H3**: 18px Medium - Subsection headers
- **Body**: 14px Normal - Regular content text
- **Small**: 12px Normal - Secondary information
- **Mono**: 13px Normal - Code and data display

### Usage Examples
```xml
<TextBlock Text="Main Heading" Style="{StaticResource H1TextStyle}"/>
<TextBlock Text="Section Title" Style="{StaticResource H2TextStyle}"/>
<TextBlock Text="Regular content" Style="{StaticResource BodyTextStyle}"/>
<TextBlock Text="Muted text" Style="{StaticResource SecondaryTextStyle}"/>
<TextBlock Text="Small details" Style="{StaticResource SmallTextStyle}"/>
<TextBlock Text="Code block" Style="{StaticResource MonoTextStyle}"/>
```

## Glass Card System

### Base Glass Card
```xml
<Border Style="{StaticResource GlassCardStyle}">
    <!-- Content -->
</Border>
```

**Specifications:**
- Background: Semi-transparent overlay (#1A1A1A with 60% opacity)
- Border: 1px gradient from white 10% to transparent
- Corner Radius: 12px
- Shadow: 0 8px 32px rgba(0,0,0,0.2)

### Interactive Glass Card
```xml
<Border Style="{StaticResource InteractiveGlassCardStyle}">
    <!-- Content -->
</Border>
```

**Additional Features:**
- Hover: Scale 1.02 (200ms ease-out)
- Press: Scale 0.98 (instant)
- Cursor changes to hand pointer

## Button Styles

### Primary Glass Button
```xml
<Button Style="{StaticResource PrimaryGlassButtonStyle}" Content="Action"/>
```
- Background: Purple-pink gradient
- Text: White
- Padding: 20,12
- Animations: Hover scale and press effects

### Secondary Glass Button
```xml
<Button Style="{StaticResource SecondaryGlassButtonStyle}" Content="Secondary"/>
```
- Background: Semi-transparent glass
- Border: Glass gradient (changes to primary on hover)
- Text: White

### Icon Glass Button
```xml
<Button Style="{StaticResource IconGlassButtonStyle}">
    <!-- Icon content -->
</Button>
```
- Size: 40x40px
- Background: Transparent (glass on hover)
- Corner Radius: 8px

### Navigation Glass Button
```xml
<Button Style="{StaticResource NavGlassButtonStyle}" Content="Navigation Item"/>
```
- Height: 48px
- Left-aligned content
- Selected state indicator (3px gradient bar)

## Status Pill System

### Status Colors
- **Success**: Green (#10B981) - Connection, completion states
- **Warning**: Amber (#F59E0B) - Caution, limited states  
- **Error**: Red (#EF4444) - Error, offline states
- **Primary**: Purple-pink gradient - Featured status

### Implementation
```xml
<Border Style="{StaticResource SuccessStatusPillStyle}">
    <TextBlock Text="Connected" Style="{StaticResource StatusPillTextStyle}"/>
</Border>
```

**Specifications:**
- Corner Radius: 20px (fully rounded)
- Padding: 12,6
- Background: 20% opacity of status color
- Border: 30% opacity of status color

## Animation System

### Core Animations

#### Hover Scale Animation
- Target: RenderTransform ScaleX/ScaleY
- Scale: 1.0 → 1.02
- Duration: 200ms
- Easing: CubicEase EaseOut

#### Press Scale Animation  
- Target: RenderTransform ScaleX/ScaleY
- Scale: 1.0 → 0.98
- Duration: 50ms
- Easing: CubicEase EaseOut

#### Fade In Animation
- Target: Opacity + TranslateY
- Opacity: 0 → 1
- TranslateY: 20 → 0
- Duration: 400ms
- Easing: CubicEase EaseOut

### Animation Usage
```xml
<!-- Element with entrance animation -->
<Border RenderTransformOrigin="0.5,0.5">
    <Border.RenderTransform>
        <CompositeTransform/>
    </Border.RenderTransform>
    <Border.Triggers>
        <EventTrigger RoutedEvent="Loaded">
            <BeginStoryboard Storyboard="{StaticResource FadeInStoryboard}"/>
        </EventTrigger>
    </Border.Triggers>
    <!-- Content -->
</Border>
```

## Input Controls

### Glass TextBox
```xml
<TextBox Style="{StaticResource GlassTextBoxStyle}" 
         Text="{Binding InputValue}"/>
```
- Background: Semi-transparent glass
- Border: Glass gradient (primary gradient on focus)
- Caret: Primary gradient color
- Selection: Primary gradient with 30% opacity

### Glass Slider
```xml
<Slider Style="{StaticResource GlassSliderStyle}" 
        Value="{Binding SliderValue}"/>
```
- Track: Semi-transparent glass background
- Thumb: Primary gradient, 16px diameter
- Hover: Thumb scales to 1.2x

### Glass Toggle Button
```xml
<ToggleButton Style="{StaticResource GlassToggleButtonStyle}"
              IsChecked="{Binding FeatureEnabled}"/>
```
- Unchecked: Glass background with glass border
- Checked: Primary gradient background
- Hover: Primary gradient border

## List Controls

### Glass ListBox
```xml
<ListBox ItemsSource="{Binding Items}"
         Style="{StaticResource GlassListBoxStyle}"
         ItemContainerStyle="{StaticResource GlassListBoxItemStyle}"/>
```

### Chat Message Template
```xml
<ListBox ItemTemplate="{StaticResource ChatMessageTemplate}"/>
```

### Model Card Template
```xml
<ListBox ItemTemplate="{StaticResource ModelCardTemplate}"/>
```

## Progress Controls

### Glass Progress Bar
```xml
<ProgressBar Style="{StaticResource GlassProgressBarStyle}"
             Value="{Binding Progress}"/>
```
- Background: Semi-transparent glass
- Foreground: Primary gradient
- Height: 8px
- Corner Radius: 4px

### Loading Spinner
```xml
<ContentControl Template="{StaticResource LoadingSpinnerTemplate}"/>
```
- Size: 32x32px
- Color: Primary gradient
- Animation: 360° rotation in 1 second, infinite repeat

## Window Styling

### Glassmorphic Window
```xml
<Window Style="{StaticResource GlassmorphicWindowStyle}">
    <!-- Content -->
</Window>
```

**Features:**
- WindowStyle: None (custom chrome)
- AllowsTransparency: True
- Corner Radius: 12px
- Drop Shadow: 0 10px 40px rgba(0,0,0,0.3)

## Implementation Guidelines

### 1. Resource Organization
- Include both Glassmorphic.xaml and GlassmorphicControls.xaml in App.xaml
- Load in correct order (base resources first)

### 2. Animation Best Practices
- Always set RenderTransformOrigin="0.5,0.5" for scale animations
- Use CompositeTransform for complex animations
- Keep animation durations under 400ms for responsiveness

### 3. Accessibility Compliance
- All text contrasts meet WCAG 2.1 AA standards
- Interactive elements have clear hover states
- Status information uses both color and text

### 4. Performance Considerations
- Glass effects use native WPF brushes (no actual blur)
- Animations target Transform properties for GPU acceleration
- Resources are shared across the application

## Color Resource Reference

### Solid Brushes
- `{StaticResource BackgroundBrush}` - Main background
- `{StaticResource TextPrimaryBrush}` - Primary text
- `{StaticResource TextSecondaryBrush}` - Secondary text
- `{StaticResource GlassBorderBrush}` - Glass borders

### Gradient Brushes
- `{StaticResource PrimaryGradientBrush}` - Diagonal gradient
- `{StaticResource PrimaryGradientHorizontalBrush}` - Horizontal gradient
- `{StaticResource GlassCardBrush}` - Semi-transparent overlay
- `{StaticResource GlassBorderGradientBrush}` - Border gradient

### Animation Resources
- `{StaticResource HoverScaleUpStoryboard}` - Hover entrance
- `{StaticResource HoverScaleDownStoryboard}` - Hover exit
- `{StaticResource PressScaleStoryboard}` - Press effect
- `{StaticResource FadeInStoryboard}` - Element entrance
- `{StaticResource CardEntranceStoryboard}` - Card animation

## Integration with Existing Code

### Replacing Old Styles
Replace existing style references in XAML:
- `MainWindowStyle` → `GlassmorphicWindowStyle`
- `NavigationButtonStyle` → `NavGlassButtonStyle`
- `IconButtonStyle` → `IconGlassButtonStyle`
- Custom colors → Glassmorphic color resources

### ViewModel Compatibility
The design system works with existing ViewModels and data binding patterns. No changes required to business logic.

### Theme Switching
To switch themes, update App.xaml resource dictionary references:
```xml
<ResourceDictionary.MergedDictionaries>
    <ResourceDictionary Source="Themes/Glassmorphic.xaml"/>
    <ResourceDictionary Source="Themes/GlassmorphicControls.xaml"/>
</ResourceDictionary.MergedDictionaries>
```

---

## Support

For questions about implementing the glassmorphic design system or extending styles for new components, refer to the examples in `GlassmorphicExamples.xaml` or consult the WPF.Stylist agent for architectural guidance.