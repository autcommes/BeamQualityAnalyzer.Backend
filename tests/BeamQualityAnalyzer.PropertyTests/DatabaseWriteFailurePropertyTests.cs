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
    /// 数据库写入失败处理属性测试
    /// Feature: beam-quality-analyzer, Property 33: 数据库写入失败处理
    /// 验证需求: 11.6, 18.5
    /// </summary>
    public class DatabaseWriteFailurePropertyTests
    {
        private readonly ILogger<SqliteDatabaseService> _sqliteLogger;
        private readonly ILogger<JsonFileDatabaseService> _jsonLogger;
        private readonly ILogger<FailoverDatabaseService> _failoverLogger;

        public DatabaseWriteFailurePropertyTests()
        {
            var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.AddConsole();
                builder.SetMinimumLevel(LogLevel.Warning);
            });
            
            _sqliteLogger = loggerFactory.CreateLogger<SqliteDatabaseService>();
            _jsonLogger = loggerFactory.CreateLogger<JsonFileDatabaseService>();
            _failoverLogger = loggerFactory.CreateLogger<FailoverDatabaseService>();
        }

        /// <summary>
        /// 创建唯一的测试数据库路径
        /// </summary>
        private string CreateUniqueDbPath()
        {
            return $"test_write_failure_{Guid.NewGuid()}.db";
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
        /// 属性 33: 数据库写入失败处理 - 失败时提供重试选项
        /// 当数据库写入失败时，系统应该能够捕获异常并提供重试机制。
        /// 通过故障转移服务，失败的写入会自动重试到备用数据库。
        /// </summary>
        [Property(MaxTest = 20, Arbitrary = new[] { typeof(MeasurementGenerators) })]
        public Property DatabaseWriteFailure_AutoRetry_SucceedsWithFallback(ValidMeasurement validMeasurement)
        {
            // Feature: beam-quality-analyzer, Property 33: 数据库写入失败处理
            
            return Prop.ForAll(
                Arb.From(Gen.Constant(validMeasurement)),
                measurement =>
                {
                    var dbPath = CreateUniqueDbPath();
                    var jsonDir = CreateUniqueJsonDirectory();
                    
                    try
                    {
                        // Arrange - 创建一个会失败的主数据库
                        var invalidConnectionString = "Data Source=/invalid/path/database.db";
                        var primaryDatabase = new SqliteDatabaseService(invalidConnectionString, _sqliteLogger);
                        
                        // 创建有效的备用数据库
                        var fallbackDatabase = new JsonFileDatabaseService(jsonDir, _jsonLogger);
                        
                        // 创建故障转移服务（提供自动重试机制）
                        var failoverService = new FailoverDatabaseService(
                            primaryDatabase,
                            fallbackDatabase,
                            _failoverLogger);

                        // Act - 尝试写入（主数据库失败，自动重试到备用数据库）
                        var savedId = failoverService.SaveMeasurementAsync(measurement.Value).Result;

                        // 验证数据已成功保存到备用数据库
                        var retrieved = failoverService.GetMeasurementByIdAsync(savedId).Result;

                        // Assert - 验证重试成功
                        return savedId > 0 && retrieved != null;
                    }
                    finally
                    {
                        CleanupResources(dbPath, jsonDir);
                    }
                });
        }

        /// <summary>
        /// 属性 33.1: 写入失败后数据不丢失
        /// 即使主数据库写入失败，数据也应该通过备用数据库保存，不会丢失
        /// </summary>
        [Property(MaxTest = 20, Arbitrary = new[] { typeof(MeasurementGenerators) })]
        public Property DatabaseWriteFailure_DataNotLost_SavedToFallback(ValidMeasurement validMeasurement)
        {
            // Feature: beam-quality-analyzer, Property 33: 数据库写入失败处理
            
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

                        var originalPointCount = measurement.Value.RawDataPoints.Count;

                        // Act - 写入失败后自动重试
                        var savedId = failoverService.SaveMeasurementAsync(measurement.Value).Result;
                        var retrieved = failoverService.GetMeasurementByIdAsync(savedId).Result;

                        // Assert - 验证数据完整性
                        return retrieved != null &&
                               retrieved.RawDataPoints.Count == originalPointCount &&
                               retrieved.Status == measurement.Value.Status;
                    }
                    finally
                    {
                        CleanupResources(dbPath, jsonDir);
                    }
                });
        }

        /// <summary>
        /// 属性 33.2: 批量写入失败处理
        /// 批量写入失败时，应该能够通过备用数据库完成所有写入
        /// </summary>
        [Property(MaxTest = 10, Arbitrary = new[] { typeof(MeasurementGenerators) })]
        public Property DatabaseWriteFailure_BatchWrite_AllDataSaved(ValidMeasurement validMeasurement)
        {
            // Feature: beam-quality-analyzer, Property 33: 数据库写入失败处理
            
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

                        // Act - 批量写入（主数据库失败，自动重试到备用）
                        var savedIds = failoverService.SaveMeasurementsBatchAsync(measurements).Result;

                        // 验证所有数据都已保存
                        var allSaved = true;
                        foreach (var id in savedIds)
                        {
                            var retrieved = failoverService.GetMeasurementByIdAsync(id).Result;
                            if (retrieved == null)
                            {
                                allSaved = false;
                                break;
                            }
                        }

                        // Assert
                        return savedIds.Count == measurements.Count && allSaved;
                    }
                    finally
                    {
                        CleanupResources(dbPath, jsonDir);
                    }
                });
        }



        /// <summary>
        /// 属性 33.4: 删除操作失败处理
        /// 删除操作失败时也应该有适当的错误处理
        /// </summary>
        [Property(MaxTest = 10, Arbitrary = new[] { typeof(MeasurementGenerators) })]
        public Property DatabaseWriteFailure_DeleteOperation_HandlesFailure(ValidMeasurement validMeasurement)
        {
            // Feature: beam-quality-analyzer, Property 33: 数据库写入失败处理
            
            return Prop.ForAll(
                Arb.From(Gen.Constant(validMeasurement)),
                measurement =>
                {
                    var dbPath = CreateUniqueDbPath();
                    var jsonDir = CreateUniqueJsonDirectory();
                    
                    try
                    {
                        // Arrange - 先保存数据到备用数据库
                        var invalidConnectionString = "Data Source=/invalid/path/database.db";
                        var primaryDatabase = new SqliteDatabaseService(invalidConnectionString, _sqliteLogger);
                        var fallbackDatabase = new JsonFileDatabaseService(jsonDir, _jsonLogger);
                        var failoverService = new FailoverDatabaseService(
                            primaryDatabase,
                            fallbackDatabase,
                            _failoverLogger);

                        var savedId = failoverService.SaveMeasurementAsync(measurement.Value).Result;

                        // Act - 删除操作（应该在备用数据库上成功）
                        failoverService.DeleteMeasurementAsync(savedId).Wait();

                        // 验证数据已删除
                        var retrieved = failoverService.GetMeasurementByIdAsync(savedId).Result;

                        // Assert
                        return retrieved == null;
                    }
                    finally
                    {
                        CleanupResources(dbPath, jsonDir);
                    }
                });
        }

        /// <summary>
        /// 属性 33.5: 查询操作失败处理
        /// 查询操作失败时应该切换到备用数据库
        /// </summary>
        [Property(MaxTest = 10, Arbitrary = new[] { typeof(MeasurementGenerators) })]
        public Property DatabaseWriteFailure_QueryOperation_SwitchesToFallback(ValidMeasurement validMeasurement)
        {
            // Feature: beam-quality-analyzer, Property 33: 数据库写入失败处理
            
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

                        // 先保存数据
                        var savedId = failoverService.SaveMeasurementAsync(measurement.Value).Result;

                        // Act - 查询操作（主数据库失败，应切换到备用）
                        var results = failoverService.QueryMeasurementsAsync(null, null, 0, 10).Result;

                        // Assert
                        return results.Count > 0 && results.Any(m => m.Id == savedId);
                    }
                    finally
                    {
                        CleanupResources(dbPath, jsonDir);
                    }
                });
        }
    }
}
