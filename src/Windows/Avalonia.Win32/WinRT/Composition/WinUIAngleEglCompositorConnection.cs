using System;
using Avalonia.MicroCom;
using Avalonia.OpenGL;
using Avalonia.OpenGL.Angle;
using Avalonia.OpenGL.Egl;
using Avalonia.OpenGL.Surfaces;
using Avalonia.Rendering;

namespace Avalonia.Win32.WinRT.Composition
{
    internal sealed class WinUIAngleEglCompositorConnection : WinUICompositorConnectionBase
    {
        private readonly EglContext _syncContext;

        public WinUIAngleEglCompositorConnection(EglPlatformOpenGlInterface gl, object pumpLock) : base(pumpLock)
        {
            Egl = gl;
            _syncContext = Egl.PrimaryEglContext;
            var angle = (AngleWin32EglDisplay)Egl.Display;
            using var device = MicroComRuntime.CreateProxyFor<IUnknown>(angle.GetDirect3DDevice(), true);

            CreateCompositionGraphicsDevice(device);
        }

        public EglPlatformOpenGlInterface Egl { get; }

        public static WinUICompositorConnectionBase TryCreateAndRegister(EglPlatformOpenGlInterface angle) =>
            angle is { Display: AngleWin32EglDisplay { PlatformApi: AngleOptions.PlatformApi.DirectX11 } } ?
                TryCreateAndRegister(pumpLock => new WinUIAngleEglCompositorConnection(angle, pumpLock)) :
                null;

        public override WinUICompositedWindowBase CreateWindow(IntPtr hWnd)
        {
            using var sc = _syncContext.EnsureLocked();

            var holder = CreateVisualTree(hWnd);

            return new WinUIAngleEglCompositedWindow(_syncContext, _pumpLock, holder);
        }

        public override IGlPlatformSurface CreateGlPlatformSurface(IWindowGlPlatformSurfaceInfo surfaceInfo) =>
            new WinUICompositedWindowSurface(this, surfaceInfo);

        public override IRenderer CreateDeferredRenderer(IRenderRoot root, IRenderLoop renderLoop, IDeferredRendererLock rendererLock) =>
            new DeferredRenderer(root, renderLoop) { RenderOnlyOnRenderThread = true };
    }
}
