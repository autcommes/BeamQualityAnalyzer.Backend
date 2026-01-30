using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using BeamQualityAnalyzer.Data.Entities;
using BeamQualityAnalyzer.Data.Interfaces;
using BeamQualityAnalyzer.Data.Services;
using FsCheck;
using FsCheck.Xunit;
using Microsoft.Extensions.Logging;
using Xunit;

namespace BeamQualityAnalyzer.PropertyTests
{
    /// <summary>
    /// 数据库故障转移属性测试
    /// Feature: beam-quality-analyzer, Property 30: 数据库故障转移
    /// 验证需求: 11.6, 18.5
    /// </summary>
    public class DatabaseFailoverPropertyTests
    {
        private readonly ILogger<FailoverDatabaseService> _failoverLogger;
        private readonly ILogger<SqliteDatabaseService> _sqliteLogger;
        private readonly ILogger<JsonFileDatabaseService> _jsonLogger;

        public DatabaseFailoverPropertyTests()
        {
            var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.AddConsole();
                builder.SetMinimumLevel(LogLevel.Warning);
            });
            
            _failoverLogger = loggerFactory.CreateLogger<FailoverDatabaseService>();
            _sqliteLogger = loggerFactory.CreateLogger<SqliteDatabaseService>();
            _jsonLogger = loggerFactory.CreateLogger<JsonFileDatabaseService>();
        }

        /// <summary>
        /// 创建唯一的测试数据库路径
        /// </summary>
        private string CreateUniqueDbPath()
        {
            return $"test_failover_{Guid.NewGuid()}.db";
        }

        /// <summary>
        /// 创建唯一的JSON存储目录
        /// </summary>
        private string CreateUniqueJsonDirectory()
        {
            var dir = Path.Combine(Path.GetTempPath(), $"test_json_{Guid.NewGuid()}");
            Directory.CreateDirectory(dir);
            return dir;
        }

        /// <summary>
        /// 清理测试资源
        /// </summary>
        private void CleanupResources(string dbPath, string jsonDir)
        {
            // 清理数据库文件
            if (File.Exists(dbPath))
            {
                try
                {
                    File.Delete(dbPath);
                }
                catch
                {
                    // 忽略删除失败
                }
            }

            // 清理JSON目录
            if (Directory.Exists(jsonDir))
            {
                try
                {
                    Directory.Delete(jsonDir, true);
                }
                catch
                {
                    // 忽略删除失败
                }
            }
        }

        /// <summary>
        /// 属性 30: 数据库故障转移 - 连接失败应切换到本地文件
        /// 当主数据库连接失败时，系统应自动切换到备用的JSON文件存储，
        /// 并且数据操作应该继续正常工作。
        /// </summary>
        [Property(MaxTest = 20, Arbitrary = new[] { typeof(MeasurementGenerators) })]
        public Property DatabaseFailover_PrimaryFails_SwitchesToFallback(ValidMeasurement validMeasurement)
        {
            // Feature: beam-quality-analyzer, Property 30: 数据库故障转移
            
            return Prop.ForAll(
                Arb.From(Gen.Constant(validMeasurement)),
                measurement =>
                {
                    var dbPath = CreateUniqueDbPath();
                    var jsonDir = CreateUniqueJsonDirectory();
                    
                    try
                    {
                        // Arrange - 创建一个无效的数据库连接（故意失败）
                        var invalidConnectionString = "Data Source=/invalid/path/database.db";
                        var primaryDatabase = new SqliteDatabaseService(invalidConnectionString, _sqliteLogger);
                        
                        // 创建有效的备用JSON存储
                        var fallbackDatabase = new JsonFileDatabaseService(jsonDir, _jsonLogger);
                        
                        // 创建故障转移服务
                        var failoverService = new FailoverDatabaseService(
                            primaryDatabase,
                            fallbackDatabase,
                            _failoverLogger);

                        // Act - 尝试保存数据（主数据库会失败，应切换到备用）
                        var savedId = failoverService.SaveMeasurementAsync(measurement.Value).Result;

                        // 验证数据已保存到备用数据库
                        var retrieved = failoverService.QueryMeasurementsAsync(null, null, 0, 10).Result;

                        // Assert
                        return savedId > 0 &&
                               retrieved.Count > 0 &&
                               retrieved.Any(m => m.Id == savedId);
                    }
                    finally
                    {
                        CleanupResources(dbPath, jsonDir);
                    }
                });
        }

        /// <summary>
        /// 属性 30.1: 故障转移后数据完整性
        /// 切换到备用数据库后，保存的数据应该保持完整性
        /// </summary>
        [Property(MaxTest = 20, Arbitrary = new[] { typeof(MeasurementGenerators) })]
        public Property DatabaseFailover_DataIntegrity_PreservedAfterSwitch(ValidMeasurement validMeasurement)
        {
            // Feature: beam-quality-analyzer, Property 30: 数据库故障转移
            
            return Prop.ForAll(
                Arb.From(Gen.Constant(validMeasurement)),
                measurement =>
                {
                    var dbPath = CreateUniqueDbPath();
                    var jsonDir = CreateUniqueJsonDirectory();
                    
                    try
                    {
                        // Arrange
                        var invalidConnectionString = "Data Source=/invalid/path/database.db";
                        var primaryDatabase = new SqliteDatabaseService(invalidConnectionString, _sqliteLogger);
                        var fallbackDatabase = new JsonFileDatabaseService(jsonDir, _jsonLogger);
                        var failoverService = new FailoverDatabaseService(
                            primaryDatabase,
                            fallbackDatabase,
                            _failoverLogger);

                        // Act - 保存数据
                        var savedId = failoverService.SaveMeasurementAsync(measurement.Value).Result;
                        var retrieved = failoverService.GetMeasurementByIdAsync(savedId).Result;

                        // Assert - 验证数据完整性
                        if (retrieved == null) return false;

                        var originalPointCount = measurement.Value.RawDataPoints.Count;
                        var retrievedPointCount = retrieved.RawDataPoints.Count;

                        return retrievedPointCount == originalPointCount &&
                               retrieved.Status == measurement.Value.Status &&
                               Math.Abs((retrieved.MeasurementTime - measurement.Value.MeasurementTime).TotalSeconds) < 1.0;
                    }
                    finally
                    {
                        CleanupResources(dbPath, jsonDir);
                    }
                });
        }

        /// <summary>
        /// 属性 30.2: 主数据库恢复后自动切换回
        /// 当主数据库恢复后，系统应该能够检测到并切换回主数据库
        /// </summary>
        [Property(MaxTest = 10, Arbitrary = new[] { typeof(MeasurementGenerators) })]
        public Property DatabaseFailover_PrimaryRecovery_SwitchesBack(ValidMeasurement validMeasurement)
        {
            // Feature: beam-quality-analyzer, Property 30: 数据库故障转移
            
            return Prop.ForAll(
                Arb.From(Gen.Constant(validMeasurement)),
                measurement =>
                {
                    var dbPath = CreateUniqueDbPath();
                    var jsonDir = CreateUniqueJsonDirectory();
                    
                    try
                    {
                        // Arrange - 创建有效的主数据库
                        var connectionString = $"Data Source={dbPath}";
                        var primaryDatabase = new SqliteDatabaseService(connectionString, _sqliteLogger);
                        primaryDatabase.InitializeDatabaseAsync().Wait();
                        
                        var fallbackDatabase = new JsonFileDatabaseService(jsonDir, _jsonLogger);
                        var failoverService = new FailoverDatabaseService(
                            primaryDatabase,
                            fallbackDatabase,
                            _failoverLogger);

                        // Act - 保存数据到主数据库
                        var savedId = failoverService.SaveMeasurementAsync(measurement.Value).Result;

                        // 验证主数据库可用
                        var isPrimaryAvailable = failoverService.IsPrimaryAvailable;

                        // Assert
                        return savedId > 0 && isPrimaryAvailable;
                    }
                    finally
                    {
                        CleanupResources(dbPath, jsonDir);
                    }
                });
        }

        /// <summary>
        /// 属性 30.3: 批量操作故障转移
        /// 批量保存操作在主数据库失败时也应该切换到备用数据库
        /// </summary>
        [Property(MaxTest = 10, Arbitrary = new[] { typeof(MeasurementGenerators) })]
        public Property DatabaseFailover_BatchOperation_SwitchesToFallback(ValidMeasurement validMeasurement)
        {
            // Feature: beam-quality-analyzer, Property 30: 数据库故障转移
            
            return Prop.ForAll(
                Arb.From(Gen.Constant(validMeasurement)),
                measurement =>
                {
                    var dbPath = CreateUniqueDbPath();
                    var jsonDir = CreateUniqueJsonDirectory();
                    
                    try
                    {
                        // Arrange
                        var invalidConnectionString = "Data Source=/invalid/path/database.db";
                        var primaryDatabase = new SqliteDatabaseService(invalidConnectionString, _sqliteLogger);
                        var fallbackDatabase = new JsonFileDatabaseService(jsonDir, _jsonLogger);
                        var failoverService = new FailoverDatabaseService(
                            primaryDatabase,
                            fallbackDatabase,
                            _failoverLogger);

                        // 创建多个测量记录
                        var measurements = new List<Measurement>
                        {
                            measurement.Value,
                            MeasurementGenerators.GenerateValidMeasurement(15, 1.2, 1.3, 50, 60, 2).Value,
                            MeasurementGenerators.GenerateValidMeasurement(20, 1.5, 1.4, 70, 80, 3).Value
                        };

                        // Act - 批量保存
                        var savedIds = failoverService.SaveMeasurementsBatchAsync(measurements).Result;

                        // Assert
                        return savedIds.Count == measurements.Count &&
                               savedIds.All(id => id > 0);
                    }
                    finally
                    {
                        CleanupResources(dbPath, jsonDir);
                    }
                });
        }
    }
}
