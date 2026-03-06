using System.ComponentModel;
using BattleshipMaui.ViewModels;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Graphics;

namespace BattleshipMaui;

public partial class MainPage : ContentPage
{
    private BoardViewModel? _viewModel;
    private BoardViewMode _currentBoardMode = BoardViewMode.Enemy;

    public MainPage()
    {
        InitializeComponent();
        EnsureBoardGridStructure(EnemyBoardCellsHost);
        EnsureBoardGridStructure(PlayerBoardCellsHost);
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
        CommandCenterBoardHost.IsVisible = true;
        EnemyBoardPage.IsVisible = true;
        PlayerBoardPage.IsVisible = true;
        ApplyOverlayBlurBackdrop();

        if (_viewModel is not null)
        {
            _viewModel.EnsureMusicPlayback();
            _ = AnimateBoardModeTransitionAsync(_viewModel.BoardViewMode, instant: true);
        }

        if (_viewModel?.IsOverlayVisible == true)
            _ = AnimateOverlayAsync(_viewModel);

        if (_viewModel?.IsSettingsOpen == true)
            _ = AnimateSettingsPopupAsync();
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (_viewModel is null)
            return;

        if (e.PropertyName == nameof(BoardViewModel.IsOverlayVisible) && _viewModel.IsOverlayVisible)
            _ = AnimateOverlayAsync(_viewModel);

        if (e.PropertyName == nameof(BoardViewModel.IsOverlayVisible))
            ApplyOverlayBlurBackdrop();

        if (e.PropertyName == nameof(BoardViewModel.BoardViewMode))
            _ = AnimateBoardModeTransitionAsync(_viewModel.BoardViewMode, instant: false);

        if (e.PropertyName == nameof(BoardViewModel.IsSettingsOpen) && _viewModel.IsSettingsOpen)
            _ = AnimateSettingsPopupAsync();
    }

    private async Task AnimateOverlayAsync(BoardViewModel vm)
    {
        await MainThread.InvokeOnMainThreadAsync(async () =>
        {
            if (!vm.IsOverlayVisible)
                return;

            foreach (var child in FleetRecapStack.Children.OfType<VisualElement>())
            {
                child.Opacity = 1;
                child.TranslationY = 0;
            }

            OverlayScrim.Opacity = 1;
            OverlayCard.Opacity = 1;
            OverlayCard.Scale = 1;
        });
    }

    private async Task AnimateSettingsPopupAsync()
    {
        await MainThread.InvokeOnMainThreadAsync(async () =>
        {
            if (_viewModel?.IsSettingsOpen != true)
                return;

            if (_viewModel.ReduceMotionMode)
            {
                SettingsScrim.Opacity = 1;
                SettingsCard.Opacity = 1;
                SettingsCard.Scale = 1;
                return;
            }

            double speed = AnimationRuntimeSettings.SpeedMultiplier;
            uint duration = ScaleDuration(240, speed);

            SettingsScrim.Opacity = 0;
            SettingsCard.Opacity = 0;
            SettingsCard.Scale = 0.94;

            await Task.WhenAll(
                SettingsScrim.FadeToAsync(1, duration, Easing.CubicInOut),
                SettingsCard.FadeToAsync(1, duration, Easing.CubicOut),
                SettingsCard.ScaleToAsync(1, duration, Easing.CubicOut));
        });
    }

    private static uint ScaleDuration(uint baseDuration, double speed)
    {
        double scaled = baseDuration * speed;
        return (uint)Math.Clamp((int)scaled, 30, 2000);
    }

    private void OnPlayerCellPointerEntered(object? sender, PointerEventArgs e)
    {
        if (_viewModel?.CanPlaceShips != true)
            return;

        if (sender is not BindableObject bindable || bindable.BindingContext is not BoardCellVm cell)
            return;

        if (_viewModel.UpdatePlacementPreviewCommand.CanExecute(cell))
            _viewModel.UpdatePlacementPreviewCommand.Execute(cell);
    }

    private void OnPlayerBoardPointerExited(object? sender, PointerEventArgs e)
    {
        if (_viewModel is null)
            return;

        if (_viewModel.ClearPlacementPreviewCommand.CanExecute(null))
            _viewModel.ClearPlacementPreviewCommand.Execute(null);
    }

    private void ApplyOverlayBlurBackdrop()
    {
        bool overlayVisible = _viewModel?.IsOverlayVisible == true;
        OverlayScrim.BackgroundColor = overlayVisible
            ? Color.FromArgb("#D20A1524")
            : Color.FromArgb("#A0000000");

        double boardOpacity = 1;
        CommandCenterBoardHost.Opacity = boardOpacity;
        EnemyBoardPage.Opacity = boardOpacity;
        PlayerBoardPage.Opacity = boardOpacity;
    }

    private void ApplyBoardModeInstant(BoardViewMode mode)
    {
        EnemyBoardPage.IsVisible = true;
        EnemyBoardPage.Opacity = 1;
        EnemyBoardPage.TranslationX = 0;
        EnemyBoardPage.Scale = 1;

        PlayerBoardPage.IsVisible = true;
        PlayerBoardPage.Opacity = 1;
        PlayerBoardPage.TranslationX = 0;
        PlayerBoardPage.Scale = 1;

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

            ApplyBoardModeInstant(targetMode);
        });
    }

    private static void EnsureBoardGridStructure(Grid host)
    {
        if (host.RowDefinitions.Count == BoardViewModel.Size && host.ColumnDefinitions.Count == BoardViewModel.Size)
            return;

        host.RowDefinitions.Clear();
        host.ColumnDefinitions.Clear();

        for (int index = 0; index < BoardViewModel.Size; index++)
        {
            host.RowDefinitions.Add(new RowDefinition { Height = BoardViewModel.CellSize });
            host.ColumnDefinitions.Add(new ColumnDefinition { Width = BoardViewModel.CellSize });
        }
    }
}
