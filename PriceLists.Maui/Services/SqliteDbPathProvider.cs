using System.IO;
using Microsoft.Maui.Storage;

namespace PriceLists.Maui.Services;

public static class SqliteDbPathProvider
{
    private const string DatabaseFileName = "pricelists.db";

    public static string GetDbPath()
    {
        return Path.Combine(FileSystem.AppDataDirectory, DatabaseFileName);
    }
}
