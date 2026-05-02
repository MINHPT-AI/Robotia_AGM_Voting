using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Mms.Application.Interfaces;
using Mms.Domain.Entities;
using A = DocumentFormat.OpenXml.Drawing;
using DW = DocumentFormat.OpenXml.Drawing.Wordprocessing;
using PIC = DocumentFormat.OpenXml.Drawing.Pictures;

namespace Mms.Infrastructure.Documents;

/// <summary>
/// Builds DOCX invitation letters from a synthetic A4 template with placeholder replacement and barcode/QR insertion.
/// Layout: A4 C-fold — shareholder info block in first 99mm (visible through envelope window).
/// </summary>
public class LetterDocxBuilder : ILetterDocxBuilder
{
    public byte[] BuildSingleLetterDocx(LetterBuildDto dto, byte[]? codeMarkBytes, CodeMarkType codeMarkType = CodeMarkType.None)
    {
        using var ms = new MemoryStream();
        using (var doc = WordprocessingDocument.Create(ms, WordprocessingDocumentType.Document, true))
        {
            var mainPart = doc.AddMainDocumentPart();
            mainPart.Document = new Document();
            var body = new Body();

            // Page setup: A4, narrow margins to maximize content in first fold
            var sectionProps = new SectionProperties(
                new PageSize { Width = 11906, Height = 16838 },          // A4 in twips
                new PageMargin
                {
                    Top = 720, Bottom = 720, Left = 1080, Right = 1080,  // ~1.27cm sides, ~1.27cm top/bottom
                    Header = 0, Footer = 0
                });

            // ── SECTION 1: Shareholder info block (Y = 0–99mm) ──

            // Company header
            AddParagraph(body, "CỘNG HÒA XÃ HỘI CHỦ NGHĨA VIỆT NAM", bold: true, fontSize: 24, alignment: JustificationValues.Center);
            AddParagraph(body, "Độc lập – Tự do – Hạnh phúc", bold: false, fontSize: 22, alignment: JustificationValues.Center);
            AddParagraph(body, "────────────────────────", alignment: JustificationValues.Center);
            AddParagraph(body, ""); // spacing

            AddParagraph(body, "THÔNG BÁO MỜI HỌP", bold: true, fontSize: 28, alignment: JustificationValues.Center);
            AddParagraph(body, "ĐẠI HỘI ĐỒNG CỔ ĐÔNG", bold: true, fontSize: 24, alignment: JustificationValues.Center);
            AddParagraph(body, "────────────────────────", alignment: JustificationValues.Center);
            AddParagraph(body, ""); // spacing

            // Shareholder info — this block MUST be visible through envelope window
            AddParagraph(body, $"Kính gửi: {dto.HoTen}", bold: true, fontSize: 22);
            AddParagraph(body, $"Địa chỉ: {dto.DiaChi}", fontSize: 20);
            AddParagraph(body, $"Điện thoại: {dto.DienThoai}", fontSize: 20);
            AddParagraph(body, $"Số ĐKSH: {dto.SoDKSH}", fontSize: 20);
            AddParagraph(body, $"Số cổ phiếu: {dto.SoCoPhieu}", fontSize: 20);

            // Insert barcode/QR image if provided
            if (codeMarkBytes is { Length: > 0 })
            {
                InsertImageParagraph(mainPart, body, codeMarkBytes, codeMarkType);
            }

            AddParagraph(body, ""); // padding before fold line

            // ── SECTION 2: Meeting details (Y = 99–198mm) ──
            AddParagraph(body, "Kính thưa Quý Cổ đông,", fontSize: 22);
            AddParagraph(body, "Hội đồng Quản trị Công ty trân trọng kính mời Quý Cổ đông tham dự Đại hội đồng Cổ đông thường niên với các nội dung sau:", fontSize: 20);
            AddParagraph(body, "");
            AddParagraph(body, "1. Thời gian: ...............................", fontSize: 20);
            AddParagraph(body, "2. Địa điểm: ................................", fontSize: 20);
            AddParagraph(body, "3. Nội dung: Theo chương trình đính kèm", fontSize: 20);
            AddParagraph(body, "4. Tài liệu: Đăng tải trên website công ty", fontSize: 20);
            AddParagraph(body, "5. Xác nhận tham dự: Trước ngày ..../.../......", fontSize: 20);

            // ── SECTION 3: Footer (Y = 198–297mm) ──
            AddParagraph(body, "");
            AddParagraph(body, "6. Ủy quyền: Theo mẫu Giấy ủy quyền đính kèm", fontSize: 20);
            AddParagraph(body, "7. Đề nghị Quý Cổ đông mang theo CMND/CCCD khi đến tham dự.", fontSize: 20);
            AddParagraph(body, "");
            AddParagraph(body, "Trân trọng kính mời.", fontSize: 20);
            AddParagraph(body, "");
            AddParagraph(body, "TM. HỘI ĐỒNG QUẢN TRỊ", bold: true, fontSize: 22, alignment: JustificationValues.Right);
            AddParagraph(body, "CHỦ TỊCH", bold: true, fontSize: 22, alignment: JustificationValues.Right);

            body.Append(sectionProps);
            mainPart.Document.Append(body);
            mainPart.Document.Save();
        }

        return ms.ToArray();
    }

    public byte[] BuildMergedDocx(IList<LetterBuildDto> letters, CodeMarkType codeMarkType, IBarQrCodeGenerator codeGen)
    {
        if (letters.Count == 0) return Array.Empty<byte>();

        // Build first letter as the main document
        var firstDto = letters[0];
        var firstCodeMark = GenerateCodeMark(codeGen, firstDto, codeMarkType);
        var mainBytes = BuildSingleLetterDocx(firstDto, firstCodeMark, codeMarkType);

        if (letters.Count == 1) return mainBytes;

        // Merge remaining letters using AltChunk
        using var ms = new MemoryStream();
        ms.Write(mainBytes, 0, mainBytes.Length);
        ms.Position = 0;

        using (var mainDoc = WordprocessingDocument.Open(ms, true))
        {
            var mainPart = mainDoc.MainDocumentPart!;
            var body = mainPart.Document.Body!;

            for (int i = 1; i < letters.Count; i++)
            {
                var dto = letters[i];
                var codeMark = GenerateCodeMark(codeGen, dto, codeMarkType);
                var letterBytes = BuildSingleLetterDocx(dto, codeMark, codeMarkType);

                var altChunkId = $"altChunk{i}";
                var chunk = mainPart.AddAlternativeFormatImportPart(
                    AlternativeFormatImportPartType.WordprocessingML, altChunkId);
                using var chunkStream = new MemoryStream(letterBytes);
                chunk.FeedData(chunkStream);

                // Add page break + AltChunk reference
                var pageBreak = new Paragraph(
                    new Run(new Break { Type = BreakValues.Page }));
                body.AppendChild(pageBreak);

                var altChunkElement = new AltChunk { Id = altChunkId };
                body.AppendChild(altChunkElement);
            }

            mainPart.Document.Save();
        }

        return ms.ToArray();
    }

    private static byte[]? GenerateCodeMark(IBarQrCodeGenerator codeGen, LetterBuildDto dto, CodeMarkType type)
    {
        if (type == CodeMarkType.None) return null;
        if (type == CodeMarkType.QRCode)
            return codeGen.GenerateQrCode(codeGen.BuildContent(dto.SoDKSH, dto.HoTen));
            
        var asciiOnlySoDKSH = new string(dto.SoDKSH.Where(c => c <= 127).ToArray());
        return codeGen.GenerateBarcode(asciiOnlySoDKSH);
    }

    // ── Helpers ──

    private static void AddParagraph(Body body, string text,
        bool bold = false, int fontSize = 20,
        JustificationValues? alignment = null)
    {
        alignment ??= JustificationValues.Left;
        var runProps = new RunProperties();
        if (bold) runProps.Append(new Bold());
        runProps.Append(new FontSize { Val = fontSize.ToString() });
        runProps.Append(new RunFonts { Ascii = "Times New Roman", HighAnsi = "Times New Roman" });

        var paraProps = new ParagraphProperties(
            new Justification { Val = alignment.Value },
            new SpacingBetweenLines { After = "60", Before = "0", Line = "276" });

        var para = new Paragraph(paraProps, new Run(runProps, new Text(text) { Space = SpaceProcessingModeValues.Preserve }));
        body.Append(para);
    }

    private static void InsertImageParagraph(MainDocumentPart mainPart, Body body, byte[] imageBytes, CodeMarkType codeMarkType)
    {
        var imagePart = mainPart.AddImagePart(ImagePartType.Png);
        using (var imgStream = new MemoryStream(imageBytes))
        {
            imagePart.FeedData(imgStream);
        }

        var imagePartId = mainPart.GetIdOfPart(imagePart);

        // Barcode size: 350×70px → EMU (1px = 9525 EMU)
        // QRCode size: 120x120px → EMU
        long widthEmu = (codeMarkType == CodeMarkType.QRCode ? 120 : 350) * 9525;
        long heightEmu = (codeMarkType == CodeMarkType.QRCode ? 120 : 70) * 9525;

        var drawing = new Drawing(
            new DW.Inline(
                new DW.Extent { Cx = widthEmu, Cy = heightEmu },
                new DW.EffectExtent { LeftEdge = 0, TopEdge = 0, RightEdge = 0, BottomEdge = 0 },
                new DW.DocProperties { Id = 1U, Name = "CodeMark" },
                new DW.NonVisualGraphicFrameDrawingProperties(
                    new A.GraphicFrameLocks { NoChangeAspect = true }),
                new A.Graphic(
                    new A.GraphicData(
                        new PIC.Picture(
                            new PIC.NonVisualPictureProperties(
                                new PIC.NonVisualDrawingProperties { Id = 0U, Name = "codemark.png" },
                                new PIC.NonVisualPictureDrawingProperties()),
                            new PIC.BlipFill(
                                new A.Blip { Embed = imagePartId },
                                new A.Stretch(new A.FillRectangle())),
                            new PIC.ShapeProperties(
                                new A.Transform2D(
                                    new A.Offset { X = 0, Y = 0 },
                                    new A.Extents { Cx = widthEmu, Cy = heightEmu }),
                                new A.PresetGeometry(new A.AdjustValueList()) { Preset = A.ShapeTypeValues.Rectangle })
                        )
                    ) { Uri = "http://schemas.openxmlformats.org/drawingml/2006/picture" }
                )
            ) { DistanceFromTop = 0U, DistanceFromBottom = 0U, DistanceFromLeft = 0U, DistanceFromRight = 0U });

        body.Append(new Paragraph(new Run(drawing)));
    }

    /// <summary>
    /// Builds a letter DOCX by performing token find-replace on an uploaded template.
    /// Uses XML surgery — no HTML conversion. Token format: [1], [2], ..., [9].
    /// Handles split-run normalization (Word may split [2] into [ + 2 + ] across runs).
    /// Removes entire lines for unselected tokens (including preceding w:br/).
    /// </summary>
    public byte[] BuildFromTemplate(LetterBuildDto dto, byte[] templateBytes, byte[]? codeMarkBytes, CodeMarkType codeMarkType)
    {
        using var ms = new MemoryStream();
        ms.Write(templateBytes, 0, templateBytes.Length);
        ms.Position = 0;

        using var doc = WordprocessingDocument.Open(ms, true);
        var body = doc.MainDocumentPart!.Document.Body!;

        // Build token → value map using [N] format
        var selectedTokens = dto.SelectedTokens ?? new List<string>();
        var tokenMap = new Dictionary<string, string>();

        // Always replace tokens that are in selectedTokens with actual values
        // Tokens NOT in selectedTokens get a sentinel for line removal
        void AddToken(string token, string value)
        {
            if (selectedTokens.Count == 0 || selectedTokens.Contains(token))
                tokenMap[token] = value;
            else
                tokenMap[token] = RemoveLineSentinel;
        }

        AddToken("[1]", dto.TenCongTy ?? "");
        AddToken("[2]", dto.HoTen);
        AddToken("[3]", dto.SoCoPhieu);
        AddToken("[4]", dto.NgayHop ?? "");
        AddToken("[5]", dto.GioHop ?? "");
        AddToken("[6]", dto.DiaDiem ?? "");
        AddToken("[7]", dto.DiaChi ?? "");
        AddToken("[8]", dto.DienThoai ?? "");

        // [9] special handling: label depends on IsOrganization, empty value → remove line
        if (selectedTokens.Count == 0 || selectedTokens.Contains("[9]"))
        {
            var idValue = dto.SoDKSH;
            if (string.IsNullOrWhiteSpace(idValue))
                tokenMap["[9]"] = RemoveLineSentinel;
            else
                tokenMap["[9]"] = idValue;
        }
        else
        {
            tokenMap["[9]"] = RemoveLineSentinel;
        }

        // Phase 1: Replace tokens in body (with split-run handling)
        ReplaceTokensInBody(body, tokenMap);

        // Phase 2: Remove lines marked with sentinel (unselected/empty tokens)
        RemoveMarkedLines(body);

        // Insert barcode/QR at BARCODE_MARK bookmark if provided
        if (codeMarkBytes is { Length: > 0 })
        {
            InsertImageAtBookmark(doc.MainDocumentPart, body, codeMarkBytes, codeMarkType);
        }

        doc.MainDocumentPart.Document.Save();
        return ms.ToArray();
    }

    // ── Sentinel for line removal ──
    internal const string RemoveLineSentinel = "\u0000__REMOVE_LINE__\u0000";

    /// <summary>
    /// Replaces token placeholders in body text, handling split-run cases
    /// where Word may split [2] into separate runs: "[" + "2" + "]".
    /// </summary>
    internal static void ReplaceTokensInBody(Body body, Dictionary<string, string> tokenMap)
    {
        foreach (var para in body.Descendants<Paragraph>().ToList())
        {
            // Get full text of paragraph to check for tokens
            var fullText = string.Concat(para.Descendants<Text>().Select(t => t.Text));

            // Skip paragraphs without any token
            if (!tokenMap.Keys.Any(token => fullText.Contains(token)))
                continue;

            // First pass: try direct replacement on individual Text elements
            foreach (var textEl in para.Descendants<Text>().ToList())
            {
                foreach (var (token, value) in tokenMap)
                {
                    if (textEl.Text.Contains(token))
                        textEl.Text = textEl.Text.Replace(token, value);
                }
            }

            // Check if any tokens remain after first pass (split-run case)
            var remainingText = string.Concat(para.Descendants<Text>().Select(t => t.Text));
            if (tokenMap.Keys.Any(token => remainingText.Contains(token)))
            {
                MergeRunsAndReplace(para, tokenMap);
            }
        }
    }

    /// <summary>
    /// Handles split-run case: merges all runs in a paragraph into a single run,
    /// performs token replacement on the combined text, then creates a new run.
    /// Preserves RunProperties from the first run.
    /// </summary>
    internal static void MergeRunsAndReplace(Paragraph para, Dictionary<string, string> tokenMap)
    {
        var runs = para.Elements<Run>().ToList();
        if (!runs.Any()) return;

        // Separate runs into groups: those with Break elements (line breaks) and text-only
        // We need to preserve <w:br/> elements while merging text runs
        var elements = new List<(bool IsBreak, Run Run, string Text)>();
        foreach (var run in runs)
        {
            var hasBreak = run.Elements<Break>().Any();
            var text = run.InnerText;
            elements.Add((hasBreak, run, text));
        }

        // Build combined text preserving break positions with a marker
        const string breakMarker = "\u0001BR\u0001";
        var sb = new System.Text.StringBuilder();
        RunProperties? firstRunProps = null;

        foreach (var (isBreak, run, text) in elements)
        {
            if (firstRunProps == null)
            {
                var rp = run.Elements<RunProperties>().FirstOrDefault();
                if (rp != null) firstRunProps = rp.CloneNode(true) as RunProperties;
            }

            if (isBreak)
                sb.Append(breakMarker);
            else
                sb.Append(text);
        }

        var combinedText = sb.ToString();

        // Replace tokens in combined text
        foreach (var (token, value) in tokenMap)
            combinedText = combinedText.Replace(token, value);

        // Remove old runs
        foreach (var run in runs) run.Remove();

        // Rebuild runs from combined text, splitting on break markers
        var parts = combinedText.Split(new[] { breakMarker }, StringSplitOptions.None);
        for (int i = 0; i < parts.Length; i++)
        {
            if (i > 0)
            {
                // Add a break run before this part
                var brRun = new Run();
                if (firstRunProps != null) brRun.AppendChild(firstRunProps.CloneNode(true));
                brRun.AppendChild(new Break());
                para.AppendChild(brRun);
            }

            if (!string.IsNullOrEmpty(parts[i]))
            {
                var textRun = new Run();
                if (firstRunProps != null) textRun.AppendChild(firstRunProps.CloneNode(true));
                textRun.AppendChild(new Text(parts[i]) { Space = SpaceProcessingModeValues.Preserve });
                para.AppendChild(textRun);
            }
        }
    }

    /// <summary>
    /// Removes lines (runs + preceding w:br/) that contain the removal sentinel.
    /// This handles unselected tokens — instead of leaving "Số CCCD: " blank, 
    /// the entire line is removed from the paragraph.
    /// </summary>
    internal static void RemoveMarkedLines(Body body)
    {
        foreach (var para in body.Descendants<Paragraph>().ToList())
        {
            var runs = para.Elements<Run>().ToList();

            for (int i = runs.Count - 1; i >= 0; i--)
            {
                var run = runs[i];
                var texts = run.Elements<Text>().ToList();
                bool hasSentinel = texts.Any(t => t.Text.Contains(RemoveLineSentinel));

                if (!hasSentinel) continue;

                // Remove this run (contains the label + sentinel)
                run.Remove();

                // Remove preceding <w:br/> run if it exists
                if (i > 0)
                {
                    var prevRun = runs[i - 1];
                    var hasBreak = prevRun.Elements<Break>().Any();
                    var hasText = prevRun.Elements<Text>().Any();

                    // Only remove if the previous run is a pure break (no text content)
                    if (hasBreak && !hasText)
                    {
                        prevRun.Remove();
                    }
                }
            }
        }
    }

    /// <summary>
    /// Attempts to insert an image at a bookmark named BARCODE_MARK.
    /// If no bookmark found, appends image as a new paragraph at the end.
    /// </summary>
    private static void InsertImageAtBookmark(MainDocumentPart mainPart, Body body, byte[] imageBytes, CodeMarkType codeMarkType)
    {
        var imagePart = mainPart.AddImagePart(ImagePartType.Png);
        using (var imgStream = new MemoryStream(imageBytes))
        {
            imagePart.FeedData(imgStream);
        }
        var imagePartId = mainPart.GetIdOfPart(imagePart);

        long widthEmu = (codeMarkType == CodeMarkType.QRCode ? 120 : 350) * 9525;
        long heightEmu = (codeMarkType == CodeMarkType.QRCode ? 120 : 70) * 9525;

        var drawing = new Drawing(
            new DW.Inline(
                new DW.Extent { Cx = widthEmu, Cy = heightEmu },
                new DW.EffectExtent { LeftEdge = 0, TopEdge = 0, RightEdge = 0, BottomEdge = 0 },
                new DW.DocProperties { Id = 2U, Name = "CodeMarkTemplate" },
                new DW.NonVisualGraphicFrameDrawingProperties(
                    new A.GraphicFrameLocks { NoChangeAspect = true }),
                new A.Graphic(
                    new A.GraphicData(
                        new PIC.Picture(
                            new PIC.NonVisualPictureProperties(
                                new PIC.NonVisualDrawingProperties { Id = 0U, Name = "codemark.png" },
                                new PIC.NonVisualPictureDrawingProperties()),
                            new PIC.BlipFill(
                                new A.Blip { Embed = imagePartId },
                                new A.Stretch(new A.FillRectangle())),
                            new PIC.ShapeProperties(
                                new A.Transform2D(
                                    new A.Offset { X = 0, Y = 0 },
                                    new A.Extents { Cx = widthEmu, Cy = heightEmu }),
                                new A.PresetGeometry(new A.AdjustValueList()) { Preset = A.ShapeTypeValues.Rectangle })
                        )
                    ) { Uri = "http://schemas.openxmlformats.org/drawingml/2006/picture" }
                )
            ) { DistanceFromTop = 0U, DistanceFromBottom = 0U, DistanceFromLeft = 0U, DistanceFromRight = 0U });

        // Try to find BARCODE_MARK bookmark
        var bookmark = body.Descendants<BookmarkStart>()
            .FirstOrDefault(b => b.Name == "BARCODE_MARK");

        if (bookmark?.Parent is Paragraph bookmarkPara)
        {
            // Insert after the bookmark paragraph
            var imgPara = new Paragraph(new Run(drawing));
            body.InsertAfter(imgPara, bookmarkPara);
        }
        else
        {
            // Fallback: append at end
            body.Append(new Paragraph(new Run(drawing)));
        }
    }

    /// <summary>
    /// Merges multiple single-letter DOCX files into one document using AltChunk.
    /// Each letter gets its own section with a page break separator.
    /// </summary>
    public static byte[] MergeDocxFiles(List<byte[]> docxFiles)
    {
        if (docxFiles.Count == 0) return Array.Empty<byte>();
        if (docxFiles.Count == 1) return docxFiles[0];

        using var ms = new MemoryStream();
        ms.Write(docxFiles[0], 0, docxFiles[0].Length);
        ms.Position = 0;

        using (var mainDoc = WordprocessingDocument.Open(ms, true))
        {
            var mainPart = mainDoc.MainDocumentPart!;
            var body = mainPart.Document.Body!;

            for (int i = 1; i < docxFiles.Count; i++)
            {
                // Page break between letters
                body.AppendChild(new Paragraph(
                    new Run(new Break { Type = BreakValues.Page })));

                // AltChunk embed
                var altChunkId = $"altChunk{i}";
                var chunk = mainPart.AddAlternativeFormatImportPart(
                    AlternativeFormatImportPartType.WordprocessingML, altChunkId);
                using var chunkStream = new MemoryStream(docxFiles[i]);
                chunk.FeedData(chunkStream);
                body.AppendChild(new AltChunk { Id = altChunkId });
            }

            mainPart.Document.Save();
        }

        return ms.ToArray();
    }
}

