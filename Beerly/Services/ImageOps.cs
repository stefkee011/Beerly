using SkiaSharp;

namespace Beerly.Services
{
    public static class ImageOps
    {
        private static readonly SKSamplingOptions Sampling = new(SKFilterMode.Linear, SKMipmapMode.Linear);

        public static SKBitmap CreateRgba(int width, int height) =>
            new(new SKImageInfo(width, height, SKColorType.Rgba8888, SKAlphaType.Premul));


        public static SKBitmap? DecodeRgba(SKCodec codec)
        {
            
            var alpha = codec.Info.AlphaType == SKAlphaType.Opaque ? SKAlphaType.Opaque : SKAlphaType.Premul;

            return SKBitmap.Decode(
                codec,
                new SKImageInfo(codec.Info.Width, codec.Info.Height, SKColorType.Rgba8888, alpha));
        }


        public static SKBitmap Fit(SKBitmap source, int maxSide)
        {
            var scale = Math.Min(1f, Math.Min((float)maxSide / source.Width, (float)maxSide / source.Height));
            var width = Math.Max(1, (int)Math.Round(source.Width * scale));
            var height = Math.Max(1, (int)Math.Round(source.Height * scale));

            var result = CreateRgba(width, height);
            using var canvas = new SKCanvas(result);
            canvas.DrawBitmap(
                source,
                new SKRect(0, 0, source.Width, source.Height),
                new SKRect(0, 0, width, height),
                Sampling);
            return result;
        }

        
        public static SKBitmap Fill(SKBitmap source, int size)
        {
            var scale = Math.Max((float)size / source.Width, (float)size / source.Height);
            var cropSide = size / scale;
            var left = (source.Width - cropSide) / 2;
            var top = (source.Height - cropSide) / 2;

            var result = CreateRgba(size, size);
            using var canvas = new SKCanvas(result);
            canvas.DrawBitmap(
                source,
                new SKRect(left, top, left + cropSide, top + cropSide),
                new SKRect(0, 0, size, size),
                Sampling);
            return result;
        }

       
        public static SKBitmap Pad(SKBitmap source, int size)
        {
            var scale = Math.Min((float)size / source.Width, (float)size / source.Height);
            var width = source.Width * scale;
            var height = source.Height * scale;
            var left = (size - width) / 2;
            var top = (size - height) / 2;

            var result = CreateRgba(size, size);
            using var canvas = new SKCanvas(result);
            canvas.Clear(SKColors.Black);
            canvas.DrawBitmap(
                source,
                new SKRect(0, 0, source.Width, source.Height),
                new SKRect(left, top, left + width, top + height),
                Sampling);
            return result;
        }

        
        public static SKBitmap Orient(SKBitmap source, SKEncodedOrigin origin)
        {
            var swapped = origin is SKEncodedOrigin.LeftTop or SKEncodedOrigin.RightTop
                or SKEncodedOrigin.RightBottom or SKEncodedOrigin.LeftBottom;

            var width = swapped ? source.Height : source.Width;
            var height = swapped ? source.Width : source.Height;

            var result = CreateRgba(width, height);
            using var canvas = new SKCanvas(result);

            switch (origin)
            {
                case SKEncodedOrigin.TopRight:
                    canvas.Translate(width, 0);
                    canvas.Scale(-1, 1);
                    break;
                case SKEncodedOrigin.BottomRight:
                    canvas.Translate(width, height);
                    canvas.RotateDegrees(180);
                    break;
                case SKEncodedOrigin.BottomLeft:
                    canvas.Translate(0, height);
                    canvas.Scale(1, -1);
                    break;
                case SKEncodedOrigin.LeftTop:
                    canvas.Scale(-1, 1);
                    canvas.RotateDegrees(90);
                    break;
                case SKEncodedOrigin.RightTop:
                    canvas.Translate(width, 0);
                    canvas.RotateDegrees(90);
                    break;
                case SKEncodedOrigin.RightBottom:
                    canvas.Translate(width, height);
                    canvas.RotateDegrees(180);
                    canvas.Scale(-1, 1);
                    canvas.RotateDegrees(90);
                    break;
                case SKEncodedOrigin.LeftBottom:
                    canvas.Translate(0, height);
                    canvas.RotateDegrees(-90);
                    break;
            }

            canvas.DrawBitmap(source, 0, 0, Sampling);
            return result;
        }
    }
}