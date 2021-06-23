using System.Drawing;
using Avalonia.Media;
using Avalonia.Rendering.Utilities;
using Avalonia.Utilities;
using Vortice.Direct2D1;

namespace Avalonia.Direct2D1.Media
{
    public sealed class ImageBrushImpl : BrushImpl
    {
        private readonly Visuals.Media.Imaging.BitmapInterpolationMode _bitmapInterpolationMode;

        public ImageBrushImpl(
            ITileBrush brush,
            ID2D1RenderTarget target,
            BitmapImpl bitmap,
            Size targetSize)
        {
            var dpi = new Vector(target.Dpi.Width, target.Dpi.Height);
            var calc = new TileBrushCalculator(brush, bitmap.PixelSize.ToSizeWithDpi(dpi), targetSize);

            if (!calc.NeedsIntermediate)
            {
                using var _bitmap = bitmap.GetDirect2DBitmap(target);
                PlatformBrush = target.CreateBitmapBrush(
                    _bitmap.Value,
                    GetBitmapBrushProperties(brush),
                    GetBrushProperties(brush, calc.DestinationRect));
            }
            else
            {
                using RenderTarget result = new(target, calc.IntermediateSize);

                RenderIntermediate(result, bitmap, calc);
                PlatformBrush = target.CreateBitmapBrush(
                    result.Bitmap,
                    GetBitmapBrushProperties(brush),
                    GetBrushProperties(brush, calc.DestinationRect));
            }

            _bitmapInterpolationMode = brush.BitmapInterpolationMode;
        }

        private static BitmapBrushProperties GetBitmapBrushProperties(ITileBrush brush)
        {
            var tileMode = brush.TileMode;

            return new BitmapBrushProperties
            {
                ExtendModeX = GetExtendModeX(tileMode),
                ExtendModeY = GetExtendModeY(tileMode),
            };
        }

        private static BrushProperties GetBrushProperties(ITileBrush brush, Rect destinationRect)
        {
            var tileTransform =
                brush.TileMode != TileMode.None ?
                Matrix.CreateTranslation(destinationRect.X, destinationRect.Y) :
                Matrix.Identity;

            return new BrushProperties
            {
                Opacity = (float)brush.Opacity,
                Transform = tileTransform.ToDirect2D(),
            };
        }

        private static ExtendMode GetExtendModeX(TileMode tileMode)
        {
            return (tileMode & TileMode.FlipX) != 0 ? ExtendMode.Mirror : ExtendMode.Wrap;
        }

        private static ExtendMode GetExtendModeY(TileMode tileMode)
        {
            return (tileMode & TileMode.FlipY) != 0 ? ExtendMode.Mirror : ExtendMode.Wrap;
        }

        private void RenderIntermediate(RenderTarget target, BitmapImpl bitmap, TileBrushCalculator calc)
        {
            using var context = target.CreateDrawingContext(null);
            var dpi = new Vector(target.Dpi.Width, target.Dpi.Height);
            var rect = new Rect(bitmap.PixelSize.ToSizeWithDpi(dpi));

            context.Clear(Colors.Transparent);
            context.PushClip(calc.IntermediateClip);
            context.Transform = calc.IntermediateTransform;

            context.DrawBitmap(RefCountable.CreateUnownedNotClonable(bitmap), 1, rect, rect, _bitmapInterpolationMode);
            context.PopClip();
        }
    }
}
