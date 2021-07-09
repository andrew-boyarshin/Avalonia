using System;
using Avalonia.MicroCom;
using Avalonia.Win32.WinRT.Composition;
using JetBrains.Annotations;

namespace Avalonia.Direct2D1.Composition
{
    internal sealed class WinUIDirect2DCompositorConnection : WinUICompositorConnectionBase
    {
        public WinUIDirect2DCompositorConnection([NotNull] object pumpLock) : base(pumpLock)
        {
            CreateCompositionGraphicsDevice(MicroComRuntime.CreateProxyFor<IUnknown>(Direct2D1Platform.Direct2D1Device.NativePointer, true));
        }

        public static WinUICompositorConnectionBase TryCreateAndRegister() =>
            TryCreateAndRegister(pumpLock => new WinUIDirect2DCompositorConnection(pumpLock));

        public override WinUICompositedWindowBase CreateWindow(IntPtr hWnd)
        {
            var holder = CreateVisualTree(hWnd);

            return new WinUIDirect2DCompositedWindow(holder, _pumpLock);
        }
    }
}
