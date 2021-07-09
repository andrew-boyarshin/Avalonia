using System;
using Avalonia.OpenGL.Surfaces;
using JetBrains.Annotations;

namespace Avalonia.OpenGL
{
    public abstract class GlPlatformRenderingSessionBase<TContext> : IGlPlatformSurfaceRenderingSession
        where TContext : IGlContext
    {
        [NotNull] protected readonly TContext GlContext;
        [NotNull] protected readonly IWindowGlPlatformSurfaceInfo WindowSurfaceInfo;
        [NotNull] private readonly IDisposable _restoreContext;
        [CanBeNull] private readonly Action _onFinish;

        protected GlPlatformRenderingSessionBase([NotNull] TContext glContext,
                                                 [NotNull] IWindowGlPlatformSurfaceInfo info,
                                                 [NotNull] IDisposable restoreContext, Action onFinish = null,
                                                 bool isYFlipped = false)
        {
            GlContext = glContext ?? throw new ArgumentNullException(nameof(glContext));
            WindowSurfaceInfo = info ?? throw new ArgumentNullException(nameof(info));
            _restoreContext = restoreContext ?? throw new ArgumentNullException(nameof(restoreContext));
            _onFinish = onFinish;
            IsYFlipped = isYFlipped;

            // Reset to default FBO first
            GlContext.GlInterface.BindFramebuffer(GlConsts.GL_FRAMEBUFFER, 0);
        }

        public IGlContext Context => GlContext;

        public PixelSize Size => WindowSurfaceInfo.Size;
        public double Scaling => WindowSurfaceInfo.Scaling;
        public bool IsYFlipped { get; }

        protected abstract void DisposeCore();

        public void Dispose()
        {
            GlContext.GlInterface.Flush();
            DisposeCore();
            _restoreContext.Dispose();
            _onFinish?.Invoke();
        }
    }
}
