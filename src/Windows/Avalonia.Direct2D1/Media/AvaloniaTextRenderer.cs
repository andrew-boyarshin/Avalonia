using System;
using System.Drawing;
using System.Numerics;
using SharpGen.Runtime;
using Vortice.DCommon;
using Vortice.Direct2D1;
using Vortice.DirectWrite;

namespace Avalonia.Direct2D1.Media
{
    internal class AvaloniaTextRenderer : TextRendererBase
    {
        private readonly DrawingContextImpl _context;

        private readonly ID2D1RenderTarget _renderTarget;

        private readonly ID2D1Brush _foreground;

        public AvaloniaTextRenderer(
            DrawingContextImpl context,
            ID2D1RenderTarget target,
            ID2D1Brush foreground)
        {
            _context = context;
            _renderTarget = target;
            _foreground = foreground;
        }

        public override void DrawGlyphRun(
            IntPtr clientDrawingContext,
            float baselineOriginX,
            float baselineOriginY,
            MeasuringMode measuringMode,
            GlyphRun glyphRun,
            GlyphRunDescription glyphRunDescription,
            IUnknown clientDrawingEffect)
        {
            var wrapper = clientDrawingEffect as BrushWrapper;

            // TODO: Work out how to get the rect below rather than passing default.
            var brush = (wrapper == null) ?
                _foreground :
                _context.CreateBrush(wrapper.Brush, default).PlatformBrush;

            _renderTarget.DrawGlyphRun(
                new PointF { X = baselineOriginX, Y = baselineOriginY },
                glyphRun,
                brush,
                measuringMode);

            if (wrapper != null)
            {
                brush.Dispose();
            }
        }

        public override Matrix3x2 GetCurrentTransform(IntPtr clientDrawingContext)
        {
            return _renderTarget.Transform;
        }

        public override  float GetPixelsPerDip(IntPtr clientDrawingContext)
        {
            return _renderTarget.Dpi.Width / 96;
        }
    }
}
