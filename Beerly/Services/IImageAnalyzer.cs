namespace Beerly.Services
{
    public record LabelSuggestions(
        string? Name,
        string? Brewery,
        string? Style,
        double? AbvPercentage,
        string? Country);

    public record ImageAnalysis(
        string Subject,
        bool PersonProminent,
        bool WatermarkOrStock,
        bool Inappropriate,
        bool LabelReadable,
        int QualityScore,
        LabelSuggestions Suggestions,
        string? Diagnostics = null);

    public interface IImageAnalyzer
    {
     
        Task<ImageAnalysis?> AnalyzeAsync(byte[] webpBytes, CancellationToken cancellationToken);
    }
}