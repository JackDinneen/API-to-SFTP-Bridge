using FluentAssertions;

namespace UnitTests;

public class SanityTests
{
    [Fact]
    public void Sanity_Check_Passes()
    {
        true.Should().BeTrue();
    }
}
