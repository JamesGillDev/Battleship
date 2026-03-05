using System.ComponentModel;
using BattleshipMaui.ViewModels;
using Microsoft.Maui.ApplicationModel;

namespace BattleshipMaui.Behaviors;

public sealed class CellImpactAnimationBehavior : Behavior<VisualElement>
{
    private VisualElement? _associatedObject;
    private BoardCellVm? _cell;
    private ShotMarkerState _lastMarkerState = ShotMarkerState.None;

    protected override void OnAttachedTo(VisualElement bindable)
    {
        base.OnAttachedTo(bindable);
        _associatedObject = bindable;
        bindable.BindingContextChanged += OnBindingContextChanged;
        AttachToCell(bindable.BindingContext as BoardCellVm);
    }

    protected override void OnDetachingFrom(VisualElement bindable)
    {
        bindable.BindingContextChanged -= OnBindingContextChanged;
        AttachToCell(null);
        _associatedObject = null;
        base.OnDetachingFrom(bindable);
    }

    private void OnBindingContextChanged(object? sender, EventArgs e)
    {
        if (sender is not VisualElement element)
            return;

        AttachToCell(element.BindingContext as BoardCellVm);
    }

    private void AttachToCell(BoardCellVm? cell)
    {
        if (_cell is not null)
            _cell.PropertyChanged -= OnCellPropertyChanged;

        _cell = cell;
        _lastMarkerState = cell?.MarkerState ?? ShotMarkerState.None;

        if (_cell is null || _associatedObject is null)
            return;

        _cell.PropertyChanged += OnCellPropertyChanged;
    }

    private async void OnCellPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(BoardCellVm.MarkerState))
            return;

        var view = _associatedObject;
        var cell = _cell;
        if (view is null || cell is null)
            return;

        if (cell.MarkerState == ShotMarkerState.Hit && _lastMarkerState != ShotMarkerState.Hit)
            await RunHitAnimationAsync(view).ConfigureAwait(false);

        _lastMarkerState = cell.MarkerState;
    }

    private static async Task RunHitAnimationAsync(VisualElement view)
    {
        await MainThread.InvokeOnMainThreadAsync(async () =>
        {
            double baseRotation = view.Rotation;

            if (AnimationRuntimeSettings.ReduceMotion)
            {
                view.Opacity = 1;
                view.Scale = 1;
                view.Rotation = baseRotation;
                return;
            }

            view.Opacity = 0;
            view.Scale = 0.32;
            view.Rotation = baseRotation - 14;

            uint burst = ScaleDuration(120);
            uint recoil = ScaleDuration(90);
            uint settle = ScaleDuration(170);

            await Task.WhenAll(
                view.FadeToAsync(1, burst, Easing.CubicOut),
                view.ScaleToAsync(1.34, burst, Easing.CubicOut),
                view.RotateToAsync(baseRotation + 10, burst, Easing.CubicOut));

            await Task.WhenAll(
                view.ScaleToAsync(0.9, recoil, Easing.CubicIn),
                view.FadeToAsync(0.96, recoil, Easing.CubicIn));

            await Task.WhenAll(
                view.ScaleToAsync(1, settle, Easing.CubicIn),
                view.RotateToAsync(baseRotation, settle, Easing.CubicOut));
        });
    }

    private static uint ScaleDuration(uint baseDuration)
    {
        double scaled = baseDuration * AnimationRuntimeSettings.SpeedMultiplier;
        return (uint)Math.Clamp((int)scaled, 30, 2000);
    }
}
