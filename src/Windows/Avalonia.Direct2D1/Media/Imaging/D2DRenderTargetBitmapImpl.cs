using System;
using System.Diagnostics;
using System.IO;
using Avalonia.Platform;
using Avalonia.Rendering;
using Avalonia.Utilities;
using Vortice.Direct2D1;
using D2DBitmap = Vortice.Direct2D1.ID2D1Bitmap;

namespace Avalonia.Direct2D1.Media.Imaging
{
    public class D2DRenderTargetBitmapImpl : D2DBitmapImpl, IDrawingContextLayerImpl, ILayerFactory
    {
        private readonly ID2D1BitmapRenderTarget _renderTarget;

        public D2DRenderTargetBitmapImpl(ID2D1BitmapRenderTarget renderTarget)
            : base(renderTarget.Bitmap)
        {
            // Debug.WriteLine($"D2DRenderTargetBitmapImpl::new(0x{renderTarget.NativePointer.ToInt64():X}) Bitmap:0x{renderTarget.Bitmap.NativePointer.ToInt64():X}");
            _renderTarget = renderTarget;
        }

        public D2DRenderTargetBitmapImpl(RenderTarget renderTarget) : this(renderTarget.Impl)
        {
        }

        public static D2DRenderTargetBitmapImpl CreateCompatible(
            ID2D1RenderTarget renderTarget,
            Size size)
        {
            return new D2DRenderTargetBitmapImpl(
                renderTarget.CreateCompatibleRenderTarget(
                    new System.Drawing.SizeF((float)size.Width, (float)size.Height),
                    CompatibleRenderTargetOptions.None
                )
            );
        }

        public IDrawingContextImpl CreateDrawingContext(IVisualBrushRenderer visualBrushRenderer)
        {
            return new DrawingContextImpl(visualBrushRenderer, this, _renderTarget, null, () => Version++);
        }

        public void Blit(IDrawingContextImpl context) => throw new NotSupportedException();

        public bool CanBlit => false;

        public IDrawingContextLayerImpl CreateLayer(Size size)
        {
            return CreateCompatible(_renderTarget, size);
        }

        public override void Dispose()
        {
            // Intentionally ignore base.Dispose (ID2D1BitmapRenderTarget.ReleaseBitmap)
            // Debug.WriteLine($"D2DRenderTargetBitmapImpl::Dispose(0x{_renderTarget.NativePointer.ToInt64():X}) Stack: {new StackTrace()}");
            _renderTarget.Dispose();
        }

        public override void Save(Stream stream)
        {
            using (var wic = new WicRenderTargetBitmapImpl(PixelSize, Dpi))
            {
                using (var dc = wic.CreateDrawingContext(null))
                {
                    dc.DrawBitmap(
                        RefCountable.CreateUnownedNotClonable(this),
                        1,
                        new Rect(PixelSize.ToSizeWithDpi(Dpi.X)),
                        new Rect(PixelSize.ToSizeWithDpi(Dpi.X)));
                }

                wic.Save(stream);
            }
        }
    }
}
