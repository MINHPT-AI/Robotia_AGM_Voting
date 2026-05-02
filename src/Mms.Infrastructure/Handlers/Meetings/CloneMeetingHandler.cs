using MediatR;
using Microsoft.EntityFrameworkCore;
using Mms.Application.Meetings.Commands;
using Mms.Domain.Entities;
using Mms.Domain.Enums;
using Mms.Infrastructure.Persistence;

namespace Mms.Infrastructure.Handlers.Meetings;

public class CloneMeetingHandler : IRequestHandler<CloneMeetingCommand, Guid>
{
    private readonly MmsDbContext _db;
    public CloneMeetingHandler(MmsDbContext db) => _db = db;

    public async Task<Guid> Handle(CloneMeetingCommand cmd, CancellationToken ct)
    {
        var source = await _db.Meetings
            .AsNoTracking()
            .Include(m => m.Resolutions)
            .Include(m => m.Candidates)
            .FirstOrDefaultAsync(m => m.Id == cmd.SourceId, ct)
            ?? throw new KeyNotFoundException($"Source meeting {cmd.SourceId} not found");

        var clone = new Meeting
        {
            Id = Guid.NewGuid(),
            CompanyId = source.CompanyId,
            Title = $"{source.Title} (Bản sao)",
            MeetingType = source.MeetingType,
            MeetingDate = source.MeetingDate,
            Location = source.Location,
            RecordDate = source.RecordDate,
            TotalVotingShares = source.TotalVotingShares,
            Chairman = source.Chairman,
            Secretary = source.Secretary,
            Notes = source.Notes,
            Status = MeetingStatus.New,
        };

        foreach (var r in source.Resolutions)
            clone.Resolutions.Add(new MeetingResolution
            {
                Id = Guid.NewGuid(), MeetingId = clone.Id,
                DisplayOrder = r.DisplayOrder, Title = r.Title, Content = r.Content,
            });

        foreach (var c in source.Candidates)
            clone.Candidates.Add(new MeetingCandidate
            {
                Id = Guid.NewGuid(), MeetingId = clone.Id,
                DisplayOrder = c.DisplayOrder, FullName = c.FullName,
                Position = c.Position, CurrentPosition = c.CurrentPosition,
                BirthYear = c.BirthYear, Notes = c.Notes,
            });

        _db.Meetings.Add(clone);
        await _db.SaveChangesAsync(ct);
        return clone.Id;
    }
}
