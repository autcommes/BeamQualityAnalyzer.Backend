using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using BeamQualityAnalyzer.Data.Entities;
using BeamQualityAnalyzer.Data.Enums;
using BeamQualityAnalyzer.Data.Interfaces;
using Microsoft.Extensions.Logging;

namespace BeamQualityAnalyzer.Data.Services
{
    /// <summary>
    /// JSON 文件数据库服务实现（故障转移备用方案）
    /// </summary>
    /// <remarks>
    /// 当主数据库连接失败时，自动切换到本地 JSON 文件存储。
    /// 数据存储在指定目录下的 JSON 文件中。
    /// </remarks>
    public class JsonFileDatabaseService : IDatabaseService
    {
        private readonly string _dataDirectory;
        private readonly ILogger<JsonFileDatabaseService> _logger;
        private readonly string _measurementsFile;
        private readonly object _fileLock = new object();
        private int _nextId = 1;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="dataDirectory">数据存储目录</param>
        /// <param name="logger">日志记录器</param>
        public JsonFileDatabaseService(string dataDirectory, ILogger<JsonFileDatabaseService> logger)
        {
            _dataDirectory = dataDirectory ?? throw new ArgumentNullException(nameof(dataDirectory));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            
            _measurementsFile = Path.Combine(_dataDirectory, "measurements.json");
            
            // 确保目录存在
            if (!Directory.Exists(_dataDirectory))
            {
                Directory.CreateDirectory(_dataDirectory);
                _logger.LogInformation("创建数据存储目录: {Directory}", _dataDirectory);
            }
            
            // 初始化下一个ID
            InitializeNextId();
        }

        /// <summary>
        /// 初始化下一个ID
        /// </summary>
        private void InitializeNextId()
        {
            try
            {
                var measurements = LoadMeasurementsFromFile();
                if (measurements.Count > 0)
                {
                    _nextId = measurements.Max(m => m.Id) + 1;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "初始化ID失败，使用默认值");
            }
        }

        /// <summary>
        /// 从文件加载测量记录
        /// </summary>
        private List<Measurement> LoadMeasurementsFromFile()
        {
            lock (_fileLock)
            {
                if (!File.Exists(_measurementsFile))
                {
                    return new List<Measurement>();
                }

                try
                {
                    var json = File.ReadAllText(_measurementsFile);
                    var measurements = JsonSerializer.Deserialize<List<Measurement>>(json);
                    return measurements ?? new List<Measurement>();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "从文件加载测量记录失败");
                    return new List<Measurement>();
                }
            }
        }

        /// <summary>
        /// 保存测量记录到文件
        /// </summary>
        private void SaveMeasurementsToFile(List<Measurement> measurements)
        {
            lock (_fileLock)
            {
                try
                {
                    var options = new JsonSerializerOptions
                    {
                        WriteIndented = true,
                        ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles
                    };
                    var json = JsonSerializer.Serialize(measurements, options);
                    File.WriteAllText(_measurementsFile, json);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "保存测量记录到文件失败");
                    throw;
                }
            }
        }

        /// <summary>
        /// 保存测量记录
        /// </summary>
        public Task<int> SaveMeasurementAsync(Measurement measurement)
        {
            if (measurement == null)
            {
                throw new ArgumentNullException(nameof(measurement));
            }

            try
            {
                var measurements = LoadMeasurementsFromFile();
                
                // 分配ID
                measurement.Id = _nextId++;
                
                measurements.Add(measurement);
                SaveMeasurementsToFile(measurements);

                _logger.LogInformation("测量记录已保存到JSON文件，ID: {MeasurementId}", measurement.Id);
                return Task.FromResult(measurement.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "保存测量记录到JSON文件失败");
                throw;
            }
        }

        /// <summary>
        /// 查询测量记录
        /// </summary>
        public Task<List<Measurement>> QueryMeasurementsAsync(
            DateTime? startTime,
            DateTime? endTime,
            int pageIndex,
            int pageSize)
        {
            if (pageIndex < 0)
            {
                throw new ArgumentException("页索引不能为负数", nameof(pageIndex));
            }

            if (pageSize <= 0)
            {
                throw new ArgumentException("页大小必须大于0", nameof(pageSize));
            }

            try
            {
                var measurements = LoadMeasurementsFromFile();

                // 应用时间范围过滤
                var query = measurements.AsEnumerable();
                
                if (startTime.HasValue)
                {
                    query = query.Where(m => m.MeasurementTime >= startTime.Value);
                }

                if (endTime.HasValue)
                {
                    query = query.Where(m => m.MeasurementTime <= endTime.Value);
                }

                // 按时间降序排序并分页
                var result = query
                    .OrderByDescending(m => m.MeasurementTime)
                    .Skip(pageIndex * pageSize)
                    .Take(pageSize)
                    .ToList();

                _logger.LogInformation("从JSON文件查询到 {Count} 条测量记录", result.Count);
                return Task.FromResult(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "从JSON文件查询测量记录失败");
                throw;
            }
        }

        /// <summary>
        /// 删除测量记录
        /// </summary>
        public Task DeleteMeasurementAsync(int id)
        {
            if (id <= 0)
            {
                throw new ArgumentException("记录ID必须大于0", nameof(id));
            }

            try
            {
                var measurements = LoadMeasurementsFromFile();
                var measurement = measurements.FirstOrDefault(m => m.Id == id);
                
                if (measurement == null)
                {
                    _logger.LogWarning("未找到ID为 {MeasurementId} 的测量记录", id);
                    return Task.CompletedTask;
                }

                measurements.Remove(measurement);
                SaveMeasurementsToFile(measurements);

                _logger.LogInformation("测量记录已从JSON文件删除，ID: {MeasurementId}", id);
                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "从JSON文件删除测量记录失败，ID: {MeasurementId}", id);
                throw;
            }
        }

        /// <summary>
        /// 测试数据库连接
        /// </summary>
        public Task<bool> TestConnectionAsync()
        {
            try
            {
                // 测试文件系统访问
                var testFile = Path.Combine(_dataDirectory, ".test");
                File.WriteAllText(testFile, "test");
                File.Delete(testFile);
                
                _logger.LogInformation("JSON文件存储连接测试成功");
                return Task.FromResult(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "JSON文件存储连接测试失败");
                return Task.FromResult(false);
            }
        }

        /// <summary>
        /// 获取数据库类型
        /// </summary>
        public DatabaseType GetDatabaseType()
        {
            return DatabaseType.SQLite; // 使用SQLite作为标识，表示本地存储
        }

        /// <summary>
        /// 初始化数据库（创建目录）
        /// </summary>
        public Task InitializeDatabaseAsync()
        {
            try
            {
                if (!Directory.Exists(_dataDirectory))
                {
                    Directory.CreateDirectory(_dataDirectory);
                }
                
                _logger.LogInformation("JSON文件存储初始化成功");
                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "JSON文件存储初始化失败");
                throw;
            }
        }

        /// <summary>
        /// 应用数据库迁移（不适用于JSON文件）
        /// </summary>
        public Task ApplyMigrationsAsync()
        {
            _logger.LogInformation("JSON文件存储不需要迁移");
            return Task.CompletedTask;
        }

        /// <summary>
        /// 批量保存测量记录（使用事务）
        /// </summary>
        public Task<List<int>> SaveMeasurementsBatchAsync(List<Measurement> measurements)
        {
            if (measurements == null || measurements.Count == 0)
            {
                throw new ArgumentException("测量记录列表不能为空", nameof(measurements));
            }

            try
            {
                var existingMeasurements = LoadMeasurementsFromFile();
                var savedIds = new List<int>();

                foreach (var measurement in measurements)
                {
                    measurement.Id = _nextId++;
                    existingMeasurements.Add(measurement);
                    savedIds.Add(measurement.Id);
                }

                SaveMeasurementsToFile(existingMeasurements);

                _logger.LogInformation("批量保存 {Count} 条测量记录到JSON文件成功", measurements.Count);
                return Task.FromResult(savedIds);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "批量保存测量记录到JSON文件失败");
                throw;
            }
        }

        /// <summary>
        /// 获取测量记录总数
        /// </summary>
        public Task<int> GetMeasurementCountAsync(DateTime? startTime = null, DateTime? endTime = null)
        {
            try
            {
                var measurements = LoadMeasurementsFromFile();
                var query = measurements.AsEnumerable();

                if (startTime.HasValue)
                {
                    query = query.Where(m => m.MeasurementTime >= startTime.Value);
                }

                if (endTime.HasValue)
                {
                    query = query.Where(m => m.MeasurementTime <= endTime.Value);
                }

                var count = query.Count();
                
                _logger.LogInformation("从JSON文件查询到 {Count} 条测量记录", count);
                return Task.FromResult(count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "从JSON文件获取测量记录总数失败");
                throw;
            }
        }

        /// <summary>
        /// 根据ID获取测量记录
        /// </summary>
        public Task<Measurement?> GetMeasurementByIdAsync(int id)
        {
            if (id <= 0)
            {
                throw new ArgumentException("记录ID必须大于0", nameof(id));
            }

            try
            {
                var measurements = LoadMeasurementsFromFile();
                var measurement = measurements.FirstOrDefault(m => m.Id == id);

                if (measurement == null)
                {
                    _logger.LogWarning("未找到ID为 {MeasurementId} 的测量记录", id);
                }

                return Task.FromResult(measurement);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "从JSON文件获取测量记录失败，ID: {MeasurementId}", id);
                throw;
            }
        }
    }
}
