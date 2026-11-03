using System;

public static class TestSettings
{
    public static string BaseUrl =>
        Environment.GetEnvironmentVariable("DUMMYJSON_BASE_URL") ?? "https://dummyjson.com";
}