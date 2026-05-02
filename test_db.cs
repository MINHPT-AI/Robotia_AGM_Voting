using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Mms.Infrastructure.Persistence;

using var db = new MmsDbContext();
var term = "1078025989";
var shs = db.Shareholders.Where(s => s.IdNumber.Contains(term)).ToList();
Console.WriteLine($"Found {shs.Count} Shareholders");
foreach (var s in shs) {
    Console.WriteLine($"- ID: {s.Id}, Name: {s.FullName}, VotingRights: {s.VotingRights}");
    var inProxies = db.Proxies.Where(p => p.GranteeShareholderId == s.Id).ToList();
    Console.WriteLine($"  - Incoming Proxies (ShareholderId): {inProxies.Count}");
}

var recs = db.ProxyRecipients.Where(r => r.IdNumber.Contains(term)).ToList();
Console.WriteLine($"Found {recs.Count} ProxyRecipients");
foreach (var r in recs) {
    Console.WriteLine($"- ID: {r.Id}, Name: {r.FullName}");
    var inProxies = db.Proxies.Where(p => p.GranteeRecipientId == r.Id).ToList();
    Console.WriteLine($"  - Incoming Proxies (RecipientId): {inProxies.Count}");
}
