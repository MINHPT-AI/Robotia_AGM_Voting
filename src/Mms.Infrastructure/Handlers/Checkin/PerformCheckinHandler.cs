using MediatR;
using Microsoft.EntityFrameworkCore;
using Mms.Application.Checkin.Commands;
using Mms.Application.Checkin.Dtos;
using Mms.Application.Common.Interfaces;
using Mms.Domain.Entities;
using Mms.Domain.Enums;
using Mms.Infrastructure.Persistence;

namespace Mms.Infrastructure.Handlers.Checkin;

public class PerformCheckinHandler : IRequestHandler<PerformCheckinCommand, CheckinResultDto>
{
    private readonly MmsDbContext _db;
    private readonly IAuditLogService _audit;

    public PerformCheckinHandler(MmsDbContext db, IAuditLogService audit)
        => (_db, _audit) = (db, audit);

    public async Task<CheckinResultDto> Handle(PerformCheckinCommand cmd, CancellationToken ct)
    {
        // ── 1. Validate meeting ──
        var meeting = await _db.Meetings.FindAsync(new object[] { cmd.MeetingId }, ct)
            ?? throw new InvalidOperationException("Cuộc họp không tồn tại.");

        // ── 2. Validate shareholder ──
        var shareholder = await _db.Shareholders
            .FirstOrDefaultAsync(s => s.Id == cmd.ShareholderId && s.MeetingId == cmd.MeetingId, ct)
            ?? throw new InvalidOperationException("Cổ đông không tồn tại trong cuộc họp.");

        // ── 3. Check RB-04: No duplicate active attendance ──
        var existingActive = await _db.AttendanceRecords
            .AnyAsync(a => a.MeetingId == cmd.MeetingId
                        && a.ShareholderId == cmd.ShareholderId
                        && a.IsActive, ct);
        if (existingActive)
            throw new InvalidOperationException("RB-04: Cổ đông đã check-in và đang có phiên tham dự hoạt động.");

        // ── 4. Calculate shares ──
        var activeStatuses = new[] { ProxyStatus.Pending, ProxyStatus.Confirmed };
        var outgoingProxyShares = await _db.Proxies
            .Where(p => p.MeetingId == cmd.MeetingId
                     && p.GrantorId == cmd.ShareholderId
                     && activeStatuses.Contains(p.Status))
            .SumAsync(p => p.Shares, ct);

        var incomingProxies = await _db.Proxies
            .Where(p => p.MeetingId == cmd.MeetingId
                     && p.GranteeShareholderId == cmd.ShareholderId
                     && activeStatuses.Contains(p.Status))
            .ToListAsync(ct);

        var directShares = Math.Max(0, shareholder.VotingRights - outgoingProxyShares);
        var proxyReceivedShares = incomingProxies.Sum(p => p.Shares);
        var totalRepresenting = directShares + proxyReceivedShares;

        if (totalRepresenting <= 0)
            throw new InvalidOperationException("CĐ không có cổ phần đại diện (đã ủy quyền hết).");

        // ── 5. Determine AttendanceType ──
        AttendanceType attendanceType;
        if (directShares > 0 && proxyReceivedShares == 0) attendanceType = AttendanceType.F1_Direct;
        else if (directShares <= 0 && proxyReceivedShares > 0) attendanceType = AttendanceType.F2_FullProxy;
        else if (directShares > 0 && proxyReceivedShares > 0) attendanceType = AttendanceType.F4_ProxyAndDirect;
        else attendanceType = AttendanceType.F3_Combined;

        // ── 6. Generate AttendCode ──
        var attendCode = await GenerateAttendCode(cmd.MeetingId, ct);

        // ── 7. Resolve phone ──
        var phoneSource = Mms.Domain.Enums.PhoneSource.Manual;
        var phone = cmd.PhoneNumber;
        if (string.IsNullOrWhiteSpace(phone) && !string.IsNullOrWhiteSpace(shareholder.Phone))
        {
            phone = shareholder.Phone;
            phoneSource = Mms.Domain.Enums.PhoneSource.VSDC;
        }

        // ── 8. Create AttendanceRecord ──
        var attendance = new AttendanceRecord
        {
            MeetingId = cmd.MeetingId,
            ShareholderId = cmd.ShareholderId,
            PhysicalAttendeeIdNumber = cmd.PhysicalAttendeeIdNumber,
            PhysicalAttendeeName = cmd.PhysicalAttendeeName,
            AttendanceType = attendanceType,
            AttendCode = attendCode,
            PhoneNumber = phone,
            PhoneSource = string.IsNullOrWhiteSpace(phone) ? null : phoneSource,
            GiftReceived = cmd.GiftReceived,
            GiftReceivedAt = cmd.GiftReceived ? DateTime.UtcNow : null,
            GiftReceivedBy = cmd.GiftReceived ? cmd.OperatorUserId : null,
            CheckedInAt = DateTime.UtcNow,
            PosTerminal = cmd.PosTerminal,
            OperatorUserId = cmd.OperatorUserId,
            IsActive = true,
        };
        _db.AttendanceRecords.Add(attendance);

        // ── 9. Confirm pending proxies ──
        foreach (var proxy in incomingProxies.Where(p => p.Status == ProxyStatus.Pending))
        {
            proxy.Status = ProxyStatus.Confirmed;
            proxy.UpdatedAt = DateTime.UtcNow;
        }

        // ── 10. Create ballot package (4 types) ──
        var ballotsIssued = new List<BallotIssuedDto>();

        // Build proxy representation note
        string? proxyNote = null;
        if (incomingProxies.Count > 0)
        {
            var names = incomingProxies
                .Select(p => $"{p.Grantor?.FullName ?? "CĐ"} ({p.Shares:N0} CP)")
                .ToList();
            proxyNote = $"Đại diện UQ: {string.Join(", ", names)}";
        }

        // Check which ballot types are needed
        var hasHDQTCandidates = await _db.MeetingCandidates
            .AnyAsync(c => c.MeetingId == cmd.MeetingId && c.CandidateBoard == CandidateBoard.HDQT, ct);
        var hasBKSCandidates = await _db.MeetingCandidates
            .AnyAsync(c => c.MeetingId == cmd.MeetingId && c.CandidateBoard == CandidateBoard.BKS, ct);

        var ballotTypes = new List<BallotType> { BallotType.VotingCard, BallotType.VotingBallot };
        if (hasHDQTCandidates) ballotTypes.Add(BallotType.ElectionHDQT);
        if (hasBKSCandidates) ballotTypes.Add(BallotType.ElectionBKS);

        foreach (var ballotType in ballotTypes)
        {
            var ballot = new Ballot
            {
                MeetingId = cmd.MeetingId,
                ShareholderId = cmd.ShareholderId,
                AttendanceRecordId = attendance.Id,
                AttendCode = $"{attendCode}-{ballotType}",
                VotingShares = totalRepresenting,
                DirectShares = directShares,
                ProxyShares = proxyReceivedShares,
                Status = BallotStatus.PendingPrint,
                BallotType = ballotType,
                IsSplitBallot = false,
                ProxyRepresentationNote = proxyNote,
                PosTerminal = cmd.PosTerminal,
                OperatorUserId = cmd.OperatorUserId,
            };
            _db.Ballots.Add(ballot);

            ballotsIssued.Add(new BallotIssuedDto(
                ballot.Id, ballotType, ballot.AttendCode,
                totalRepresenting, null, BallotStatus.PendingPrint));
        }

        await _db.SaveChangesAsync(ct);

        // ── 11. Audit ──
        try
        {
            await _audit.LogAsync(AuditCategory.CheckIn, nameof(AttendanceRecord), attendance.Id,
                $"Check-in: {shareholder.FullName} ({attendanceType}) — {totalRepresenting:N0} CP — {ballotsIssued.Count} phiếu",
                cmd.OperatorUserId, cmd.Actor, cmd.MeetingId, ct);
        }
        catch { }

        return new CheckinResultDto(
            attendance.Id, attendCode, attendanceType,
            totalRepresenting, ballotsIssued);
    }

    private async Task<string> GenerateAttendCode(Guid meetingId, CancellationToken ct)
    {
        var meeting = await _db.Meetings.Include(m => m.Company)
            .FirstAsync(m => m.Id == meetingId, ct);

        var ticker = meeting.Company?.StockCode ?? "AGM";
        var year = meeting.MeetingDate.Year;

        var lastCode = await _db.AttendanceRecords
            .Where(a => a.MeetingId == meetingId)
            .OrderByDescending(a => a.AttendCode)
            .Select(a => a.AttendCode)
            .FirstOrDefaultAsync(ct);

        int nextNum = 1;
        if (lastCode != null)
        {
            var parts = lastCode.Split('-');
            if (parts.Length >= 3 && int.TryParse(parts[^1], out var lastNum))
                nextNum = lastNum + 1;
        }

        return $"{ticker}-{year}-{nextNum:D5}";
    }
}
