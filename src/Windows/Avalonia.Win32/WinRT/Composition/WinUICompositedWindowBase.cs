using System;
using System.Numerics;
using System.Reactive.Disposables;
using System.Threading;
using Avalonia.MicroCom;
using Avalonia.Win32.Interop;
using JetBrains.Annotations;

namespace Avalonia.Win32.WinRT.Composition
{
    public abstract class WinUICompositedWindowBase : IDisposable
    {
        private WinUIVisualTreeHolder _holder;
        private object _pumpLock;
        private PixelSize _size;

        private IVisual BlurVisual => _holder._blur;
        private IVisual ContentVisual => _holder._visual;
        private ICompositionDrawingSurfaceInterop SurfaceInterop => _holder._surfaceInterop;

        private static readonly Guid IID_ID3D11Texture2D = Guid.Parse("6f15aaf2-d208-4e89-9ab4-489535d34f9c");

        protected WinUICompositedWindowBase(WinUIVisualTreeHolder holder, [NotNull] object pumpLock)
        {
            _holder = holder;
            _pumpLock = pumpLock ?? throw new ArgumentNullException(nameof(pumpLock));
        }

        public virtual void ResizeIfNeeded(PixelSize size)
        {
            if (_size == size)
                return;

            SurfaceInterop.Resize(new UnmanagedMethods.POINT { X = size.Width, Y = size.Height });
            ContentVisual.SetSize(new Vector2(size.Width, size.Height));
            _size = size;
        }

        public virtual unsafe IUnknown BeginDrawToTexture(out PixelPoint offset)
        {
            var iid = IID_ID3D11Texture2D;
            void* pTexture;
            var off = SurfaceInterop.BeginDraw(null, &iid, &pTexture);
            offset = new PixelPoint(off.X, off.Y);
            return MicroComRuntime.CreateProxyFor<IUnknown>(pTexture, true);
        }

        public virtual void EndDraw()
        {
            SurfaceInterop.EndDraw();
        }

        public virtual void SetBlur(bool enable)
        {
            BlurVisual.SetIsVisible(enable ? 1 : 0);
        }

        public IDisposable BeginTransaction()
        {
            Monitor.Enter(_pumpLock);
            return Disposable.Create(() => Monitor.Exit(_pumpLock));
        }

        protected virtual void DisposeCore()
        {
        }

        public void Dispose()
        {
            if (_pumpLock == null)
                return;

            _holder.Dispose();
            _pumpLock = null;
            DisposeCore();
        }
    }
}
