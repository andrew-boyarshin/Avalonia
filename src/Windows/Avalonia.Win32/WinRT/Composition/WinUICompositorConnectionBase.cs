using System;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Logging;
using Avalonia.MicroCom;
using Avalonia.OpenGL;
using Avalonia.OpenGL.Surfaces;
using Avalonia.Rendering;
using Avalonia.Win32.Interop;
using JetBrains.Annotations;

namespace Avalonia.Win32.WinRT.Composition
{
    public abstract class WinUICompositorConnectionBase : IRenderTimer
    {
        private readonly ICompositor _compositor;
        private readonly ICompositor2 _compositor2;
        private readonly ICompositor5 _compositor5;
        private readonly ICompositorInterop _compositorInterop;
        private ICompositionGraphicsDevice _device;
        private readonly ICompositorDesktopInterop _compositorDesktopInterop;
        private readonly ICompositionBrush _blurBrush;
        protected readonly object _pumpLock;

        protected WinUICompositorConnectionBase([NotNull] object pumpLock)
        {
            _pumpLock = pumpLock ?? throw new ArgumentNullException(nameof(pumpLock));

            _compositor = NativeWinRTMethods.CreateInstance<ICompositor>("Windows.UI.Composition.Compositor");
            _compositor2 = _compositor.QueryInterface<ICompositor2>();
            _compositor5 = _compositor.QueryInterface<ICompositor5>();
            _compositorInterop = _compositor.QueryInterface<ICompositorInterop>();
            _compositorDesktopInterop = _compositor.QueryInterface<ICompositorDesktopInterop>();

            _blurBrush = CreateBlurBrush();
        }

        protected void CreateCompositionGraphicsDevice(IUnknown renderingDevice) =>
            _device = _compositorInterop.CreateGraphicsDevice(renderingDevice);

        private class RunLoopHandler : IAsyncActionCompletedHandler, IMicroComShadowContainer
        {
            private readonly WinUICompositorConnectionBase _parent;
            private readonly Stopwatch _st = Stopwatch.StartNew();

            public RunLoopHandler(WinUICompositorConnectionBase parent)
            {
                _parent = parent;
            }

            public void Dispose()
            {
            }

            public void Invoke(IAsyncAction asyncInfo, AsyncStatus asyncStatus)
            {
                _parent.Tick?.Invoke(_st.Elapsed);
                using var act = _parent._compositor5.RequestCommitAsync();
                act.SetCompleted(this);
            }

            public MicroComShadow Shadow { get; set; }

            public void OnReferencedFromNative()
            {
            }

            public void OnUnreferencedFromNative()
            {
            }
        }

        private void RunLoop()
        {
            using (var act = _compositor5.RequestCommitAsync())
                act.SetCompleted(new RunLoopHandler(this));

            while (true)
            {
                UnmanagedMethods.GetMessage(out var msg, IntPtr.Zero, 0, 0);
                lock (_pumpLock)
                    UnmanagedMethods.DispatchMessage(ref msg);
            }
        }

        public abstract WinUICompositedWindowBase CreateWindow(IntPtr hWnd);

        public virtual IGlPlatformSurface CreateGlPlatformSurface(IWindowGlPlatformSurfaceInfo surfaceInfo) => null;

        public virtual IRenderer CreateDeferredRenderer(IRenderRoot root, IRenderLoop renderLoop,
                                                        IDeferredRendererLock rendererLock) => null;

        private ICompositionBrush CreateBlurBrush()
        {
            Debug.Assert(_compositor != null, nameof(_compositor) + " != null");
            Debug.Assert(_compositor2 != null, nameof(_compositor2) + " != null");

            using var backDropParameterFactory = NativeWinRTMethods.CreateActivationFactory<ICompositionEffectSourceParameterFactory>(
                "Windows.UI.Composition.CompositionEffectSourceParameter");
            using var backdropString = new HStringInterop("backdrop");
            using var backDropParameter = backDropParameterFactory.Create(backdropString.Handle);
            using var backDropParameterAsSource = backDropParameter.QueryInterface<IGraphicsEffectSource>();
            var blurEffect = new WinUIGaussianBlurEffect(backDropParameterAsSource);
            using var blurEffectFactory = _compositor.CreateEffectFactory(blurEffect);
            using var backdrop = _compositor2.CreateBackdropBrush();
            using var backdropBrush = backdrop.QueryInterface<ICompositionBrush>();

            var saturateEffect = new SaturationEffect(blurEffect);
            using var satEffectFactory = _compositor.CreateEffectFactory(saturateEffect);
            using var sat = satEffectFactory.CreateBrush();
            sat.SetSourceParameter(backdropString.Handle, backdropBrush);
            return sat.QueryInterface<ICompositionBrush>();
        }

        private IVisual CreateBlurVisual()
        {
            using var spriteVisual = _compositor.CreateSpriteVisual();
            using var visual = spriteVisual.QueryInterface<IVisual>();
            using var visual2 = spriteVisual.QueryInterface<IVisual2>();

            spriteVisual.SetBrush(_blurBrush);
            visual.SetIsVisible(0);
            visual2.SetRelativeSizeAdjustment(new Vector2(1.0f, 1.0f));

            return visual.CloneReference();
        }

        public event Action<TimeSpan> Tick;

        public delegate WinUICompositorConnectionBase CreateCompositorConnectionDelegate(object pumpLock);

        private static WinUICompositorConnectionBase TryCreateAndRegisterCore([NotNull] CreateCompositorConnectionDelegate createDelegate)
        {
            if (createDelegate == null) throw new ArgumentNullException(nameof(createDelegate));

            var tcs = new TaskCompletionSource<WinUICompositorConnectionBase>();
            var pumpLock = new object();
            var th = new Thread(() =>
            {
                WinUICompositorConnectionBase connect;
                try
                {
                    NativeWinRTMethods.CreateDispatcherQueueController(new NativeWinRTMethods.DispatcherQueueOptions
                    {
                        apartmentType = NativeWinRTMethods.DISPATCHERQUEUE_THREAD_APARTMENTTYPE.DQTAT_COM_NONE,
                        dwSize = Marshal.SizeOf<NativeWinRTMethods.DispatcherQueueOptions>(),
                        threadType = NativeWinRTMethods.DISPATCHERQUEUE_THREAD_TYPE.DQTYPE_THREAD_CURRENT
                    });
                    connect = createDelegate(pumpLock);
                    tcs.SetResult(connect);
                }
                catch (Exception e)
                {
                    tcs.SetException(e);
                    return;
                }

                if (connect == null)
                    return;

                connect.RunLoop();
            })
            {
                IsBackground = true
            };

            th.SetApartmentState(ApartmentState.STA);
            th.Start();

            var connection = tcs.Task.Result;

            if (connection != null)
                th.Name = $"Avalonia-CompositorLoop-{connection.GetType().Name.Replace("CompositorConnection", string.Empty)}";

            return connection;
        }

        protected static WinUICompositorConnectionBase TryCreateAndRegister([NotNull] CreateCompositorConnectionDelegate createDelegate)
        {
            if (createDelegate == null) throw new ArgumentNullException(nameof(createDelegate));

            const int majorRequired = 10;
            const int buildRequired = 17134;

            var windowsVersion = Win32Platform.WindowsVersion;
            var majorInstalled = windowsVersion.Major;
            var buildInstalled = windowsVersion.Build;

            if (majorInstalled >= majorRequired && buildInstalled >= buildRequired)
            {
                try
                {
                    return TryCreateAndRegisterCore(createDelegate);
                }
                catch (Exception e)
                {
                    Logger.TryGet(LogEventLevel.Error, "WinUIComposition")
                         ?.Log(null, "Unable to initialize WinUI compositor: {0}", e);
                    return null;
                }
            }

            Logger.TryGet(LogEventLevel.Warning, "WinUIComposition")
                 ?.Log(
                       null,
                       $"Unable to initialize WinUI compositor: Windows {majorRequired} Build {buildRequired} is required. Your machine has Windows {majorInstalled} Build {buildInstalled} installed."
                   );

            return null;
        }

        protected WinUIVisualTreeHolder CreateVisualTree(IntPtr hWnd)
        {
            using var desktopTarget = _compositorDesktopInterop.CreateDesktopWindowTarget(hWnd, 0);
            var target = desktopTarget.QueryInterface<ICompositionTarget>();

            var blur = CreateBlurVisual();

            using var drawingSurface = _device.CreateDrawingSurface(new UnmanagedMethods.SIZE(),
                                                                    DirectXPixelFormat.B8G8R8A8UIntNormalized,
                                                                    DirectXAlphaMode.Premultiplied);
            using var surface = drawingSurface.QueryInterface<ICompositionSurface>();
            var surfaceInterop = drawingSurface.QueryInterface<ICompositionDrawingSurfaceInterop>();

            using var spriteVisual = _compositor.CreateSpriteVisual();
            var visual = spriteVisual.QueryInterface<IVisual>();

            using var surfaceBrush = _compositor.CreateSurfaceBrushWithSurface(surface);
            using var brush = surfaceBrush.QueryInterface<ICompositionBrush>();
            using var container = _compositor.CreateContainerVisual();
            using var containerVisual = container.QueryInterface<IVisual>();

            using var containerVisual2 = container.QueryInterface<IVisual2>();
            containerVisual2.SetRelativeSizeAdjustment(new Vector2(1, 1));

            spriteVisual.SetBrush(brush);
            target.SetRoot(containerVisual);

            using var containerChildren = container.Children;
            containerChildren.InsertAtTop(blur);
            containerChildren.InsertAtTop(visual);

            return new WinUIVisualTreeHolder(target, surfaceInterop, visual, blur);
        }
    }
}
