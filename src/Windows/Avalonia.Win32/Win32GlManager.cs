using System;
using Avalonia.OpenGL;
using Avalonia.OpenGL.Angle;
using Avalonia.OpenGL.Egl;
using Avalonia.Win32.OpenGl;
using Avalonia.Win32.WinRT.Composition;

namespace Avalonia.Win32
{
    internal static class Win32GlManager
    {
        private static readonly Version Windows7 = new(6, 1);

        public static IPlatformOpenGlInterface CreatePlatformOpenGlInterface()
        {
            var opts = AvaloniaLocator.Current.GetService<Win32PlatformOptions>();

            if (opts?.UseWgl == true)
                return WglPlatformOpenGlInterface.TryCreate();

            if (opts?.AllowEglInitialization ?? Win32Platform.WindowsVersion > Windows7)
                return EglPlatformOpenGlInterface.TryCreate(() => new AngleWin32EglDisplay());

            return null;
        }

        public static WinUICompositorConnectionBase CreateCompositorConnection()
        {
            var platformGl = AvaloniaLocator.Current.GetService<IPlatformOpenGlInterface>();
            var opts = AvaloniaLocator.Current.GetService<Win32PlatformOptions>();

            if (!(opts?.UseWindowsUIComposition ?? true))
                return null;

            return platformGl switch
            {
                EglPlatformOpenGlInterface egl => WinUIAngleEglCompositorConnection.TryCreateAndRegister(egl),
                _ => null
            };
        }
    }
}
