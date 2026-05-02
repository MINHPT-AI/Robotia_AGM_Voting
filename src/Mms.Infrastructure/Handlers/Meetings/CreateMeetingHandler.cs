using MediatR;
using Mms.Application.Common.Interfaces;
using Mms.Application.Meetings.Commands;
using Mms.Domain.Entities;
using Mms.Domain.Enums;
using Mms.Infrastructure.Persistence;

namespace Mms.Infrastructure.Handlers.Meetings;

public class CreateMeetingHandler : IRequestHandler<CreateMeetingCommand, Guid>
{
    private readonly MmsDbContext _db;
    private readonly IAuditLogService _audit;

    public CreateMeetingHandler(MmsDbContext db, IAuditLogService audit)
        => (_db, _audit) = (db, audit);

    public async Task<Guid> Handle(CreateMeetingCommand cmd, CancellationToken ct)
    {
        var meeting = new Meeting
        {
            Id = Guid.NewGuid(),
            CompanyId = cmd.CompanyId,
            Title = cmd.Title,
            MeetingType = cmd.MeetingType,
            MeetingDate = cmd.MeetingDate,
            Location = cmd.Location,
            RecordDate = cmd.RecordDate,
            TotalVotingShares = cmd.TotalVotingShares,
            Chairman = cmd.Chairman,
            Secretary = cmd.Secretary,
            Notes = cmd.Notes,
            Status = MeetingStatus.New,
        };

        var rOrder = 1;
        foreach (var r in cmd.Resolutions)
            meeting.Resolutions.Add(new MeetingResolution
            {
                Id = Guid.NewGuid(), MeetingId = meeting.Id,
                DisplayOrder = rOrder++, Title = r.Title, Content = r.Content,
            });

        var cOrder = 1;
        foreach (var c in cmd.Candidates)
            meeting.Candidates.Add(new MeetingCandidate
            {
                Id = Guid.NewGuid(), MeetingId = meeting.Id,
                DisplayOrder = cOrder++, FullName = c.FullName,
                Position = c.Position, CurrentPosition = c.CurrentPosition,
                BirthYear = c.BirthYear, Notes = c.Notes,
            });

        _db.Meetings.Add(meeting);
        await _db.SaveChangesAsync(ct);

        try
        {
            await _audit.LogAsync(AuditCategory.Meeting, nameof(Meeting), meeting.Id,
                $"Meeting created: {meeting.Title}", null, "system", meeting.Id, ct);
        }
        catch { /* audit failure should not block save */ }

        return meeting.Id;
    }
}
