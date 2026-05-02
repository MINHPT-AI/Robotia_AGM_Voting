using System.Drawing;
using Mms.Application.Interfaces;
using QRCoder;
using ZXing;
using ZXing.Common;
using ZXing.Rendering;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Formats.Png;

namespace Mms.Infrastructure.Documents;

/// <summary>
/// Generates Code128 barcodes (ZXing.Net) and QR codes (QRCoder) as PNG byte arrays.
/// </summary>
public class BarQrCodeGenerator : IBarQrCodeGenerator
{
    public string BuildContent(string idNumber, string fullName)
        => $"{idNumber}|{fullName}";

    public byte[] GenerateBarcode(string content)
    {
        var writer = new ZXing.BarcodeWriterPixelData
        {
            Format = BarcodeFormat.CODE_128,
            Options = new EncodingOptions
            {
                Width = 350,
                Height = 70,
                Margin = 5,
                PureBarcode = true
            }
        };

        var pixelData = writer.Write(content);

        // Convert PixelData to PNG via ImageSharp
        using var image = SixLabors.ImageSharp.Image.LoadPixelData<Rgba32>(
            pixelData.Pixels, pixelData.Width, pixelData.Height);
        using var ms = new MemoryStream();
        image.Save(ms, new PngEncoder());
        return ms.ToArray();
    }

    public byte[] GenerateQrCode(string content)
    {
        using var qrGenerator = new QRCodeGenerator();
        var qrData = qrGenerator.CreateQrCode(content, QRCodeGenerator.ECCLevel.M);
        var qrCode = new PngByteQRCode(qrData);
        return qrCode.GetGraphic(8); // 8 pixels per module → ~200×200px
    }
}
