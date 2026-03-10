using System.Runtime.CompilerServices;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using Windows.System;
using WinRT.Interop;
using WinUiWindow = Microsoft.UI.Xaml.Window;
using WinUiKeyboardAccelerator = Microsoft.UI.Xaml.Input.KeyboardAccelerator;

namespace BattleshipMaui.WinUI;

internal static class FullScreenHotkeyController
{
    private static readonly ConditionalWeakTable<WinUiWindow, WindowHotkeyBinding> Bindings = new();

    public static void Attach(WinUiWindow window)
    {
        _ = Bindings.GetValue(window, static createdWindow => new WindowHotkeyBinding(createdWindow));
    }

    private sealed class WindowHotkeyBinding
    {
        private readonly WinUiWindow _window;
        private readonly WinUiKeyboardAccelerator _fullScreenAccelerator;
        private bool _restoreMaximized;

        public WindowHotkeyBinding(WinUiWindow window)
        {
            _window = window;
            _fullScreenAccelerator = new WinUiKeyboardAccelerator
            {
                Key = VirtualKey.F11
            };

            _fullScreenAccelerator.Invoked += OnFullScreenInvoked;
            _window.Activated += OnWindowActivated;
            AttachToRoot();
        }

        private void OnWindowActivated(object sender, WindowActivatedEventArgs args)
        {
            AttachToRoot();
        }

        private void AttachToRoot()
        {
            if (_window.Content is not FrameworkElement root)
            {
                return;
            }

            if (!root.KeyboardAccelerators.Contains(_fullScreenAccelerator))
            {
                root.KeyboardAccelerators.Add(_fullScreenAccelerator);
            }
        }

        private void OnFullScreenInvoked(WinUiKeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
        {
            ToggleFullScreen();
            args.Handled = true;
        }

        private void ToggleFullScreen()
        {
            AppWindow? appWindow = TryGetAppWindow(_window);
            if (appWindow is null)
            {
                return;
            }

            if (appWindow.Presenter.Kind == AppWindowPresenterKind.FullScreen)
            {
                appWindow.SetPresenter(AppWindowPresenterKind.Overlapped);

                if (appWindow.Presenter is OverlappedPresenter overlappedPresenter)
                {
                    if (_restoreMaximized)
                    {
                        overlappedPresenter.Maximize();
                    }
                    else
                    {
                        overlappedPresenter.Restore();
                    }
                }

                return;
            }

            if (appWindow.Presenter is OverlappedPresenter overlapped)
            {
                _restoreMaximized = overlapped.State == OverlappedPresenterState.Maximized;
            }
            else
            {
                _restoreMaximized = false;
            }

            appWindow.SetPresenter(AppWindowPresenterKind.FullScreen);
        }

        private static AppWindow? TryGetAppWindow(WinUiWindow window)
        {
            nint hwnd = WindowNative.GetWindowHandle(window);
            if (hwnd == 0)
            {
                return null;
            }

            Microsoft.UI.WindowId windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hwnd);
            return AppWindow.GetFromWindowId(windowId);
        }
    }
}
