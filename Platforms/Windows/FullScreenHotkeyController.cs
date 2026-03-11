using System.Runtime.CompilerServices;
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
        private bool _startupFullScreenApplied;

        public WindowHotkeyBinding(WinUiWindow window)
        {
            _window = window;
            _window.Activated += OnWindowActivated;
            AttachToRoot();
            EnsureStartupFullScreen();
        }

        private void AttachToRoot()
        {
            if (ReferenceEquals(_attachedRoot, _window.Content))
                return;

            if (_attachedRoot is not null)
                _attachedRoot.KeyDown -= OnRootKeyDown;

            _attachedRoot = _window.Content as FrameworkElement;
            if (_attachedRoot is not null)
                _attachedRoot.KeyDown += OnRootKeyDown;
        }

        private void OnWindowActivated(object sender, WindowActivatedEventArgs args)
        {
            AttachToRoot();
            EnsureStartupFullScreen();
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
            if (_startupFullScreenApplied)
                return;

            _startupFullScreenApplied = true;
            SetFullScreen(enabled: true);
        }

        public void ToggleFullScreen()
        {
            AppWindow? appWindow = TryGetAppWindow(_window);
            if (appWindow is null)
                return;

            bool enableFullScreen = appWindow.Presenter.Kind != AppWindowPresenterKind.FullScreen;
            SetFullScreen(enableFullScreen);
        }

        private void SetFullScreen(bool enabled)
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

        private static AppWindow? TryGetAppWindow(WinUiWindow window)
        {
            nint hwnd = WindowNative.GetWindowHandle(window);
            if (hwnd == 0)
                return null;

            Microsoft.UI.WindowId windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hwnd);
            return AppWindow.GetFromWindowId(windowId);
        }
    }
}
