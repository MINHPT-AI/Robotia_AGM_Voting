using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Mms.Domain.Entities;

namespace Mms.Infrastructure.Parsing;

/// <summary>
/// Matches CPN (postal service) report rows to InvitationLetter records using a 5-tier matching algorithm.
/// Priority: TrackingCode → FullName → Phone → Name+Phone → Address prefix.
/// </summary>
public class CpnRowMatcher
{
    public IList<CpnMatchResult> Match(
        IList<CpnReportRow> cpnRows,
        IList<InvitationLetter> letters)
    {
        var results = new List<CpnMatchResult>();

        // Pre-compute normalized lookups
        var byTrackingCode = letters
            .Where(l => !string.IsNullOrWhiteSpace(l.TrackingCode))
            .GroupBy(l => l.TrackingCode!.Trim().ToUpperInvariant())
            .Where(g => g.Count() == 1)
            .ToDictionary(g => g.Key, g => g.First());

        var byNormName = letters
            .GroupBy(l => NormVN(l.ShareholderName))
            .Where(g => g.Count() == 1)
            .ToDictionary(g => g.Key, g => g.First());

        var byPhoneLast9 = letters
            .Where(l => !string.IsNullOrWhiteSpace(l.ShareholderPhone))
            .GroupBy(l => GetPhoneLast9(l.ShareholderPhone))
            .Where(g => g.Count() == 1 && g.Key.Length > 0)
            .ToDictionary(g => g.Key, g => g.First());

        var byNamePhone = letters
            .Where(l => !string.IsNullOrWhiteSpace(l.ShareholderPhone))
            .GroupBy(l => $"{NormVN(l.ShareholderName)}|{GetPhoneLast9(l.ShareholderPhone)}")
            .Where(g => g.Count() == 1)
            .ToDictionary(g => g.Key, g => g.First());

        foreach (var cpn in cpnRows)
        {
            // Tier 1: TrackingCode exact match
            if (!string.IsNullOrWhiteSpace(cpn.TrackingCode))
            {
                var key = cpn.TrackingCode.Trim().ToUpperInvariant();
                if (byTrackingCode.TryGetValue(key, out var match))
                {
                    results.Add(new CpnMatchResult(match.Id, cpn.Name, cpn.Phone, cpn.Address, match.ShareholderName, match.ShareholderPhone, match.ShareholderAddress,
                        cpn.TrackingCode, MatchTier.TrackingCode, MatchConfidence.High));
                    continue;
                }
            }

            // Tier 2: Normalized name unique match
            var normCpnName = NormVN(cpn.Name);
            if (normCpnName.Length > 0 && byNormName.TryGetValue(normCpnName, out var nameMatch))
            {
                results.Add(new CpnMatchResult(nameMatch.Id, cpn.Name, cpn.Phone, cpn.Address, nameMatch.ShareholderName, nameMatch.ShareholderPhone, nameMatch.ShareholderAddress,
                    cpn.TrackingCode, MatchTier.Name, MatchConfidence.High));
                continue;
            }

            // Tier 3: Phone last 9 digits unique match
            var cpnPhone9 = GetPhoneLast9(cpn.Phone);
            if (cpnPhone9.Length > 0 && byPhoneLast9.TryGetValue(cpnPhone9, out var phoneMatch))
            {
                results.Add(new CpnMatchResult(phoneMatch.Id, cpn.Name, cpn.Phone, cpn.Address, phoneMatch.ShareholderName, phoneMatch.ShareholderPhone, phoneMatch.ShareholderAddress,
                    cpn.TrackingCode, MatchTier.Phone, MatchConfidence.High));
                continue;
            }

            // Tier 4: Name + Phone combined
            if (normCpnName.Length > 0 && cpnPhone9.Length > 0)
            {
                var combined = $"{normCpnName}|{cpnPhone9}";
                if (byNamePhone.TryGetValue(combined, out var npMatch))
                {
                    results.Add(new CpnMatchResult(npMatch.Id, cpn.Name, cpn.Phone, cpn.Address, npMatch.ShareholderName, npMatch.ShareholderPhone, npMatch.ShareholderAddress,
                        cpn.TrackingCode, MatchTier.NamePhone, MatchConfidence.High));
                    continue;
                }
            }

            // Tier 5: Address prefix similarity (≥70%)
            if (!string.IsNullOrWhiteSpace(cpn.Address))
            {
                var normAddr = NormVN(cpn.Address);
                InvitationLetter? bestAddrMatch = null;
                double bestSimilarity = 0;

                foreach (var letter in letters)
                {
                    if (string.IsNullOrWhiteSpace(letter.ShareholderAddress)) continue;
                    var sim = ComputeSimilarity(normAddr, NormVN(letter.ShareholderAddress));
                    if (sim >= 0.70 && sim > bestSimilarity)
                    {
                        bestSimilarity = sim;
                        bestAddrMatch = letter;
                    }
                }

                if (bestAddrMatch != null)
                {
                    results.Add(new CpnMatchResult(bestAddrMatch.Id, cpn.Name, cpn.Phone, cpn.Address, bestAddrMatch.ShareholderName, bestAddrMatch.ShareholderPhone, bestAddrMatch.ShareholderAddress,
                        cpn.TrackingCode, MatchTier.Address, MatchConfidence.Low));
                    continue;
                }
            }

            // No match
            results.Add(new CpnMatchResult(null, cpn.Name, cpn.Phone, cpn.Address, null, null, null,
                cpn.TrackingCode, MatchTier.NoMatch, MatchConfidence.NoMatch));
        }

        return results;
    }

    /// <summary>
    /// Removes Vietnamese diacritics, uppercases, and normalizes whitespace.
    /// </summary>
    internal static string NormVN(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return "";
        var d = s.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder();
        foreach (char c in d)
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                sb.Append(c);
        return Regex.Replace(sb.ToString().ToUpperInvariant(), @"\s+", " ").Trim();
    }

    private static string GetPhoneLast9(string? phone)
    {
        if (string.IsNullOrWhiteSpace(phone)) return "";
        var digits = new string(phone.Where(char.IsDigit).ToArray());
        return digits.Length >= 9 ? digits[^9..] : digits;
    }

    /// <summary>
    /// Simple Jaccard-like similarity: shared words / total unique words.
    /// </summary>
    private static double ComputeSimilarity(string a, string b)
    {
        if (a.Length == 0 || b.Length == 0) return 0;
        var wordsA = a.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToHashSet();
        var wordsB = b.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToHashSet();
        var intersection = wordsA.Intersect(wordsB).Count();
        var union = wordsA.Union(wordsB).Count();
        return union == 0 ? 0 : (double)intersection / union;
    }
}

// ── Supporting types ──

public record CpnReportRow(string Name, string? Phone, string? Address, string? TrackingCode);

public record CpnMatchResult(
    Guid? InvitationLetterId,
    string CpnName,
    string? CpnPhone,
    string? CpnAddress,
    string? MatchedDbName,
    string? DbPhone,
    string? DbAddress,
    string? TrackingCode,
    MatchTier Tier,
    MatchConfidence Confidence);

public enum MatchTier { TrackingCode, Name, Phone, NamePhone, Address, NoMatch }
public enum MatchConfidence { High, Low, NoMatch }
