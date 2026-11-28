using System.Text.Json;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using SkiaSharp;

namespace Beerly.Services
{
    public class LocalImageAnalyzer : IImageAnalyzer, IDisposable
    {
        
        private const float FaceThreshold = 0.6f;
        private const float ProminentFaceRatio = 0.02f;   
        private const double MinSubjectConfidence = 0.45;

        private static readonly float[] ClipMean = { 0.48145466f, 0.4578275f, 0.40821073f };
        private static readonly float[] ClipStd = { 0.26862954f, 0.26130258f, 0.27577711f };

        private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

        private sealed record Prompt(string Class, string Text, float[] Embedding);
        private sealed record Loaded(InferenceSession Clip, InferenceSession Face, List<Prompt> Prompts);

        private readonly ILogger<LocalImageAnalyzer> _logger;
        private readonly string _modelsPath;
        private readonly Lazy<Loaded?> _models;

        public LocalImageAnalyzer(IWebHostEnvironment environment, ILogger<LocalImageAnalyzer> logger)
        {
            _logger = logger;
            _modelsPath = Path.Combine(environment.ContentRootPath, "DetectorModels");
            _models = new Lazy<Loaded?>(Load);
        }

        private Loaded? Load()
        {
            try
            {
                var clip = new InferenceSession(Path.Combine(_modelsPath, "clip-visual.onnx"));
                var face = new InferenceSession(Path.Combine(_modelsPath, "yunet.onnx"));
                var json = File.ReadAllText(Path.Combine(_modelsPath, "clip-prompts.json"));
                var prompts = JsonSerializer.Deserialize<List<Prompt>>(json, JsonOptions)!;

                return new Loaded(clip, face, prompts);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Could not load detector models from {Path}", _modelsPath);
                return null;
            }
        }

        public Task<ImageAnalysis?> AnalyzeAsync(byte[] webpBytes, CancellationToken cancellationToken)
        {
            var models = _models.Value;
            if (models is null) return Task.FromResult<ImageAnalysis?>(null);

            return Task.Run(() => AnalyzeSafely(webpBytes, models), cancellationToken);
        }

        private ImageAnalysis? AnalyzeSafely(byte[] webpBytes, Loaded models)
        {
            try
            {
                using var data = SKData.CreateCopy(webpBytes);
                using var codec = SKCodec.Create(data);
                if (codec is null) return null;

                using var bitmap = ImageOps.DecodeRgba(codec);
                return bitmap is null ? null : Analyze(bitmap, models);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Photo analysis failed");
                return null;
            }
        }

        private ImageAnalysis Analyze(SKBitmap bitmap, Loaded models)
        {
            var probabilities = Classify(bitmap, models);
            var best = probabilities.MaxBy(pair => pair.Value);
            var subject = best.Value >= MinSubjectConfidence ? best.Key : "unclear";

            var faceRatio = LargestFaceRatio(bitmap, models.Face);
            var (score, sharpness) = MeasureQuality(bitmap);
            var diagnostics =
            $"subject={subject} confidence={best.Value:P0} face={faceRatio:P1} sharpness={sharpness:F2} score={score}";

            _logger.LogInformation("Detector: {Diagnostics}", diagnostics);

            return new ImageAnalysis(
                Subject: subject,
                PersonProminent: faceRatio >= ProminentFaceRatio,
                WatermarkOrStock: false,   
                Inappropriate: false,      
                LabelReadable: sharpness >= 0.3,
                QualityScore: score,
                Suggestions: new LabelSuggestions(null, null, null, null, null),
                Diagnostics: diagnostics);
        }

        
        private Dictionary<string, double> Classify(SKBitmap bitmap, Loaded models)
        {
            const int size = 224;
            const int plane = size * size;

            using var crop = ImageOps.Fill(bitmap, size);
            var pixels = crop.Bytes;   // R, G, B, A for every pixel
            var stride = crop.RowBytes;

            var data = new float[3 * plane];
            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var p = y * stride + x * 4;
                    var i = y * size + x;
                    data[i] = (pixels[p] / 255f - ClipMean[0]) / ClipStd[0];
                    data[plane + i] = (pixels[p + 1] / 255f - ClipMean[1]) / ClipStd[1];
                    data[2 * plane + i] = (pixels[p + 2] / 255f - ClipMean[2]) / ClipStd[2];
                }
            }

            var input = new DenseTensor<float>(data, new[] { 1, 3, size, size });

            using var results = models.Clip.Run(new[]
            {
                NamedOnnxValue.CreateFromTensor(models.Clip.InputMetadata.Keys.First(), input)
            });

            var embedding = results.First().AsTensor<float>().ToArray();
            if (embedding.Length != 512) throw new InvalidOperationException("Unexpected CLIP output size.");
            Normalize(embedding);

            
            var logits = models.Prompts.Select(p => 100.0 * Dot(embedding, p.Embedding)).ToArray();
            var max = logits.Max();
            var exp = logits.Select(l => Math.Exp(l - max)).ToArray();
            var total = exp.Sum();

            var byClass = new Dictionary<string, double>();
            for (var i = 0; i < exp.Length; i++)
            {
                var name = models.Prompts[i].Class;
                byClass[name] = byClass.GetValueOrDefault(name) + exp[i] / total;
            }

            return byClass;
        }

      
        private float LargestFaceRatio(SKBitmap bitmap, InferenceSession face)
        {
            const int size = 640;
            const int plane = size * size;

            using var padded = ImageOps.Pad(bitmap, size);
            var pixels = padded.Bytes;
            var stride = padded.RowBytes;

         
            var data = new float[3 * plane];
            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var p = y * stride + x * 4;
                    var i = y * size + x;
                    data[i] = pixels[p + 2];
                    data[plane + i] = pixels[p + 1];
                    data[2 * plane + i] = pixels[p];
                }
            }

            var input = new DenseTensor<float>(data, new[] { 1, 3, size, size });

            using var results = face.Run(new[]
            {
                NamedOnnxValue.CreateFromTensor(face.InputMetadata.Keys.First(), input)
            });

            var outputs = results.ToDictionary(r => r.Name, r => r.AsTensor<float>().ToArray());

            var scale = Math.Min((float)size / bitmap.Width, (float)size / bitmap.Height);
            var imageArea = bitmap.Width * scale * bitmap.Height * scale;
            var largest = 0f;

            foreach (var stride8 in new[] { 8, 16, 32 })
            {
                var cls = outputs[$"cls_{stride8}"];
                var obj = outputs[$"obj_{stride8}"];
                var box = outputs[$"bbox_{stride8}"];

                for (var i = 0; i < cls.Length; i++)
                {
                    var score = MathF.Sqrt(Math.Clamp(cls[i], 0f, 1f) * Math.Clamp(obj[i], 0f, 1f));
                    if (score < FaceThreshold) continue;

                    var width = MathF.Exp(box[i * 4 + 2]) * stride8;
                    var height = MathF.Exp(box[i * 4 + 3]) * stride8;
                    largest = Math.Max(largest, width * height / imageArea);
                }
            }

            return largest;
        }

        
        private static (int Score, double Sharpness) MeasureQuality(SKBitmap bitmap)
        {
            using var small = ImageOps.Fit(bitmap, 512);
            var width = small.Width;
            var height = small.Height;
            var pixels = small.Bytes;
            var stride = small.RowBytes;

            var gray = new int[width * height];
            double brightness = 0;

            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    var p = y * stride + x * 4;
                    var luma = (int)(0.299 * pixels[p] + 0.587 * pixels[p + 1] + 0.114 * pixels[p + 2]);
                    gray[y * width + x] = luma;
                    brightness += luma;
                }
            }

            brightness /= gray.Length;

            
            double sum = 0, sumSquares = 0;
            var count = 0;

            for (var y = 1; y < height - 1; y++)
            {
                for (var x = 1; x < width - 1; x++)
                {
                    var i = y * width + x;
                    var laplacian = gray[i - width] + gray[i + width] + gray[i - 1] + gray[i + 1] - 4 * gray[i];

                    sum += laplacian;
                    sumSquares += (double)laplacian * laplacian;
                    count++;
                }
            }

            var mean = sum / count;
            var variance = sumSquares / count - mean * mean;

            var sharpness = Math.Clamp((variance - 20) / 280, 0, 1);
            var exposure = brightness switch
            {
                < 60 => brightness / 60,
                > 200 => (255 - brightness) / 55,
                _ => 1.0
            };

            var score = (int)Math.Round(100 * (0.7 * sharpness + 0.3 * exposure));
            return (score, sharpness);
        }

        private static void Normalize(float[] vector)
        {
            var length = MathF.Sqrt(vector.Sum(v => v * v));
            for (var i = 0; i < vector.Length; i++) vector[i] /= length;
        }

        private static float Dot(float[] a, float[] b)
        {
            var total = 0f;
            for (var i = 0; i < a.Length; i++) total += a[i] * b[i];
            return total;
        }

        public void Dispose()
        {
            if (_models.IsValueCreated && _models.Value is { } loaded)
            {
                loaded.Clip.Dispose();
                loaded.Face.Dispose();
            }
        }
    }
}