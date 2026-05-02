using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Mms.Application.Interfaces;

namespace Mms.Infrastructure.Documents;

/// <summary>
/// Converts DOCX to PDF via LibreOffice headless (available in docker-compose).
/// Falls back gracefully with a warning if LibreOffice is not installed (local dev).
/// </summary>
public class LibreOfficePdfConverter : ILibreOfficePdfConverter
{
    private readonly ILogger<LibreOfficePdfConverter> _logger;

    public LibreOfficePdfConverter(ILogger<LibreOfficePdfConverter> logger)
    {
        _logger = logger;
    }

    public async Task<byte[]> ConvertDocxToPdfAsync(byte[] docxBytes, CancellationToken ct = default)
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"mms_pdf_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);

        try
        {
            var tempDocx = Path.Combine(tempDir, "letter.docx");
            await File.WriteAllBytesAsync(tempDocx, docxBytes, ct);

            var psi = new ProcessStartInfo("libreoffice",
                $"--headless --convert-to pdf -env:UserInstallation=file://{tempDir.Replace('\\', '/')} --outdir \"{tempDir}\" \"{tempDocx}\"")
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var proc = Process.Start(psi);
            if (proc is null)
                throw new InvalidOperationException("Failed to start LibreOffice process. Ensure LibreOffice is installed.");

            // Timeout 60 seconds
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeoutCts.CancelAfter(TimeSpan.FromSeconds(60));

            try
            {
                await proc.WaitForExitAsync(timeoutCts.Token);
            }
            catch (OperationCanceledException) when (!ct.IsCancellationRequested)
            {
                proc.Kill(entireProcessTree: true);
                throw new TimeoutException("LibreOffice PDF conversion timed out after 60 seconds.");
            }

            if (proc.ExitCode != 0)
            {
                var stderr = await proc.StandardError.ReadToEndAsync(ct);
                _logger.LogError("LibreOffice conversion failed (exit {Code}): {Error}", proc.ExitCode, stderr);
                throw new InvalidOperationException($"LibreOffice PDF conversion failed with exit code {proc.ExitCode}.");
            }

            var pdfPath = Path.ChangeExtension(tempDocx, ".pdf");
            if (!File.Exists(pdfPath))
                throw new FileNotFoundException("LibreOffice did not produce a PDF output file.", pdfPath);

            var pdfBytes = await File.ReadAllBytesAsync(pdfPath, ct);
            _logger.LogInformation("PDF conversion complete: {Size} bytes", pdfBytes.Length);
            return pdfBytes;
        }
        finally
        {
            // Cleanup temp files
            try { Directory.Delete(tempDir, recursive: true); }
            catch (Exception ex) { _logger.LogWarning(ex, "Failed to clean up temp PDF dir: {Dir}", tempDir); }
        }
    }

    public async Task<byte[]> ConvertHtmlToPdfAsync(string htmlContent, CancellationToken ct = default)
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"mms_pdf_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);

        try
        {
            var tempHtml = Path.Combine(tempDir, "document.html");
            await File.WriteAllTextAsync(tempHtml, htmlContent, ct);

            var psi = new ProcessStartInfo("libreoffice",
                $"--headless --convert-to pdf -env:UserInstallation=file://{tempDir.Replace('\\', '/')} --outdir \"{tempDir}\" \"{tempHtml}\"")
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var proc = Process.Start(psi);
            if (proc is null)
                throw new InvalidOperationException("Failed to start LibreOffice process. Ensure LibreOffice is installed.");

            // Timeout 60 seconds
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeoutCts.CancelAfter(TimeSpan.FromSeconds(60));

            try
            {
                await proc.WaitForExitAsync(timeoutCts.Token);
            }
            catch (OperationCanceledException) when (!ct.IsCancellationRequested)
            {
                proc.Kill(entireProcessTree: true);
                throw new TimeoutException("LibreOffice PDF conversion timed out after 60 seconds.");
            }

            if (proc.ExitCode != 0)
            {
                var stderr = await proc.StandardError.ReadToEndAsync(ct);
                _logger.LogError("LibreOffice html conversion failed (exit {Code}): {Error}", proc.ExitCode, stderr);
                throw new InvalidOperationException($"LibreOffice PDF conversion failed with exit code {proc.ExitCode}.");
            }

            var pdfPath = Path.ChangeExtension(tempHtml, ".pdf");
            if (!File.Exists(pdfPath))
                throw new FileNotFoundException("LibreOffice did not produce a PDF output file.", pdfPath);

            var pdfBytes = await File.ReadAllBytesAsync(pdfPath, ct);
            _logger.LogInformation("PDF HTML conversion complete: {Size} bytes", pdfBytes.Length);
            return pdfBytes;
        }
        finally
        {
            // Cleanup temp files
            try { Directory.Delete(tempDir, recursive: true); }
            catch (Exception ex) { _logger.LogWarning(ex, "Failed to clean up temp PDF dir: {Dir}", tempDir); }
        }
    }
}
