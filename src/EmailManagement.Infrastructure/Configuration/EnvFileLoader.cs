namespace EmailManagement.Infrastructure.Configuration;

public static class EnvFileLoader
{
    public static string? LoadIfPresent(string? startDirectory = null)
    {
        var dir = startDirectory ?? Directory.GetCurrentDirectory();

        var current = new DirectoryInfo(dir);
        while (current is not null)
        {
            var candidate = Path.Combine(current.FullName, ".env");
            if (File.Exists(candidate))
            {
                DotNetEnv.Env.Load(candidate);
                return candidate;
            }
            current = current.Parent;
        }

        return null;
    }
}