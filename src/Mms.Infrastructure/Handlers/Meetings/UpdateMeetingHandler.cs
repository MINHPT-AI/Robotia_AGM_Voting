using MediatR;
using Microsoft.EntityFrameworkCore;
using Mms.Application.Common.Interfaces;
using Mms.Application.Meetings.Commands;
using Mms.Domain.Entities;
using Mms.Domain.Enums;
using Mms.Infrastructure.Persistence;

namespace Mms.Infrastructure.Handlers.Meetings;

public class UpdateMeetingHandler : IRequestHandler<UpdateMeetingCommand>
{
    private readonly MmsDbContext _db;
    private readonly IAuditLogService _audit;

    public UpdateMeetingHandler(MmsDbContext db, IAuditLogService audit)
        => (_db, _audit) = (db, audit);

    public async Task Handle(UpdateMeetingCommand cmd, CancellationToken ct)
    {
        var meeting = await _db.Meetings
            .IgnoreQueryFilters() // bypass IsDeleted filter
            .FirstOrDefaultAsync(m => m.Id == cmd.Id, ct)
            ?? throw new KeyNotFoundException($"Meeting {cmd.Id} not found");

        meeting.Title = cmd.Title;
        meeting.MeetingType = cmd.MeetingType;
        meeting.MeetingDate = cmd.MeetingDate;
        meeting.Location = cmd.Location;
        meeting.RecordDate = cmd.RecordDate;
        meeting.TotalVotingShares = cmd.TotalVotingShares;
        meeting.Chairman = cmd.Chairman;
        meeting.Secretary = cmd.Secretary;
        meeting.Notes = cmd.Notes;
        meeting.UpdatedAt = DateTime.UtcNow;

        // Step 1: Delete old children via raw SQL (avoids change tracker conflicts)
        await _db.Database.ExecuteSqlInterpolatedAsync(
            $"DELETE FROM meeting_resolutions WHERE \"MeetingId\" = {cmd.Id}", ct);
        await _db.Database.ExecuteSqlInterpolatedAsync(
            $"DELETE FROM meeting_candidates WHERE \"MeetingId\" = {cmd.Id}", ct);

        // Step 2: Save meeting updates
        await _db.SaveChangesAsync(ct);

        // Step 3: Add new children
        var rOrder = 1;
        foreach (var r in cmd.Resolutions)
            _db.MeetingResolutions.Add(new MeetingResolution
            {
                Id = Guid.NewGuid(), MeetingId = meeting.Id,
                DisplayOrder = rOrder++, Title = r.Title, Content = r.Content,
            });

        var cOrder = 1;
        foreach (var c in cmd.Candidates)
            _db.MeetingCandidates.Add(new MeetingCandidate
            {
                Id = Guid.NewGuid(), MeetingId = meeting.Id,
                DisplayOrder = cOrder++, FullName = c.FullName,
                Position = c.Position, CurrentPosition = c.CurrentPosition,
                BirthYear = c.BirthYear, Notes = c.Notes,
            });

        await _db.SaveChangesAsync(ct);

        try
        {
            await _audit.LogAsync(AuditCategory.Meeting, nameof(Meeting), meeting.Id,
                $"Meeting updated: {meeting.Title}", null, "system", meeting.Id, ct);
        }
        catch { /* audit failure should not block save */ }
    }
}
