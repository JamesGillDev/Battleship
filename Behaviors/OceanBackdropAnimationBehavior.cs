using System.ComponentModel;
using BattleshipMaui.ViewModels;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;

namespace BattleshipMaui.Behaviors;

public sealed class OceanBackdropAnimationBehavior : Behavior<Grid>
{
    private const string GlowLayerAClassId = "OceanBackdropGlowA";
    private const string GlowLayerBClassId = "OceanBackdropGlowB";

    private Grid? _associatedObject;
    private CancellationTokenSource? _animationCts;

    protected override void OnAttachedTo(Grid bindable)
    {
        base.OnAttachedTo(bindable);
        _associatedObject = bindable;
        bindable.PropertyChanged += OnAssociatedObjectPropertyChanged;
        StartAnimation();
    }

    protected override void OnDetachingFrom(Grid bindable)
    {
        bindable.PropertyChanged -= OnAssociatedObjectPropertyChanged;
        StopAnimation(resetVisual: true);
        _associatedObject = null;
        base.OnDetachingFrom(bindable);
    }

    private void OnAssociatedObjectPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(VisualElement.IsVisible))
            return;

        if (_associatedObject?.IsVisible == true)
            StartAnimation();
        else
            StopAnimation(resetVisual: true);
    }

    private void StartAnimation()
    {
        var surface = _associatedObject;
        if (surface is null || !surface.IsVisible || _animationCts is not null)
            return;

        var cts = new CancellationTokenSource();
        _animationCts = cts;
        _ = RunBackdropLoopAsync(surface, cts.Token);
    }

    private void StopAnimation(bool resetVisual)
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

            ResetBackdropVisuals(_associatedObject);
        });
    }

    private async Task RunBackdropLoopAsync(Grid surface, CancellationToken cancellationToken)
    {
        try
        {
            await MainThread.InvokeOnMainThreadAsync(() => ResetBackdropVisuals(surface));

            while (!cancellationToken.IsCancellationRequested)
            {
                if (AnimationRuntimeSettings.ReduceMotion)
                {
                    await Task.Delay((int)ScaleDuration(1400), cancellationToken).ConfigureAwait(false);
                    continue;
                }

                await AnimatePhaseAsync(
                    surface,
                    targetAX: 180,
                    targetAY: 90,
                    targetAOpacity: 0.28,
                    targetAScale: 1.05,
                    targetBX: -150,
                    targetBY: 72,
                    targetBOpacity: 0.2,
                    targetBScale: 1.04,
                    duration: ScaleDuration(9000)).ConfigureAwait(false);

                if (cancellationToken.IsCancellationRequested)
                    break;

                await AnimatePhaseAsync(
                    surface,
                    targetAX: -120,
                    targetAY: 56,
                    targetAOpacity: 0.2,
                    targetAScale: 1.01,
                    targetBX: 170,
                    targetBY: -36,
                    targetBOpacity: 0.16,
                    targetBScale: 1.02,
                    duration: ScaleDuration(9000)).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException)
        {
        }
        finally
        {
            if (_associatedObject is not null)
            {
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    if (_associatedObject is null)
                        return;

                    ResetBackdropVisuals(_associatedObject);
                });
            }
        }
    }

    private static async Task AnimatePhaseAsync(
        Grid surface,
        double targetAX,
        double targetAY,
        double targetAOpacity,
        double targetAScale,
        double targetBX,
        double targetBY,
        double targetBOpacity,
        double targetBScale,
        uint duration)
    {
        await MainThread.InvokeOnMainThreadAsync(async () =>
        {
            var (layerA, layerB) = ResolveGlowLayers(surface);
            var tasks = new List<Task>(8);

            if (layerA is not null)
            {
                tasks.Add(layerA.TranslateToAsync(targetAX, targetAY, duration, Easing.SinInOut));
                tasks.Add(layerA.FadeToAsync(targetAOpacity, duration, Easing.CubicInOut));
                tasks.Add(layerA.ScaleToAsync(targetAScale, duration, Easing.SinInOut));
            }

            if (layerB is not null)
            {
                tasks.Add(layerB.TranslateToAsync(targetBX, targetBY, duration, Easing.SinInOut));
                tasks.Add(layerB.FadeToAsync(targetBOpacity, duration, Easing.CubicInOut));
                tasks.Add(layerB.ScaleToAsync(targetBScale, duration, Easing.SinInOut));
            }

            if (tasks.Count > 0)
                await Task.WhenAll(tasks);
        });
    }

    private static void ResetBackdropVisuals(Grid surface)
    {
        var (layerA, layerB) = ResolveGlowLayers(surface);
        if (layerA is not null)
        {
            layerA.TranslationX = 0;
            layerA.TranslationY = 0;
            layerA.Opacity = 0.24;
            layerA.Scale = 1;
        }

        if (layerB is not null)
        {
            layerB.TranslationX = 0;
            layerB.TranslationY = 0;
            layerB.Opacity = 0.18;
            layerB.Scale = 1;
        }
    }

    private static (VisualElement? LayerA, VisualElement? LayerB) ResolveGlowLayers(Grid surface)
    {
        VisualElement? layerA = surface.Children
            .OfType<VisualElement>()
            .FirstOrDefault(element => string.Equals(element.ClassId, GlowLayerAClassId, StringComparison.Ordinal));
        VisualElement? layerB = surface.Children
            .OfType<VisualElement>()
            .FirstOrDefault(element => string.Equals(element.ClassId, GlowLayerBClassId, StringComparison.Ordinal));
        return (layerA, layerB);
    }

    private static uint ScaleDuration(uint baseDuration)
    {
        double scaled = baseDuration * AnimationRuntimeSettings.SpeedMultiplier;
        return (uint)Math.Clamp((int)scaled, 400, 22000);
    }
}
