using Avalonia.OpenGL.Surfaces;
using JetBrains.Annotations;

namespace Avalonia.OpenGL.Egl
{
    public class EglGlPlatformSurface : EglGlPlatformSurfaceBase
    {
        private readonly EglPlatformOpenGlInterface _egl;
        private readonly IWindowGlPlatformSurfaceInfo _info;
        
        public EglGlPlatformSurface(EglPlatformOpenGlInterface egl, IWindowGlPlatformSurfaceInfo info) : base()
        {
            _egl = egl;
            _info = info;
        }

        public override IGlPlatformSurfaceRenderTarget CreateGlRenderTarget()
        {
            var glSurface = _egl.CreateWindowSurface(_info.Handle);
            return new RenderTarget(_egl, glSurface, _info);
        }

        private sealed class RenderTarget : EglPlatformSurfaceRenderTargetBase
        {
            [CanBeNull] private EglSurface _glSurface;
            private PixelSize _currentSize;

            public RenderTarget([NotNull] EglPlatformOpenGlInterface egl,
                                [CanBeNull] EglSurface glSurface,
                                [NotNull] IWindowGlPlatformSurfaceInfo info) : base(egl, info)
            {
                _glSurface = glSurface;
                _currentSize = info.Size;
            }

            public override void Dispose() => _glSurface?.Dispose();

            public override IGlPlatformSurfaceRenderingSession BeginDraw()
            {
                if (_glSurface == null || WindowSurfaceInfo.Size != _currentSize)
                {
                    _glSurface?.Dispose();
                    _glSurface = null;
                    _glSurface = Egl.CreateWindowSurface(WindowSurfaceInfo.Handle);
                    _currentSize = WindowSurfaceInfo.Size;
                }
                return base.BeginDraw(_glSurface);
            }
        }
    }
}

