# 数据库故障转移与数据迁移指南

## 概述

本系统提供了完善的数据库故障转移和数据迁移功能，确保数据安全和系统可靠性。

## 功能特性

### 1. 自动故障转移

当主数据库（SQLite/MySQL/SQL Server）连接失败时，系统会自动切换到本地 JSON 文件存储，确保数据不丢失。

### 2. 数据迁移

支持在不同数据库之间迁移数据：
- JSON 文件 → SQLite/MySQL/SQL Server
- SQLite → MySQL/SQL Server
- MySQL → SQLite/SQL Server
- SQL Server → SQLite/MySQL

### 3. 数据同步

当主数据库恢复后，可以将 JSON 文件中的数据同步回主数据库。

## 使用场景

### 场景 1：SQLite 连接失败自动切换到 JSON

```csharp
// 配置故障转移服务
var primaryDatabase = new SqliteDatabaseService(connectionString, logger);
var fallbackDatabase = new JsonFileDatabaseService("./fallback_data", logger);
var failoverService = new FailoverDatabaseService(primaryDatabase, fallbackDatabase, logger);

// 使用故障转移服务（自动处理故障）
await failoverService.SaveMeasurementAsync(measurement);

// 检查当前使用的数据库
if (!failoverService.IsPrimaryAvailable)
{
    Console.WriteLine("当前使用备用 JSON 文件存储");
}
```

### 场景 2：从 JSON 文件导入到 SQLite

```csharp
// 创建迁移服务
var migrationService = new DatabaseMigrationService(logger);

// 创建目标数据库服务
var targetDatabase = new SqliteDatabaseService("Data Source=beam_analyzer.db", logger);

// 从 JSON 文件导入
var result = await migrationService.ImportFromJsonAsync(
    "./fallback_data/measurements.json",
    targetDatabase);

if (result.Success)
{
    Console.WriteLine($"成功导入 {result.MigratedRecords} 条记录");
}
else
{
    Console.WriteLine($"导入失败：{result.ErrorMessage}");
}
```

### 场景 3：从 JSON 文件导入到 MySQL

```csharp
// 创建 MySQL 数据库服务（需要先实现 MySqlDatabaseService）
var mysqlDatabase = new MySqlDatabaseService(
    "Server=localhost;Database=beam_analyzer;User=root;Password=***;",
    logger);

// 从 JSON 文件导入到 MySQL
var result = await migrationService.ImportFromJsonAsync(
    "./fallback_data/measurements.json",
    mysqlDatabase);

Console.WriteLine($"迁移结果：{result.MigratedRecords}/{result.TotalRecords} 条记录");
Console.WriteLine($"成功率：{result.SuccessRate:F1}%");
Console.WriteLine($"耗时：{result.Duration}");
```

### 场景 4：SQLite 迁移到 MySQL

```csharp
var sqliteDatabase = new SqliteDatabaseService("Data Source=beam_analyzer.db", logger);
var mysqlDatabase = new MySqlDatabaseService("Server=localhost;Database=beam_analyzer;...", logger);

// 执行迁移
var result = await migrationService.MigrateDataAsync(
    sqliteDatabase,
    mysqlDatabase,
    batchSize: 100);

// 验证迁移结果
var isValid = await migrationService.ValidateMigrationAsync(
    sqliteDatabase,
    mysqlDatabase);

if (isValid)
{
    Console.WriteLine("迁移验证通过");
}
```

### 场景 5：增量迁移（只迁移指定时间范围的数据）

```csharp
// 只迁移最近 7 天的数据
var startTime = DateTime.Now.AddDays(-7);
var endTime = DateTime.Now;

var result = await migrationService.MigrateDataAsync(
    sourceDatabase,
    targetDatabase,
    startTime: startTime,
    endTime: endTime,
    batchSize: 50);
```

### 场景 6：导出数据库到 JSON 文件（备份）

```csharp
var sqliteDatabase = new SqliteDatabaseService("Data Source=beam_analyzer.db", logger);

// 导出到 JSON 文件
var result = await migrationService.ExportToJsonAsync(
    sqliteDatabase,
    "./backup/measurements_backup.json");

Console.WriteLine($"成功导出 {result.MigratedRecords} 条记录到 JSON 文件");
```

### 场景 7：主数据库恢复后同步数据

```csharp
var failoverService = new FailoverDatabaseService(primaryDatabase, fallbackDatabase, logger);

// 检查主数据库是否恢复
var isPrimaryAvailable = await failoverService.CheckPrimaryConnectionAsync();

if (isPrimaryAvailable)
{
    // 同步备用数据库的数据到主数据库
    var syncedCount = await failoverService.SyncFallbackToPrimaryAsync();
    Console.WriteLine($"成功同步 {syncedCount} 条记录到主数据库");
}
```

## 配置示例

### appsettings.json

```json
{
  "Database": {
    "DatabaseType": "SQLite",
    "ConnectionString": "Data Source=beam_analyzer.db",
    "EnableFailover": true,
    "FallbackDataDirectory": "./fallback_data",
    "RetryInterval": "00:05:00"
  }
}
```

### Program.cs 配置

```csharp
// 注册数据库服务
builder.Services.AddSingleton<IDatabaseService>(sp =>
{
    var logger = sp.GetRequiredService<ILogger<SqliteDatabaseService>>();
    var config = sp.GetRequiredService<IConfiguration>();
    
    var connectionString = config["Database:ConnectionString"];
    var enableFailover = config.GetValue<bool>("Database:EnableFailover");
    
    var primaryDatabase = new SqliteDatabaseService(connectionString, logger);
    
    if (enableFailover)
    {
        var fallbackLogger = sp.GetRequiredService<ILogger<JsonFileDatabaseService>>();
        var failoverLogger = sp.GetRequiredService<ILogger<FailoverDatabaseService>>();
        var fallbackDirectory = config["Database:FallbackDataDirectory"] ?? "./fallback_data";
        
        var fallbackDatabase = new JsonFileDatabaseService(fallbackDirectory, fallbackLogger);
        return new FailoverDatabaseService(primaryDatabase, fallbackDatabase, failoverLogger);
    }
    
    return primaryDatabase;
});

// 注册迁移服务
builder.Services.AddSingleton<DatabaseMigrationService>();
```

## 命令行工具示例

可以创建一个命令行工具来执行数据迁移：

```bash
# 从 JSON 导入到 SQLite
dotnet run -- migrate --source json --source-path ./fallback_data --target sqlite --target-conn "Data Source=beam_analyzer.db"

# 从 SQLite 导出到 JSON
dotnet run -- export --source sqlite --source-conn "Data Source=beam_analyzer.db" --target-path ./backup/data.json

# 从 SQLite 迁移到 MySQL
dotnet run -- migrate --source sqlite --source-conn "Data Source=beam_analyzer.db" --target mysql --target-conn "Server=localhost;Database=beam_analyzer;User=root;Password=***;"

# 验证迁移结果
dotnet run -- validate --source sqlite --source-conn "..." --target mysql --target-conn "..."
```

## 最佳实践

### 1. 定期备份

建议定期将数据库导出到 JSON 文件作为备份：

```csharp
// 每天凌晨 2 点执行备份
var timer = new Timer(async _ =>
{
    var backupPath = $"./backup/measurements_{DateTime.Now:yyyyMMdd}.json";
    await migrationService.ExportToJsonAsync(database, backupPath);
}, null, TimeSpan.Zero, TimeSpan.FromDays(1));
```

### 2. 监控故障转移状态

```csharp
// 定期检查主数据库状态
var timer = new Timer(async _ =>
{
    if (!failoverService.IsPrimaryAvailable)
    {
        var isRecovered = await failoverService.CheckPrimaryConnectionAsync();
        if (isRecovered)
        {
            // 主数据库已恢复，同步数据
            await failoverService.SyncFallbackToPrimaryAsync();
        }
    }
}, null, TimeSpan.Zero, TimeSpan.FromMinutes(5));
```

### 3. 批量处理大数据

迁移大量数据时，使用合适的批量大小：

```csharp
// 小数据量（< 1000 条）：批量大小 100
// 中等数据量（1000-10000 条）：批量大小 500
// 大数据量（> 10000 条）：批量大小 1000

var batchSize = totalRecords switch
{
    < 1000 => 100,
    < 10000 => 500,
    _ => 1000
};

var result = await migrationService.MigrateDataAsync(
    sourceDatabase,
    targetDatabase,
    batchSize: batchSize);
```

### 4. 错误处理和重试

```csharp
var maxRetries = 3;
var retryCount = 0;
MigrationResult? result = null;

while (retryCount < maxRetries)
{
    result = await migrationService.MigrateDataAsync(sourceDatabase, targetDatabase);
    
    if (result.Success)
    {
        break;
    }
    
    retryCount++;
    Console.WriteLine($"迁移失败，重试 {retryCount}/{maxRetries}...");
    await Task.Delay(TimeSpan.FromSeconds(5));
}

if (result?.Success == true)
{
    Console.WriteLine("迁移成功");
}
else
{
    Console.WriteLine($"迁移失败：{result?.ErrorMessage}");
}
```

## 注意事项

1. **ID 重置**：迁移时会重置所有记录的 ID，让目标数据库自动分配新 ID
2. **关联关系**：系统会自动处理实体之间的关联关系（Measurement、RawDataPoint、CalculationResult、FitParameter）
3. **事务支持**：批量保存使用事务，确保数据一致性
4. **性能考虑**：大数据量迁移时建议在非业务高峰期执行
5. **磁盘空间**：JSON 文件会占用较多磁盘空间，注意监控磁盘使用情况

## 故障排查

### 问题 1：JSON 文件损坏

```csharp
// 尝试修复 JSON 文件
try
{
    var json = File.ReadAllText("measurements.json");
    var measurements = JsonSerializer.Deserialize<List<Measurement>>(json);
    // 重新保存
    var newJson = JsonSerializer.Serialize(measurements, new JsonSerializerOptions { WriteIndented = true });
    File.WriteAllText("measurements_fixed.json", newJson);
}
catch (Exception ex)
{
    Console.WriteLine($"JSON 文件损坏：{ex.Message}");
}
```

### 问题 2：迁移中断

```csharp
// 查看迁移结果，找出失败的批次
if (!result.Success && result.FailedBatches.Count > 0)
{
    Console.WriteLine($"失败的批次：{string.Join(", ", result.FailedBatches)}");
    
    // 可以针对失败的批次重新迁移
    foreach (var batchIndex in result.FailedBatches)
    {
        var measurements = await sourceDatabase.QueryMeasurementsAsync(
            null, null, batchIndex, batchSize);
        await targetDatabase.SaveMeasurementsBatchAsync(measurements);
    }
}
```

### 问题 3：主数据库无法恢复

```csharp
// 如果主数据库长期无法恢复，可以将 JSON 文件作为新的主数据库
// 或者迁移到另一个数据库类型

// 方案 1：使用 JSON 文件作为主数据库
var jsonDatabase = new JsonFileDatabaseService("./data", logger);

// 方案 2：迁移到 MySQL
var mysqlDatabase = new MySqlDatabaseService(connectionString, logger);
await migrationService.ImportFromJsonAsync("./fallback_data/measurements.json", mysqlDatabase);
```

## 总结

通过故障转移和数据迁移功能，系统可以：

1. ✅ 自动处理数据库连接失败，切换到 JSON 文件存储
2. ✅ 主数据库恢复后自动同步数据
3. ✅ 支持在不同数据库之间迁移数据
4. ✅ 支持数据备份和恢复
5. ✅ 支持增量迁移和批量处理
6. ✅ 提供详细的迁移结果和验证功能

这确保了数据的安全性和系统的可靠性！
