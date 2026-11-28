using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Beerly.Services;

namespace Beerly.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ImagesController : ControllerBase
    {
        private const long MaxBytes = 8 * 1024 * 1024;

        private readonly ImageProcessor _processor;
        private readonly IImageAnalyzer _analyzer;
        private readonly IImageStorage _storage;
        private readonly ILogger<ImagesController> _logger;
        private readonly IWebHostEnvironment _environment;

        public ImagesController(
            ImageProcessor processor,
            IImageAnalyzer analyzer,
            IImageStorage storage,
            ILogger<ImagesController> logger,
            IWebHostEnvironment environment)
        {
            _processor = processor;
            _analyzer = analyzer;
            _storage = storage;
            _logger = logger;
            _environment = environment;
        }

        [HttpPost]
        [EnableRateLimiting("uploads")]
        [RequestSizeLimit(9 * 1024 * 1024)]
        public async Task<IActionResult> Upload(IFormFile file, CancellationToken cancellationToken)
        {
            if (file is null || file.Length == 0 || file.Length > MaxBytes)
            {
                return BadRequest("Missing file, or file larger than 8 MB.");
            }

            using var buffer = new MemoryStream();
            await file.CopyToAsync(buffer, cancellationToken);
            buffer.Position = 0;

            // Cheap checks first
            using var processed = await _processor.ProcessAsync(buffer, cancellationToken);
            if (processed is null)
            {
                return BadRequest("This isn't a usable JPG, PNG or WebP image.");
            }

            // Then the detector
            var analysis = await _analyzer.AnalyzeAsync(processed.ToArray(), cancellationToken);
            if (analysis is null)
            {
                return StatusCode(StatusCodes.Status503ServiceUnavailable, new { code = "unavailable" });
            }

            var verdict = PhotoPolicy.Evaluate(analysis);
            var diagnostics = _environment.IsDevelopment() ? analysis.Diagnostics : null;

            _logger.LogInformation(
                "Photo check: subject={Subject} score={Score} reject={Reject}",
                analysis.Subject, verdict.Score, verdict.RejectCode ?? "none");

            if (verdict.RejectCode is not null)
            {
                return UnprocessableEntity(new { code = verdict.RejectCode, diagnostics });
            }

            // Rejected photos never touch the disk
            processed.Position = 0;
            var path = await _storage.SaveAsync(processed, cancellationToken);

            return Ok(new { imageUrl = path, suggestions = analysis.Suggestions, diagnostics });
        }
    }
}