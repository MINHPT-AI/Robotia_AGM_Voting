using MediatR;
using Microsoft.EntityFrameworkCore;
using Mms.Application.Checkin.Dtos;
using Mms.Application.Checkin.Queries;
using Mms.Domain.Enums;
using Mms.Domain.Entities;
using Mms.Infrastructure.Persistence;

namespace Mms.Infrastructure.Handlers.Checkin;

public class IdentifyCheckinSituationHandler
    : IRequestHandler<IdentifyCheckinSituationQuery, CheckinSituationDto?>
{
    private readonly MmsDbContext _db;
    public IdentifyCheckinSituationHandler(MmsDbContext db) => _db = db;

    public async Task<CheckinSituationDto?> Handle(
        IdentifyCheckinSituationQuery query, CancellationToken ct)
    {
        var term = query.SearchTerm.Trim().ToLower();

        // Tra cứu CĐ theo QR code, CCCD, hoặc tên
        var shareholder = await _db.Shareholders
            .FirstOrDefaultAsync(s => s.MeetingId == query.MeetingId
                && (s.IdNumber.ToLower() == term
                    || (s.InvestorCode != null && s.InvestorCode.ToLower() == term)
                    || s.FullName.ToLower().Contains(term)), ct);

        if (shareholder is null)
        {
            // Tìm TẤT CẢ các bản ghi bị trùng trong ProxyRecipient (do import nhiều lần)
            var recipients = await _db.ProxyRecipients
                .Where(r => r.IdNumber.ToLower() == term || r.FullName.ToLower().Contains(term))
                .ToListAsync(ct);

            if (recipients.Any())
            {
                var recipientIds = recipients.Select(r => r.Id).ToList();

                var proxiesForMeeting = await _db.Proxies
                    .Where(p => p.MeetingId == query.MeetingId && p.GranteeRecipientId != null && recipientIds.Contains(p.GranteeRecipientId.Value))
                    .ToListAsync(ct);

                if (proxiesForMeeting.Any())
                {
                    var firstRecipient = recipients.First();

                    // Lười tạo (JIT) Shareholder "ảo" (0 CP trực tiếp) để họ có thể check-in như CĐ bình thường
                    shareholder = new Shareholder
                    {
                        MeetingId = query.MeetingId,
                        FullName = firstRecipient.FullName,
                        IdNumber = firstRecipient.IdNumber, // Giữ CCCD làm khóa
                        VotingRights = 0,
                        VsdcRow = null,
                        InvestorCode = null
                    };
                    _db.Shareholders.Add(shareholder);

                    // Cập nhật TẤT CẢ các Proxy thuộc về các recipient trùng lặp này sang Shareholder mới tạo
                    foreach (var p in proxiesForMeeting)
                    {
                        p.GranteeShareholder = shareholder;
                        p.GranteeRecipientId = null;
                    }

                    await _db.SaveChangesAsync(ct);
                }
            }
        }

        if (shareholder is null) return null;

        // Kiểm tra đã check-in chưa
        var existingAttendance = await _db.AttendanceRecords
            .FirstOrDefaultAsync(a => a.MeetingId == query.MeetingId
                && a.ShareholderId == shareholder.Id
                && a.IsActive, ct);

        // Lấy proxy nhận được (incoming)
        var activeStatuses = new[] { ProxyStatus.Pending, ProxyStatus.Confirmed };
        var incomingProxies = await _db.Proxies
            .Where(p => p.MeetingId == query.MeetingId
                     && p.GranteeShareholderId == shareholder.Id
                     && activeStatuses.Contains(p.Status))
            .Select(p => new ProxyInfoDto(p.Id, p.Grantor.FullName, p.Shares, p.Status))
            .ToListAsync(ct);

        // Kiểm tra proxy ủy quyền đi (outgoing)
        var outgoingProxyShares = await _db.Proxies
            .Where(p => p.MeetingId == query.MeetingId
                     && p.GrantorId == shareholder.Id
                     && activeStatuses.Contains(p.Status))
            .SumAsync(p => p.Shares, ct);

        var proxyReceivedShares = incomingProxies.Sum(p => p.Shares);
        var directShares = shareholder.VotingRights - outgoingProxyShares;

        // ── Phân loại tình huống ──
        AttendanceType attendanceType;
        string situationCode;
        string situationLabel;

        if (directShares > 0 && proxyReceivedShares == 0)
        {
            attendanceType = AttendanceType.F1_Direct;
            situationCode = "F1";
            situationLabel = "Cổ đông trực tiếp toàn phần";
        }
        else if (directShares <= 0 && proxyReceivedShares > 0)
        {
            attendanceType = AttendanceType.F2_FullProxy;
            situationCode = "F2";
            situationLabel = "Người nhận ủy quyền (toàn bộ)";
        }
        else if (directShares > 0 && proxyReceivedShares > 0)
        {
            attendanceType = AttendanceType.F4_ProxyAndDirect;
            situationCode = "F4";
            situationLabel = $"CĐ trực tiếp + nhận UQ ({incomingProxies.Count} người)";
        }
        else if (directShares < shareholder.VotingRights && outgoingProxyShares > 0)
        {
            attendanceType = AttendanceType.F3_Combined;
            situationCode = "F3";
            situationLabel = "CĐ giữ một phần — đã ủy quyền một phần";
        }
        else
        {
            // CĐ đã ủy quyền toàn bộ → không thể check-in
            attendanceType = AttendanceType.F2_FullProxy;
            situationCode = "FULLY_PROXIED";
            situationLabel = "⚠ CĐ đã ủy quyền toàn bộ — không thể check-in";
        }

        // ── Kiểm tra trùng ĐKSH (BRD v2.3 Mục 2.3) ──
        DuplicateDkshDto? duplicateInfo = null;
        var hasDuplicate = false;

        var duplicates = await _db.Shareholders
            .Where(s => s.MeetingId == query.MeetingId
                     && s.IdNumber == shareholder.IdNumber
                     && s.Id != shareholder.Id)
            .ToListAsync(ct);

        if (duplicates.Count > 0)
        {
            hasDuplicate = true;
            var dup = duplicates[0];
            duplicateInfo = new DuplicateDkshDto(
                shareholder.Id, shareholder.FullName, shareholder.IdIssueDate, shareholder.VotingRights,
                dup.Id, dup.FullName, dup.IdIssueDate, dup.VotingRights);
        }

        // ── Phone (3 nguồn ưu tiên) ──
        string? phone = null;
        PhoneSource? phoneSource = null;

        if (!string.IsNullOrWhiteSpace(shareholder.Phone))
        {
            phone = shareholder.Phone;
            phoneSource = Mms.Domain.Enums.PhoneSource.VSDC;
        }
        else
        {
            // Tìm trong proxy_recipients
            var recipientPhone = await _db.Proxies
                .Where(p => p.MeetingId == query.MeetingId
                         && p.GranteeShareholderId == shareholder.Id
                         && p.GranteeRecipient != null
                         && p.GranteeRecipient.PhoneNumber != null)
                .Select(p => p.GranteeRecipient!.PhoneNumber)
                .FirstOrDefaultAsync(ct);

            if (!string.IsNullOrWhiteSpace(recipientPhone))
            {
                phone = recipientPhone;
                phoneSource = Mms.Domain.Enums.PhoneSource.ProxyRecipient;
            }
        }

        string? warning = null;
        if (existingAttendance != null)
        {
            situationCode = "ALREADY_CHECKED_IN";
            warning = $"CĐ đã check-in lúc {existingAttendance.CheckedInAt:HH:mm} — Mã: {existingAttendance.AttendCode}";
        }
        if (hasDuplicate)
        {
            warning = "⚠ PHÁT HIỆN 2 TÀI KHOẢN CÙNG SỐ CCCD — Xác nhận MERGE?";
        }

        return new CheckinSituationDto(
            situationCode, situationLabel, warning,
            shareholder.Id, shareholder.FullName, shareholder.IdNumber,
            Math.Max(0, directShares), proxyReceivedShares,
            Math.Max(0, directShares) + proxyReceivedShares,
            attendanceType,
            incomingProxies,
            phone, phoneSource,
            hasDuplicate, duplicateInfo,
            existingAttendance != null,
            existingAttendance?.Id,
            shareholder.VsdcRow);
    }
}
