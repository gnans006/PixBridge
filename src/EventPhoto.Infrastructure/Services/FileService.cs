using EventPhoto.Application.Common.Interfaces;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace EventPhoto.Infrastructure.Services
{
    public class FileService : IFileService
    {
        public async Task<byte[]> ReadAllBytesAsync(string path, CancellationToken cancellationToken)
        {
            return await File.ReadAllBytesAsync(path, cancellationToken);
        }

        public bool FileExists(string path)
        {
            return File.Exists(path);
        }

        public async Task DeleteFileAsync(string path, CancellationToken cancellationToken)
        {
            if (File.Exists(path))
            {
                // Use Task.Run to make it cancellable
                await Task.Run(() => File.Delete(path), cancellationToken);
            }
        }

        public async Task EnsureDirectoryExistsAsync(string directoryPath, CancellationToken cancellationToken)
        {
            await Task.Run(() =>
            {
                if (!Directory.Exists(directoryPath))
                {
                    Directory.CreateDirectory(directoryPath);
                }
            }, cancellationToken);
        }
    }
}
