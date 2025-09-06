using System;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Lazarus.Desktop.Utilities
{
    /// <summary>
    /// Comprehensive accessibility helper utility providing WCAG 2.1 AA compliance tools,
    /// contrast ratio validation, and keyboard navigation enhancements.
    /// </summary>
    public static class AccessibilityHelper
    {
        #region Contrast Ratio Calculations

        /// <summary>
        /// Calculates the contrast ratio between two colors according to WCAG 2.1 standards.
        /// </summary>
        /// <param name="foreground">The foreground color</param>
        /// <param name="background">The background color</param>
        /// <returns>Contrast ratio value (1.0 to 21.0)</returns>
        public static double CalculateContrastRatio(Color foreground, Color background)
        {
            var foregroundLuminance = CalculateRelativeLuminance(foreground);
            var backgroundLuminance = CalculateRelativeLuminance(background);

            var lighter = Math.Max(foregroundLuminance, backgroundLuminance);
            var darker = Math.Min(foregroundLuminance, backgroundLuminance);

            return (lighter + 0.05) / (darker + 0.05);
        }

        /// <summary>
        /// Validates if a color combination meets WCAG 2.1 AA contrast requirements.
        /// </summary>
        /// <param name="foreground">Foreground color</param>
        /// <param name="background">Background color</param>
        /// <param name="isLargeText">True if text is 18pt+ or 14pt+ bold</param>
        /// <returns>True if contrast meets AA standards</returns>
        public static bool ValidateContrastAA(Color foreground, Color background, bool isLargeText = false)
        {
            var ratio = CalculateContrastRatio(foreground, background);
            return isLargeText ? ratio >= 3.0 : ratio >= 4.5;
        }

        /// <summary>
        /// Validates if a color combination meets WCAG 2.1 AAA contrast requirements.
        /// </summary>
        /// <param name="foreground">Foreground color</param>
        /// <param name="background">Background color</param>
        /// <param name="isLargeText">True if text is 18pt+ or 14pt+ bold</param>
        /// <returns>True if contrast meets AAA standards</returns>
        public static bool ValidateContrastAAA(Color foreground, Color background, bool isLargeText = false)
        {
            var ratio = CalculateContrastRatio(foreground, background);
            return isLargeText ? ratio >= 4.5 : ratio >= 7.0;
        }

        /// <summary>
        /// Calculates relative luminance of a color according to WCAG formula.
        /// </summary>
        private static double CalculateRelativeLuminance(Color color)
        {
            var r = NormalizeColorChannel(color.R);
            var g = NormalizeColorChannel(color.G);
            var b = NormalizeColorChannel(color.B);

            return 0.2126 * r + 0.7152 * g + 0.0722 * b;
        }

        /// <summary>
        /// Normalizes a color channel value for luminance calculation.
        /// </summary>
        private static double NormalizeColorChannel(byte channel)
        {
            var normalized = channel / 255.0;
            return normalized <= 0.03928
                ? normalized / 12.92
                : Math.Pow((normalized + 0.055) / 1.055, 2.4);
        }

        #endregion

        #region Automation Properties Setup

        /// <summary>
        /// Configures comprehensive automation properties for a UI element.
        /// </summary>
        /// <param name="element">The UI element to configure</param>
        /// <param name="name">Accessible name</param>
        /// <param name="helpText">Help text description</param>
        /// <param name="liveSetting">Live region setting</param>
        public static void ConfigureAccessibility(
            UIElement element,
            string name,
            string? helpText = null,
            AutomationLiveSetting? liveSetting = null)
        {
            if (element == null) throw new ArgumentNullException(nameof(element));
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Name cannot be empty", nameof(name));

            AutomationProperties.SetName(element, name);

            if (!string.IsNullOrWhiteSpace(helpText))
            {
                AutomationProperties.SetHelpText(element, helpText);
            }

            if (liveSetting.HasValue)
            {
                AutomationProperties.SetLiveSetting(element, liveSetting.Value);
            }
        }

        /// <summary>
        /// Sets up a live region for dynamic content updates.
        /// </summary>
        /// <param name="element">Element to configure as live region</param>
        /// <param name="setting">Live setting (Polite, Assertive, or Off)</param>
        /// <param name="name">Accessible name for the live region</param>
        public static void SetupLiveRegion(UIElement element, AutomationLiveSetting setting, string name)
        {
            AutomationProperties.SetLiveSetting(element, setting);
            AutomationProperties.SetName(element, name);
        }

        /// <summary>
        /// Configures heading level for proper document structure.
        /// </summary>
        /// <param name="textBlock">TextBlock to configure as heading</param>
        /// <param name="level">Heading level (1-6)</param>
        public static void SetHeading(TextBlock textBlock, int level)
        {
            if (level < 1 || level > 6) throw new ArgumentOutOfRangeException(nameof(level), "Heading level must be between 1 and 6");

            AutomationProperties.SetHeadingLevel(textBlock, (AutomationHeadingLevel)level);
        }

        #endregion

        #region Keyboard Navigation

        /// <summary>
        /// Establishes logical tab order for a container's children.
        /// </summary>
        /// <param name="container">Container element</param>
        public static void EstablishTabOrder(DependencyObject container)
        {
            if (container == null) throw new ArgumentNullException(nameof(container));

            var focusableElements = GetFocusableChildren(container);

            for (int i = 0; i < focusableElements.Count; i++)
            {
                KeyboardNavigation.SetTabIndex(focusableElements[i], i + 1);
            }
        }

        /// <summary>
        /// Configures keyboard navigation behavior for a container.
        /// </summary>
        /// <param name="container">Container to configure</param>
        /// <param name="tabNavigation">Tab navigation mode</param>
        /// <param name="directionalNavigation">Directional navigation mode</param>
        public static void ConfigureKeyboardNavigation(
            UIElement container,
            KeyboardNavigationMode tabNavigation = KeyboardNavigationMode.Continue,
            KeyboardNavigationMode directionalNavigation = KeyboardNavigationMode.Continue)
        {
            KeyboardNavigation.SetTabNavigation(container, tabNavigation);
            KeyboardNavigation.SetDirectionalNavigation(container, directionalNavigation);
        }

        /// <summary>
        /// Gets all focusable child elements in visual order.
        /// </summary>
        private static List<UIElement> GetFocusableChildren(DependencyObject parent)
        {
            var focusable = new List<UIElement>();

            int childCount = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < childCount; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);

                if (child is UIElement element && element.Focusable)
                {
                    focusable.Add(element);
                }

                // Recursively check children
                focusable.AddRange(GetFocusableChildren(child));
            }

            return focusable;
        }

        #endregion

        #region Focus Management

        /// <summary>
        /// Sets focus to an element with validation and fallback.
        /// </summary>
        /// <param name="element">Element to focus</param>
        /// <returns>True if focus was successfully set</returns>
        public static bool SetFocus(UIElement element)
        {
            if (element == null || !element.IsEnabled || !element.IsVisible)
                return false;

            return element.Focus();
        }

        /// <summary>
        /// Moves focus to the next focusable element in tab order.
        /// </summary>
        /// <param name="currentElement">Currently focused element</param>
        /// <returns>True if focus was moved successfully</returns>
        public static bool MoveFocusNext(UIElement currentElement)
        {
            var request = new TraversalRequest(FocusNavigationDirection.Next);
            return currentElement.MoveFocus(request);
        }

        /// <summary>
        /// Moves focus to the previous focusable element in tab order.
        /// </summary>
        /// <param name="currentElement">Currently focused element</param>
        /// <returns>True if focus was moved successfully</returns>
        public static bool MoveFocusPrevious(UIElement currentElement)
        {
            var request = new TraversalRequest(FocusNavigationDirection.Previous);
            return currentElement.MoveFocus(request);
        }

        #endregion

        #region Validation Helpers

        /// <summary>
        /// Validates accessibility compliance for a UI element tree.
        /// </summary>
        /// <param name="root">Root element to validate</param>
        /// <returns>Validation results</returns>
        public static AccessibilityValidationResults ValidateAccessibility(DependencyObject root)
        {
            var results = new AccessibilityValidationResults();
            ValidateElement(root, results);
            return results;
        }

        /// <summary>
        /// Recursively validates accessibility for an element and its children.
        /// </summary>
        private static void ValidateElement(DependencyObject element, AccessibilityValidationResults results)
        {
            if (element is UIElement uiElement)
            {
                // Check for missing names on focusable elements
                if (uiElement.Focusable && string.IsNullOrEmpty(AutomationProperties.GetName(uiElement)))
                {
                    results.AddIssue($"Focusable element {element.GetType().Name} missing accessible name");
                }

                // Check for proper heading structure
                if (element is TextBlock textBlock &&
                    AutomationProperties.GetHeadingLevel(textBlock) != AutomationHeadingLevel.None)
                {
                    results.HeadingCount++;
                }

                // Validate color contrast if possible
                if (element is Control control)
                {
                    ValidateControlContrast(control, results);
                }
            }

            // Recursively check children
            int childCount = VisualTreeHelper.GetChildrenCount(element);
            for (int i = 0; i < childCount; i++)
            {
                ValidateElement(VisualTreeHelper.GetChild(element, i), results);
            }
        }

        /// <summary>
        /// Validates color contrast for a control.
        /// </summary>
        private static void ValidateControlContrast(Control control, AccessibilityValidationResults results)
        {
            if (control.Foreground is SolidColorBrush foregroundBrush &&
                control.Background is SolidColorBrush backgroundBrush)
            {
                var foregroundColor = foregroundBrush.Color;
                var backgroundColor = backgroundBrush.Color;

                if (!ValidateContrastAA(foregroundColor, backgroundColor))
                {
                    var ratio = CalculateContrastRatio(foregroundColor, backgroundColor);
                    results.AddIssue($"Control {control.GetType().Name} has insufficient contrast ratio: {ratio:F2}");
                }
            }
        }

        #endregion

        #region Screen Reader Utilities

        /// <summary>
        /// Announces a message to screen readers via a temporary live region.
        /// </summary>
        /// <param name="message">Message to announce</param>
        /// <param name="priority">Announcement priority</param>
        public static void AnnounceToScreenReader(string message, AutomationLiveSetting priority = AutomationLiveSetting.Polite)
        {
            if (string.IsNullOrWhiteSpace(message)) return;

            var announcement = new TextBlock
            {
                Text = message,
                Width = 1,
                Height = 1,
                Margin = new Thickness(-1),
                Opacity = 0
            };

            AutomationProperties.SetLiveSetting(announcement, priority);
            AutomationProperties.SetName(announcement, message);

            // Add to main window temporarily
            if (Application.Current?.MainWindow?.Content is Panel panel)
            {
                panel.Children.Add(announcement);

                // Remove after a delay to allow screen reader to process
                var timer = new System.Windows.Threading.DispatcherTimer
                {
                    Interval = TimeSpan.FromSeconds(2)
                };
                timer.Tick += (s, e) =>
                {
                    panel.Children.Remove(announcement);
                    timer.Stop();
                };
                timer.Start();
            }
        }

        #endregion
    }

    /// <summary>
    /// Results from accessibility validation.
    /// </summary>
    public class AccessibilityValidationResults
    {
        public List<string> Issues { get; } = new();
        public int HeadingCount { get; set; }
        public bool HasIssues => Issues.Count > 0;

        public void AddIssue(string issue)
        {
            Issues.Add(issue);
        }

        public override string ToString()
        {
            if (!HasIssues)
                return "No accessibility issues found.";

            return $"Found {Issues.Count} accessibility issues:\n" +
                   string.Join("\n", Issues.Select((issue, index) => $"{index + 1}. {issue}"));
        }
    }
}