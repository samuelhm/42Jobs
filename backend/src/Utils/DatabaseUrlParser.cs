namespace src.Utils;

public static class DatabaseUrlParser
{
    public static string ToConnectionString(string databaseUrl)
    {
        var uri = new Uri(databaseUrl);
        var userInfo = uri.UserInfo.Split(':');
        var user = userInfo[0];
        var password = userInfo.Length > 1 ? userInfo[1] : "";
        return $"Host={uri.Host};Port={uri.Port};Database={uri.AbsolutePath.TrimStart('/')};Username={user};Password={password};MinPoolSize=1;MaxPoolSize=20;Keepalive=30;Connection Idle Lifetime=120";
    }
}
