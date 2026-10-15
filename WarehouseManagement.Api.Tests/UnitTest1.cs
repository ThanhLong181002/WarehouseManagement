using WarehouseManagement.Api.Validation;
using Xunit;

namespace WarehouseManagement.Api.Tests;

public class UnitTest1
{
    public static IEnumerable<object[]> GetQuantityTestData =>
        new List<object[]>
        {
            new object[] { 1m, true },
            new object[] { 1.2m, true },
            new object[] { 1.234m, true },
            new object[] { 0.001m, true },
            new object[] { 1.2345m, false },
            new object[] { 0m, false },
            new object[] { -1m, false },
            // Ép kiểu tường minh bằng cách parse từ string để giữ độ chính xác tuyệt đối của decimal
            new object[] { decimal.Parse("999999999999999.999"), true },
            new object[] { decimal.Parse("1000000000000000"), false }
        };

    [Theory]
    [MemberData(nameof(GetQuantityTestData))]
    public void IsValid_ReturnsExpectedResult(
        decimal quantity,
        bool expected)
    {
        var result = QuantityValidator.IsValid(quantity);

        Assert.Equal(expected, result);
    }
}
