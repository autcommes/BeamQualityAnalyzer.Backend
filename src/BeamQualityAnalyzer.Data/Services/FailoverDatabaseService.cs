using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BeamQualityAnalyzer.Data.Entities;
using BeamQualityAnalyzer.Data.Enums;
using BeamQualityAnalyzer.Data.Interfaces;
using Microsoft.Extensions.Logging;

namespace BeamQualityAnalyzer.Data.Services
{
    /// <summary>
    /// 数据库故障转移服务
    /// </summary>
    /// <remarks>
    /// 当主数据库连接失败时，自动切换到备用的 JSON 文件存储。
    /// 当主数据库恢复后，可以选择同步数据。
    /// </remarks>
    public class FailoverDatabaseService : IDatabaseService
    {
        private readonly IDatabaseService _primaryDatabase;
        private readonly IDatabaseService _fallbackDatabase;
        private readonly ILogger<FailoverDatabaseService> _logger;
        private bool _isPrimaryAvailable = true;
        private DateTime _lastFailureTime = DateTime.MinValue;
        private readonly TimeSpan _retryInterval = TimeSpan.FromMinutes(5);

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="primaryDatabase">主数据库服务</param>
        /// <param name="fallbackDatabase">备用数据库服务（JSON文件）</param>
        /// <param name="logger">日志记录器</param>
        public FailoverDatabaseService(
            IDatabaseService primaryDatabase,
            IDatabaseService fallbackDatabase,
            ILogger<FailoverDatabaseService> logger)
        {
            _primaryDatabase = primaryDatabase ?? throw new ArgumentNullException(nameof(primaryDatabase));
            _fallbackDatabase = fallbackDatabase ?? throw new ArgumentNullException(nameof(fallbackDatabase));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// 获取当前活动的数据库服务
        /// </summary>
        private async Task<IDatabaseService> GetActiveDatabaseAsync()
        {
            // 如果主数据库可用，直接返回
            if (_isPrimaryAvailable)
            {
                return _primaryDatabase;
            }

            // 如果距离上次失败已经超过重试间隔，尝试重新连接主数据库
            if (DateTime.Now - _lastFailureTime > _retryInterval)
            {
                _logger.LogInformation("尝试重新连接主数据库...");
                
                try
                {
                    var isConnected = await _primaryDatabase.TestConnectionAsync();
                    if (isConnected)
                    {
                        _isPrimaryAvailable = true;
                        _logger.LogInformation("主数据库已恢复，切换回主数据库");
                        return _primaryDatabase;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "重新连接主数据库失败");
                }

                _lastFailureTime = DateTime.Now;
            }

            // 使用备用数据库
            return _fallbackDatabase;
        }

        /// <summary>
        /// 标记主数据库为不可用
        /// </summary>
        private void MarkPrimaryAsUnavailable()
        {
            if (_isPrimaryAvailable)
            {
                _isPrimaryAvailable = false;
                _lastFailureTime = DateTime.Now;
                _logger.LogWarning("主数据库连接失败，切换到备用JSON文件存储");
            }
        }

        /// <summary>
        /// 执行数据库操作，带故障转移
        /// </summary>
        private async Task<T> ExecuteWithFailoverAsync<T>(Func<IDatabaseService, Task<T>> operation)
        {
            var database = await GetActiveDatabaseAsync();

            try
            {
                return await operation(database);
            }
            catch (Exception ex)
            {
                // 如果是主数据库失败，切换到备用数据库
                if (database == _primaryDatabase)
                {
                    _logger.LogError(ex, "主数据库操作失败，切换到备用数据库");
                    MarkPrimaryAsUnavailable();
                    
                    // 使用备用数据库重试
                    return await operation(_fallbackDatabase);
                }

                // 如果备用数据库也失败，抛出异常
                _logger.LogError(ex, "备用数据库操作也失败");
                throw;
            }
        }

        /// <summary>
        /// 保存测量记录
        /// </summary>
        public async Task<int> SaveMeasurementAsync(Measurement measurement)
        {
            return await ExecuteWithFailoverAsync(db => db.SaveMeasurementAsync(measurement));
        }

        /// <summary>
        /// 查询测量记录
        /// </summary>
        public async Task<List<Measurement>> QueryMeasurementsAsync(
            DateTime? startTime,
            DateTime? endTime,
            int pageIndex,
            int pageSize)
        {
            return await ExecuteWithFailoverAsync(db => 
                db.QueryMeasurementsAsync(startTime, endTime, pageIndex, pageSize));
        }

        /// <summary>
        /// 删除测量记录
        /// </summary>
        public async Task DeleteMeasurementAsync(int id)
        {
            await ExecuteWithFailoverAsync(async db =>
            {
                await db.DeleteMeasurementAsync(id);
                return true;
            });
        }

        /// <summary>
        /// 测试数据库连接
        /// </summary>
        public async Task<bool> TestConnectionAsync()
        {
            var database = await GetActiveDatabaseAsync();
            return await database.TestConnectionAsync();
        }

        /// <summary>
        /// 获取数据库类型
        /// </summary>
        public DatabaseType GetDatabaseType()
        {
            return _isPrimaryAvailable 
                ? _primaryDatabase.GetDatabaseType() 
                : _fallbackDatabase.GetDatabaseType();
        }

        /// <summary>
        /// 初始化数据库
        /// </summary>
        public async Task InitializeDatabaseAsync()
        {
            try
            {
                await _primaryDatabase.InitializeDatabaseAsync();
                _isPrimaryAvailable = true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "初始化主数据库失败，使用备用数据库");
                MarkPrimaryAsUnavailable();
                await _fallbackDatabase.InitializeDatabaseAsync();
            }
        }

        /// <summary>
        /// 应用数据库迁移
        /// </summary>
        public async Task ApplyMigrationsAsync()
        {
            try
            {
                await _primaryDatabase.ApplyMigrationsAsync();
                _isPrimaryAvailable = true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "应用主数据库迁移失败");
                MarkPrimaryAsUnavailable();
            }
        }

        /// <summary>
        /// 批量保存测量记录（使用事务）
        /// </summary>
        public async Task<List<int>> SaveMeasurementsBatchAsync(List<Measurement> measurements)
        {
            return await ExecuteWithFailoverAsync(db => db.SaveMeasurementsBatchAsync(measurements));
        }

        /// <summary>
        /// 获取测量记录总数
        /// </summary>
        public async Task<int> GetMeasurementCountAsync(DateTime? startTime = null, DateTime? endTime = null)
        {
            return await ExecuteWithFailoverAsync(db => db.GetMeasurementCountAsync(startTime, endTime));
        }

        /// <summary>
        /// 根据ID获取测量记录
        /// </summary>
        public async Task<Measurement?> GetMeasurementByIdAsync(int id)
        {
            return await ExecuteWithFailoverAsync(db => db.GetMeasurementByIdAsync(id));
        }

        /// <summary>
        /// 同步备用数据库到主数据库
        /// </summary>
        /// <remarks>
        /// 当主数据库恢复后，可以调用此方法将备用数据库中的数据同步到主数据库。
        /// </remarks>
        public async Task<int> SyncFallbackToPrimaryAsync()
        {
            if (_isPrimaryAvailable)
            {
                _logger.LogInformation("主数据库可用，开始同步备用数据库数据...");

                try
                {
                    // 获取备用数据库中的所有记录
                    var fallbackMeasurements = await _fallbackDatabase.QueryMeasurementsAsync(
                        null, null, 0, int.MaxValue);

                    if (fallbackMeasurements.Count == 0)
                    {
                        _logger.LogInformation("备用数据库中没有数据需要同步");
                        return 0;
                    }

                    // 批量保存到主数据库
                    var savedIds = await _primaryDatabase.SaveMeasurementsBatchAsync(fallbackMeasurements);

                    _logger.LogInformation("成功同步 {Count} 条记录到主数据库", savedIds.Count);
                    return savedIds.Count;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "同步备用数据库到主数据库失败");
                    throw;
                }
            }
            else
            {
                _logger.LogWarning("主数据库不可用，无法同步数据");
                return 0;
            }
        }

        /// <summary>
        /// 检查主数据库是否可用
        /// </summary>
        public bool IsPrimaryAvailable => _isPrimaryAvailable;

        /// <summary>
        /// 强制检查主数据库连接状态
        /// </summary>
        public async Task<bool> CheckPrimaryConnectionAsync()
        {
            try
            {
                var isConnected = await _primaryDatabase.TestConnectionAsync();
                _isPrimaryAvailable = isConnected;
                
                if (isConnected)
                {
                    _logger.LogInformation("主数据库连接正常");
                }
                else
                {
                    _logger.LogWarning("主数据库连接失败");
                    MarkPrimaryAsUnavailable();
                }

                return isConnected;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "检查主数据库连接失败");
                MarkPrimaryAsUnavailable();
                return false;
            }
        }
    }
}
