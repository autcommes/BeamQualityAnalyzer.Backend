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
    /// SQLite 数据库服务实现
    /// </summary>
    public class SqliteDatabaseService : IDatabaseService
    {
        private readonly string _connectionString;
        private readonly ILogger<SqliteDatabaseService> _logger;

        /// <summary>
        /// 构造函数
        /// </summary>
        public SqliteDatabaseService(string connectionString, ILogger<SqliteDatabaseService> logger)
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
            optionsBuilder.UseSqlite(_connectionString);
            return new BeamAnalyzerDbContext(optionsBuilder.Options);
        }

        /// <summary>
        /// 保存测量记录
        /// </summary>
        public async Task<int> SaveMeasurementAsync(Measurement measurement)
        {
            if (measurement == null)
            {
                throw new ArgumentNullException(nameof(measurement));
            }

            try
            {
                using var context = CreateDbContext();
                
                // 添加测量记录
                context.Measurements.Add(measurement);
                await context.SaveChangesAsync();

                _logger.LogInformation("测量记录已保存，ID: {MeasurementId}", measurement.Id);
                return measurement.Id;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "保存测量记录失败");
                throw;
            }
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
                using var context = CreateDbContext();

                var query = context.Measurements
                    .Include(m => m.RawDataPoints)
                    .Include(m => m.CalculationResult)
                        .ThenInclude(cr => cr!.FitParameters)
                    .AsQueryable();

                // 应用时间范围过滤
                if (startTime.HasValue)
                {
                    query = query.Where(m => m.MeasurementTime >= startTime.Value);
                }

                if (endTime.HasValue)
                {
                    query = query.Where(m => m.MeasurementTime <= endTime.Value);
                }

                // 按时间降序排序并分页
                var measurements = await query
                    .OrderByDescending(m => m.MeasurementTime)
                    .Skip(pageIndex * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                _logger.LogInformation("查询到 {Count} 条测量记录", measurements.Count);
                return measurements;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "查询测量记录失败");
                throw;
            }
        }

        /// <summary>
        /// 删除测量记录
        /// </summary>
        public async Task DeleteMeasurementAsync(int id)
        {
            if (id <= 0)
            {
                throw new ArgumentException("记录ID必须大于0", nameof(id));
            }

            try
            {
                using var context = CreateDbContext();

                var measurement = await context.Measurements.FindAsync(id);
                if (measurement == null)
                {
                    _logger.LogWarning("未找到ID为 {MeasurementId} 的测量记录", id);
                    return;
                }

                context.Measurements.Remove(measurement);
                await context.SaveChangesAsync();

                _logger.LogInformation("测量记录已删除，ID: {MeasurementId}", id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "删除测量记录失败，ID: {MeasurementId}", id);
                throw;
            }
        }

        /// <summary>
        /// 测试数据库连接
        /// </summary>
        public async Task<bool> TestConnectionAsync()
        {
            try
            {
                using var context = CreateDbContext();
                await context.Database.CanConnectAsync();
                
                _logger.LogInformation("数据库连接测试成功");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "数据库连接测试失败");
                return false;
            }
        }

        /// <summary>
        /// 获取数据库类型
        /// </summary>
        public DatabaseType GetDatabaseType()
        {
            return DatabaseType.SQLite;
        }

        /// <summary>
        /// 初始化数据库（创建表结构）
        /// </summary>
        public async Task InitializeDatabaseAsync()
        {
            try
            {
                using var context = CreateDbContext();
                await context.Database.EnsureCreatedAsync();
                
                _logger.LogInformation("数据库初始化成功");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "数据库初始化失败");
                throw;
            }
        }

        /// <summary>
        /// 应用数据库迁移
        /// </summary>
        public async Task ApplyMigrationsAsync()
        {
            try
            {
                using var context = CreateDbContext();
                await context.Database.MigrateAsync();
                
                _logger.LogInformation("数据库迁移应用成功");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "应用数据库迁移失败");
                throw;
            }
        }

        /// <summary>
        /// 批量保存测量记录（使用事务）
        /// </summary>
        public async Task<List<int>> SaveMeasurementsBatchAsync(List<Measurement> measurements)
        {
            if (measurements == null || measurements.Count == 0)
            {
                throw new ArgumentException("测量记录列表不能为空", nameof(measurements));
            }

            var savedIds = new List<int>();

            try
            {
                using var context = CreateDbContext();
                
                // 使用事务确保所有记录要么全部保存成功，要么全部失败
                using var transaction = await context.Database.BeginTransactionAsync();
                
                try
                {
                    foreach (var measurement in measurements)
                    {
                        context.Measurements.Add(measurement);
                    }
                    
                    await context.SaveChangesAsync();
                    
                    // 收集保存后的ID
                    savedIds.AddRange(measurements.Select(m => m.Id));
                    
                    await transaction.CommitAsync();
                    
                    _logger.LogInformation("批量保存 {Count} 条测量记录成功", measurements.Count);
                }
                catch
                {
                    await transaction.RollbackAsync();
                    _logger.LogError("批量保存测量记录失败，事务已回滚");
                    throw;
                }

                return savedIds;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "批量保存测量记录失败");
                throw;
            }
        }

        /// <summary>
        /// 获取测量记录总数
        /// </summary>
        public async Task<int> GetMeasurementCountAsync(DateTime? startTime = null, DateTime? endTime = null)
        {
            try
            {
                using var context = CreateDbContext();

                var query = context.Measurements.AsQueryable();

                // 应用时间范围过滤
                if (startTime.HasValue)
                {
                    query = query.Where(m => m.MeasurementTime >= startTime.Value);
                }

                if (endTime.HasValue)
                {
                    query = query.Where(m => m.MeasurementTime <= endTime.Value);
                }

                var count = await query.CountAsync();
                
                _logger.LogInformation("查询到 {Count} 条测量记录", count);
                return count;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取测量记录总数失败");
                throw;
            }
        }

        /// <summary>
        /// 根据ID获取测量记录
        /// </summary>
        public async Task<Measurement?> GetMeasurementByIdAsync(int id)
        {
            if (id <= 0)
            {
                throw new ArgumentException("记录ID必须大于0", nameof(id));
            }

            try
            {
                using var context = CreateDbContext();

                var measurement = await context.Measurements
                    .Include(m => m.RawDataPoints)
                    .Include(m => m.CalculationResult)
                        .ThenInclude(cr => cr!.FitParameters)
                    .FirstOrDefaultAsync(m => m.Id == id);

                if (measurement == null)
                {
                    _logger.LogWarning("未找到ID为 {MeasurementId} 的测量记录", id);
                }

                return measurement;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取测量记录失败，ID: {MeasurementId}", id);
                throw;
            }
        }
    }
}
