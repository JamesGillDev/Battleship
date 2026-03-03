using System.ComponentModel;
using BattleshipMaui.ViewModels;
using Microsoft.Maui.ApplicationModel;
#if WINDOWS
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Controls.Primitives;
using WinUIThickness = Microsoft.UI.Xaml.Thickness;
using WinUIStyle = Microsoft.UI.Xaml.Style;
using WinUISetter = Microsoft.UI.Xaml.Setter;
using WinUIHorizontalAlignment = Microsoft.UI.Xaml.HorizontalAlignment;
using WinUIVerticalAlignment = Microsoft.UI.Xaml.VerticalAlignment;
using WinUIScrollBarVisibility = Microsoft.UI.Xaml.Controls.ScrollBarVisibility;
using WinUIScrollMode = Microsoft.UI.Xaml.Controls.ScrollMode;
using WinUIZoomMode = Microsoft.UI.Xaml.Controls.ZoomMode;
#endif

namespace BattleshipMaui;

public partial class MainPage : ContentPage
{
    private BoardViewModel? _viewModel;
    private BoardViewMode _currentBoardMode = BoardViewMode.Enemy;
    private bool _isBoardTransitionRunning;

    public MainPage()
    {
        InitializeComponent();
        HookBoardCollectionViewHandlers();
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
        DisableBoardScrolling(EnemyBoardCellsView);
        DisableBoardScrolling(PlayerBoardCellsView);

        if (_viewModel is not null)
            _ = AnimateBoardModeTransitionAsync(_viewModel.BoardViewMode, instant: true);

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

    private void HookBoardCollectionViewHandlers()
    {
        EnemyBoardCellsView.HandlerChanged += OnBoardCollectionViewHandlerChanged;
        PlayerBoardCellsView.HandlerChanged += OnBoardCollectionViewHandlerChanged;
    }

    private void OnBoardCollectionViewHandlerChanged(object? sender, EventArgs e)
    {
        if (sender is not CollectionView board)
            return;

        board.Dispatcher.Dispatch(() => DisableBoardScrolling(board));
    }

    private static void DisableBoardScrolling(CollectionView board)
    {
#if WINDOWS
        if (board.Handler?.PlatformView is ListViewBase listView)
        {
            // Hard-lock the board layer so it cannot pan independently of ship overlays.
            ScrollViewer.SetVerticalScrollMode(listView, WinUIScrollMode.Disabled);
            ScrollViewer.SetHorizontalScrollMode(listView, WinUIScrollMode.Disabled);
            ScrollViewer.SetVerticalScrollBarVisibility(listView, WinUIScrollBarVisibility.Hidden);
            ScrollViewer.SetHorizontalScrollBarVisibility(listView, WinUIScrollBarVisibility.Hidden);
            ScrollViewer.SetZoomMode(listView, WinUIZoomMode.Disabled);
            listView.IsSwipeEnabled = false;
            listView.CanDragItems = false;
            listView.CanReorderItems = false;
            listView.ManipulationMode = ManipulationModes.None;
            listView.Margin = new WinUIThickness(0);

            // Remove WinUI container spacing so A-J / 1-10 rails line up exactly with cells.
            if (listView.ItemContainerStyle is null)
            {
                var itemStyle = new WinUIStyle(typeof(SelectorItem));
                itemStyle.Setters.Add(new WinUISetter(FrameworkElement.MarginProperty, new WinUIThickness(0)));
                itemStyle.Setters.Add(new WinUISetter(Control.PaddingProperty, new WinUIThickness(0)));
                itemStyle.Setters.Add(new WinUISetter(Control.HorizontalContentAlignmentProperty, WinUIHorizontalAlignment.Stretch));
                itemStyle.Setters.Add(new WinUISetter(Control.VerticalContentAlignmentProperty, WinUIVerticalAlignment.Stretch));
                listView.ItemContainerStyle = itemStyle;
            }

            return;
        }

        if (board.Handler?.PlatformView is FrameworkElement root)
        {
            var scrollViewer = FindDescendant<ScrollViewer>(root);
            if (scrollViewer is null)
                return;

            scrollViewer.VerticalScrollMode = WinUIScrollMode.Disabled;
            scrollViewer.HorizontalScrollMode = WinUIScrollMode.Disabled;
            scrollViewer.VerticalScrollBarVisibility = WinUIScrollBarVisibility.Hidden;
            scrollViewer.HorizontalScrollBarVisibility = WinUIScrollBarVisibility.Hidden;
            scrollViewer.ZoomMode = WinUIZoomMode.Disabled;
            scrollViewer.ManipulationMode = ManipulationModes.None;
        }
#endif
    }

#if WINDOWS
    private static T? FindDescendant<T>(DependencyObject? node) where T : DependencyObject
    {
        if (node is null)
            return null;

        if (node is T target)
            return target;

        int childCount = VisualTreeHelper.GetChildrenCount(node);
        for (int index = 0; index < childCount; index++)
        {
            var child = VisualTreeHelper.GetChild(node, index);
            var match = FindDescendant<T>(child);
            if (match is not null)
                return match;
        }

        return null;
    }
#endif

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
