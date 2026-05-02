using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Mms.Domain.Entities;
using Mms.Infrastructure.Identity;

namespace Mms.Infrastructure.Persistence;

public class MmsDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, Guid>
{
    public MmsDbContext(DbContextOptions<MmsDbContext> options) : base(options) { }

    public DbSet<Company> Companies => Set<Company>();
    public DbSet<Meeting> Meetings => Set<Meeting>();
    public DbSet<MeetingResolution> MeetingResolutions => Set<MeetingResolution>();
    public DbSet<MeetingCandidate> MeetingCandidates => Set<MeetingCandidate>();
    public DbSet<Shareholder> Shareholders => Set<Shareholder>();
    public DbSet<Proxy> Proxies => Set<Proxy>();
    public DbSet<Ballot> Ballots => Set<Ballot>();
    public DbSet<Template> Templates => Set<Template>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<InvitationLetter> InvitationLetters => Set<InvitationLetter>();

    // ── Phase Ủy quyền / Check-in / Kiểm phiếu ──
    public DbSet<ProxyRecipient> ProxyRecipients => Set<ProxyRecipient>();
    public DbSet<MeetingTemplateConfig> MeetingTemplateConfigs => Set<MeetingTemplateConfig>();
    public DbSet<AttendanceRecord> AttendanceRecords => Set<AttendanceRecord>();
    public DbSet<BallotGroup> BallotGroups => Set<BallotGroup>();
    public DbSet<AttendanceSnapshot> AttendanceSnapshots => Set<AttendanceSnapshot>();
    public DbSet<VoteResult> VoteResults => Set<VoteResult>();
    public DbSet<ElectionVote> ElectionVotes => Set<ElectionVote>();
    public DbSet<TallySnapshot> TallySnapshots => Set<TallySnapshot>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(typeof(MmsDbContext).Assembly);
    }
}
