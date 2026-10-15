
namespace WarehouseManagement.Api.Services;

public static class StockTransactionNumberGenerator
{
    public static string Generate(string transactionType)
    {
        var prefix = transactionType switch
        {
            "StockIn" => "PN",
            "StockOut" => "PX",
            _ => throw new ArgumentException(
                "Unsupported transaction type.",
                nameof(transactionType))
        };

        return $"{prefix}-{DateTime.UtcNow:yyyyMMddHHmmssfff}-" +
               $"{Guid.NewGuid():N}"[..8];
    }
}
