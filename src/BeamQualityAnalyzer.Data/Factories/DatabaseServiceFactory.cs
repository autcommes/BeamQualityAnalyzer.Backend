using System;
using BeamQualityAnalyzer.Data.Enums;
using BeamQualityAnalyzer.Data.Interfaces;
using BeamQualityAnalyzer.Data.Services;
using Microsoft.Extensions.Logging;

namespace BeamQualityAnalyzer.Data.Factories
{
    /// <summary>
    /// 数据库服务工厂类
    /// </summary>
    public static class DatabaseServiceFactory
    {
        /// <summary>
        /// 创建数据库服务实例
        /// </summary>
        /// <param name="databaseType">数据库类型</param>
        /// <param name="connectionString">连接字符串</param>
        /// <param name="logger">日志记录器</param>
        /// <returns>数据库服务实例</returns>
        public static IDatabaseService Create(DatabaseType databaseType, string connectionString, ILogger logger)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new ArgumentNullException(nameof(connectionString), "连接字符串不能为空");
            }

            return databaseType switch
            {
                DatabaseType.SQLite => new SqliteDatabaseService(connectionString, (ILogger<SqliteDatabaseService>)logger),
                DatabaseType.MySQL => new MySqlDatabaseService(connectionString, (ILogger<MySqlDatabaseService>)logger),
                DatabaseType.SqlServer => new SqlServerDatabaseService(connectionString, (ILogger<SqlServerDatabaseService>)logger),
                _ => throw new ArgumentException($"不支持的数据库类型: {databaseType}", nameof(databaseType))
            };
        }

        /// <summary>
        /// 验证数据库类型是否已实现
        /// </summary>
        /// <param name="databaseType">数据库类型</param>
        /// <returns>如果已实现返回 true，否则返回 false</returns>
        public static bool IsDatabaseTypeImplemented(DatabaseType databaseType)
        {
            return databaseType switch
            {
                DatabaseType.SQLite => true,
                DatabaseType.MySQL => false,
                DatabaseType.SqlServer => false,
                _ => false
            };
        }

        /// <summary>
        /// 获取数据库类型的默认连接字符串示例
        /// </summary>
        /// <param name="databaseType">数据库类型</param>
        /// <returns>连接字符串示例</returns>
        public static string GetDefaultConnectionStringExample(DatabaseType databaseType)
        {
            return databaseType switch
            {
                DatabaseType.SQLite => "Data Source=beam_analyzer.db",
                DatabaseType.MySQL => "Server=localhost;Port=3306;Database=beam_analyzer;User=root;Password=yourpassword;",
                DatabaseType.SqlServer => "Server=localhost;Database=beam_analyzer;User Id=sa;Password=yourpassword;TrustServerCertificate=True;",
                _ => throw new ArgumentException($"不支持的数据库类型: {databaseType}", nameof(databaseType))
            };
        }

        /// <summary>
        /// 获取数据库类型所需的 NuGet 包名称
        /// </summary>
        /// <param name="databaseType">数据库类型</param>
        /// <returns>NuGet 包名称</returns>
        public static string GetRequiredNuGetPackage(DatabaseType databaseType)
        {
            return databaseType switch
            {
                DatabaseType.SQLite => "Microsoft.EntityFrameworkCore.Sqlite",
                DatabaseType.MySQL => "Pomelo.EntityFrameworkCore.MySql",
                DatabaseType.SqlServer => "Microsoft.EntityFrameworkCore.SqlServer",
                _ => throw new ArgumentException($"不支持的数据库类型: {databaseType}", nameof(databaseType))
            };
        }
    }
}
