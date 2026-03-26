using FluentAssertions;

public class PaginationTests
{
    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    public void PagedResult_ThrowsException_WhenPageSizeIsInvalid(int pageSize)
    {
        var pagedResult = new PagedResult<BaseEntity>([], 10, null, null, pageSize, false);

        Action action = () => _ = pagedResult.TotalPages;
        action.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Theory]
    [InlineData(10, 10, 1)]
    [InlineData(11, 10, 2)]
    [InlineData(19, 10, 2)]
    [InlineData(20, 10, 2)]
    [InlineData(21, 10, 3)]
    [InlineData(0, 10, 0)]
    public void PagedResult_Calculates_TotalPages(
        int totalCount,
        int pageSize,
        int expectedTotalPages
    )
    {
        var pagedResult = new PagedResult<BaseEntity>([], totalCount, null, null, pageSize, false);
        pagedResult.TotalPages.Should().Be(expectedTotalPages);
    }
}
