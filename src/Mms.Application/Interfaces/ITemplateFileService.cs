namespace Mms.Application.Interfaces;

public interface ITemplateFileService
{
    Task<(string FilePath, long FileSize)> SaveAsync(Stream stream, CancellationToken ct = default);
    Task<byte[]> GetDocxBytesAsync(string filePath);
    void Delete(string filePath);
}
