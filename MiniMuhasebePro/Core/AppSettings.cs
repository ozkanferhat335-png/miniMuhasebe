using System.Configuration;

namespace MiniMuhasebePro.Core
{
    public static class AppSettings
    {
        public static string DatabaseProvider => ConfigurationManager.AppSettings["DatabaseProvider"] ?? "SQLite";
        public static string SqliteConnectionString => ConfigurationManager.AppSettings["SqliteConnectionString"];
        public static string SqlServerConnectionString => ConfigurationManager.AppSettings["SqlServerConnectionString"];
    }
}
