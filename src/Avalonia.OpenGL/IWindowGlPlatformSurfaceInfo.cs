using System;

namespace Avalonia.OpenGL
{
    public interface IWindowGlPlatformSurfaceInfo
    {
        IntPtr Handle { get; }
        PixelSize Size { get; }
        double Scaling { get; }
    }
}
