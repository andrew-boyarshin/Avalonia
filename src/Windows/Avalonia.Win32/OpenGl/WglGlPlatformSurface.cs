using System;
using Avalonia.OpenGL;
using Avalonia.OpenGL.Surfaces;
using Avalonia.Win32.Interop;

namespace Avalonia.Win32.OpenGl
{
    internal sealed class WglGlPlatformSurface : IGlPlatformSurface
    {
        private readonly WglContext _context;
        private readonly IWindowGlPlatformSurfaceInfo _info;

        public WglGlPlatformSurface(WglContext context, IWindowGlPlatformSurfaceInfo info)
        {
            _context = context;
            _info = info;
        }

        public IGlPlatformSurfaceRenderTarget CreateGlRenderTarget()
        {
            return new RenderTarget(_context, _info);
        }

        private sealed class RenderTarget : IGlPlatformSurfaceRenderTarget
        {
            private readonly WglContext _context;
            private readonly IWindowGlPlatformSurfaceInfo _info;
            private IntPtr _hdc;
            public RenderTarget(WglContext context,  IWindowGlPlatformSurfaceInfo info)
            {
                _context = context;
                _info = info;
                _hdc = context.CreateConfiguredDeviceContext(info.Handle);
            }

            public void Dispose()
            {
                UnmanagedMethods.ReleaseDC(_hdc, _info.Handle);
            }

            public IGlPlatformSurfaceRenderingSession BeginDraw()
            {
                var oldContext = _context.MakeCurrent(_hdc);
                return new Session(_context, _hdc, _info, oldContext);
            }

            private sealed class Session : GlPlatformRenderingSessionBase<WglContext>
            {
                private readonly IntPtr _hdc;

                public Session(WglContext context, IntPtr hdc, IWindowGlPlatformSurfaceInfo info,
                               IDisposable clearContext) : base(context, info, clearContext)
                {
                    _hdc = hdc;
                }

                protected override void DisposeCore()
                {
                    UnmanagedMethods.SwapBuffers(_hdc);
                }
            }
        }
    }
}
