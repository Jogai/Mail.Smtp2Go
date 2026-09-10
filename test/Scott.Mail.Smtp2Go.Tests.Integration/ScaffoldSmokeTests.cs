namespace Scott.Mail.Smtp2Go.Tests.Integration;

/// <summary>Proves the project is discovered and run by Microsoft.Testing.Platform. Replaced by real tests from plan 02 onwards.</summary>
public class ScaffoldSmokeTests
{
    [Fact]
    public void Test_runner_discovers_this_project() => typeof(ScaffoldSmokeTests).Assembly.GetName().Name.Should().Be("Scott.Mail.Smtp2Go.Tests.Integration");
}
