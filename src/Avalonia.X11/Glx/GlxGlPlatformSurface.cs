using System;
using Avalonia.OpenGL;
using Avalonia.OpenGL.Surfaces;

namespace Avalonia.X11.Glx
{
    internal sealed class GlxGlPlatformSurface: IGlPlatformSurface
    {
        private readonly GlxDisplay _display;
        private readonly GlxContext _context;
        private readonly IWindowGlPlatformSurfaceInfo _info;
        
        public GlxGlPlatformSurface(GlxDisplay display, GlxContext context, IWindowGlPlatformSurfaceInfo info)
        {
            _display = display;
            _context = context;
            _info = info;
        }
        
        public IGlPlatformSurfaceRenderTarget CreateGlRenderTarget()
        {
            return new RenderTarget(_context, _info);
        }

        private sealed class RenderTarget : IGlPlatformSurfaceRenderTarget
        {
            private readonly GlxContext _context;
            private readonly IWindowGlPlatformSurfaceInfo _info;

            public RenderTarget(GlxContext context,  IWindowGlPlatformSurfaceInfo info)
            {
                _context = context;
                _info = info;
            }

            public void Dispose()
            {
                // No-op
            }

            public IGlPlatformSurfaceRenderingSession BeginDraw()
            {
                var oldContext = _context.MakeCurrent(_info.Handle);
                return new Session(_context, _info, oldContext);
            }

            private sealed class Session : GlPlatformRenderingSessionBase<GlxContext>
            {
                public Session(GlxContext context, IWindowGlPlatformSurfaceInfo info,
                               IDisposable clearContext) : base(context, info, clearContext)
                {
                }

                protected override void DisposeCore()
                {
                    GlContext.Glx.WaitGL();
                    GlContext.Display.SwapBuffers(WindowSurfaceInfo.Handle);
                    GlContext.Glx.WaitX();
                }
            }
        }
    }
}
