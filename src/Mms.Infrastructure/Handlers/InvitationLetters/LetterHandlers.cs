using System.Text.Json;
using ClosedXML.Excel;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Mms.Application.InvitationLetters.Commands;
using Mms.Application.InvitationLetters.Dtos;
using Mms.Application.InvitationLetters.Queries;
using Mms.Application.Interfaces;
using Mms.Domain.Entities;
using Mms.Infrastructure.Documents;
using Mms.Infrastructure.Parsing;
using Mms.Infrastructure.Persistence;

namespace Mms.Infrastructure.Handlers.InvitationLetters;

// ── Generate letters from shareholder data ──
public class GenerateLettersHandler : IRequestHandler<GenerateLettersCommand, int>
{
    private readonly MmsDbContext _db;
    private readonly ILogger<GenerateLettersHandler> _logger;

    public GenerateLettersHandler(MmsDbContext db, ILogger<GenerateLettersHandler> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<int> Handle(GenerateLettersCommand req, CancellationToken ct)
    {
        // 1. Get shareholders for this meeting
        var shareholders = await _db.Shareholders
            .Where(s => s.MeetingId == req.MeetingId)
            .AsNoTracking()
            .ToListAsync(ct);

        if (shareholders.Count == 0)
            throw new InvalidOperationException("Chưa có danh sách cổ đông. Vui lòng import trước.");

        // 2. Get existing letters to avoid duplicates
        var existingIds = await _db.InvitationLetters
            .Where(l => l.MeetingId == req.MeetingId)
            .Select(l => l.ShareholderIdNumber)
            .ToListAsync(ct);

        var existingSet = existingIds.ToHashSet();

        var newLetters = shareholders
            .Where(s => !existingSet.Contains(s.IdNumber))
            .Select(s => new InvitationLetter
            {
                MeetingId = req.MeetingId,
                ShareholderIdNumber = s.IdNumber,
                ShareholderName = s.FullName,
                ShareholderAddress = s.Address,
                ShareholderPhone = s.Phone,
                VotingRights = s.VotingRights,
                SharesTotal = s.SharesTotal,
                Status = InvitationStatus.NotSent,
                CodeMarkType = CodeMarkType.Barcode,
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow
            })
            .ToList();

        if (newLetters.Count > 0)
        {
            await _db.InvitationLetters.AddRangeAsync(newLetters, ct);
            await _db.SaveChangesAsync(ct);
        }

        _logger.LogInformation("Generated {Count} new invitation letters for meeting {MeetingId} (total shareholders: {Total})",
            newLetters.Count, req.MeetingId, shareholders.Count);
        return newLetters.Count; // 0 = all already created previously
    }
}

// ── Export merged DOCX ──
public class ExportLettersDocxHandler : IRequestHandler<ExportLettersDocxCommand, (byte[] FileBytes, string FileName)>
{
    private readonly MmsDbContext _db;
    private readonly ILetterDocxBuilder _builder;
    private readonly IBarQrCodeGenerator _codeGen;
    private readonly ITemplateFileService _fileService;

    public ExportLettersDocxHandler(MmsDbContext db, ILetterDocxBuilder builder,
        IBarQrCodeGenerator codeGen, ITemplateFileService fileService)
    {
        _db = db;
        _builder = builder;
        _codeGen = codeGen;
        _fileService = fileService;
    }

    public async Task<(byte[] FileBytes, string FileName)> Handle(ExportLettersDocxCommand req, CancellationToken ct)
    {
        var letters = await _db.InvitationLetters
            .Where(l => l.MeetingId == req.MeetingId)
            .OrderBy(l => l.ShareholderName)
            .AsNoTracking()
            .ToListAsync(ct);

        // Load meeting + company info for template token replacement
        var meeting = await _db.Meetings
            .Include(m => m.Company)
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == req.MeetingId, ct);

        // Load shareholder IsOrganization data for [9] label
        var shareholderOrgs = await _db.Shareholders
            .Where(s => s.MeetingId == req.MeetingId)
            .Select(s => new { s.IdNumber, s.IsOrganization })
            .AsNoTracking()
            .ToListAsync(ct);
        var orgLookup = shareholderOrgs.ToDictionary(s => s.IdNumber, s => s.IsOrganization);

        // Template lookup: meeting-specific → global → synthetic fallback
        var (templateBytes, selectedTokens) = await FindTemplateAsync(req.MeetingId, ct);

        var dtos = letters.Select(l => new LetterBuildDto(
            l.ShareholderName,
            l.ShareholderAddress ?? "",
            l.ShareholderPhone ?? "",
            l.ShareholderIdNumber,
            l.SharesTotal.ToString("N0"),
            l.TrackingCode)
        {
            NgayHop = meeting?.MeetingDate.ToString("dd/MM/yyyy"),
            GioHop = meeting?.MeetingDate.ToString("HH:mm"),
            DiaDiem = meeting?.Location,
            TenCongTy = meeting?.Company?.Name,
            IsOrganization = orgLookup.TryGetValue(l.ShareholderIdNumber, out var isOrg) && isOrg,
            SelectedTokens = selectedTokens,
        }).ToList();

        byte[] docxBytes;
        if (templateBytes is not null)
        {
            // Build from uploaded template — each letter is a separate DOCX, then merge
            var docxParts = new List<byte[]>(dtos.Count);
            foreach (var dto in dtos)
            {
                var codeMark = GenerateCodeMarkForLetter(dto, req.CodeMarkType);
                var letterBytes = _builder.BuildFromTemplate(dto, templateBytes, codeMark, req.CodeMarkType);
                docxParts.Add(letterBytes);
            }

            docxBytes = LetterDocxBuilder.MergeDocxFiles(docxParts);

            // Record template usage on letters
            var template = await FindTemplateEntityAsync(req.MeetingId, ct);
            if (template is not null)
            {
                var letterIds = letters.Select(l => l.Id).ToList();
                await _db.InvitationLetters
                    .Where(l => letterIds.Contains(l.Id))
                    .ExecuteUpdateAsync(s => s.SetProperty(l => l.TemplateId, template.Id), ct);
            }
        }
        else
        {
            // Fallback to synthetic builder
            docxBytes = _builder.BuildMergedDocx(dtos, req.CodeMarkType, _codeGen);
        }

        var fileName = $"ThuMoi_DHCD_{req.MeetingId:N}.docx";
        return (docxBytes, fileName);
    }

    private async Task<(byte[]? TemplateBytes, IList<string>? SelectedTokens)> FindTemplateAsync(Guid meetingId, CancellationToken ct)
    {
        var template = await FindTemplateEntityAsync(meetingId, ct);
        if (template?.FilePath is null) return (null, null);

        var bytes = await _fileService.GetDocxBytesAsync(template.FilePath);
        var tokens = !string.IsNullOrEmpty(template.SelectedTokens)
            ? JsonSerializer.Deserialize<List<string>>(template.SelectedTokens)
            : null;
        return (bytes, tokens);
    }

    private async Task<Template?> FindTemplateEntityAsync(Guid meetingId, CancellationToken ct)
    {
        // Priority 1: meeting-specific finalized template
        var template = await _db.Templates
            .Where(t => t.MeetingId == meetingId
                     && t.TemplateType == Domain.Enums.TemplateType.Invitation
                     && t.IsFinalized)
            .AsNoTracking()
            .FirstOrDefaultAsync(ct);

        // Priority 2: global finalized template
        template ??= await _db.Templates
            .Where(t => t.MeetingId == null
                     && t.TemplateType == Domain.Enums.TemplateType.Invitation
                     && t.IsFinalized)
            .AsNoTracking()
            .FirstOrDefaultAsync(ct);

        return template;
    }

    private byte[]? GenerateCodeMarkForLetter(LetterBuildDto dto, CodeMarkType type)
    {
        if (type == CodeMarkType.None) return null;
        if (type == CodeMarkType.QRCode)
            return _codeGen.GenerateQrCode(_codeGen.BuildContent(dto.SoDKSH, dto.HoTen));
        var asciiOnly = new string(dto.SoDKSH.Where(c => c <= 127).ToArray());
        return _codeGen.GenerateBarcode(asciiOnly);
    }
}

// ── Export PDF (same template lookup as DOCX, then convert via LibreOffice) ──
public class ExportLettersPdfHandler : IRequestHandler<ExportLettersPdfCommand, (byte[] FileBytes, string FileName)>
{
    private readonly MmsDbContext _db;
    private readonly ILetterDocxBuilder _builder;
    private readonly IBarQrCodeGenerator _codeGen;
    private readonly ILibreOfficePdfConverter _pdfConverter;
    private readonly ITemplateFileService _fileService;

    public ExportLettersPdfHandler(MmsDbContext db, ILetterDocxBuilder builder,
        IBarQrCodeGenerator codeGen, ILibreOfficePdfConverter pdfConverter,
        ITemplateFileService fileService)
    {
        _db = db;
        _builder = builder;
        _codeGen = codeGen;
        _pdfConverter = pdfConverter;
        _fileService = fileService;
    }

    public async Task<(byte[] FileBytes, string FileName)> Handle(ExportLettersPdfCommand req, CancellationToken ct)
    {
        var letters = await _db.InvitationLetters
            .Where(l => l.MeetingId == req.MeetingId)
            .OrderBy(l => l.ShareholderName)
            .AsNoTracking()
            .ToListAsync(ct);

        // Load meeting + company info
        var meeting = await _db.Meetings
            .Include(m => m.Company)
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == req.MeetingId, ct);

        // Load shareholder IsOrganization data for [9] label
        var shareholderOrgs = await _db.Shareholders
            .Where(s => s.MeetingId == req.MeetingId)
            .Select(s => new { s.IdNumber, s.IsOrganization })
            .AsNoTracking()
            .ToListAsync(ct);
        var orgLookup = shareholderOrgs.ToDictionary(s => s.IdNumber, s => s.IsOrganization);

        // Template lookup: meeting-specific → global → synthetic fallback
        var (templateBytes, selectedTokens) = await FindTemplateAsync(req.MeetingId, ct);

        var dtos = letters.Select(l => new LetterBuildDto(
            l.ShareholderName,
            l.ShareholderAddress ?? "",
            l.ShareholderPhone ?? "",
            l.ShareholderIdNumber,
            l.SharesTotal.ToString("N0"),
            l.TrackingCode)
        {
            NgayHop = meeting?.MeetingDate.ToString("dd/MM/yyyy"),
            GioHop = meeting?.MeetingDate.ToString("HH:mm"),
            DiaDiem = meeting?.Location,
            TenCongTy = meeting?.Company?.Name,
            IsOrganization = orgLookup.TryGetValue(l.ShareholderIdNumber, out var isOrg) && isOrg,
            SelectedTokens = selectedTokens,
        }).ToList();

        byte[] docxBytes;
        if (templateBytes is not null)
        {
            // Build from uploaded template
            var docxParts = new List<byte[]>(dtos.Count);
            foreach (var dto in dtos)
            {
                var codeMark = GenerateCodeMarkForLetter(dto, req.CodeMarkType);
                var letterBytes = _builder.BuildFromTemplate(dto, templateBytes, codeMark, req.CodeMarkType);
                docxParts.Add(letterBytes);
            }
            docxBytes = LetterDocxBuilder.MergeDocxFiles(docxParts);
        }
        else
        {
            // Fallback to synthetic builder
            docxBytes = _builder.BuildMergedDocx(dtos, req.CodeMarkType, _codeGen);
        }

        // Convert DOCX → PDF via LibreOffice headless
        var pdfBytes = await _pdfConverter.ConvertDocxToPdfAsync(docxBytes, ct);
        var fileName = $"ThuMoi_DHCD_{req.MeetingId:N}.pdf";

        return (pdfBytes, fileName);
    }

    private async Task<(byte[]? TemplateBytes, IList<string>? SelectedTokens)> FindTemplateAsync(Guid meetingId, CancellationToken ct)
    {
        var template = await _db.Templates
            .Where(t => t.MeetingId == meetingId
                     && t.TemplateType == Domain.Enums.TemplateType.Invitation
                     && t.IsFinalized)
            .AsNoTracking()
            .FirstOrDefaultAsync(ct);

        template ??= await _db.Templates
            .Where(t => t.MeetingId == null
                     && t.TemplateType == Domain.Enums.TemplateType.Invitation
                     && t.IsFinalized)
            .AsNoTracking()
            .FirstOrDefaultAsync(ct);

        if (template?.FilePath is null) return (null, null);

        var bytes = await _fileService.GetDocxBytesAsync(template.FilePath);
        var tokens = !string.IsNullOrEmpty(template.SelectedTokens)
            ? JsonSerializer.Deserialize<List<string>>(template.SelectedTokens)
            : null;
        return (bytes, tokens);
    }

    private byte[]? GenerateCodeMarkForLetter(LetterBuildDto dto, CodeMarkType type)
    {
        if (type == CodeMarkType.None) return null;
        if (type == CodeMarkType.QRCode)
            return _codeGen.GenerateQrCode(_codeGen.BuildContent(dto.SoDKSH, dto.HoTen));
        var asciiOnly = new string(dto.SoDKSH.Where(c => c <= 127).ToArray());
        return _codeGen.GenerateBarcode(asciiOnly);
    }
}

// ── Import CPN postal report ──
public class ImportCpnReportHandler : IRequestHandler<ImportCpnReportCommand, CpnImportResult>
{
    private readonly MmsDbContext _db;
    private readonly CpnRowMatcher _matcher;
    private readonly ILogger<ImportCpnReportHandler> _logger;

    public ImportCpnReportHandler(MmsDbContext db, CpnRowMatcher matcher, ILogger<ImportCpnReportHandler> logger)
    {
        _db = db;
        _matcher = matcher;
        _logger = logger;
    }

    public async Task<CpnImportResult> Handle(ImportCpnReportCommand req, CancellationToken ct)
    {
        // 1. Parse CPN Excel
        using var workbook = new XLWorkbook(req.FileStream);
        var ws = workbook.Worksheets.First();
        var cpnRows = new List<CpnReportRow>();

        var headerRow = ws.FirstRowUsed()!;
        var colMap = new Dictionary<string, int>();
        foreach (var cell in headerRow.CellsUsed())
        {
            colMap[cell.GetString()?.Trim() ?? ""] = cell.Address.ColumnNumber;
        }

        int nameCol = FindColumn(colMap, req.Mapping.NameColumn);
        int phoneCol = FindColumn(colMap, req.Mapping.PhoneColumn);
        int addrCol = FindColumn(colMap, req.Mapping.AddressColumn);
        int trackCol = FindColumn(colMap, req.Mapping.TrackingCodeColumn);

        foreach (var row in ws.RowsUsed().Skip(1))
        {
            var name = nameCol > 0 ? row.Cell(nameCol).GetString()?.Trim() ?? "" : "";
            if (string.IsNullOrWhiteSpace(name)) continue;

            cpnRows.Add(new CpnReportRow(
                name,
                phoneCol > 0 ? row.Cell(phoneCol).GetString()?.Trim() : null,
                addrCol > 0 ? row.Cell(addrCol).GetString()?.Trim() : null,
                trackCol > 0 ? row.Cell(trackCol).GetString()?.Trim() : null
            ));
        }

        // 2. Get existing letters
        var letters = await _db.InvitationLetters
            .Where(l => l.MeetingId == req.MeetingId)
            .ToListAsync(ct);

        // 3. Match
        var matchResults = _matcher.Match(cpnRows, letters);

        // 4. If not dry run, update matched records
        if (!req.DryRun)
        {
            foreach (var match in matchResults.Where(m => m.InvitationLetterId.HasValue))
            {
                var letter = letters.FirstOrDefault(l => l.Id == match.InvitationLetterId);
                if (letter == null) continue;

                if (!string.IsNullOrWhiteSpace(match.TrackingCode))
                    letter.TrackingCode = match.TrackingCode;

                letter.Status = InvitationStatus.Dispatched;
                letter.DispatchedAt = DateTime.UtcNow;
                letter.StatusUpdatedAt = DateTime.UtcNow;
                letter.UpdatedAtUtc = DateTime.UtcNow;
            }

            await _db.SaveChangesAsync(ct);
            _logger.LogInformation("CPN import: {Matched} matched, {Unmatched} unmatched for meeting {MeetingId}",
                matchResults.Count(r => r.InvitationLetterId.HasValue),
                matchResults.Count(r => !r.InvitationLetterId.HasValue),
                req.MeetingId);
        }

        // 5. Build result
        var details = matchResults.Select(r => new CpnMatchResultDto(
            r.InvitationLetterId, r.CpnName, r.CpnPhone, r.CpnAddress, 
            r.MatchedDbName, r.DbPhone, r.DbAddress,
            r.TrackingCode, r.Tier.ToString(), r.Confidence.ToString()
        )).ToList();

        return new CpnImportResult(
            matchResults.Count(r => r.Confidence == MatchConfidence.High),
            matchResults.Count(r => r.Confidence == MatchConfidence.NoMatch),
            matchResults.Count(r => r.Confidence == MatchConfidence.Low),
            details);
    }

    private static int FindColumn(Dictionary<string, int> colMap, string? colName)
    {
        if (string.IsNullOrWhiteSpace(colName)) return 0;
        return colMap.TryGetValue(colName.Trim(), out var col) ? col : 0;
    }
}

// ── Update letter status ──
public class UpdateLetterStatusHandler : IRequestHandler<UpdateLetterStatusCommand, bool>
{
    private readonly MmsDbContext _db;

    public UpdateLetterStatusHandler(MmsDbContext db) => _db = db;

    public async Task<bool> Handle(UpdateLetterStatusCommand req, CancellationToken ct)
    {
        var letter = await _db.InvitationLetters.FindAsync(new object[] { req.LetterId }, ct);
        if (letter is null) return false;

        letter.Status = req.Status;
        letter.FailureReason = req.FailureReason;
        letter.StatusUpdatedAt = DateTime.UtcNow;
        letter.UpdatedAtUtc = DateTime.UtcNow;

        if (req.Status == InvitationStatus.Dispatched && letter.DispatchedAt is null)
            letter.DispatchedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);
        return true;
    }
}

// ── Get paged letters ──
public class GetLettersHandler : IRequestHandler<GetLettersQuery, GetLettersResult>
{
    private readonly MmsDbContext _db;

    public GetLettersHandler(MmsDbContext db) => _db = db;

    public async Task<GetLettersResult> Handle(GetLettersQuery req, CancellationToken ct)
    {
        var query = _db.InvitationLetters
            .Where(l => l.MeetingId == req.MeetingId)
            .AsNoTracking();

        if (req.StatusFilter.HasValue)
            query = query.Where(l => l.Status == req.StatusFilter.Value);

        if (!string.IsNullOrWhiteSpace(req.SearchTerm))
        {
            var term = req.SearchTerm.Trim().ToLower();
            query = query.Where(l =>
                l.ShareholderName.ToLower().Contains(term) ||
                l.ShareholderIdNumber.ToLower().Contains(term));
        }

        var totalCount = await query.CountAsync(ct);

        var items = await query
            .OrderBy(l => l.ShareholderName)
            .Skip((req.Page - 1) * req.PageSize)
            .Take(req.PageSize)
            .Select(l => new LetterListItem(
                l.Id, l.ShareholderName, l.ShareholderIdNumber,
                l.ShareholderPhone, l.TrackingCode, l.Status,
                l.StatusUpdatedAt, l.FailureReason))
            .ToListAsync(ct);

        return new GetLettersResult(items, totalCount);
    }
}

// ── Get letter stats ──
public class GetLetterStatsHandler : IRequestHandler<GetLetterStatsQuery, LetterStatsDto>
{
    private readonly MmsDbContext _db;

    public GetLetterStatsHandler(MmsDbContext db) => _db = db;

    public async Task<LetterStatsDto> Handle(GetLetterStatsQuery req, CancellationToken ct)
    {
        var counts = await _db.InvitationLetters
            .Where(l => l.MeetingId == req.MeetingId)
            .GroupBy(l => l.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        int Get(InvitationStatus s) => counts.FirstOrDefault(c => c.Status == s)?.Count ?? 0;

        return new LetterStatsDto(
            counts.Sum(c => c.Count),
            Get(InvitationStatus.NotSent),
            Get(InvitationStatus.Dispatched),
            Get(InvitationStatus.Delivered),
            Get(InvitationStatus.Failed),
            Get(InvitationStatus.Returned));
    }
}
