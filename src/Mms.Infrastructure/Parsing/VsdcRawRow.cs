namespace Mms.Infrastructure.Parsing;

public record VsdcRawRow(
    int RowIndex,            // Dòng Excel gốc (để debug)
    string?[] Cells,         // 16 giá trị text thô theo column map
    string SectionType,      // "I" (trong nước) hoặc "II" (nước ngoài)
    string SubSectionType);  // "1. Cá nhân" hoặc "2. Tổ chức"
