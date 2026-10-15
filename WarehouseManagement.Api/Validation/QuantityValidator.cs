
namespace WarehouseManagement.Api.Validation;

public static class QuantityValidator
{
    public static bool IsValid(decimal quantity)
    {
        const decimal maxQuantity = 999999999999999.999m;

        if (quantity <= 0)
        {
            return false;
        }

        if (quantity > maxQuantity)
        {
            return false;
        }

        if (decimal.Round(quantity, 3) != quantity)
        {
            return false;
        }

        return true;
    }
}
