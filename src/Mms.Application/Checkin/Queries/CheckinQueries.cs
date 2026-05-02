using MediatR;
using Mms.Application.Checkin.Dtos;

namespace Mms.Application.Checkin.Queries;

/// <summary>Tra cứu + phân loại tình huống tại quầy (F1-F4, MERGE, etc.).</summary>
public record IdentifyCheckinSituationQuery(Guid MeetingId, string SearchTerm)
    : IRequest<CheckinSituationDto?>;

/// <summary>Lấy thống kê Topbar 3 dòng.</summary>
public record GetCheckinTopbarQuery(Guid MeetingId)
    : IRequest<CheckinTopbarDto>;

/// <summary>Lấy hàng đợi phiếu cần in lại.</summary>
public record GetReprintQueueQuery(Guid MeetingId)
    : IRequest<IList<ReprintQueueItemDto>>;

/// <summary>Lấy danh sách phiếu đã phát cho 1 AttendanceRecord.</summary>
public record GetIssuedBallotsQuery(Guid AttendanceRecordId)
    : IRequest<IList<BallotIssuedDto>>;
