
namespace WarehouseManagement.Api.Validation;

public static class StringValidation
{
    public static bool IsRequiredTextValid(
        string? value,
        int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var trimmedValue = value.Trim();

        return trimmedValue.Length <= maxLength;
    }
}
