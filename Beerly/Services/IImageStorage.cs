namespace Beerly.Services
{
    public interface IImageStorage
    {
        
        Task<string> SaveAsync(Stream content, CancellationToken cancellationToken);

        bool Exists(string path);
    }
}