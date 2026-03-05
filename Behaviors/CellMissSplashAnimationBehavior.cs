using System.ComponentModel;
using System.Linq;
using BattleshipMaui.ViewModels;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;

namespace BattleshipMaui.Behaviors;

public sealed class CellMissSplashAnimationBehavior : Behavior<Grid>
{
    private Grid? _associatedObject;
    private BoardCellVm? _cell;
    private ShotMarkerState _lastMarkerState = ShotMarkerState.None;
    private CancellationTokenSource? _animationCts;

    protected override void OnAttachedTo(Grid bindable)
    {
        base.OnAttachedTo(bindable);
        _associatedObject = bindable;
        bindable.BindingContextChanged += OnBindingContextChanged;
        bindable.PropertyChanged += OnAssociatedObjectPropertyChanged;
        AttachToCell(bindable.BindingContext as BoardCellVm);
    }

    protected override void OnDetachingFrom(Grid bindable)
    {
        bindable.BindingContextChanged -= OnBindingContextChanged;
        bindable.PropertyChanged -= OnAssociatedObjectPropertyChanged;
        AttachToCell(null);
        StopSplashAnimation(resetVisual: true);
        _associatedObject = null;
        base.OnDetachingFrom(bindable);
    }

    private void OnBindingContextChanged(object? sender, EventArgs e)
    {
        if (sender is not Grid grid)
            return;

        AttachToCell(grid.BindingContext as BoardCellVm);
    }

    private void OnAssociatedObjectPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(VisualElement.IsVisible))
            return;

        if (_associatedObject is null || _cell is null)
            return;

        if (_associatedObject.IsVisible && _cell.MarkerState == ShotMarkerState.Miss)
            StartSplashAnimation();
    }

    private void AttachToCell(BoardCellVm? cell)
    {
        if (_cell is not null)
            _cell.PropertyChanged -= OnCellPropertyChanged;

        _cell = cell;
        _lastMarkerState = cell?.MarkerState ?? ShotMarkerState.None;

        if (_cell is null || _associatedObject is null)
        {
            StopSplashAnimation(resetVisual: true);
            return;
        }

        _cell.PropertyChanged += OnCellPropertyChanged;
        if (_cell.MarkerState == ShotMarkerState.Miss)
            StartSplashAnimation();
        else
            StopSplashAnimation(resetVisual: true);
    }

    private void OnCellPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(BoardCellVm.MarkerState))
            return;

        var cell = _cell;
        if (cell is null)
            return;

        if (cell.MarkerState == ShotMarkerState.Miss && _lastMarkerState != ShotMarkerState.Miss)
        {
            StartSplashAnimation();
        }
        else if (cell.MarkerState != ShotMarkerState.Miss && _lastMarkerState == ShotMarkerState.Miss)
        {
            StopSplashAnimation(resetVisual: true);
        }

        _lastMarkerState = cell.MarkerState;
    }

    private void StartSplashAnimation()
    {
        var surface = _associatedObject;
        if (surface is null)
            return;

        if (_animationCts is not null)
        {
            _animationCts.Cancel();
            _animationCts.Dispose();
            _animationCts = null;
        }

        var cts = new CancellationTokenSource();
        _animationCts = cts;
        _ = RunSplashAnimationAsync(surface, cts.Token);
    }

    private void StopSplashAnimation(bool resetVisual)
    {
        if (_animationCts is not null)
        {
            _animationCts.Cancel();
            _animationCts.Dispose();
            _animationCts = null;
        }

        if (!resetVisual || _associatedObject is null)
            return;

        MainThread.BeginInvokeOnMainThread(() =>
        {
            if (_associatedObject is null)
                return;

            ResetSplashVisuals(ResolveSplashParts(_associatedObject));
        });
    }

    private async Task RunSplashAnimationAsync(Grid surface, CancellationToken token)
    {
        try
        {
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                var parts = ResolveSplashParts(surface);
                ResetSplashVisuals(parts);

                if (AnimationRuntimeSettings.ReduceMotion)
                    return;

                uint burst = ScaleDuration(120);
                uint settle = ScaleDuration(180);
                uint rippleA = ScaleDuration(420);
                uint rippleB = ScaleDuration(560);
                uint dropletFlight = ScaleDuration(300);
                uint dropletSettle = ScaleDuration(220);

                if (parts.Core is not null)
                {
                    parts.Core.Scale = 0.62;
                    parts.Core.Opacity = 0.66;
                }

                if (parts.Foam is not null)
                {
                    parts.Foam.Scale = 0.35;
                    parts.Foam.Opacity = 0.48;
                }

                if (parts.Halo is not null)
                    parts.Halo.Opacity = 0.2;

                var openingTasks = new List<Task>(4);
                if (parts.Core is not null)
                {
                    openingTasks.Add(Task.WhenAll(
                        parts.Core.ScaleToAsync(1.18, burst, Easing.CubicOut),
                        parts.Core.FadeToAsync(0.95, burst, Easing.CubicOut)));
                }

                if (parts.Foam is not null)
                {
                    openingTasks.Add(Task.WhenAll(
                        parts.Foam.ScaleToAsync(1.1, burst, Easing.CubicOut),
                        parts.Foam.FadeToAsync(1, burst, Easing.CubicOut)));
                }

                if (parts.Halo is not null)
                {
                    openingTasks.Add(parts.Halo.FadeToAsync(0.45, burst, Easing.CubicOut));
                }

                if (openingTasks.Count > 0)
                    await Task.WhenAll(openingTasks);

                if (token.IsCancellationRequested)
                    return;

                var waveTasks = new List<Task>(5);
                if (parts.RingA is not null)
                {
                    parts.RingA.Opacity = 0.9;
                    parts.RingA.Scale = 0.46;
                    waveTasks.Add(Task.WhenAll(
                        parts.RingA.ScaleToAsync(1.38, rippleA, Easing.CubicOut),
                        parts.RingA.FadeToAsync(0, rippleA, Easing.CubicOut)));
                }

                if (parts.DropletLeft is not null)
                {
                    parts.DropletLeft.Opacity = 0.94;
                    parts.DropletLeft.Scale = 0.55;
                    parts.DropletLeft.TranslationX = -10;
                    parts.DropletLeft.TranslationY = -6;
                    waveTasks.Add(Task.WhenAll(
                        parts.DropletLeft.TranslateToAsync(-14, -13, dropletFlight, Easing.CubicOut),
                        parts.DropletLeft.ScaleToAsync(1, dropletFlight, Easing.CubicOut),
                        parts.DropletLeft.FadeToAsync(0.05, dropletFlight, Easing.CubicIn)));
                }

                if (parts.DropletCenter is not null)
                {
                    parts.DropletCenter.Opacity = 0.98;
                    parts.DropletCenter.Scale = 0.45;
                    parts.DropletCenter.TranslationX = 0;
                    parts.DropletCenter.TranslationY = -9;
                    waveTasks.Add(Task.WhenAll(
                        parts.DropletCenter.TranslateToAsync(0, -16, dropletFlight, Easing.CubicOut),
                        parts.DropletCenter.ScaleToAsync(0.92, dropletFlight, Easing.CubicOut),
                        parts.DropletCenter.FadeToAsync(0.08, dropletFlight, Easing.CubicIn)));
                }

                if (parts.DropletRight is not null)
                {
                    parts.DropletRight.Opacity = 0.94;
                    parts.DropletRight.Scale = 0.55;
                    parts.DropletRight.TranslationX = 10;
                    parts.DropletRight.TranslationY = -6;
                    waveTasks.Add(Task.WhenAll(
                        parts.DropletRight.TranslateToAsync(14, -13, dropletFlight, Easing.CubicOut),
                        parts.DropletRight.ScaleToAsync(1, dropletFlight, Easing.CubicOut),
                        parts.DropletRight.FadeToAsync(0.05, dropletFlight, Easing.CubicIn)));
                }

                await Task.Delay((int)ScaleDuration(80));
                if (token.IsCancellationRequested)
                    return;

                if (parts.RingB is not null)
                {
                    parts.RingB.Opacity = 0.72;
                    parts.RingB.Scale = 0.42;
                    waveTasks.Add(Task.WhenAll(
                        parts.RingB.ScaleToAsync(1.6, rippleB, Easing.CubicOut),
                        parts.RingB.FadeToAsync(0, rippleB, Easing.CubicOut)));
                }

                if (waveTasks.Count > 0)
                    await Task.WhenAll(waveTasks);

                if (token.IsCancellationRequested)
                    return;

                var settleTasks = new List<Task>(3);
                if (parts.Core is not null)
                {
                    settleTasks.Add(Task.WhenAll(
                        parts.Core.ScaleToAsync(1, settle, Easing.CubicInOut),
                        parts.Core.FadeToAsync(0.9, settle, Easing.CubicInOut)));
                }

                if (parts.Foam is not null)
                {
                    settleTasks.Add(Task.WhenAll(
                        parts.Foam.ScaleToAsync(1, settle, Easing.CubicInOut),
                        parts.Foam.FadeToAsync(0.96, settle, Easing.CubicInOut)));
                }

                if (parts.Halo is not null)
                {
                    settleTasks.Add(parts.Halo.FadeToAsync(0.32, dropletSettle, Easing.CubicInOut));
                }

                if (settleTasks.Count > 0)
                    await Task.WhenAll(settleTasks);

                if (parts.DropletLeft is not null)
                    parts.DropletLeft.Opacity = 0;
                if (parts.DropletCenter is not null)
                    parts.DropletCenter.Opacity = 0;
                if (parts.DropletRight is not null)
                    parts.DropletRight.Opacity = 0;
            });
        }
        catch (OperationCanceledException)
        {
            // Expected when the board is reset or rebound while splash animation is in flight.
        }
        finally
        {
            if (_animationCts is not null && _animationCts.Token == token)
            {
                _animationCts.Dispose();
                _animationCts = null;
            }
        }
    }

    private static SplashParts ResolveSplashParts(Grid surface)
    {
        VisualElement? Find(string classId) =>
            surface.Children.OfType<VisualElement>()
                .FirstOrDefault(child => string.Equals(child.ClassId, classId, StringComparison.Ordinal));

        return new SplashParts(
            Halo: Find("MissSplashHalo"),
            Core: Find("MissSplashCore"),
            Foam: Find("MissSplashFoam"),
            RingA: Find("MissSplashRingA"),
            RingB: Find("MissSplashRingB"),
            DropletLeft: Find("MissSplashDropletLeft"),
            DropletCenter: Find("MissSplashDropletCenter"),
            DropletRight: Find("MissSplashDropletRight"));
    }

    private static void ResetSplashVisuals(SplashParts parts)
    {
        ResetElement(parts.Halo, opacity: 0.32, scale: 1, translationX: 0, translationY: 0);
        ResetElement(parts.Core, opacity: 0.9, scale: 1, translationX: 0, translationY: 0);
        ResetElement(parts.Foam, opacity: 0.96, scale: 1, translationX: 0, translationY: 0);
        ResetElement(parts.RingA, opacity: 0, scale: 0.55, translationX: 0, translationY: 0);
        ResetElement(parts.RingB, opacity: 0, scale: 0.42, translationX: 0, translationY: 0);
        ResetElement(parts.DropletLeft, opacity: 0, scale: 0.45, translationX: -10, translationY: -6);
        ResetElement(parts.DropletCenter, opacity: 0, scale: 0.42, translationX: 0, translationY: -9);
        ResetElement(parts.DropletRight, opacity: 0, scale: 0.45, translationX: 10, translationY: -6);
    }

    private static void ResetElement(
        VisualElement? element,
        double opacity,
        double scale,
        double translationX,
        double translationY)
    {
        if (element is null)
            return;

        element.CancelAnimations();
        element.Opacity = opacity;
        element.Scale = scale;
        element.TranslationX = translationX;
        element.TranslationY = translationY;
    }

    private static uint ScaleDuration(uint baseDuration)
    {
        double scaled = baseDuration * AnimationRuntimeSettings.SpeedMultiplier;
        return (uint)Math.Clamp((int)scaled, 30, 2000);
    }

    private readonly record struct SplashParts(
        VisualElement? Halo,
        VisualElement? Core,
        VisualElement? Foam,
        VisualElement? RingA,
        VisualElement? RingB,
        VisualElement? DropletLeft,
        VisualElement? DropletCenter,
        VisualElement? DropletRight);
}
