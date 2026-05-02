using Mms.Domain.Entities;

namespace Mms.Application.InvitationLetters.Dtos;

public record LetterListItem(
    Guid Id,
    string ShareholderName,
    string ShareholderIdNumber,
    string? ShareholderPhone,
    string? TrackingCode,
    InvitationStatus Status,
    DateTime? StatusUpdatedAt,
    string? FailureReason);

public record LetterStatsDto(
    int Total,
    int NotSent,
    int Dispatched,
    int Delivered,
    int Failed,
    int Returned);

public record CpnColumnMapping(
    string? NameColumn,
    string? PhoneColumn,
    string? AddressColumn,
    string? TrackingCodeColumn);

public record CpnImportResult(
    int Matched,
    int Unmatched,
    int LowConfidence,
    IList<CpnMatchResultDto> Details);

public record CpnMatchResultDto(
    Guid? InvitationLetterId,
    string CpnName,
    string? CpnPhone,
    string? CpnAddress,
    string? MatchedDbName,
    string? DbPhone,
    string? DbAddress,
    string? TrackingCode,
    string Tier,
    string Confidence);
