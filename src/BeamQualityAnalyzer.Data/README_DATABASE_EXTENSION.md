# 数据库扩展指南

本文档说明如何为光束质量分析系统添加新的数据库支持（MySQL、SQL Server 或其他数据库）。

## 概述

系统采用工厂模式设计数据库服务层，支持多种数据库类型。当前已实现 SQLite 支持，MySQL 和 SQL Server 为预留接口（占位符）。

## 架构设计

```
IDatabaseService (接口)
    ├─ SqliteDatabaseService (已实现)
    ├─ MySqlDatabaseService (占位符)
    ├─ SqlServerDatabaseService (占位符)
    └─ [您的自定义数据库服务]

DatabaseServiceFactory (工厂类)
    └─ 根据 DatabaseType 创建对应服务实例
```

## 添加新数据库支持的步骤

### 步骤 1：添加数据库类型枚举

编辑 `Enums/DatabaseType.cs`，添加新的数据库类型：

```csharp
public enum DatabaseType
{
    SQLite,
    MySQL,
    SqlServer,
    PostgreSQL,  // 新增
    Oracle       // 新增
}
```

### 步骤 2：安装 NuGet 包

根据目标数据库，安装对应的 Entity Framework Core Provider：

**MySQL**:
```bash
dotnet add package Pomelo.EntityFrameworkCore.MySql --version 8.0.0
```

**SQL Server**:
```bash
dotnet add package Microsoft.EntityFrameworkCore.SqlServer --version 8.0.0
```

**PostgreSQL**:
```bash
dotnet add package Npgsql.EntityFrameworkCore.PostgreSQL --version 8.0.0
```

**Oracle**:
```bash
dotnet add package Oracle.EntityFrameworkCore --version 8.0.0
```

### 步骤 3：实现数据库服务类

参考 `SqliteDatabaseService.cs` 的实现，创建新的数据库服务类。

#### 示例：实现 MySQL 数据库服务

编辑 `Services/MySqlDatabaseService.cs`：

```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BeamQualityAnalyzer.Data.DbContext;
using BeamQualityAnalyzer.Data.Entities;
using BeamQualityAnalyzer.Data.Enums;
using BeamQualityAnalyzer.Data.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BeamQualityAnalyzer.Data.Services
{
    /// <summary>
    /// MySQL 数据库服务实现
    /// </summary>
    public class MySqlDatabaseService : IDatabaseService
    {
        private readonly string _connectionString;
        private readonly ILogger<MySqlDatabaseService> _logger;

        public MySqlDatabaseService(string connectionString, ILogger<MySqlDatabaseService> logger)
        {
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// 创建 DbContext 实例
        /// </summary>
        private BeamAnalyzerDbContext CreateDbContext()
        {
            var optionsBuilder = new DbContextOptionsBuilder<BeamAnalyzerDbContext>();
            
            // 使用 MySQL Provider
            optionsBuilder.UseMySql(
                _connectionString,
                ServerVersion.AutoDetect(_connectionString));
            
            return new BeamAnalyzerDbContext(optionsBuilder.Options);
        }

        // 实现 IDatabaseService 的所有方法
        // 参考 SqliteDatabaseService 的实现
        
        public async Task<int> SaveMeasurementAsync(Measurement measurement)
        {
            if (measurement == null)
                throw new ArgumentNullException(nameof(measurement));

            try
            {
                using var context = CreateDbContext();
                context.Measurements.Add(measurement);
                await context.SaveChangesAsync();

                _logger.LogInformation("测量记录已保存到 MySQL，ID: {MeasurementId}", measurement.Id);
                return measurement.Id;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "保存测量记录到 MySQL 失败");
                throw;
            }
        }

        // ... 实现其他方法 ...

        public DatabaseType GetDatabaseType()
        {
            return DatabaseType.MySQL;
        }
    }
}
```

### 步骤 4：更新工厂类

编辑 `Factories/DatabaseServiceFactory.cs`，添加新数据库类型的创建逻辑：

```csharp
public IDatabaseService CreateDatabaseService(DatabaseType databaseType, string connectionString)
{
    if (string.IsNullOrWhiteSpace(connectionString))
        throw new ArgumentNullException(nameof(connectionString));

    return databaseType switch
    {
        DatabaseType.SQLite => new SqliteDatabaseService(
            connectionString,
            _loggerFactory.CreateLogger<SqliteDatabaseService>()),

        DatabaseType.MySQL => new MySqlDatabaseService(
            connectionString,
            _loggerFactory.CreateLogger<MySqlDatabaseService>()),

        DatabaseType.SqlServer => new SqlServerDatabaseService(
            connectionString,
            _loggerFactory.CreateLogger<SqlServerDatabaseService>()),

        DatabaseType.PostgreSQL => new PostgreSqlDatabaseService(  // 新增
            connectionString,
            _loggerFactory.CreateLogger<PostgreSqlDatabaseService>()),

        _ => throw new ArgumentException($"不支持的数据库类型: {databaseType}")
    };
}
```

同时更新辅助方法：

```csharp
public static bool IsDatabaseTypeImplemented(DatabaseType databaseType)
{
    return databaseType switch
    {
        DatabaseType.SQLite => true,
        DatabaseType.MySQL => true,      // 更新为 true
        DatabaseType.SqlServer => false,
        DatabaseType.PostgreSQL => true, // 新增
        _ => false
    };
}

public static string GetDefaultConnectionStringExample(DatabaseType databaseType)
{
    return databaseType switch
    {
        DatabaseType.SQLite => "Data Source=beam_analyzer.db",
        DatabaseType.MySQL => "Server=localhost;Port=3306;Database=beam_analyzer;User=root;Password=yourpassword;",
        DatabaseType.SqlServer => "Server=localhost;Database=beam_analyzer;User Id=sa;Password=yourpassword;",
        DatabaseType.PostgreSQL => "Host=localhost;Port=5432;Database=beam_analyzer;Username=postgres;Password=yourpassword;",
        _ => throw new ArgumentException($"不支持的数据库类型: {databaseType}")
    };
}

public static string GetRequiredNuGetPackage(DatabaseType databaseType)
{
    return databaseType switch
    {
        DatabaseType.SQLite => "Microsoft.EntityFrameworkCore.Sqlite",
        DatabaseType.MySQL => "Pomelo.EntityFrameworkCore.MySql",
        DatabaseType.SqlServer => "Microsoft.EntityFrameworkCore.SqlServer",
        DatabaseType.PostgreSQL => "Npgsql.EntityFrameworkCore.PostgreSQL",
        _ => throw new ArgumentException($"不支持的数据库类型: {databaseType}")
    };
}
```

### 步骤 5：配置连接字符串

在 `appsettings.json` 中配置数据库连接：

```json
{
  "Database": {
    "DatabaseType": "MySQL",
    "ConnectionString": "Server=localhost;Port=3306;Database=beam_analyzer;User=root;Password=yourpassword;"
  }
}
```

### 步骤 6：注册服务

在 `Program.cs` 中使用工厂创建数据库服务：

```csharp
// 读取配置
var databaseType = Enum.Parse<DatabaseType>(
    builder.Configuration["Database:DatabaseType"] ?? "SQLite");
var connectionString = builder.Configuration["Database:ConnectionString"] 
    ?? "Data Source=beam_analyzer.db";

// 使用工厂创建服务
var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
var databaseFactory = new DatabaseServiceFactory(loggerFactory);
var databaseService = databaseFactory.CreateDatabaseService(databaseType, connectionString);

// 注册为单例
builder.Services.AddSingleton<IDatabaseService>(databaseService);

// 初始化数据库
await databaseService.InitializeDatabaseAsync();
```

### 步骤 7：测试数据库连接

创建测试代码验证数据库服务：

```csharp
[Fact]
public async Task MySqlDatabaseService_TestConnection_ShouldSucceed()
{
    // Arrange
    var connectionString = "Server=localhost;Database=beam_analyzer_test;User=root;Password=test;";
    var logger = new Mock<ILogger<MySqlDatabaseService>>().Object;
    var service = new MySqlDatabaseService(connectionString, logger);

    // Act
    var result = await service.TestConnectionAsync();

    // Assert
    Assert.True(result);
}
```

## 数据库特定注意事项

### MySQL

**连接字符串格式**:
```
Server=localhost;Port=3306;Database=beam_analyzer;User=root;Password=yourpassword;
```

**特殊配置**:
- 使用 `Pomelo.EntityFrameworkCore.MySql` Provider
- 需要指定 `ServerVersion`（推荐使用 `ServerVersion.AutoDetect`）
- 注意字符集配置（推荐 utf8mb4）

**迁移命令**:
```bash
dotnet ef migrations add InitialCreate --context BeamAnalyzerDbContext
dotnet ef database update --context BeamAnalyzerDbContext
```

### SQL Server

**连接字符串格式**:
```
Server=localhost;Database=beam_analyzer;User Id=sa;Password=yourpassword;TrustServerCertificate=True;
```

**特殊配置**:
- 使用 `Microsoft.EntityFrameworkCore.SqlServer` Provider
- 注意 SQL Server 版本兼容性
- 生产环境建议使用 Windows 身份验证

**迁移命令**:
```bash
dotnet ef migrations add InitialCreate --context BeamAnalyzerDbContext
dotnet ef database update --context BeamAnalyzerDbContext
```

### PostgreSQL

**连接字符串格式**:
```
Host=localhost;Port=5432;Database=beam_analyzer;Username=postgres;Password=yourpassword;
```

**特殊配置**:
- 使用 `Npgsql.EntityFrameworkCore.PostgreSQL` Provider
- 注意大小写敏感性
- 推荐使用 Schema 隔离不同环境

## 常见问题

### Q1: 如何切换数据库类型？

修改 `appsettings.json` 中的 `DatabaseType` 和 `ConnectionString` 配置即可。

### Q2: 如何处理数据库迁移？

每个数据库类型需要单独生成迁移文件：

```bash
# SQLite
dotnet ef migrations add InitialCreate --context BeamAnalyzerDbContext -- --provider SQLite

# MySQL
dotnet ef migrations add InitialCreate --context BeamAnalyzerDbContext -- --provider MySQL

# SQL Server
dotnet ef migrations add InitialCreate --context BeamAnalyzerDbContext -- --provider SqlServer
```

### Q3: 如何支持多数据库同时运行？

系统设计为单一数据库模式。如需多数据库支持，需要：
1. 修改 `IDatabaseService` 接口，添加数据库标识
2. 注册多个数据库服务实例
3. 在业务逻辑中根据需求选择对应服务

### Q4: 占位符服务抛出 NotImplementedException 怎么办？

这是预期行为。占位符服务仅用于预留接口，需要按照本文档步骤完整实现后才能使用。

## 性能优化建议

### 连接池配置

**MySQL**:
```
Server=localhost;Database=beam_analyzer;User=root;Password=yourpassword;Pooling=true;MinimumPoolSize=5;MaximumPoolSize=100;
```

**SQL Server**:
```
Server=localhost;Database=beam_analyzer;User Id=sa;Password=yourpassword;Min Pool Size=5;Max Pool Size=100;
```

### 查询优化

1. 为常用查询字段添加索引
2. 使用 `AsNoTracking()` 进行只读查询
3. 分页查询避免一次性加载大量数据
4. 使用 `Include()` 预加载关联数据，避免 N+1 查询

### 批量操作

```csharp
// 批量插入
context.Measurements.AddRange(measurements);
await context.SaveChangesAsync();

// 批量更新（使用 EF Core 7+ 的 ExecuteUpdate）
await context.Measurements
    .Where(m => m.MeasurementTime < cutoffDate)
    .ExecuteUpdateAsync(s => s.SetProperty(m => m.IsArchived, true));
```

## 参考资料

- [Entity Framework Core 文档](https://docs.microsoft.com/ef/core/)
- [Pomelo MySQL Provider](https://github.com/PomeloFoundation/Pomelo.EntityFrameworkCore.MySql)
- [SQL Server Provider](https://docs.microsoft.com/ef/core/providers/sql-server/)
- [PostgreSQL Provider](https://www.npgsql.org/efcore/)

## 联系支持

如有问题，请参考项目主 README 或提交 Issue。
