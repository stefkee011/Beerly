using SkiaSharp;

namespace Beerly.Services
{
    public class ImageProcessor
    {
        private const long MaxPixels = 40_000_000;
        private readonly int _minSide;
        private const int MaxSide = 1600;

        public ImageProcessor(IConfiguration config)
        {
            _minSide = config.GetValue("Images:MinSide", 400);
        }

        public Task<MemoryStream?> ProcessAsync(Stream input, CancellationToken cancellationToken) =>
            Task.FromResult(Process(input));

        private MemoryStream? Process(Stream input)
        {
            try
            {
                using var data = SKData.Create(input);
                using var codec = SKCodec.Create(data);
                if (codec is null) return null;

                if (codec.EncodedFormat is not (SKEncodedImageFormat.Jpeg
                    or SKEncodedImageFormat.Png
                    or SKEncodedImageFormat.Webp))
                {
                    return null;
                }

                var info = codec.Info;
                if ((long)info.Width * info.Height > MaxPixels) return null;
                if (Math.Min(info.Width, info.Height) < _minSide) return null;

                using var decoded = ImageOps.DecodeRgba(codec);
                if (decoded is null) return null;

                using var oriented = ImageOps.Orient(decoded, codec.EncodedOrigin);
                using var sized = ImageOps.Fit(oriented, MaxSide);
                using var encoded = sized.Encode(SKEncodedImageFormat.Webp, 85);

                return encoded is null ? null : new MemoryStream(encoded.ToArray());
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}