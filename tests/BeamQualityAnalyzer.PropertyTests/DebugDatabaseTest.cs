using System;
using System.Linq;
using BeamQualityAnalyzer.Data.Services;
using Microsoft.Extensions.Logging;
using Xunit;
using Xunit.Abstractions;

namespace BeamQualityAnalyzer.PropertyTests
{
    /// <summary>
    /// 调试数据库往返测试，找出失败原因
    /// </summary>
    public class DebugDatabaseTest
    {
        private readonly ITestOutputHelper _output;
        private readonly ILogger<SqliteDatabaseService> _logger;

        public DebugDatabaseTest(ITestOutputHelper output)
        {
            _output = output;
            
            var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.AddConsole();
                builder.SetMinimumLevel(LogLevel.Debug);
            });
            _logger = loggerFactory.CreateLogger<SqliteDatabaseService>();
        }

        [Fact]
        public async Task Debug_RawDataPoints_SaveAndRetrieve()
        {
            // Arrange - 生成测试数据
            var validMeasurement = MeasurementGenerators.GenerateValidMeasurement(
                pointCount: 15,
                mSquaredX: 1.5,
                mSquaredY: 1.6,
                waistDiameterX: 50.0,
                waistDiameterY: 55.0,
                fitParamCount: 4
            );
            
            var measurement = validMeasurement.Value;
            
            _output.WriteLine($"生成的测量记录:");
            _output.WriteLine($"  原始数据点数量: {measurement.RawDataPoints.Count}");
            
            foreach (var point in measurement.RawDataPoints.Take(5))
            {
                _output.WriteLine($"  数据点: Position={point.DetectorPosition:F2}, DiameterX={point.BeamDiameterX:F2}, DiameterY={point.BeamDiameterY:F2}");
            }

            var dbPath = $"debug_test_{Guid.NewGuid()}.db";
            try
            {
                var connectionString = $"Data Source={dbPath}";
                var service = new SqliteDatabaseService(connectionString, _logger);
                
                // 初始化数据库
                await service.InitializeDatabaseAsync();
                _output.WriteLine($"\n数据库已初始化: {dbPath}");

                // Act - 保存
                var savedId = await service.SaveMeasurementAsync(measurement);
                _output.WriteLine($"保存成功，ID: {savedId}");

                // 读取
                var retrievedList = await service.QueryMeasurementsAsync(null, null, 0, 10);
                var retrieved = retrievedList.FirstOrDefault(m => m.Id == savedId);

                // Assert - 详细验证
                Assert.NotNull(retrieved);
                _output.WriteLine($"\n读取的测量记录:");
                _output.WriteLine($"  原始数据点数量: {retrieved.RawDataPoints.Count}");
                
                if (retrieved.RawDataPoints.Count == 0)
                {
                    _output.WriteLine("  ❌ 错误：没有读取到任何数据点！");
                }
                else
                {
                    foreach (var point in retrieved.RawDataPoints.Take(5))
                    {
                        _output.WriteLine($"  数据点: Position={point.DetectorPosition:F2}, DiameterX={point.BeamDiameterX:F2}, DiameterY={point.BeamDiameterY:F2}");
                        
                        if (point.DetectorPosition <= 0)
                        {
                            _output.WriteLine($"    ❌ 错误：DetectorPosition <= 0");
                        }
                        if (point.BeamDiameterX <= 0)
                        {
                            _output.WriteLine($"    ❌ 错误：BeamDiameterX <= 0");
                        }
                        if (point.BeamDiameterY <= 0)
                        {
                            _output.WriteLine($"    ❌ 错误：BeamDiameterY <= 0");
                        }
                    }
                }

                // 验证数量
                Assert.Equal(measurement.RawDataPoints.Count, retrieved.RawDataPoints.Count);
                
                // 验证每个数据点
                var invalidPoints = retrieved.RawDataPoints.Where(rp =>
                    rp.DetectorPosition <= 0 ||
                    rp.BeamDiameterX <= 0 ||
                    rp.BeamDiameterY <= 0).ToList();
                
                if (invalidPoints.Any())
                {
                    _output.WriteLine($"\n❌ 发现 {invalidPoints.Count} 个无效数据点:");
                    foreach (var point in invalidPoints)
                    {
                        _output.WriteLine($"  Position={point.DetectorPosition:F2}, DiameterX={point.BeamDiameterX:F2}, DiameterY={point.BeamDiameterY:F2}");
                    }
                }
                
                Assert.Empty(invalidPoints);
                
                // 等待一小段时间确保所有数据库操作完成
                await Task.Delay(100);
            }
            finally
            {
                // 强制垃圾回收以释放数据库连接
                GC.Collect();
                GC.WaitForPendingFinalizers();
                
                // 等待文件句柄释放
                await Task.Delay(200);
                
                if (System.IO.File.Exists(dbPath))
                {
                    try
                    {
                        System.IO.File.Delete(dbPath);
                    }
                    catch (IOException ex)
                    {
                        // 如果文件仍被占用，记录警告但不失败测试
                        _output.WriteLine($"警告：无法删除测试数据库文件 {dbPath}: {ex.Message}");
                    }
                }
            }
        }
    }
}
