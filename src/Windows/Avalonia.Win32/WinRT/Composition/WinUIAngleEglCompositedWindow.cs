using System;
using System.Diagnostics;
using Avalonia.MicroCom;
using Avalonia.OpenGL.Egl;

namespace Avalonia.Win32.WinRT.Composition
{
    public sealed class WinUIAngleEglCompositedWindow : WinUICompositedWindowBase
    {
        private EglContext _syncContext;

        internal WinUIAngleEglCompositedWindow(EglContext syncContext, object pumpLock, WinUIVisualTreeHolder holder)
            : base(holder, pumpLock)
        {
            _syncContext = syncContext;
        }

        public override void ResizeIfNeeded(PixelSize size)
        {
            using (_syncContext.EnsureLocked())
                base.ResizeIfNeeded(size);
        }

        public override IUnknown BeginDrawToTexture(out PixelPoint offset)
        {
            if (!_syncContext.IsCurrent)
                throw new InvalidOperationException();

            return base.BeginDrawToTexture(out offset);
        }

        public override void EndDraw()
        {
            if (!_syncContext.IsCurrent)
                throw new InvalidOperationException();

            base.EndDraw();
        }

        public override void SetBlur(bool enable)
        {
            using (_syncContext.EnsureLocked())
                base.SetBlur(enable);
        }

        protected override void DisposeCore()
        {
            Debug.Assert(_syncContext != null);
            _syncContext = null;
        }
    }
}
