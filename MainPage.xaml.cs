using System.ComponentModel;
using System.Diagnostics;
using System.Threading;
using BattleshipMaui.ViewModels;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Graphics;
#if WINDOWS
using Windows.Media.Core;
using Windows.Media.Playback;
using WinUiWindow = Microsoft.UI.Xaml.Window;
#endif

namespace BattleshipMaui;

public partial class MainPage : ContentPage
{
    private BoardViewModel? _viewModel;
    private BoardViewMode _currentBoardMode = BoardViewMode.Enemy;
    private bool _startupSequenceStarted;
    private bool _startupSequenceCompleted;
    private CancellationTokenSource? _startupSequenceCts;
#if WINDOWS
    private MediaPlayer? _startupAudioPlayer;
#endif

    public MainPage()
    {
        InitializeComponent();
        EnsureBoardGridStructure(EnemyBoardCellsHost, BoardViewModel.CellSize);
        EnsureBoardGridStructure(PlayerBoardCellsHost, BoardViewModel.CellSize);
    }

    protected override void OnBindingContextChanged()
    {
        if (_viewModel is not null)
            _viewModel.PropertyChanged -= OnViewModelPropertyChanged;

        base.OnBindingContextChanged();
        _viewModel = BindingContext as BoardViewModel;

        if (_viewModel is not null)
        {
            _viewModel.ApplyBuildFlavorDefaults();
            RefreshBoardGridStructure();
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
            RefreshBoardGridStructure();
            _viewModel.EnsureMusicPlayback();
            _ = AnimateBoardModeTransitionAsync(_viewModel.BoardViewMode, instant: true);
        }

        if (_viewModel?.IsOverlayVisible == true)
            _ = AnimateOverlayAsync(_viewModel);

        if (_viewModel?.IsSettingsOpen == true)
            _ = AnimateSettingsPopupAsync();

        if (!_startupSequenceStarted)
        {
            _startupSequenceStarted = true;
            _startupSequenceCts = new CancellationTokenSource();
            _ = RunStartupSequenceAsync(_startupSequenceCts.Token);
        }
    }

    public void HandleEscapeKey()
    {
        if (!_startupSequenceCompleted)
        {
            SkipStartupSequence(skipToGameplay: true);
            return;
        }

        _viewModel?.HandleEscapeKey();
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

        if (e.PropertyName == nameof(BoardViewModel.IsTurnTransitionActive) && _viewModel.IsTurnTransitionActive)
            _ = AnimateTurnTransitionAsync();

        if (e.PropertyName == nameof(BoardViewModel.IsIntelBubbleVisible) && _viewModel.IsIntelBubbleVisible)
            _ = AnimateIntelBubbleAsync();

        if (e.PropertyName == nameof(BoardViewModel.CellPixelSize) || e.PropertyName == nameof(BoardViewModel.BoardPixelSize))
            RefreshBoardGridStructure();
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

    private async Task RunStartupSequenceAsync(CancellationToken cancellationToken)
    {
        try
        {
            StartupSequenceScrim.IsVisible = true;
            StartupSequenceScrim.Opacity = 1;
            StartupSkipChip.Opacity = 0.78;
            StartupCreatorPrefixLabel.Text = string.Empty;
            StartupCreatorNameLabel.Text = string.Empty;
            StartupVsCodeLabel.Text = string.Empty;
            StartupTitleTopLabel.Opacity = 0;
            StartupTitleMainLabel.Opacity = 0;
            StartupTitleTopLabel.Scale = 0.9;
            StartupTitleMainLabel.Scale = 0.88;

            await RunCreatorStartupSceneAsync(cancellationToken);
            await RunVsCodeStartupSceneAsync(cancellationToken);
            await RunTitleStartupSceneAsync(cancellationToken);
            await CompleteStartupSequenceAsync(skipToGameplay: false);
        }
        catch (OperationCanceledException)
        {
            await CompleteStartupSequenceAsync(skipToGameplay: true);
        }
        catch
        {
            await CompleteStartupSequenceAsync(skipToGameplay: true);
        }
    }

    private void SkipStartupSequence(bool skipToGameplay)
    {
        if (_startupSequenceCompleted)
            return;

        _startupSequenceCts?.Cancel();
        _ = CompleteStartupSequenceAsync(skipToGameplay);
    }

    private async Task CompleteStartupSequenceAsync(bool skipToGameplay)
    {
        if (_startupSequenceCompleted)
            return;

        _startupSequenceCompleted = true;
        _startupSequenceCts?.Dispose();
        _startupSequenceCts = null;
        StopStartupAudio();

        if (!skipToGameplay && StartupSequenceScrim.IsVisible)
            await StartupSequenceScrim.FadeToAsync(0, 420, Easing.CubicIn);

        StartupCreatorScene.IsVisible = false;
        StartupVsCodeScene.IsVisible = false;
        StartupTitleScene.IsVisible = false;
        StartupSequenceScrim.IsVisible = false;
        StartupSequenceScrim.Opacity = 0;

        if (skipToGameplay)
            _viewModel?.HandleEscapeKey();

        await Task.CompletedTask;
    }

    private async Task RunCreatorStartupSceneAsync(CancellationToken cancellationToken)
    {
        await PrepareStartupSceneAsync(StartupCreatorScene, cancellationToken);
        PlayStartupAudio(AppAudio.StartupCreator);

        var stopwatch = Stopwatch.StartNew();
        await StartupCreatorScene.FadeToAsync(1, 850, Easing.CubicOut);
        await TypeLabelAsync(StartupCreatorPrefixLabel, "Created By", 1400, cancellationToken);
        await DelayAsync(300, cancellationToken);
        await TypeLabelAsync(StartupCreatorNameLabel, "Echoing1822Tide", 2600, cancellationToken);
        await WaitForRemainingSceneTimeAsync(stopwatch, 7000, cancellationToken);
        await StartupCreatorScene.FadeToAsync(0, 850, Easing.CubicIn);
        StartupCreatorScene.IsVisible = false;
    }

    private async Task RunVsCodeStartupSceneAsync(CancellationToken cancellationToken)
    {
        await PrepareStartupSceneAsync(StartupVsCodeScene, cancellationToken);
        PlayStartupAudio(AppAudio.StartupVsCode);

        var stopwatch = Stopwatch.StartNew();
        await StartupVsCodeScene.FadeToAsync(1, 850, Easing.CubicOut);
        await TypeLabelAsync(StartupVsCodeLabel, "Developed with VS Code", 2600, cancellationToken);
        await WaitForRemainingSceneTimeAsync(stopwatch, 8000, cancellationToken);
        await StartupVsCodeScene.FadeToAsync(0, 850, Easing.CubicIn);
        StartupVsCodeScene.IsVisible = false;
    }

    private async Task RunTitleStartupSceneAsync(CancellationToken cancellationToken)
    {
        await PrepareStartupSceneAsync(StartupTitleScene, cancellationToken);
        PlayStartupAudio(AppAudio.StartupTitle);

        var stopwatch = Stopwatch.StartNew();
        await StartupTitleScene.FadeToAsync(1, 500, Easing.CubicOut);
        await Task.WhenAll(
            StartupTitleTopLabel.FadeToAsync(1, 1800, Easing.CubicOut),
            StartupTitleTopLabel.ScaleToAsync(1, 1800, Easing.CubicOut),
            StartupTitleMainLabel.FadeToAsync(1, 2600, Easing.CubicOut),
            StartupTitleMainLabel.ScaleToAsync(1, 2600, Easing.CubicOut));
        await WaitForRemainingSceneTimeAsync(stopwatch, 7000, cancellationToken);
        await StartupTitleScene.FadeToAsync(0, 900, Easing.CubicIn);
        StartupTitleScene.IsVisible = false;
    }

    private async Task PrepareStartupSceneAsync(VisualElement scene, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        StartupCreatorScene.IsVisible = ReferenceEquals(scene, StartupCreatorScene);
        StartupVsCodeScene.IsVisible = ReferenceEquals(scene, StartupVsCodeScene);
        StartupTitleScene.IsVisible = ReferenceEquals(scene, StartupTitleScene);
        StartupCreatorScene.Opacity = ReferenceEquals(scene, StartupCreatorScene) ? 0 : 0;
        StartupVsCodeScene.Opacity = ReferenceEquals(scene, StartupVsCodeScene) ? 0 : 0;
        StartupTitleScene.Opacity = ReferenceEquals(scene, StartupTitleScene) ? 0 : 0;
        scene.Opacity = 0;
        await Task.Yield();
    }

    private static async Task TypeLabelAsync(Label label, string text, int durationMs, CancellationToken cancellationToken)
    {
        label.Text = string.Empty;
        if (string.IsNullOrEmpty(text))
            return;

        int stepDelay = Math.Max(24, durationMs / Math.Max(1, text.Length));
        for (int index = 1; index <= text.Length; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            label.Text = text[..index];
            await Task.Delay(stepDelay, cancellationToken);
        }
    }

    private static async Task WaitForRemainingSceneTimeAsync(Stopwatch stopwatch, int totalMilliseconds, CancellationToken cancellationToken)
    {
        int remaining = totalMilliseconds - (int)stopwatch.ElapsedMilliseconds;
        if (remaining > 0)
            await Task.Delay(remaining, cancellationToken);
    }

    private static Task DelayAsync(int milliseconds, CancellationToken cancellationToken)
    {
        return milliseconds <= 0
            ? Task.CompletedTask
            : Task.Delay(milliseconds, cancellationToken);
    }

    private void PlayStartupAudio(string fileName)
    {
#if WINDOWS
        try
        {
            string? path = AppAudio.ResolvePath(fileName);
            if (string.IsNullOrWhiteSpace(path))
                return;

            _startupAudioPlayer ??= new MediaPlayer
            {
                IsLoopingEnabled = false,
                AutoPlay = false,
                AudioCategory = MediaPlayerAudioCategory.GameMedia,
                Volume = 1
            };

            _startupAudioPlayer.Pause();
            _startupAudioPlayer.Source = MediaSource.CreateFromUri(new Uri(path));
            _startupAudioPlayer.Play();
        }
        catch
        {
        }
#else
        _ = fileName;
#endif
    }

    private void StopStartupAudio()
    {
#if WINDOWS
        try
        {
            _startupAudioPlayer?.Pause();
            if (_startupAudioPlayer?.PlaybackSession is not null)
                _startupAudioPlayer.PlaybackSession.Position = TimeSpan.Zero;
        }
        catch
        {
        }
#endif
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

    private void OnEnemyCellPointerEntered(object? sender, PointerEventArgs e)
    {
        if (_viewModel is null)
            return;

        if (sender is not BindableObject bindable || bindable.BindingContext is not BoardCellVm cell)
            return;

        _viewModel.UpdateEnemyHoverTarget(cell);
    }

    private void OnEnemyBoardPointerExited(object? sender, PointerEventArgs e)
    {
        _viewModel?.ClearEnemyHoverTarget();
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

    private async Task AnimateTurnTransitionAsync()
    {
        await MainThread.InvokeOnMainThreadAsync(async () =>
        {
            if (_viewModel?.IsTurnTransitionActive != true)
                return;

            if (_viewModel.ReduceMotionMode)
            {
                TurnTransitionScrim.Opacity = 1;
                TurnTransitionCard.Opacity = 1;
                TurnTransitionCard.Scale = 1;
                TurnTransitionReticle.Opacity = 1;
                TurnTransitionReticle.Scale = 1;
                TurnTransitionPulseRing.Opacity = 0.28;
                TurnTransitionPulseRing.Scale = 1;
                TurnTransitionCoordinateChip.Opacity = 1;
                TurnTransitionCoordinateChip.Scale = 1;
                TurnTransitionSweepHost.Rotation = 0;
                return;
            }

            uint intro = ScaleDuration(320, AnimationRuntimeSettings.SpeedMultiplier);
            uint sweep = ScaleDuration(900, AnimationRuntimeSettings.SpeedMultiplier);

            TurnTransitionScrim.Opacity = 0;
            TurnTransitionCard.Opacity = 0;
            TurnTransitionCard.Scale = 0.92;
            TurnTransitionReticle.Opacity = 0;
            TurnTransitionReticle.Scale = 2.1;
            TurnTransitionPulseRing.Opacity = 0.08;
            TurnTransitionPulseRing.Scale = 0.84;
            TurnTransitionCoordinateChip.Opacity = 0;
            TurnTransitionCoordinateChip.Scale = 0.74;
            TurnTransitionSweepHost.Rotation = 0;

            await Task.WhenAll(
                TurnTransitionScrim.FadeToAsync(1, intro, Easing.CubicOut),
                TurnTransitionCard.FadeToAsync(1, intro, Easing.CubicOut),
                TurnTransitionCard.ScaleToAsync(1, intro, Easing.CubicOut),
                TurnTransitionReticle.FadeToAsync(1, intro, Easing.CubicOut),
                TurnTransitionReticle.ScaleToAsync(1, intro, Easing.CubicOut),
                TurnTransitionPulseRing.FadeToAsync(0.3, intro, Easing.CubicOut),
                TurnTransitionPulseRing.ScaleToAsync(1.12, intro, Easing.SinOut),
                TurnTransitionCoordinateChip.FadeToAsync(1, intro, Easing.CubicOut),
                TurnTransitionCoordinateChip.ScaleToAsync(1, intro, Easing.SpringOut),
                TurnTransitionSweepHost.RotateToAsync(360, sweep, Easing.Linear));
        });
    }

    private async Task AnimateIntelBubbleAsync()
    {
        await MainThread.InvokeOnMainThreadAsync(async () =>
        {
            if (_viewModel?.IsIntelBubbleVisible != true)
                return;

            if (_viewModel.ReduceMotionMode)
            {
                IntelBubbleCard.Opacity = 1;
                IntelBubbleCard.Scale = 1;
                IntelBubbleCard.TranslationY = 0;
                return;
            }

            uint duration = ScaleDuration(220, AnimationRuntimeSettings.SpeedMultiplier);
            IntelBubbleCard.Opacity = 0;
            IntelBubbleCard.Scale = 0.9;
            IntelBubbleCard.TranslationY = -10;

            await Task.WhenAll(
                IntelBubbleCard.FadeToAsync(1, duration, Easing.CubicOut),
                IntelBubbleCard.ScaleToAsync(1, duration, Easing.CubicOut),
                IntelBubbleCard.TranslateToAsync(0, 0, duration, Easing.CubicOut));
        });
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

    private void RefreshBoardGridStructure()
    {
        double cellSize = _viewModel?.CellPixelSize ?? BoardViewModel.CellSize;
        EnsureBoardGridStructure(EnemyBoardCellsHost, cellSize);
        EnsureBoardGridStructure(PlayerBoardCellsHost, cellSize);
    }

    private static void EnsureBoardGridStructure(Grid host, double cellSize)
    {
        if (host.RowDefinitions.Count == BoardViewModel.Size && host.ColumnDefinitions.Count == BoardViewModel.Size)
        {
            for (int index = 0; index < BoardViewModel.Size; index++)
            {
                host.RowDefinitions[index].Height = cellSize;
                host.ColumnDefinitions[index].Width = cellSize;
            }

            return;
        }

        host.RowDefinitions.Clear();
        host.ColumnDefinitions.Clear();

        for (int index = 0; index < BoardViewModel.Size; index++)
        {
            host.RowDefinitions.Add(new RowDefinition { Height = cellSize });
            host.ColumnDefinitions.Add(new ColumnDefinition { Width = cellSize });
        }
    }

    private void OnToggleFullScreenClicked(object? sender, EventArgs e)
    {
#if WINDOWS
        BattleshipMaui.WinUI.FullScreenHotkeyController.ToggleCurrentWindow();
#endif
        _viewModel?.CloseCommandMenu();
    }

    private void OnQuitApplicationClicked(object? sender, EventArgs e)
    {
#if WINDOWS
        if (Application.Current?.Windows.FirstOrDefault()?.Handler?.PlatformView is WinUiWindow window)
        {
            window.Close();
            return;
        }
#endif

        Process.GetCurrentProcess().Kill();
    }
}
