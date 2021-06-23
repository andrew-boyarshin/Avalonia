using System.Diagnostics;
using System.Drawing;
using Avalonia.Direct2D1.Media;
using Avalonia.Direct2D1.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Rendering;
using Vortice.Direct2D1;

namespace Avalonia.Direct2D1
{
    public sealed class RenderTarget : IRenderTarget, ILayerFactory
    {
        public RenderTarget(ID2D1RenderTarget renderTarget, Size size)
        {
            Impl = renderTarget.CreateCompatibleRenderTarget(
                size.ToVortice(),
                CompatibleRenderTargetOptions.None
            );
        }

        public SizeF Dpi => Impl.Dpi;
        public ID2D1Bitmap Bitmap => Impl.Bitmap;

        public void Clear() => Impl.Clear(null);

        /// <summary>
        /// The render target.
        /// </summary>
        internal ID2D1BitmapRenderTarget Impl { get; private set; }

        /// <summary>
        /// Creates a drawing context for a rendering session.
        /// </summary>
        /// <returns>An <see cref="IDrawingContextImpl"/>.</returns>
        public IDrawingContextImpl CreateDrawingContext(IVisualBrushRenderer visualBrushRenderer)
        {
            Debug.Assert(Impl is not null);
            return new DrawingContextImpl(visualBrushRenderer, this, Impl);
        }

        public IDrawingContextLayerImpl CreateLayer(Size size)
        {
            Debug.Assert(Impl is not null);
            return D2DRenderTargetBitmapImpl.CreateCompatible(Impl, size);
        }

        public void Dispose()
        {
            Impl.Dispose();
            Impl = null;
        }
    }
}
