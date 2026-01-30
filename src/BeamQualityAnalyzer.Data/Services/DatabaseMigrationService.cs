using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BeamQualityAnalyzer.Data.Entities;
using BeamQualityAnalyzer.Data.Interfaces;
using Microsoft.Extensions.Logging;

namespace BeamQualityAnalyzer.Data.Services
{
    /// <summary>
    /// 数据库迁移服务
    /// </summary>
    /// <remarks>
    /// 提供数据库之间的数据迁移功能，支持：
    /// 1. JSON 文件 → SQLite/MySQL/SQL Server
    /// 2. SQLite → MySQL/SQL Server
    /// 3. MySQL → SQLite/SQL Server
    /// 4. SQL Server → SQLite/MySQL
    /// </remarks>
    public class DatabaseMigrationService
    {
        private readonly ILogger<DatabaseMigrationService> _logger;

        /// <summary>
        /// 构造函数
        /// </summary>
        public DatabaseMigrationService(ILogger<DatabaseMigrationService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// 迁移数据从源数据库到目标数据库
        /// </summary>
        /// <param name="sourceDatabase">源数据库服务</param>
        /// <param name="targetDatabase">目标数据库服务</param>
        /// <param name="startTime">开始时间（可选，用于增量迁移）</param>
        /// <param name="endTime">结束时间（可选）</param>
        /// <param name="batchSize">批量处理大小</param>
        /// <returns>迁移结果</returns>
        public async Task<MigrationResult> MigrateDataAsync(
            IDatabaseService sourceDatabase,
            IDatabaseService targetDatabase,
            DateTime? startTime = null,
            DateTime? endTime = null,
            int batchSize = 100)
        {
            if (sourceDatabase == null)
            {
                throw new ArgumentNullException(nameof(sourceDatabase));
            }

            if (targetDatabase == null)
            {
                throw new ArgumentNullException(nameof(targetDatabase));
            }

            var result = new MigrationResult
            {
                StartTime = DateTime.Now,
                SourceDatabaseType = sourceDatabase.GetDatabaseType(),
                TargetDatabaseType = targetDatabase.GetDatabaseType()
            };

            try
            {
                _logger.LogInformation(
                    "开始数据迁移：{Source} → {Target}",
                    result.SourceDatabaseType,
                    result.TargetDatabaseType);

                // 1. 测试源数据库连接
                var sourceConnected = await sourceDatabase.TestConnectionAsync();
                if (!sourceConnected)
                {
                    result.Success = false;
                    result.ErrorMessage = "源数据库连接失败";
                    _logger.LogError(result.ErrorMessage);
                    return result;
                }

                // 2. 测试目标数据库连接
                var targetConnected = await targetDatabase.TestConnectionAsync();
                if (!targetConnected)
                {
                    result.Success = false;
                    result.ErrorMessage = "目标数据库连接失败";
                    _logger.LogError(result.ErrorMessage);
                    return result;
                }

                // 3. 获取源数据库中的记录总数
                var totalCount = await sourceDatabase.GetMeasurementCountAsync(startTime, endTime);
                result.TotalRecords = totalCount;

                if (totalCount == 0)
                {
                    _logger.LogInformation("源数据库中没有数据需要迁移");
                    result.Success = true;
                    result.EndTime = DateTime.Now;
                    return result;
                }

                _logger.LogInformation("源数据库中共有 {Count} 条记录需要迁移", totalCount);

                // 4. 分批迁移数据
                var pageIndex = 0;
                var migratedCount = 0;

                while (migratedCount < totalCount)
                {
                    // 查询一批数据
                    var measurements = await sourceDatabase.QueryMeasurementsAsync(
                        startTime,
                        endTime,
                        pageIndex,
                        batchSize);

                    if (measurements.Count == 0)
                    {
                        break;
                    }

                    try
                    {
                        // 重置ID，让目标数据库自动分配新ID
                        foreach (var measurement in measurements)
                        {
                            measurement.Id = 0;
                            
                            // 重置关联实体的ID
                            if (measurement.RawDataPoints != null)
                            {
                                foreach (var point in measurement.RawDataPoints)
                                {
                                    point.Id = 0;
                                    point.MeasurementId = 0;
                                }
                            }

                            if (measurement.CalculationResult != null)
                            {
                                measurement.CalculationResult.Id = 0;
                                measurement.CalculationResult.MeasurementId = 0;

                                if (measurement.CalculationResult.FitParameters != null)
                                {
                                    foreach (var param in measurement.CalculationResult.FitParameters)
                                    {
                                        param.Id = 0;
                                        param.CalculationResultId = 0;
                                    }
                                }
                            }
                        }

                        // 批量保存到目标数据库
                        var savedIds = await targetDatabase.SaveMeasurementsBatchAsync(measurements);
                        
                        migratedCount += savedIds.Count;
                        result.MigratedRecords = migratedCount;

                        _logger.LogInformation(
                            "已迁移 {Migrated}/{Total} 条记录 ({Percentage:F1}%)",
                            migratedCount,
                            totalCount,
                            (double)migratedCount / totalCount * 100);
                    }
                    catch (Exception ex)
                    {
                        result.FailedRecords += measurements.Count;
                        _logger.LogError(ex, "迁移第 {Page} 批数据失败", pageIndex);
                        
                        // 记录失败的批次
                        result.FailedBatches.Add(pageIndex);
                    }

                    pageIndex++;
                }

                result.Success = result.FailedRecords == 0;
                result.EndTime = DateTime.Now;

                _logger.LogInformation(
                    "数据迁移完成：成功 {Success} 条，失败 {Failed} 条，耗时 {Duration}",
                    result.MigratedRecords,
                    result.FailedRecords,
                    result.Duration);

                return result;
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = ex.Message;
                result.EndTime = DateTime.Now;
                
                _logger.LogError(ex, "数据迁移失败");
                return result;
            }
        }

        /// <summary>
        /// 从 JSON 文件导入到数据库
        /// </summary>
        /// <param name="jsonFilePath">JSON 文件路径</param>
        /// <param name="targetDatabase">目标数据库服务</param>
        /// <returns>迁移结果</returns>
        public async Task<MigrationResult> ImportFromJsonAsync(
            string jsonFilePath,
            IDatabaseService targetDatabase)
        {
            if (string.IsNullOrEmpty(jsonFilePath))
            {
                throw new ArgumentException("JSON 文件路径不能为空", nameof(jsonFilePath));
            }

            if (targetDatabase == null)
            {
                throw new ArgumentNullException(nameof(targetDatabase));
            }

            _logger.LogInformation("从 JSON 文件导入数据：{FilePath}", jsonFilePath);

            // 创建临时的 JSON 文件数据库服务
            var dataDirectory = System.IO.Path.GetDirectoryName(jsonFilePath) 
                ?? throw new InvalidOperationException("无法获取文件目录");
            
            var loggerFactory = Microsoft.Extensions.Logging.LoggerFactory.Create(builder =>
            {
                builder.SetMinimumLevel(LogLevel.Information);
            });
            
            var jsonLogger = loggerFactory.CreateLogger<JsonFileDatabaseService>();
            var jsonDatabase = new JsonFileDatabaseService(dataDirectory, jsonLogger);

            // 执行迁移
            return await MigrateDataAsync(jsonDatabase, targetDatabase);
        }

        /// <summary>
        /// 导出数据库到 JSON 文件
        /// </summary>
        /// <param name="sourceDatabase">源数据库服务</param>
        /// <param name="jsonFilePath">JSON 文件路径</param>
        /// <param name="startTime">开始时间（可选）</param>
        /// <param name="endTime">结束时间（可选）</param>
        /// <returns>迁移结果</returns>
        public async Task<MigrationResult> ExportToJsonAsync(
            IDatabaseService sourceDatabase,
            string jsonFilePath,
            DateTime? startTime = null,
            DateTime? endTime = null)
        {
            if (sourceDatabase == null)
            {
                throw new ArgumentNullException(nameof(sourceDatabase));
            }

            if (string.IsNullOrEmpty(jsonFilePath))
            {
                throw new ArgumentException("JSON 文件路径不能为空", nameof(jsonFilePath));
            }

            _logger.LogInformation("导出数据库到 JSON 文件：{FilePath}", jsonFilePath);

            // 创建临时的 JSON 文件数据库服务
            var dataDirectory = System.IO.Path.GetDirectoryName(jsonFilePath) 
                ?? throw new InvalidOperationException("无法获取文件目录");
            
            var loggerFactory = Microsoft.Extensions.Logging.LoggerFactory.Create(builder =>
            {
                builder.SetMinimumLevel(LogLevel.Information);
            });
            
            var jsonLogger = loggerFactory.CreateLogger<JsonFileDatabaseService>();
            var jsonDatabase = new JsonFileDatabaseService(dataDirectory, jsonLogger);

            // 执行迁移
            return await MigrateDataAsync(sourceDatabase, jsonDatabase, startTime, endTime);
        }

        /// <summary>
        /// 验证迁移结果
        /// </summary>
        /// <param name="sourceDatabase">源数据库</param>
        /// <param name="targetDatabase">目标数据库</param>
        /// <param name="startTime">开始时间（可选）</param>
        /// <param name="endTime">结束时间（可选）</param>
        /// <returns>验证是否通过</returns>
        public async Task<bool> ValidateMigrationAsync(
            IDatabaseService sourceDatabase,
            IDatabaseService targetDatabase,
            DateTime? startTime = null,
            DateTime? endTime = null)
        {
            try
            {
                var sourceCount = await sourceDatabase.GetMeasurementCountAsync(startTime, endTime);
                var targetCount = await targetDatabase.GetMeasurementCountAsync(startTime, endTime);

                if (sourceCount != targetCount)
                {
                    _logger.LogWarning(
                        "记录数量不匹配：源数据库 {Source} 条，目标数据库 {Target} 条",
                        sourceCount,
                        targetCount);
                    return false;
                }

                _logger.LogInformation("迁移验证通过：记录数量匹配 ({Count} 条)", sourceCount);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "验证迁移结果失败");
                return false;
            }
        }
    }

    /// <summary>
    /// 迁移结果
    /// </summary>
    public class MigrationResult
    {
        /// <summary>
        /// 是否成功
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// 错误信息
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// 源数据库类型
        /// </summary>
        public Data.Enums.DatabaseType SourceDatabaseType { get; set; }

        /// <summary>
        /// 目标数据库类型
        /// </summary>
        public Data.Enums.DatabaseType TargetDatabaseType { get; set; }

        /// <summary>
        /// 总记录数
        /// </summary>
        public int TotalRecords { get; set; }

        /// <summary>
        /// 已迁移记录数
        /// </summary>
        public int MigratedRecords { get; set; }

        /// <summary>
        /// 失败记录数
        /// </summary>
        public int FailedRecords { get; set; }

        /// <summary>
        /// 失败的批次索引
        /// </summary>
        public List<int> FailedBatches { get; set; } = new List<int>();

        /// <summary>
        /// 开始时间
        /// </summary>
        public DateTime StartTime { get; set; }

        /// <summary>
        /// 结束时间
        /// </summary>
        public DateTime? EndTime { get; set; }

        /// <summary>
        /// 迁移耗时
        /// </summary>
        public TimeSpan Duration => EndTime.HasValue 
            ? EndTime.Value - StartTime 
            : TimeSpan.Zero;

        /// <summary>
        /// 成功率
        /// </summary>
        public double SuccessRate => TotalRecords > 0 
            ? (double)MigratedRecords / TotalRecords * 100 
            : 0;
    }
}
