using System.ComponentModel;
using BattleshipMaui.ViewModels;
using Microsoft.Maui.ApplicationModel;

namespace BattleshipMaui.Behaviors;

public sealed class ShipSpriteAnimationBehavior : Behavior<VisualElement>
{
    private VisualElement? _associatedObject;
    private ShipSpriteVm? _sprite;

    protected override void OnAttachedTo(VisualElement bindable)
    {
        base.OnAttachedTo(bindable);
        _associatedObject = bindable;
        bindable.BindingContextChanged += OnBindingContextChanged;
        AttachToSprite(bindable.BindingContext as ShipSpriteVm);
    }

    protected override void OnDetachingFrom(VisualElement bindable)
    {
        bindable.BindingContextChanged -= OnBindingContextChanged;
        AttachToSprite(null);
        _associatedObject = null;
        base.OnDetachingFrom(bindable);
    }

    private void OnBindingContextChanged(object? sender, EventArgs e)
    {
        if (sender is not VisualElement element)
            return;

        AttachToSprite(element.BindingContext as ShipSpriteVm);
    }

    private void AttachToSprite(ShipSpriteVm? sprite)
    {
        if (_sprite is not null)
            _sprite.PropertyChanged -= OnSpritePropertyChanged;

        _sprite = sprite;

        if (_sprite is null || _associatedObject is null)
            return;

        _associatedObject.Opacity = _sprite.Opacity;
        _associatedObject.Scale = 1;
        _associatedObject.TranslationX = 0;
        _associatedObject.TranslationY = 0;

        _sprite.PropertyChanged += OnSpritePropertyChanged;
    }

    private async void OnSpritePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        var view = _associatedObject;
        var sprite = _sprite;
        if (view is null || sprite is null)
            return;

        if (e.PropertyName == nameof(ShipSpriteVm.IsRevealed) && sprite.IsRevealed)
        {
            await RunRevealAnimationAsync(view, sprite).ConfigureAwait(false);
            if (sprite.IsSunk)
                await RunSunkAnimationAsync(view, sprite).ConfigureAwait(false);
            return;
        }

        if (e.PropertyName == nameof(ShipSpriteVm.IsSunk) && sprite.IsSunk)
            await RunSunkAnimationAsync(view, sprite).ConfigureAwait(false);
    }

    private static async Task RunRevealAnimationAsync(VisualElement view, ShipSpriteVm sprite)
    {
        await MainThread.InvokeOnMainThreadAsync(async () =>
        {
            if (AnimationRuntimeSettings.ReduceMotion)
            {
                view.Opacity = sprite.Opacity;
                view.Scale = 1;
                view.TranslationX = 0;
                view.TranslationY = 0;
                return;
            }

            if (sprite.TryConsumePlacementEntry(out var offset))
            {
                view.Opacity = 1;
                view.Scale = 1;
                view.TranslationX = offset.X;
                view.TranslationY = offset.Y;

                uint entryDuration = ScaleDuration(420);
                await Task.WhenAll(
                    view.TranslateToAsync(0, 0, entryDuration, Easing.CubicOut),
                    view.FadeToAsync(sprite.Opacity, ScaleDuration(260), Easing.CubicOut));
                return;
            }

            view.Opacity = 0;
            view.Scale = 0.88;
            view.TranslationX = 0;
            view.TranslationY = 0;

            uint duration = ScaleDuration(260);
            await Task.WhenAll(
                view.FadeToAsync(sprite.Opacity, duration, Easing.CubicOut),
                view.ScaleToAsync(1, duration, Easing.CubicOut));
        });
    }

    private static async Task RunSunkAnimationAsync(VisualElement view, ShipSpriteVm sprite)
    {
        await MainThread.InvokeOnMainThreadAsync(async () =>
        {
            if (AnimationRuntimeSettings.ReduceMotion)
            {
                view.Opacity = sprite.Opacity;
                view.Scale = 1;
                view.TranslationX = 0;
                view.TranslationY = 0;
                return;
            }

            await view.ScaleToAsync(1.08, ScaleDuration(120), Easing.CubicOut);
            await view.ScaleToAsync(0.95, ScaleDuration(120), Easing.CubicIn);
            await view.ScaleToAsync(1.0, ScaleDuration(120), Easing.CubicOut);

            await view.TranslateToAsync(-2, 0, ScaleDuration(45), Easing.Linear);
            await view.TranslateToAsync(2, 0, ScaleDuration(45), Easing.Linear);
            await view.TranslateToAsync(0, 0, ScaleDuration(45), Easing.Linear);
            await view.FadeToAsync(sprite.Opacity, ScaleDuration(220), Easing.CubicOut);
        });
    }

    private static uint ScaleDuration(uint baseDuration)
    {
        double scaled = baseDuration * AnimationRuntimeSettings.SpeedMultiplier;
        return (uint)Math.Clamp((int)scaled, 30, 2000);
    }
}
