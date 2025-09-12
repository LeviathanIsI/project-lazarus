using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;

namespace Lazarus.Desktop.Behaviors;

/// <summary>
/// Auto-scrolls a ScrollViewer to the bottom when new content arrives,
/// but only if the user was already at (or near) the bottom. If the user
/// scrolls up, auto-scrolling is suspended until they return to bottom.
/// </summary>
public static class AutoScrollBehavior
{
    public static readonly DependencyProperty EnableProperty =
        DependencyProperty.RegisterAttached(
            "Enable",
            typeof(bool),
            typeof(AutoScrollBehavior),
            new PropertyMetadata(false, OnEnableChanged));

    public static void SetEnable(DependencyObject element, bool value) => element.SetValue(EnableProperty, value);
    public static bool GetEnable(DependencyObject element) => (bool)element.GetValue(EnableProperty);

    private sealed class State
    {
        public bool WasAtBottom;
    }

    // Track per-ScrollViewer state without leaking
    private static readonly ConditionalWeakTable<ScrollViewer, State> _states = new();

    private static void OnEnableChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is ScrollViewer sv)
        {
            if (Equals(e.NewValue, true))
            {
                _states.GetOrCreateValue(sv).WasAtBottom = IsAtBottom(sv);
                sv.ScrollChanged += OnScrollChanged;
                sv.Loaded += OnLoaded;
            }
            else
            {
                sv.ScrollChanged -= OnScrollChanged;
                sv.Loaded -= OnLoaded;
                _states.Remove(sv);
            }
        }
    }

    private static void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (sender is ScrollViewer sv)
        {
            // If initially empty or already at bottom, ensure we are at bottom
            if (IsAtBottom(sv)) sv.ScrollToEnd();
        }
    }

    private static void OnScrollChanged(object sender, ScrollChangedEventArgs e)
    {
        if (sender is not ScrollViewer sv)
            return;

        var state = _states.GetOrCreateValue(sv);

        // Determine if user is at bottom before reacting to extent change
        bool atBottomBefore = state.WasAtBottom;

        // If content grew and user was at bottom, keep them at bottom
        if (e.ExtentHeightChange > 0 && atBottomBefore)
        {
            sv.ScrollToEnd();
        }

        // Update state based on current positions (post-change)
        state.WasAtBottom = IsAtBottom(sv);
    }

    private static bool IsAtBottom(ScrollViewer sv)
    {
        // Allow a tiny epsilon for float comparisons
        const double epsilon = 1.0;
        return sv.VerticalOffset + sv.ViewportHeight >= sv.ExtentHeight - epsilon;
    }
}

