namespace Mms.Infrastructure.Parsing;

public record VsdcValidationResult(
    List<VsdcRowError> Errors,
    List<VsdcRowWarning> Warnings)
{
    public bool HasErrors => Errors.Count > 0;
}
