namespace Mms.Infrastructure.Parsing;

public record VsdcParseResult(
    List<VsdcRawRow> Rows,
    int HeaderRowIndex,
    int DataStartRow,
    int[] ColumnMap);  // 17 entries: columnMap[1]=2, columnMap[2]=3, ...
