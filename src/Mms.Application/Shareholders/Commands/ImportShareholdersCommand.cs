using MediatR;
using Mms.Application.Shareholders.Dtos;

namespace Mms.Application.Shareholders.Commands;

public record ImportShareholdersCommand(
    Guid MeetingId,
    List<ShareholderImportDto> ValidRows
) : IRequest<ImportResultDto>;
