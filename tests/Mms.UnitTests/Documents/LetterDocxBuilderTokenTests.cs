using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using FluentAssertions;
using Mms.Infrastructure.Documents;

namespace Mms.UnitTests.Documents;

/// <summary>
/// Tests for LetterDocxBuilder token replacement, including split-run normalization
/// and unselected token line removal.
/// </summary>
public class LetterDocxBuilderTokenTests
{
    /// <summary>
    /// Verifies that tokens within a single run are replaced correctly.
    /// </summary>
    [Fact]
    public void ReplaceTokensInBody_SingleRun_ReplacesCorrectly()
    {
        // Arrange
        var body = new Body(
            new Paragraph(
                new Run(
                    new Text("Kính gửi: [2]") { Space = SpaceProcessingModeValues.Preserve })));

        var tokenMap = new Dictionary<string, string> { ["[2]"] = "Nguyễn Văn A" };

        // Act
        LetterDocxBuilder.ReplaceTokensInBody(body, tokenMap);

        // Assert
        var result = string.Concat(body.Descendants<Text>().Select(t => t.Text));
        result.Should().Be("Kính gửi: Nguyễn Văn A");
    }

    /// <summary>
    /// Verifies that split-run tokens (Word splitting [2] into "[" + "2" + "]") 
    /// are correctly merged and replaced.
    /// This is the critical test requested by the reviewer.
    /// </summary>
    [Fact]
    public void ReplaceTokensInBody_SplitRun_MergesAndReplaces()
    {
        // Arrange: Word splits [2] into 3 separate runs: "[" + "2" + "]"
        var body = new Body(
            new Paragraph(
                new Run(
                    new RunProperties(new Bold()),
                    new Text("Kính gửi: ") { Space = SpaceProcessingModeValues.Preserve }),
                new Run(
                    new Text("[") { Space = SpaceProcessingModeValues.Preserve }),
                new Run(
                    new Text("2") { Space = SpaceProcessingModeValues.Preserve }),
                new Run(
                    new Text("]") { Space = SpaceProcessingModeValues.Preserve }),
                new Run(
                    new Text(" thân mến") { Space = SpaceProcessingModeValues.Preserve })));

        var tokenMap = new Dictionary<string, string> { ["[2]"] = "Trần Thị B" };

        // Act
        LetterDocxBuilder.ReplaceTokensInBody(body, tokenMap);

        // Assert: token should be replaced even though it was split across runs
        var result = string.Concat(body.Descendants<Text>().Select(t => t.Text));
        result.Should().Be("Kính gửi: Trần Thị B thân mến");
        result.Should().NotContain("[2]");
    }

    /// <summary>
    /// Verifies that multiple tokens in the same paragraph (some split) are all replaced.
    /// </summary>
    [Fact]
    public void ReplaceTokensInBody_MultipleTokens_AllReplaced()
    {
        // Arrange: paragraph with [2] and [7], where [7] is split
        var body = new Body(
            new Paragraph(
                new Run(new Text("[2] - Địa chỉ: ") { Space = SpaceProcessingModeValues.Preserve }),
                new Run(new Text("[") { Space = SpaceProcessingModeValues.Preserve }),
                new Run(new Text("7") { Space = SpaceProcessingModeValues.Preserve }),
                new Run(new Text("]") { Space = SpaceProcessingModeValues.Preserve })));

        var tokenMap = new Dictionary<string, string>
        {
            ["[2]"] = "Lê Văn C",
            ["[7]"] = "123 Hai Bà Trưng, Hà Nội"
        };

        // Act
        LetterDocxBuilder.ReplaceTokensInBody(body, tokenMap);

        // Assert
        var result = string.Concat(body.Descendants<Text>().Select(t => t.Text));
        result.Should().Be("Lê Văn C - Địa chỉ: 123 Hai Bà Trưng, Hà Nội");
    }

    /// <summary>
    /// Verifies that lines marked with RemoveLineSentinel (including preceding w:br/) are removed.
    /// Simulates: Kính gửi: [2]<br/>Địa chỉ: [7]<br/>Số CCCD: [9] — where [9] is unselected.
    /// </summary>
    [Fact]
    public void RemoveMarkedLines_UnselectedToken_RemovesLineAndPrecedingBreak()
    {
        // Arrange: paragraph with line break structure
        var body = new Body(
            new Paragraph(
                new Run(new Text("Kính gửi: Nguyễn Văn A") { Space = SpaceProcessingModeValues.Preserve }),
                new Run(new Break()),                                     // <w:br/>
                new Run(new Text("Địa chỉ: Hà Nội") { Space = SpaceProcessingModeValues.Preserve }),
                new Run(new Break()),                                     // <w:br/>
                new Run(new Text($"Số CCCD: {LetterDocxBuilder.RemoveLineSentinel}") 
                    { Space = SpaceProcessingModeValues.Preserve })));

        // Act
        LetterDocxBuilder.RemoveMarkedLines(body);

        // Assert: sentinel run and preceding break should be removed
        var result = string.Concat(body.Descendants<Text>().Select(t => t.Text));
        result.Should().Be("Kính gửi: Nguyễn Văn AĐịa chỉ: Hà Nội");
        result.Should().NotContain("Số CCCD");
        result.Should().NotContain(LetterDocxBuilder.RemoveLineSentinel);

        // Should have exactly 1 break left (between line 1 and 2)
        body.Descendants<Break>().Count().Should().Be(1);
    }

    /// <summary>
    /// Verifies that paragraphs without any tokens are left untouched.
    /// </summary>
    [Fact]
    public void ReplaceTokensInBody_NoTokens_LeavesUnchanged()
    {
        // Arrange
        var originalText = "Đây là đoạn văn bình thường không có token.";
        var body = new Body(
            new Paragraph(
                new Run(new Text(originalText) { Space = SpaceProcessingModeValues.Preserve })));

        var tokenMap = new Dictionary<string, string> { ["[2]"] = "Test" };

        // Act
        LetterDocxBuilder.ReplaceTokensInBody(body, tokenMap);

        // Assert
        var result = string.Concat(body.Descendants<Text>().Select(t => t.Text));
        result.Should().Be(originalText);
    }

    /// <summary>
    /// Verifies that split-run replacement preserves line breaks (w:br/).
    /// </summary>
    [Fact]
    public void MergeRunsAndReplace_PreservesBreaks()
    {
        // Arrange: paragraph with [2] split across runs AND a line break
        var para = new Paragraph(
            new Run(new Text("Tên: ") { Space = SpaceProcessingModeValues.Preserve }),
            new Run(new Text("[") { Space = SpaceProcessingModeValues.Preserve }),
            new Run(new Text("2") { Space = SpaceProcessingModeValues.Preserve }),
            new Run(new Text("]") { Space = SpaceProcessingModeValues.Preserve }),
            new Run(new Break()),
            new Run(new Text("Dòng 2") { Space = SpaceProcessingModeValues.Preserve }));

        var tokenMap = new Dictionary<string, string> { ["[2]"] = "ABC" };

        // Act
        LetterDocxBuilder.MergeRunsAndReplace(para, tokenMap);

        // Assert: text should be replaced and break preserved
        var text = string.Concat(para.Descendants<Text>().Select(t => t.Text));
        text.Should().Be("Tên: ABCDòng 2");
        para.Descendants<Break>().Count().Should().Be(1);
    }
}
