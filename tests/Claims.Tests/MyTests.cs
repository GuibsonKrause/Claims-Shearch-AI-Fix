namespace Claims.Tests;

public class MyTests
{
    private static ClaimsRepository Repo() => new(Db.CreateSeeded());

    [Fact]
    public void Search_PartialAndCaseInsensitive()
        => Assert.Equal(new long[] { 101, 103 }, Repo().Search(1, "dental").Select(c => c.Id).OrderBy(x => x));

    [Fact]
    public void Search_ApostropheName_IsFound()
        => Assert.Equal(new long[] { 102 }, Repo().Search(1, "O'Brien").Select(c => c.Id));

    [Theory]
    [InlineData("amount, (SELECT name FROM members)")]
    [InlineData("amount;DROP TABLE claims")]
    [InlineData("submitted_at")]
    [InlineData("")]
    public void Search_BadSortBy_IsRejected(string sortBy)
        => Assert.Throws<ArgumentException>(() => Repo().Search(1, "City", sortBy, "asc"));

    [Fact]
    public void Search_PercentIsLiteral()
        => Assert.Equal(new long[] { 104 }, Repo().Search(1, "100%").Select(c => c.Id));

    [Fact]
    public void Search_NeverReturnsOtherMembers()
    {
        Assert.Empty(Repo().Search(1, "' OR 1=1 --"));
        Assert.DoesNotContain(Repo().Search(1, ""), c => c.Id >= 200);
    }

    [Fact]
    public void Search_BracketIsLiteral()
        => Assert.Empty(Repo().Search(1, "[a-z]"));

    [Fact]
    public void Search_NegativePage_IsRejected()
        => Assert.Throws<ArgumentOutOfRangeException>(() => Repo().Search(1, "dental", "date", "asc", -1));
}
