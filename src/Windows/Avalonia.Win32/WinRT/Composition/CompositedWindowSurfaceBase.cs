using System;
using System.Diagnostics;
using Avalonia.OpenGL;
using Avalonia.OpenGL.Surfaces;
using Avalonia.Utilities;
using JetBrains.Annotations;

namespace Avalonia.Win32.WinRT.Composition
{
    internal abstract class CompositedWindowSurfaceBase : IGlPlatformSurface, IBlurHost, IDisposable
    {
        [NotNull] private readonly WinUICompositorConnectionBase _connection;
        [CanBeNull] private IRef<WinUICompositedWindowBase> _window;
        [NotNull] protected IWindowGlPlatformSurfaceInfo WindowSurfaceInfo { get; }
        private bool _enableBlur;

        protected CompositedWindowSurfaceBase([NotNull] WinUICompositorConnectionBase connection,
                                              [NotNull] IWindowGlPlatformSurfaceInfo info)
        {
            _connection = connection ?? throw new ArgumentNullException(nameof(connection));
            WindowSurfaceInfo = info ?? throw new ArgumentNullException(nameof(info));
        }

        private void CreateWindowIfNeeded()
        {
            if (_window?.Item == null)
            {
                var windowItem = _connection.CreateWindow(WindowSurfaceInfo.Handle);
                windowItem.SetBlur(_enableBlur);
                _window = RefCountable.Create(windowItem);
            }
        }

        [NotNull]
        protected IRef<WinUICompositedWindowBase> Window
        {
            get
            {
                CreateWindowIfNeeded();
                Debug.Assert(_window != null, nameof(_window) + " != null");
                Debug.Assert(_window.Item != null, nameof(_window) + "." + nameof(_window.Item) + " != null");
                return _window;
            }
        }

        public void SetBlur(bool enable)
        {
            _enableBlur = enable;
            _window?.Item?.SetBlur(enable);
        }

        public virtual void Dispose()
        {
            _window?.Dispose();
            _window = null;
        }

        public abstract IGlPlatformSurfaceRenderTarget CreateGlRenderTarget();
    }
}
