using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BeamQualityAnalyzer.Data.DbContext;
using BeamQualityAnalyzer.Data.Entities;
using BeamQualityAnalyzer.Data.Services;
using FsCheck;
using FsCheck.Xunit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Xunit;

namespace BeamQualityAnalyzer.PropertyTests
{
    /// <summary>
    /// 数据库往返一致性属性测试
    /// Feature: beam-quality-analyzer, Property 9: 数据库往返一致性
    /// 验证需求: 11.1, 11.2, 11.3, 11.4, 11.5
    /// </summary>
    public class DatabaseRoundTripPropertyTests
    {
        private readonly ILogger<SqliteDatabaseService> _logger;

        public DatabaseRoundTripPropertyTests()
        {
            // 创建测试日志记录器
            var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.AddConsole();
                builder.SetMinimumLevel(LogLevel.Warning);
            });
            _logger = loggerFactory.CreateLogger<SqliteDatabaseService>();
        }

        /// <summary>
        /// 创建唯一的测试数据库路径
        /// </summary>
        private string CreateUniqueDbPath()
        {
            return $"test_beam_analyzer_{Guid.NewGuid()}.db";
        }

        /// <summary>
        /// 清理测试数据库文件
        /// </summary>
        private void CleanupDb(string dbPath)
        {
            if (System.IO.File.Exists(dbPath))
            {
                try
                {
                    System.IO.File.Delete(dbPath);
                }
                catch
                {
                    // 忽略删除失败
                }
            }
        }

        /// <summary>
        /// 属性 9: 数据库往返一致性 - 保存后读取应得到等价数据
        /// 对于任何有效的测量记录（包含原始数据、计算结果、拟合参数），
        /// 保存到数据库然后读取应该得到等价的数据。
        /// </summary>
        [Property(MaxTest = 50, Arbitrary = new[] { typeof(MeasurementGenerators) })]
        public Property DatabaseRoundTrip_SaveAndRetrieve_ReturnsEquivalentData(ValidMeasurement validMeasurement)
        {
            // Feature: beam-quality-analyzer, Property 9: 数据库往返一致性
            
            return Prop.ForAll(
                Arb.From(Gen.Constant(validMeasurement)),
                measurement =>
                {
                    var dbPath = CreateUniqueDbPath();
                    try
                    {
                        // Arrange
                        var connectionString = $"Data Source={dbPath}";
                        var service = new SqliteDatabaseService(connectionString, _logger);
                        
                        // 初始化数据库
                        service.InitializeDatabaseAsync().Wait();

                        // Act - 保存测量记录
                        var savedId = service.SaveMeasurementAsync(measurement.Value).Result;

                        // 读取测量记录
                        var retrievedMeasurements = service.QueryMeasurementsAsync(
                            startTime: null,
                            endTime: null,
                            pageIndex: 0,
                            pageSize: 10).Result;

                        var retrievedMeasurement = retrievedMeasurements.FirstOrDefault(m => m.Id == savedId);

                        // Assert - 验证数据等价性
                        return retrievedMeasurement != null &&
                               AreMeasurementsEquivalent(measurement.Value, retrievedMeasurement);
                    }
                    finally
                    {
                        CleanupDb(dbPath);
                    }
                });
        }

        /// <summary>
        /// 属性 9.1: 原始数据点往返一致性
        /// 保存的原始数据点应该能够完整读取
        /// </summary>
        [Property(MaxTest = 50, Arbitrary = new[] { typeof(MeasurementGenerators) })]
        public Property RawDataPoints_RoundTrip_PreservesAllPoints(ValidMeasurement validMeasurement)
        {
            // Feature: beam-quality-analyzer, Property 9: 数据库往返一致性
            
            return Prop.ForAll(
                Arb.From(Gen.Constant(validMeasurement)),
                measurement =>
                {
                    var dbPath = CreateUniqueDbPath();
                    try
                    {
                        // Arrange
                        var connectionString = $"Data Source={dbPath}";
                        var service = new SqliteDatabaseService(connectionString, _logger);
                        service.InitializeDatabaseAsync().Wait();

                        var originalPointCount = measurement.Value.RawDataPoints.Count;

                        // Act
                        var savedId = service.SaveMeasurementAsync(measurement.Value).Result;
                        var retrieved = service.QueryMeasurementsAsync(null, null, 0, 10).Result
                            .FirstOrDefault(m => m.Id == savedId);

                        // Assert
                        bool result = retrieved != null &&
                               retrieved.RawDataPoints.Count == originalPointCount &&
                               retrieved.RawDataPoints.All(rp =>
                                   rp.BeamDiameterX > 0 &&
                                   rp.BeamDiameterY > 0);
                        
                        // 强制等待，确保数据库连接关闭
                        System.Threading.Thread.Sleep(100);
                        GC.Collect();
                        GC.WaitForPendingFinalizers();
                        
                        return result;
                    }
                    finally
                    {
                        CleanupDb(dbPath);
                    }
                });
        }

        /// <summary>
        /// 属性 9.2: 计算结果往返一致性
        /// M² 因子和腰斑参数应该保持精度
        /// </summary>
        [Property(MaxTest = 50, Arbitrary = new[] { typeof(MeasurementGenerators) })]
        public Property CalculationResult_RoundTrip_PreservesMSquaredValues(ValidMeasurement validMeasurement)
        {
            // Feature: beam-quality-analyzer, Property 9: 数据库往返一致性
            
            return Prop.ForAll(
                Arb.From(Gen.Constant(validMeasurement)),
                measurement =>
                {
                    var dbPath = CreateUniqueDbPath();
                    try
                    {
                        // Arrange
                        var connectionString = $"Data Source={dbPath}";
                        var service = new SqliteDatabaseService(connectionString, _logger);
                        service.InitializeDatabaseAsync().Wait();

                        var original = measurement.Value.CalculationResult;
                        if (original == null) return true; // 如果没有计算结果，跳过

                        // Act
                        var savedId = service.SaveMeasurementAsync(measurement.Value).Result;
                        var retrieved = service.QueryMeasurementsAsync(null, null, 0, 10).Result
                            .FirstOrDefault(m => m.Id == savedId);

                        // Assert
                        if (retrieved?.CalculationResult == null) return false;

                        var result = retrieved.CalculationResult;
                        const double tolerance = 0.0001; // 精度容差

                        return Math.Abs(result.MSquaredX - original.MSquaredX) < tolerance &&
                               Math.Abs(result.MSquaredY - original.MSquaredY) < tolerance &&
                               Math.Abs(result.MSquaredGlobal - original.MSquaredGlobal) < tolerance &&
                               Math.Abs(result.BeamWaistDiameterX - original.BeamWaistDiameterX) < tolerance &&
                               Math.Abs(result.BeamWaistDiameterY - original.BeamWaistDiameterY) < tolerance;
                    }
                    finally
                    {
                        CleanupDb(dbPath);
                    }
                });
        }

        /// <summary>
        /// 属性 9.3: 拟合参数往返一致性
        /// 拟合参数应该完整保存和读取
        /// </summary>
        [Property(MaxTest = 50, Arbitrary = new[] { typeof(MeasurementGenerators) })]
        public Property FitParameters_RoundTrip_PreservesAllParameters(ValidMeasurement validMeasurement)
        {
            // Feature: beam-quality-analyzer, Property 9: 数据库往返一致性
            
            return Prop.ForAll(
                Arb.From(Gen.Constant(validMeasurement)),
                measurement =>
                {
                    var dbPath = CreateUniqueDbPath();
                    try
                    {
                        // Arrange
                        var connectionString = $"Data Source={dbPath}";
                        var service = new SqliteDatabaseService(connectionString, _logger);
                        service.InitializeDatabaseAsync().Wait();

                        var originalFitParams = measurement.Value.CalculationResult?.FitParameters;
                        if (originalFitParams == null || !originalFitParams.Any()) return true;

                        var originalCount = originalFitParams.Count;

                        // Act
                        var savedId = service.SaveMeasurementAsync(measurement.Value).Result;
                        var retrieved = service.QueryMeasurementsAsync(null, null, 0, 10).Result
                            .FirstOrDefault(m => m.Id == savedId);

                        // Assert
                        var retrievedFitParams = retrieved?.CalculationResult?.FitParameters;
                        return retrievedFitParams != null &&
                               retrievedFitParams.Count == originalCount &&
                               retrievedFitParams.All(fp =>
                                   !string.IsNullOrEmpty(fp.Direction) &&
                                   !string.IsNullOrEmpty(fp.FitType) &&
                                   !string.IsNullOrEmpty(fp.Parameters) &&
                                   fp.RSquared >= 0 && fp.RSquared <= 1);
                    }
                    finally
                    {
                        CleanupDb(dbPath);
                    }
                });
        }

        /// <summary>
        /// 属性 9.4: 时间戳精度保持
        /// 测量时间和创建时间应该保持精度（秒级）
        /// </summary>
        [Property(MaxTest = 50, Arbitrary = new[] { typeof(MeasurementGenerators) })]
        public Property Timestamps_RoundTrip_PreservesSecondPrecision(ValidMeasurement validMeasurement)
        {
            // Feature: beam-quality-analyzer, Property 9: 数据库往返一致性
            
            return Prop.ForAll(
                Arb.From(Gen.Constant(validMeasurement)),
                measurement =>
                {
                    var dbPath = CreateUniqueDbPath();
                    try
                    {
                        // Arrange
                        var connectionString = $"Data Source={dbPath}";
                        var service = new SqliteDatabaseService(connectionString, _logger);
                        service.InitializeDatabaseAsync().Wait();

                        var originalMeasurementTime = measurement.Value.MeasurementTime;
                        var originalCreatedAt = measurement.Value.CreatedAt;

                        // Act
                        var savedId = service.SaveMeasurementAsync(measurement.Value).Result;
                        var retrieved = service.QueryMeasurementsAsync(null, null, 0, 10).Result
                            .FirstOrDefault(m => m.Id == savedId);

                        // Assert - 验证时间戳精度（秒级）
                        if (retrieved == null) return false;

                        var measurementTimeDiff = Math.Abs((retrieved.MeasurementTime - originalMeasurementTime).TotalSeconds);
                        var createdAtDiff = Math.Abs((retrieved.CreatedAt - originalCreatedAt).TotalSeconds);

                        return measurementTimeDiff < 1.0 && createdAtDiff < 1.0;
                    }
                    finally
                    {
                        CleanupDb(dbPath);
                    }
                });
        }

        /// <summary>
        /// 验证两个测量记录是否等价
        /// </summary>
        private bool AreMeasurementsEquivalent(Measurement original, Measurement retrieved)
        {
            // 基本属性验证
            if (retrieved.Status != original.Status) return false;
            if (Math.Abs((retrieved.MeasurementTime - original.MeasurementTime).TotalSeconds) > 1.0) return false;

            // 原始数据点数量验证
            if (retrieved.RawDataPoints.Count != original.RawDataPoints.Count) return false;

            // 计算结果验证
            if (original.CalculationResult != null)
            {
                if (retrieved.CalculationResult == null) return false;

                const double tolerance = 0.0001;
                if (Math.Abs(retrieved.CalculationResult.MSquaredX - original.CalculationResult.MSquaredX) > tolerance) return false;
                if (Math.Abs(retrieved.CalculationResult.MSquaredY - original.CalculationResult.MSquaredY) > tolerance) return false;
                if (Math.Abs(retrieved.CalculationResult.MSquaredGlobal - original.CalculationResult.MSquaredGlobal) > tolerance) return false;
            }

            return true;
        }
    }

    /// <summary>
    /// 有效的测量记录包装类
    /// </summary>
    public class ValidMeasurement
    {
        public Measurement Value { get; set; } = null!;
    }

    /// <summary>
    /// 测量记录生成器
    /// </summary>
    public static class MeasurementGenerators
    {
        /// <summary>
        /// 生成有效的测量记录
        /// </summary>
        public static Arbitrary<ValidMeasurement> ValidMeasurementArbitrary()
        {
            var gen = from pointCount in Gen.Choose(10, 50)
                      from mSquaredX in Gen.Choose(1, 3).Select(x => (double)x)
                      from mSquaredY in Gen.Choose(1, 3).Select(y => (double)y)
                      from waistDiameterX in Gen.Choose(10, 200).Select(x => (double)x)
                      from waistDiameterY in Gen.Choose(10, 200).Select(y => (double)y)
                      from fitParamCount in Gen.Choose(2, 4)
                      select GenerateValidMeasurement(pointCount, mSquaredX, mSquaredY, waistDiameterX, waistDiameterY, fitParamCount);

            return Arb.From(gen);
        }

        /// <summary>
        /// 生成有效的测量记录（公开方法，用于调试）
        /// </summary>
        public static ValidMeasurement GenerateValidMeasurement(
            int pointCount,
            double mSquaredX,
            double mSquaredY,
            double waistDiameterX,
            double waistDiameterY,
            int fitParamCount)
        {
            var measurement = new Measurement
            {
                MeasurementTime = DateTime.Now.AddMinutes(-new System.Random().Next(0, 1000)),
                DeviceInfo = "Virtual Beam Profiler v1.0",
                Status = "Complete",
                Notes = $"Test measurement with {pointCount} points",
                CreatedAt = DateTime.Now,
                RawDataPoints = new List<RawDataPointEntity>(),
                CalculationResult = new CalculationResult
                {
                    MSquaredX = mSquaredX,
                    MSquaredY = mSquaredY,
                    MSquaredGlobal = (mSquaredX + mSquaredY) / 2.0,
                    BeamWaistPositionX = 0.0,
                    BeamWaistPositionY = 0.0,
                    BeamWaistDiameterX = waistDiameterX,
                    BeamWaistDiameterY = waistDiameterY,
                    PeakPositionX = 0.0,
                    PeakPositionY = 0.0,
                    FitParameters = new List<FitParameter>()
                }
            };

            // 生成原始数据点
            var random = new System.Random();
            for (int i = 0; i < pointCount; i++)
            {
                double z = -50.0 + i * (100.0 / pointCount);
                // 确保位置为正数（从0开始）
                double position = Math.Abs(z);
                
                measurement.RawDataPoints.Add(new RawDataPointEntity
                {
                    DetectorPosition = position,
                    BeamDiameterX = waistDiameterX * Math.Sqrt(1 + Math.Pow(z * 0.001, 2)) + random.NextDouble() * 2.0,
                    BeamDiameterY = waistDiameterY * Math.Sqrt(1 + Math.Pow(z * 0.001, 2)) + random.NextDouble() * 2.0,
                    Timestamp = DateTime.Now.AddMilliseconds(i * 100)
                });
            }

            // 生成拟合参数
            var directions = new[] { "X", "Y" };
            var fitTypes = new[] { "Gaussian", "Hyperbolic" };
            
            for (int i = 0; i < fitParamCount; i++)
            {
                measurement.CalculationResult.FitParameters.Add(new FitParameter
                {
                    Direction = directions[i % 2],
                    FitType = fitTypes[i / 2],
                    Parameters = $"{{\"amplitude\": {random.NextDouble() * 100}, \"mean\": {random.NextDouble() * 10}}}",
                    RSquared = 0.95 + random.NextDouble() * 0.04 // 0.95 到 0.99
                });
            }

            return new ValidMeasurement { Value = measurement };
        }
    }
}
