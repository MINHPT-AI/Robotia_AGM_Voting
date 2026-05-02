using MediatR;
using Microsoft.EntityFrameworkCore;
using Mms.Application.Meetings.Commands;
using Mms.Infrastructure.Persistence;

namespace Mms.Infrastructure.Handlers.Meetings;

public class DeleteMeetingHandler : IRequestHandler<DeleteMeetingCommand>
{
    private readonly MmsDbContext _db;
    public DeleteMeetingHandler(MmsDbContext db) => _db = db;

    public async Task Handle(DeleteMeetingCommand cmd, CancellationToken ct)
    {
        var meeting = await _db.Meetings
            .Include(m => m.Shareholders)
            .IgnoreQueryFilters() // bypass IsDeleted filter to find the target
            .FirstOrDefaultAsync(m => m.Id == cmd.Id, ct)
            ?? throw new KeyNotFoundException($"Meeting {cmd.Id} not found");

        if (meeting.Shareholders.Any())
            throw new InvalidOperationException(
                "Không thể xóa cuộc họp đã có danh sách cổ đông. Hãy xóa danh sách cổ đông trước.");

        meeting.IsDeleted = true;
        meeting.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
    }
}
