using System;
using Avalonia.MicroCom;
using Avalonia.OpenGL;
using Avalonia.OpenGL.Angle;
using Avalonia.OpenGL.Egl;
using Avalonia.OpenGL.Surfaces;
using Avalonia.Utilities;
using JetBrains.Annotations;

namespace Avalonia.Win32.WinRT.Composition
{
    internal class WinUICompositedWindowSurface : CompositedWindowSurfaceBase
    {
        private readonly EglPlatformOpenGlInterface _egl;

        public WinUICompositedWindowSurface(WinUIAngleEglCompositorConnection connection, IWindowGlPlatformSurfaceInfo info) : base(connection, info)
        {
            _egl = connection.Egl;
        }

        public override IGlPlatformSurfaceRenderTarget CreateGlRenderTarget()
        {
            using (_egl.PrimaryContext.EnsureCurrent())
                return new CompositionRenderTarget(_egl, Window, WindowSurfaceInfo);
        }

        private sealed class CompositionRenderTarget : EglPlatformSurfaceRenderTargetBase
        {
            [NotNull] private readonly IRef<WinUICompositedWindowBase> _window;

            public CompositionRenderTarget([NotNull] EglPlatformOpenGlInterface egl,
                                           [NotNull] IRef<WinUICompositedWindowBase> window,
                                           [NotNull] IWindowGlPlatformSurfaceInfo info) : base(egl, info)
            {
                _window = window.Clone();
                _window.Item.ResizeIfNeeded(WindowSurfaceInfo.Size);
            }

            public override IGlPlatformSurfaceRenderingSession BeginDraw()
            {
                var contextLock = Egl.PrimaryEglContext.EnsureCurrent();
                IUnknown texture = null;
                EglSurface surface = null;
                IDisposable transaction = null;
                var success = false;
                try
                {
                    if (_window.Item is not { } window)
                        throw new ObjectDisposedException(GetType().FullName);

                    var size = WindowSurfaceInfo.Size;
                    transaction = window.BeginTransaction();
                    window.ResizeIfNeeded(size);
                    texture = window.BeginDrawToTexture(out var offset);

                    surface = ((AngleWin32EglDisplay) Egl.Display).WrapDirect3D11Texture(Egl,
                        texture.GetNativeIntPtr(),
                        offset.X, offset.Y, size.Width, size.Height);

                    var res = base.BeginDraw(surface, () =>
                    {
                        surface?.Dispose();
                        texture?.Dispose();
                        window.EndDraw();
                        transaction?.Dispose();
                        contextLock?.Dispose();
                    }, true);
                    success = true;
                    return res;
                }
                finally
                {
                    if (!success)
                    {
                        surface?.Dispose();
                        texture?.Dispose();
                        transaction?.Dispose();
                        contextLock.Dispose();
                    }
                }
            }
        }

        public override void Dispose()
        {
            using (_egl.PrimaryEglContext.EnsureLocked())
                base.Dispose();
        }
    }
}
