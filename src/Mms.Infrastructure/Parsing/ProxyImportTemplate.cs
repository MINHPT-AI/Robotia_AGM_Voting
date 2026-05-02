using ClosedXML.Excel;

namespace Mms.Infrastructure.Parsing;

public static class ProxyImportTemplate
{
    /// <summary>
    /// Tạo file Excel mẫu cho import ủy quyền.
    /// </summary>
    public static byte[] Generate()
    {
        using var workbook = new XLWorkbook();
        var ws = workbook.AddWorksheet("Danh sách ủy quyền");

        // Title
        ws.Cell(1, 1).Value = "DANH SÁCH ỦY QUYỀN THAM DỰ ĐẠI HỘI";
        ws.Range("A1:G1").Merge();
        ws.Cell(1, 1).Style
            .Font.SetBold(true)
            .Font.SetFontSize(14)
            .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

        // Headers (row 3)
        var headers = new[] { "STT", "Cổ đông ủy quyền", "Số ĐKSH", "Người nhận ủy quyền", "Số ĐKSH", "Số CP ủy quyền", "SĐT Người nhận" };
        for (int i = 0; i < headers.Length; i++)
        {
            var cell = ws.Cell(3, i + 1);
            cell.Value = headers[i];
            cell.Style
                .Font.SetBold(true)
                .Fill.SetBackgroundColor(XLColor.FromHtml("#1275BC"))
                .Font.SetFontColor(XLColor.White)
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center)
                .Border.SetOutsideBorder(XLBorderStyleValues.Thin);
        }

        // Sample data (row 4-5)
        var sample1 = new object[] { 1, "NGUYỄN VĂN A", "012345678901", "TRẦN VĂN B", "098765432100", 10000, "0901234567" };
        var sample2 = new object[] { 2, "LÊ THỊ C", "036012345678", "PHẠM VĂN D", "052098765432", 5000, "" };

        for (int i = 0; i < sample1.Length; i++)
        {
            ws.Cell(4, i + 1).Value = sample1[i] is int iv ? iv : sample1[i] is long lv ? lv : sample1[i]?.ToString() ?? "";
            ws.Cell(5, i + 1).Value = sample2[i] is int iv2 ? iv2 : sample2[i] is long lv2 ? lv2 : sample2[i]?.ToString() ?? "";
        }

        // Set number format for shares column
        ws.Column(6).Style.NumberFormat.Format = "#,##0";

        // Column widths
        ws.Column(1).Width = 6;
        ws.Column(2).Width = 25;
        ws.Column(3).Width = 18;
        ws.Column(4).Width = 25;
        ws.Column(5).Width = 18;
        ws.Column(6).Width = 18;
        ws.Column(7).Width = 20;

        // Note row
        ws.Cell(7, 1).Value = "Ghi chú:";
        ws.Cell(7, 1).Style.Font.SetBold(true);
        ws.Cell(8, 1).Value = "- Số ĐKSH: Số CMND/CCCD/Hộ chiếu của cổ đông, phải khớp với danh sách VSDC đã import.";
        ws.Range("A8:G8").Merge();
        ws.Cell(9, 1).Value = "- Số CP ủy quyền: Không được vượt quá số CP khả dụng của cổ đông.";
        ws.Range("A9:G9").Merge();
        ws.Cell(10, 1).Value = "- SĐT Người nhận: Không bắt buộc.";
        ws.Range("A10:G10").Merge();

        using var ms = new MemoryStream();
        workbook.SaveAs(ms);
        return ms.ToArray();
    }
}
