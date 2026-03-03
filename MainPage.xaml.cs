using System.ComponentModel;
using BattleshipMaui.ViewModels;
using Microsoft.Maui.ApplicationModel;

namespace BattleshipMaui;

public partial class MainPage : ContentPage
{
    private BoardViewModel? _viewModel;
    private BoardViewMode _currentBoardMode = BoardViewMode.Enemy;
    private bool _isBoardTransitionRunning;

    public MainPage()
    {
        InitializeComponent();
    }

    protected override void OnBindingContextChanged()
    {
        if (_viewModel is not null)
            _viewModel.PropertyChanged -= OnViewModelPropertyChanged;

        base.OnBindingContextChanged();
        _viewModel = BindingContext as BoardViewModel;

        if (_viewModel is not null)
        {
            _viewModel.PropertyChanged += OnViewModelPropertyChanged;
            _ = AnimateBoardModeTransitionAsync(_viewModel.BoardViewMode, instant: true);
        }
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        if (_viewModel is not null)
            _ = AnimateBoardModeTransitionAsync(_viewModel.BoardViewMode, instant: true);

        if (_viewModel?.IsOverlayVisible == true)
            _ = AnimateOverlayAsync(_viewModel);
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (_viewModel is null)
            return;

        if (e.PropertyName == nameof(BoardViewModel.IsOverlayVisible) && _viewModel.IsOverlayVisible)
            _ = AnimateOverlayAsync(_viewModel);

        if (e.PropertyName == nameof(BoardViewModel.BoardViewMode))
            _ = AnimateBoardModeTransitionAsync(_viewModel.BoardViewMode, instant: false);
    }

    private async Task AnimateOverlayAsync(BoardViewModel vm)
    {
        await MainThread.InvokeOnMainThreadAsync(async () =>
        {
            if (!vm.IsOverlayVisible)
                return;

            double speed = AnimationRuntimeSettings.SpeedMultiplier;
            if (vm.ReduceMotionMode)
            {
                OverlayScrim.Opacity = 1;
                OverlayCard.Opacity = 1;
                OverlayCard.Scale = 1;
                foreach (var child in FleetRecapStack.Children.OfType<VisualElement>())
                {
                    child.Opacity = 1;
                    child.TranslationY = 0;
                }
                return;
            }

            uint cardDuration = ScaleDuration(280, speed);
            OverlayScrim.Opacity = 0;
            OverlayCard.Opacity = 0;
            OverlayCard.Scale = 0.92;

            await Task.WhenAll(
                OverlayScrim.FadeToAsync(1, cardDuration, Easing.CubicInOut),
                OverlayCard.FadeToAsync(1, cardDuration, Easing.CubicOut),
                OverlayCard.ScaleToAsync(1, cardDuration, Easing.CubicOut));

            if (!vm.ShowOverlayRecap || FleetRecapStack.Children.Count == 0)
                return;

            int index = 0;
            foreach (var child in FleetRecapStack.Children.OfType<VisualElement>())
            {
                child.Opacity = 0;
                child.TranslationY = 8;
                await Task.Delay((int)ScaleDuration((uint)(35 + (index * 18)), speed));
                _ = Task.WhenAll(
                    child.FadeToAsync(1, ScaleDuration(180, speed), Easing.CubicOut),
                    child.TranslateToAsync(0, 0, ScaleDuration(180, speed), Easing.CubicOut));
                index++;
            }
        });
    }

    private static uint ScaleDuration(uint baseDuration, double speed)
    {
        double scaled = baseDuration * speed;
        return (uint)Math.Clamp((int)scaled, 30, 2000);
    }

    private void ApplyBoardModeInstant(BoardViewMode mode)
    {
        bool enemyFocused = mode == BoardViewMode.Enemy;

        EnemyBoardPage.IsVisible = true;
        EnemyBoardPage.Opacity = enemyFocused ? 1 : 0.9;
        EnemyBoardPage.TranslationX = 0;
        EnemyBoardPage.Scale = enemyFocused ? 1 : 0.985;

        PlayerBoardPage.IsVisible = true;
        PlayerBoardPage.Opacity = enemyFocused ? 0.9 : 1;
        PlayerBoardPage.TranslationX = 0;
        PlayerBoardPage.Scale = enemyFocused ? 0.985 : 1;

        _currentBoardMode = mode;
    }

    private async Task AnimateBoardModeTransitionAsync(BoardViewMode targetMode, bool instant)
    {
        await MainThread.InvokeOnMainThreadAsync(async () =>
        {
            if (EnemyBoardPage is null || PlayerBoardPage is null)
                return;

            if (_currentBoardMode == targetMode)
            {
                ApplyBoardModeInstant(targetMode);
                return;
            }

            if (_isBoardTransitionRunning)
            {
                ApplyBoardModeInstant(targetMode);
                return;
            }

            bool reduceMotion = instant || _viewModel?.ReduceMotionMode == true;
            if (reduceMotion)
            {
                ApplyBoardModeInstant(targetMode);
                return;
            }

            var incoming = targetMode == BoardViewMode.Enemy ? EnemyBoardPage : PlayerBoardPage;
            var outgoing = targetMode == BoardViewMode.Enemy ? PlayerBoardPage : EnemyBoardPage;

            double speed = AnimationRuntimeSettings.SpeedMultiplier;
            uint duration = ScaleDuration(220, speed);

            _isBoardTransitionRunning = true;
            try
            {
                incoming.IsVisible = true;
                incoming.Opacity = 0.94;
                incoming.TranslationX = 0;
                incoming.Scale = 0.985;

                outgoing.IsVisible = true;
                outgoing.Scale = 1;
                outgoing.TranslationX = 0;

                await Task.WhenAll(
                    incoming.FadeToAsync(1, duration, Easing.CubicOut),
                    incoming.ScaleToAsync(1, duration, Easing.CubicOut),
                    outgoing.FadeToAsync(0.9, ScaleDuration(180, speed), Easing.CubicInOut),
                    outgoing.ScaleToAsync(0.985, duration, Easing.CubicInOut));

                incoming.Opacity = 1;
                incoming.Scale = 1;
                outgoing.Opacity = 0.9;
                outgoing.Scale = 0.985;
                _currentBoardMode = targetMode;
            }
            finally
            {
                _isBoardTransitionRunning = false;
            }
        });
    }
}
