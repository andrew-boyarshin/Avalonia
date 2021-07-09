using System;
using JetBrains.Annotations;

namespace Avalonia.Win32.WinRT.Composition
{
    public struct WinUIVisualTreeHolder : IDisposable
    {
        internal ICompositionTarget _target;
        internal ICompositionDrawingSurfaceInterop _surfaceInterop;
        internal IVisual _visual, _blur;

        internal WinUIVisualTreeHolder([NotNull] ICompositionTarget target,
                                       [NotNull] ICompositionDrawingSurfaceInterop surfaceInterop,
                                       [NotNull] IVisual visual, [NotNull] IVisual blur)
        {
            _target = target ?? throw new ArgumentNullException(nameof(target));
            _surfaceInterop = surfaceInterop ?? throw new ArgumentNullException(nameof(surfaceInterop));
            _visual = visual ?? throw new ArgumentNullException(nameof(visual));
            _blur = blur ?? throw new ArgumentNullException(nameof(blur));
        }

        public void Dispose()
        {
            _target?.Dispose();
            _surfaceInterop?.Dispose();
            _visual?.Dispose();
            _blur?.Dispose();
            _target = null;
            _surfaceInterop = null;
            _visual = _blur = null;
        }
    }
}
