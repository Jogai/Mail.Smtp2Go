namespace Scott.Mail.Smtp2Go.Tests.Shared;

/// <summary>Reads JSON samples from the Fixtures folder copied next to the test assembly.</summary>
public static class Fixture
{
    public static string Read(string relativePath)
    {
        return File.ReadAllText(PathOf(relativePath));
    }

    public static string PathOf(string relativePath)
    {
        return Path.Combine(AppContext.BaseDirectory, "Fixtures", relativePath.Replace('/', Path.DirectorySeparatorChar));
    }
}
