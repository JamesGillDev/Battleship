using System.ComponentModel;
using BattleshipMaui.ViewModels;
using Microsoft.Maui.ApplicationModel;

namespace BattleshipMaui.Behaviors;

public sealed class ShipSunkSmokeAnimationBehavior : Behavior<VisualElement>
{
    private VisualElement? _associatedObject;
    private ShipSpriteVm? _sprite;
    private CancellationTokenSource? _animationCts;

    protected override void OnAttachedTo(VisualElement bindable)
    {
        base.OnAttachedTo(bindable);
        _associatedObject = bindable;
        bindable.BindingContextChanged += OnBindingContextChanged;
        bindable.PropertyChanged += OnAssociatedObjectPropertyChanged;
        AttachToSprite(bindable.BindingContext as ShipSpriteVm);
    }

    protected override void OnDetachingFrom(VisualElement bindable)
    {
        bindable.BindingContextChanged -= OnBindingContextChanged;
        bindable.PropertyChanged -= OnAssociatedObjectPropertyChanged;
        AttachToSprite(null);
        StopSmokeAnimation(resetVisual: true);
        _associatedObject = null;
        base.OnDetachingFrom(bindable);
    }

    private void OnBindingContextChanged(object? sender, EventArgs e)
    {
        if (sender is not VisualElement element)
            return;

        AttachToSprite(element.BindingContext as ShipSpriteVm);
    }

    private void OnAssociatedObjectPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(VisualElement.IsVisible))
            return;

        UpdateAnimationState();
    }

    private void AttachToSprite(ShipSpriteVm? sprite)
    {
        if (_sprite is not null)
            _sprite.PropertyChanged -= OnSpritePropertyChanged;

        _sprite = sprite;
        if (_sprite is not null)
            _sprite.PropertyChanged += OnSpritePropertyChanged;

        UpdateAnimationState();
    }

    private void OnSpritePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(ShipSpriteVm.IsSunk) or nameof(ShipSpriteVm.IsRevealed) or nameof(ShipSpriteVm.IsSunkSmokeVisible))
            UpdateAnimationState();
    }

    private void UpdateAnimationState()
    {
        var view = _associatedObject;
        var sprite = _sprite;
        if (view is null || sprite is null)
        {
            StopSmokeAnimation(resetVisual: true);
            return;
        }

        bool shouldAnimate = view.IsVisible && sprite.IsSunkSmokeVisible;
        if (shouldAnimate)
            StartSmokeAnimation();
        else
            StopSmokeAnimation(resetVisual: true);
    }

    private void StartSmokeAnimation()
    {
        var view = _associatedObject;
        if (view is null || _animationCts is not null)
            return;

        var cts = new CancellationTokenSource();
        _animationCts = cts;
        _ = RunSmokeLoopAsync(view, cts.Token);
    }

    private void StopSmokeAnimation(bool resetVisual)
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

            _associatedObject.AbortAnimation("ShipSunkSmoke");
            _associatedObject.Opacity = 0;
            _associatedObject.TranslationX = 0;
            _associatedObject.TranslationY = 0;
            _associatedObject.Scale = 1;
        });
    }

    private async Task RunSmokeLoopAsync(VisualElement view, CancellationToken cancellationToken)
    {
        try
        {
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                view.AbortAnimation("ShipSunkSmoke");
                view.Opacity = AnimationRuntimeSettings.ReduceMotion ? 0.48 : 0.36;
                view.TranslationX = 0;
                view.TranslationY = 0;
                view.Scale = 1;
            });

            while (!cancellationToken.IsCancellationRequested)
            {
                if (AnimationRuntimeSettings.ReduceMotion)
                {
                    await Task.Delay((int)ScaleDuration(240), cancellationToken).ConfigureAwait(false);
                    continue;
                }

                double driftX = (Random.Shared.NextDouble() - 0.5) * 2.6;
                double liftY = -1.7 - (Random.Shared.NextDouble() * 2.1);
                double peakOpacity = 0.48 + (Random.Shared.NextDouble() * 0.18);
                double valleyOpacity = 0.32 + (Random.Shared.NextDouble() * 0.14);
                double peakScale = 1.03 + (Random.Shared.NextDouble() * 0.1);

                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    if (cancellationToken.IsCancellationRequested)
                        return;

                    view.AbortAnimation("ShipSunkSmoke");
                    await Task.WhenAll(
                        view.FadeToAsync(peakOpacity, ScaleDuration(300), Easing.CubicOut),
                        view.TranslateToAsync(driftX, liftY, ScaleDuration(300), Easing.CubicOut),
                        view.ScaleToAsync(peakScale, ScaleDuration(300), Easing.CubicOut));

                    if (cancellationToken.IsCancellationRequested)
                        return;

                    await Task.WhenAll(
                        view.FadeToAsync(valleyOpacity, ScaleDuration(340), Easing.CubicInOut),
                        view.TranslateToAsync(0, 0, ScaleDuration(340), Easing.CubicInOut),
                        view.ScaleToAsync(1, ScaleDuration(340), Easing.CubicInOut));
                });

                await Task.Delay((int)ScaleDuration(50), cancellationToken).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException)
        {
            // Expected when sunk smoke animation is interrupted by state changes.
        }
        finally
        {
            if (_associatedObject is not null)
            {
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    if (_associatedObject is null)
                        return;

                    _associatedObject.AbortAnimation("ShipSunkSmoke");
                    _associatedObject.Opacity = 0;
                    _associatedObject.TranslationX = 0;
                    _associatedObject.TranslationY = 0;
                    _associatedObject.Scale = 1;
                });
            }
        }
    }

    private static uint ScaleDuration(uint baseDuration)
    {
        double scaled = baseDuration * AnimationRuntimeSettings.SpeedMultiplier;
        return (uint)Math.Clamp((int)scaled, 30, 3000);
    }
}
