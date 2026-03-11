using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Microsoft.Maui.Controls;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using Windows.System;
using WinRT.Interop;
using WinUiWindow = Microsoft.UI.Xaml.Window;

namespace BattleshipMaui.WinUI;

internal static class FullScreenHotkeyController
{
    private static readonly ConditionalWeakTable<WinUiWindow, WindowHotkeyBinding> Bindings = new();

    public static void Attach(WinUiWindow window)
    {
        _ = Bindings.GetValue(window, static createdWindow => new WindowHotkeyBinding(createdWindow));
    }

    public static void ToggleCurrentWindow()
    {
        if (Microsoft.Maui.Controls.Application.Current?.Windows.FirstOrDefault()?.Handler?.PlatformView is not WinUiWindow window)
            return;

        WindowHotkeyBinding binding = Bindings.TryGetValue(window, out var existingBinding)
            ? existingBinding
            : Bindings.GetValue(window, static createdWindow => new WindowHotkeyBinding(createdWindow));

        binding.ToggleFullScreen();
    }

    private sealed class WindowHotkeyBinding
    {
        private readonly WinUiWindow _window;
        private FrameworkElement? _attachedRoot;
        private bool _restoreMaximized;
        private bool _isClosed;
        private bool _startupFullScreenApplied;
        private bool _startupFullScreenQueued;

        public WindowHotkeyBinding(WinUiWindow window)
        {
            _window = window;
            _window.Activated += OnWindowActivated;
            _window.Closed += OnWindowClosed;
            QueueStartupFullScreen();
        }

        private void OnWindowActivated(object sender, WindowActivatedEventArgs args)
        {
            if (_isClosed)
                return;

            AttachToRoot();
            QueueStartupFullScreen();
        }

        private void OnWindowClosed(object sender, WindowEventArgs args)
        {
            _isClosed = true;
            _window.Activated -= OnWindowActivated;
            _window.Closed -= OnWindowClosed;

            if (_attachedRoot is not null)
                _attachedRoot.KeyDown -= OnRootKeyDown;

            _attachedRoot = null;
        }

        private void AttachToRoot()
        {
            if (_isClosed)
                return;

            FrameworkElement? currentRoot;
            try
            {
                currentRoot = _window.Content as FrameworkElement;
            }
            catch (COMException ex)
            {
                CrashLog.Write("FullScreenHotkeyController.AttachToRoot", ex);
                return;
            }

            if (ReferenceEquals(_attachedRoot, currentRoot))
                return;

            if (_attachedRoot is not null)
                _attachedRoot.KeyDown -= OnRootKeyDown;

            _attachedRoot = currentRoot;
            if (_attachedRoot is not null)
                _attachedRoot.KeyDown += OnRootKeyDown;
        }

        private void OnRootKeyDown(object sender, KeyRoutedEventArgs args)
        {
            if (args.Handled)
                return;

            switch (args.Key)
            {
                case VirtualKey.F11:
                    ToggleFullScreen();
                    args.Handled = true;
                    break;
                case VirtualKey.Escape:
                    args.Handled = TryRouteEscapeCommand();
                    break;
            }
        }

        private bool TryRouteEscapeCommand()
        {
            try
            {
                if (Shell.Current?.CurrentPage is MainPage page)
                {
                    page.HandleEscapeKey();
                    return true;
                }
            }
            catch
            {
            }

            return false;
        }

        private void EnsureStartupFullScreen()
        {
            if (_isClosed || _startupFullScreenApplied)
                return;

            if (TryGetAppWindow(_window) is null)
            {
                QueueStartupFullScreen();
                return;
            }

            _startupFullScreenApplied = true;
            SetFullScreen(enabled: true);
        }

        public void ToggleFullScreen()
        {
            if (_isClosed)
                return;

            AppWindow? appWindow = TryGetAppWindow(_window);
            if (appWindow is null)
                return;

            bool enableFullScreen = appWindow.Presenter.Kind != AppWindowPresenterKind.FullScreen;
            SetFullScreen(enableFullScreen);
        }

        private void SetFullScreen(bool enabled)
        {
            try
            {
                AppWindow? appWindow = TryGetAppWindow(_window);
                if (appWindow is null)
                    return;

                if (enabled)
                {
                    if (appWindow.Presenter.Kind == AppWindowPresenterKind.FullScreen)
                        return;

                    if (appWindow.Presenter is OverlappedPresenter overlapped)
                        _restoreMaximized = overlapped.State == OverlappedPresenterState.Maximized;
                    else
                        _restoreMaximized = false;

                    appWindow.SetPresenter(AppWindowPresenterKind.FullScreen);
                    return;
                }

                if (appWindow.Presenter.Kind != AppWindowPresenterKind.FullScreen)
                    return;

                appWindow.SetPresenter(AppWindowPresenterKind.Overlapped);
                if (appWindow.Presenter is not OverlappedPresenter restoredPresenter)
                    return;

                if (_restoreMaximized)
                    restoredPresenter.Maximize();
                else
                    restoredPresenter.Restore();
            }
            catch (COMException ex)
            {
                CrashLog.Write("FullScreenHotkeyController.SetFullScreen", ex, $"Enabled={enabled}");
                if (enabled)
                    _startupFullScreenApplied = false;
            }
        }

        private static AppWindow? TryGetAppWindow(WinUiWindow window)
        {
            try
            {
                nint hwnd = WindowNative.GetWindowHandle(window);
                if (hwnd == 0)
                    return null;

                Microsoft.UI.WindowId windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hwnd);
                return AppWindow.GetFromWindowId(windowId);
            }
            catch (COMException ex)
            {
                CrashLog.Write("FullScreenHotkeyController.TryGetAppWindow", ex);
                return null;
            }
        }

        private void QueueStartupFullScreen()
        {
            if (_isClosed || _startupFullScreenApplied || _startupFullScreenQueued)
                return;

            _startupFullScreenQueued = true;

            _ = Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(180).ConfigureAwait(false);
                }
                catch
                {
                    _startupFullScreenQueued = false;
                    return;
                }

                try
                {
                    if (_window.DispatcherQueue is null)
                    {
                        _startupFullScreenQueued = false;
                        return;
                    }

                    _window.DispatcherQueue.TryEnqueue(() =>
                    {
                        _startupFullScreenQueued = false;
                        if (_isClosed)
                            return;

                        AttachToRoot();
                        EnsureStartupFullScreen();
                    });
                }
                catch (Exception ex)
                {
                    _startupFullScreenQueued = false;
                    CrashLog.Write("FullScreenHotkeyController.QueueStartupFullScreen", ex);
                }
            });
        }
    }
}
