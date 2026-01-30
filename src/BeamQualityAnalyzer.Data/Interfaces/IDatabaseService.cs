using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BeamQualityAnalyzer.Data.Entities;
using BeamQualityAnalyzer.Data.Enums;

namespace BeamQualityAnalyzer.Data.Interfaces
{
    /// <summary>
    /// 数据库服务接口
    /// </summary>
    public interface IDatabaseService
    {
        /// <summary>
        /// 保存测量记录
        /// </summary>
        /// <param name="measurement">测量记录</param>
        /// <returns>记录ID</returns>
        Task<int> SaveMeasurementAsync(Measurement measurement);

        /// <summary>
        /// 查询测量记录
        /// </summary>
        /// <param name="startTime">开始时间（可选）</param>
        /// <param name="endTime">结束时间（可选）</param>
        /// <param name="pageIndex">页索引</param>
        /// <param name="pageSize">页大小</param>
        /// <returns>测量记录列表</returns>
        Task<List<Measurement>> QueryMeasurementsAsync(
            DateTime? startTime,
            DateTime? endTime,
            int pageIndex,
            int pageSize);

        /// <summary>
        /// 删除测量记录
        /// </summary>
        /// <param name="id">记录ID</param>
        Task DeleteMeasurementAsync(int id);

        /// <summary>
        /// 测试数据库连接
        /// </summary>
        /// <returns>连接是否成功</returns>
        Task<bool> TestConnectionAsync();

        /// <summary>
        /// 获取数据库类型
        /// </summary>
        DatabaseType GetDatabaseType();

        /// <summary>
        /// 初始化数据库（创建表结构）
        /// </summary>
        Task InitializeDatabaseAsync();

        /// <summary>
        /// 应用数据库迁移
        /// </summary>
        Task ApplyMigrationsAsync();

        /// <summary>
        /// 批量保存测量记录（使用事务）
        /// </summary>
        /// <param name="measurements">测量记录列表</param>
        /// <returns>保存的记录ID列表</returns>
        Task<List<int>> SaveMeasurementsBatchAsync(List<Measurement> measurements);

        /// <summary>
        /// 获取测量记录总数
        /// </summary>
        /// <param name="startTime">开始时间（可选）</param>
        /// <param name="endTime">结束时间（可选）</param>
        /// <returns>记录总数</returns>
        Task<int> GetMeasurementCountAsync(DateTime? startTime = null, DateTime? endTime = null);

        /// <summary>
        /// 根据ID获取测量记录
        /// </summary>
        /// <param name="id">记录ID</param>
        /// <returns>测量记录，如果不存在则返回null</returns>
        Task<Measurement?> GetMeasurementByIdAsync(int id);
    }
}
