using System.Text.Json;
using MediatR;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Mms.Application.Interfaces;
using Mms.Application.Templates;
using Mms.Domain.Entities;
using Mms.Infrastructure.Persistence;

namespace Mms.Infrastructure.Handlers.Templates;

// ── Upload Template ──
public class UploadTemplateHandler : IRequestHandler<UploadTemplateCommand, TemplateUploadResultDto>
{
    private readonly MmsDbContext _db;
    private readonly ITemplateFileService _fileService;

    public UploadTemplateHandler(MmsDbContext db, ITemplateFileService fileService)
    {
        _db = db;
        _fileService = fileService;
    }

    public async Task<TemplateUploadResultDto> Handle(UploadTemplateCommand req, CancellationToken ct)
    {
        if (!req.OriginalFileName.EndsWith(".docx", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Chỉ chấp nhận file .docx");

        var (filePath, fileSize) = await _fileService.SaveAsync(req.FileStream, ct);

        var template = new Template
        {
            Name = req.Name,
            TemplateType = req.TemplateType,
            Language = req.Language,
            FilePath = filePath,
            FileSize = fileSize,
            SelectedTokens = JsonSerializer.Serialize(req.SelectedTokenCodes),
            UseSignatureAndSeal = req.UseSignatureAndSeal,
            UploadedBy = req.UploadedBy,
            UploadedAt = DateTime.UtcNow,
        };

        _db.Templates.Add(template);
        await _db.SaveChangesAsync(ct);

        return new TemplateUploadResultDto(template.Id);
    }
}

// ── Save Template HTML Content (draft) ──
public class SaveTemplateContentHandler : IRequestHandler<SaveTemplateContentCommand>
{
    private readonly MmsDbContext _db;

    public SaveTemplateContentHandler(MmsDbContext db) => _db = db;

    public async Task Handle(SaveTemplateContentCommand req, CancellationToken ct)
    {
        var template = await _db.Templates.FindAsync(new object[] { req.Id }, ct)
            ?? throw new InvalidOperationException("Không tìm thấy mẫu văn bản");

        if (template.IsFinalized)
            throw new InvalidOperationException("Không thể sửa mẫu đã chốt");

        template.HtmlContent = req.HtmlContent;
        template.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
    }
}

// ── Update Template Name ──
public class UpdateTemplateNameHandler : IRequestHandler<UpdateTemplateNameCommand>
{
    private readonly MmsDbContext _db;

    public UpdateTemplateNameHandler(MmsDbContext db) => _db = db;

    public async Task Handle(UpdateTemplateNameCommand req, CancellationToken ct)
    {
        var template = await _db.Templates.FindAsync(new object[] { req.Id }, ct)
            ?? throw new InvalidOperationException("Không tìm thấy mẫu văn bản");

        if (template.IsFinalized)
            throw new InvalidOperationException("Không thể sửa mẫu đã chốt");

        template.Name = req.Name;
        template.Language = req.Language;
        template.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
    }
}

// ── Finalize Template ──
public class FinalizeTemplateHandler : IRequestHandler<FinalizeTemplateCommand>
{
    private readonly MmsDbContext _db;

    public FinalizeTemplateHandler(MmsDbContext db) => _db = db;

    public async Task Handle(FinalizeTemplateCommand req, CancellationToken ct)
    {
        var template = await _db.Templates.FindAsync(new object[] { req.Id }, ct)
            ?? throw new InvalidOperationException("Không tìm thấy mẫu văn bản");

        if (string.IsNullOrEmpty(template.FilePath) && string.IsNullOrEmpty(template.HtmlContent))
            throw new InvalidOperationException("Chưa có nội dung — vui lòng tải lên file DOCX hoặc soạn thảo trước khi chốt");

        template.IsFinalized = true;
        template.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
    }
}

// ── Clone Template ──
public class CloneTemplateHandler : IRequestHandler<CloneTemplateCommand, Guid>
{
    private readonly MmsDbContext _db;
    private readonly ITemplateFileService _fileService;
    private readonly IWebHostEnvironment _env;

    public CloneTemplateHandler(MmsDbContext db, ITemplateFileService fileService, IWebHostEnvironment env)
    {
        _db = db;
        _fileService = fileService;
        _env = env;
    }

    public async Task<Guid> Handle(CloneTemplateCommand req, CancellationToken ct)
    {
        var source = await _db.Templates.AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == req.SourceId, ct)
            ?? throw new InvalidOperationException("Không tìm thấy mẫu nguồn");

        string? newFilePath = null;
        long? newFileSize = null;

        if (source.FilePath is not null)
        {
            var fullSourcePath = Path.Combine(_env.WebRootPath, source.FilePath);
            await using var sourceStream = File.OpenRead(fullSourcePath);
            var (path, size) = await _fileService.SaveAsync(sourceStream, ct);
            newFilePath = path;
            newFileSize = size;
        }

        var clone = new Template
        {
            Name = req.NewName,
            TemplateType = source.TemplateType,
            Language = source.Language,
            Version = source.Version + 1,
            FilePath = newFilePath,
            FileSize = newFileSize,
            HtmlContent = source.HtmlContent,
            SelectedTokens = source.SelectedTokens,
            UseSignatureAndSeal = source.UseSignatureAndSeal,
            IsFinalized = false,
            MeetingId = null,
            UploadedBy = req.ClonedBy,
            UploadedAt = DateTime.UtcNow,
        };

        _db.Templates.Add(clone);
        await _db.SaveChangesAsync(ct);
        return clone.Id;
    }
}

// ── Delete Template ──
public class DeleteTemplateHandler : IRequestHandler<DeleteTemplateCommand>
{
    private readonly MmsDbContext _db;
    private readonly ITemplateFileService _fileService;

    public DeleteTemplateHandler(MmsDbContext db, ITemplateFileService fileService)
    {
        _db = db;
        _fileService = fileService;
    }

    public async Task Handle(DeleteTemplateCommand req, CancellationToken ct)
    {
        var template = await _db.Templates.FindAsync(new object[] { req.Id }, ct)
            ?? throw new InvalidOperationException("Không tìm thấy mẫu văn bản");

        if (template.IsFinalized)
            throw new InvalidOperationException("Không thể xóa mẫu đã chốt");

        if (template.FilePath is not null)
            _fileService.Delete(template.FilePath);

        _db.Templates.Remove(template);
        await _db.SaveChangesAsync(ct);
    }
}

// ── Get Templates (List) ──
public class GetTemplatesHandler : IRequestHandler<GetTemplatesQuery, IList<TemplateListItemDto>>
{
    private readonly MmsDbContext _db;

    public GetTemplatesHandler(MmsDbContext db) => _db = db;

    public async Task<IList<TemplateListItemDto>> Handle(GetTemplatesQuery req, CancellationToken ct)
    {
        var q = _db.Templates.AsNoTracking()
            .Where(t => !req.GlobalOnly || t.MeetingId == null);

        if (req.FilterType.HasValue)
            q = q.Where(t => t.TemplateType == req.FilterType.Value);

        var templates = await q
            .OrderByDescending(t => t.UploadedAt)
            .ToListAsync(ct);

        return templates.Select(t => new TemplateListItemDto(
            t.Id, t.Name, t.TemplateType,
            t.Language, t.Version, t.FileSize,
            t.IsFinalized, !string.IsNullOrEmpty(t.HtmlContent),
            t.UseSignatureAndSeal, t.UploadedAt
        )).ToList();
    }
}

// ── Get Template Detail (for editor page) ──
public class GetTemplateDetailHandler : IRequestHandler<GetTemplateDetailQuery, TemplateDetailDto>
{
    private readonly MmsDbContext _db;

    public GetTemplateDetailHandler(MmsDbContext db) => _db = db;

    public async Task<TemplateDetailDto> Handle(GetTemplateDetailQuery req, CancellationToken ct)
    {
        var t = await _db.Templates.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == req.Id, ct)
            ?? throw new InvalidOperationException("Không tìm thấy mẫu văn bản");

        return new TemplateDetailDto(
            t.Id, t.Name, t.TemplateType, t.Language,
            t.HtmlContent, t.SelectedTokens,
            t.UseSignatureAndSeal, t.IsFinalized, 
            t.MarginTop, t.MarginBottom, t.MarginLeft, t.MarginRight, 
            t.FilePath);
    }
}

// ── Get Template DOCX Bytes (for Mammoth.js conversion) ──
public class GetTemplateDocxBytesHandler : IRequestHandler<GetTemplateDocxBytesQuery, byte[]>
{
    private readonly MmsDbContext _db;
    private readonly ITemplateFileService _fileService;

    public GetTemplateDocxBytesHandler(MmsDbContext db, ITemplateFileService fileService)
    {
        _db = db;
        _fileService = fileService;
    }

    public async Task<byte[]> Handle(GetTemplateDocxBytesQuery req, CancellationToken ct)
    {
        var template = await _db.Templates.AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == req.Id, ct)
            ?? throw new InvalidOperationException("Không tìm thấy mẫu văn bản");

        if (template.FilePath is null)
            throw new InvalidOperationException("Mẫu chưa có file DOCX");

        return await _fileService.GetDocxBytesAsync(template.FilePath);
    }
}

// ── Get All Tokens (static registry, no DB) ──
public class GetAllTokensHandler : IRequestHandler<GetAllTokensQuery, IList<TokenRegistry.TokenInfo>>
{
    public Task<IList<TokenRegistry.TokenInfo>> Handle(GetAllTokensQuery req, CancellationToken ct)
        => Task.FromResult(TokenRegistry.GetAllTokens());
}

// ── Update Margins ──
public class UpdateTemplateMarginsHandler : IRequestHandler<UpdateTemplateMarginsCommand>
{
    private readonly MmsDbContext _db;

    public UpdateTemplateMarginsHandler(MmsDbContext db) => _db = db;

    public async Task Handle(UpdateTemplateMarginsCommand req, CancellationToken ct)
    {
        var template = await _db.Templates.FindAsync(new object[] { req.Id }, ct)
            ?? throw new InvalidOperationException("Không tìm thấy mẫu văn bản");

        if (template.IsFinalized)
            throw new InvalidOperationException("Không thể sửa mẫu đã chốt");

        template.MarginTop = req.MarginTop;
        template.MarginBottom = req.MarginBottom;
        template.MarginLeft = req.MarginLeft;
        template.MarginRight = req.MarginRight;
        template.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);
    }
}

// ── Preview HTML ──
public class PreviewTemplateHtmlHandler : IRequestHandler<PreviewTemplateHtmlQuery, byte[]>
{
    private readonly MmsDbContext _db;
    private readonly ILibreOfficePdfConverter _pdfConverter;

    public PreviewTemplateHtmlHandler(MmsDbContext db, ILibreOfficePdfConverter pdfConverter)
    {
        _db = db;
        _pdfConverter = pdfConverter;
    }

    public async Task<byte[]> Handle(PreviewTemplateHtmlQuery req, CancellationToken ct)
    {
        var html = req.HtmlContent ?? "";
        var tokens = new Dictionary<string, string>();

        // Lấy dữ liệu mẫu nếu có meeting
        if (req.MeetingId.HasValue)
        {
            var meeting = await _db.Meetings.AsNoTracking()
                .FirstOrDefaultAsync(m => m.Id == req.MeetingId, ct);
            var company = await _db.Companies.AsNoTracking().FirstOrDefaultAsync(ct);
            var firstShareholder = await _db.Shareholders.AsNoTracking()
                .Where(s => s.MeetingId == req.MeetingId)
                .OrderBy(s => s.Id)
                .FirstOrDefaultAsync(ct);

            tokens["[1]"] = company?.Name ?? "CÔNG TY CỔ PHẦN MẪU";
            tokens["[2]"] = firstShareholder?.FullName ?? "Nguyễn Văn A";
            tokens["[3]"] = (firstShareholder?.SharesTotal ?? 10000).ToString("N0");
            tokens["[4]"] = meeting?.MeetingDate.ToString("dd/MM/yyyy") ?? "25/04/2026";
            tokens["[5]"] = meeting?.MeetingDate.ToString("HH:mm") ?? "08:00";
            tokens["[6]"] = meeting?.Location ?? "Khách sạn Pan Pacific";
            tokens["[7]"] = firstShareholder?.Address ?? "Hà Nội";
            tokens["[8]"] = firstShareholder?.Phone ?? "0900000000";
            tokens["[9]"] = firstShareholder?.IdNumber ?? "001000000000";
            tokens["[10]"] = company?.StockCode ?? "MMS";
            tokens["[11]"] = company?.LegalRepName ?? "Lê Văn B";
            tokens["[12]"] = company?.LegalRepTitle ?? "Chủ tịch HĐQT";
            tokens["[13]"] = company?.TaxCode ?? "0100000000";
            tokens["[14]"] = (company?.CharterCapital ?? 100000000000).ToString("N0");
            tokens["[15]"] = (company?.TotalSharesIssued ?? 10000000).ToString("N0");
        }
        else
        {
            // Dữ liệu giả lập hoàn toàn
            tokens["[1]"] = "CÔNG TY CỔ PHẦN MẪU";
            tokens["[2]"] = "Nguyễn Văn A";
            tokens["[3]"] = "10,000";
            tokens["[4]"] = "25/04/2026";
            tokens["[5]"] = "08:00";
            tokens["[6]"] = "Khách sạn quốc tế";
            tokens["[7]"] = "Hà Nội";
            tokens["[8]"] = "0901234567";
            tokens["[9]"] = "001090123456";
            tokens["[10]"] = "MMS";
            tokens["[11]"] = "Lê Văn B";
            tokens["[12]"] = "Chủ tịch HĐQT";
            tokens["[13]"] = "0100000000";
            tokens["[14]"] = "100,000,000,000";
            tokens["[15]"] = "10,000,000";
        }

        // Thay thế token
        html = TokenRegistry.ReplaceTokensInHtml(html, tokens);

        // Bọc vào HTML chuẩn
        var fullHtml = $@"<!DOCTYPE html>
<html>
<head>
    <meta charset='UTF-8'>
    <style>
        @page {{ size: A4; margin: {req.MarginTop}cm {req.MarginRight}cm {req.MarginBottom}cm {req.MarginLeft}cm; }}
        body {{ font-family: 'Times New Roman', serif; font-size: 13pt; line-height: 1.4; }}
        /* Clean up tinyMCE specific classes for print */
        .mce-token {{ background: transparent !important; border: none !important; color: inherit !important; font-weight: normal !important; }}
        .sign-seal-block {{ text-align: center; display: inline-block; min-width: 300px; }}
        hr.deco-line-short {{ width: 5cm; margin: 2px auto; border: none; border-top: 1px solid #000; }}
        hr.deco-line-long {{ width: 8cm; margin: 2px auto; border: none; border-top: 1px solid #000; }}
    </style>
</head>
<body>
{html}
</body>
</html>";

        return await _pdfConverter.ConvertHtmlToPdfAsync(fullHtml, ct);
    }
}
