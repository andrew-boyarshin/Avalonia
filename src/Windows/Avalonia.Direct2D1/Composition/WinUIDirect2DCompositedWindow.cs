using Avalonia.Win32.WinRT.Composition;
using JetBrains.Annotations;

namespace Avalonia.Direct2D1.Composition
{
    internal sealed class WinUIDirect2DCompositedWindow : WinUICompositedWindowBase
    {
        public WinUIDirect2DCompositedWindow(WinUIVisualTreeHolder holder, [NotNull] object pumpLock) : base(holder, pumpLock)
        {
        }
    }
}
