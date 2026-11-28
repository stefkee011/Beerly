namespace Beerly.Services
{
    public class LocalImageStorage : IImageStorage
    {
        private const string UrlPrefix = "/uploads/beers/";
        private readonly string _folder;

        public LocalImageStorage(string folder)
        {
            _folder = folder;
        }

        public async Task<string> SaveAsync(Stream content, CancellationToken cancellationToken)
        {
            // server generated filename
            var fileName = $"{Guid.NewGuid():N}.webp";

            await using var file = File.Create(Path.Combine(_folder, fileName));
            await content.CopyToAsync(file, cancellationToken);

            return UrlPrefix + fileName;
        }

        public bool Exists(string path)
        {
            if (!path.StartsWith(UrlPrefix)) return false;

            var fileName = path[UrlPrefix.Length..];

            // Plain file names only
            if (fileName != Path.GetFileName(fileName)) return false;

            return File.Exists(Path.Combine(_folder, fileName));
        }
    }
}