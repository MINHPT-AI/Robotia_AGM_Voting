namespace Mms.Infrastructure.Parsing;

public class VsdcFormatException : Exception
{
    public int? RowIndex { get; }

    public VsdcFormatException(string message, int? rowIndex = null)
        : base(message)
    {
        RowIndex = rowIndex;
    }
}
