using System;
using Avalonia.OpenGL.Surfaces;
using JetBrains.Annotations;

namespace Avalonia.OpenGL.Egl
{
    public abstract class EglGlPlatformSurfaceBase : IGlPlatformSurface
    {
        public abstract IGlPlatformSurfaceRenderTarget CreateGlRenderTarget();
    }

    public abstract class EglPlatformSurfaceRenderTargetBase : IGlPlatformSurfaceRenderTarget
    {
        [NotNull] protected EglPlatformOpenGlInterface Egl { get; }
        [NotNull] protected IWindowGlPlatformSurfaceInfo WindowSurfaceInfo { get; }

        protected EglPlatformSurfaceRenderTargetBase([NotNull] EglPlatformOpenGlInterface egl,
                                                     [NotNull] IWindowGlPlatformSurfaceInfo windowSurfaceInfo)
        {
            Egl = egl ?? throw new ArgumentNullException(nameof(egl));
            WindowSurfaceInfo = windowSurfaceInfo ?? throw new ArgumentNullException(nameof(windowSurfaceInfo));
        }

        public virtual void Dispose()
        {
        }

        public abstract IGlPlatformSurfaceRenderingSession BeginDraw();

        protected IGlPlatformSurfaceRenderingSession BeginDraw(EglSurface surface, Action onFinish = null, bool isYFlipped = false)
        {
            var restoreContext = Egl.PrimaryEglContext.MakeCurrent(surface);
            var success = false;
            try
            {
                var egli = Egl.Display.EglInterface;
                egli.WaitClient();
                egli.WaitGL();
                egli.WaitNative(EglConsts.EGL_CORE_NATIVE_ENGINE);

                success = true;
                return new Session(Egl.PrimaryEglContext, surface, WindowSurfaceInfo, restoreContext, onFinish, isYFlipped);
            }
            finally
            {
                if(!success)
                    restoreContext.Dispose();
            }
        }

        private sealed class Session : GlPlatformRenderingSessionBase<EglContext>
        {
            private readonly EglSurface _glSurface;

            public Session([NotNull] EglContext context, [NotNull] EglSurface glSurface,
                           [NotNull] IWindowGlPlatformSurfaceInfo info,
                           [NotNull] IDisposable restoreContext, [CanBeNull] Action onFinish,
                           bool isYFlipped) : base(context, info, restoreContext, onFinish, isYFlipped)
            {
                _glSurface = glSurface;
            }

            protected override void DisposeCore()
            {
                var display = GlContext.Display;
                display.EglInterface.WaitGL();
                _glSurface.SwapBuffers();
                display.EglInterface.WaitClient();
                display.EglInterface.WaitGL();
                display.EglInterface.WaitNative(EglConsts.EGL_CORE_NATIVE_ENGINE);
            }
        }
    }
}
