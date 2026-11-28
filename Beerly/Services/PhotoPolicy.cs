namespace Beerly.Services
{
    public static class PhotoPolicy
    {
        private const int MinScore = 25;

       
        public static (string? RejectCode, int Score) Evaluate(ImageAnalysis a)
        {
            if (a.Inappropriate) return ("rejected", 0);

            var isPackage = a.Subject is "beer_bottle" or "beer_can" or "multiple_beers";
            var isGlass = a.Subject == "beer_glass_only";

            if (!isPackage && !isGlass)
            {
                return (a.Subject == "unclear" ? "poor_quality" : "not_beer", 0);
            }

            if (a.PersonProminent) return ("person", 0);
            if (a.WatermarkOrStock) return ("watermark", 0);
            if (isPackage && !a.LabelReadable) return ("poor_quality", 0);

            var score = a.QualityScore;
            if (isGlass) score = Math.Min(score, 40);
            if (a.Subject == "multiple_beers") score = Math.Min(score, 60);

            return score < MinScore ? ("poor_quality", score) : (null, score);
        }
    }
}