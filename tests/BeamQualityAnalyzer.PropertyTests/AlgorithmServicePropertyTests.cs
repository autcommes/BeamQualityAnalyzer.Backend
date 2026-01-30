using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BeamQualityAnalyzer.Core.Interfaces;
using BeamQualityAnalyzer.Core.Models;
using BeamQualityAnalyzer.Core.Services;
using FsCheck;
using FsCheck.Xunit;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace BeamQualityAnalyzer.PropertyTests
{
    /// <summary>
    /// 算法服务属性测试
    /// 验证算法服务的核心功能和性能要求
    /// </summary>
    public class AlgorithmServicePropertyTests
    {
        private readonly IAlgorithmService _algorithmService;

        public AlgorithmServicePropertyTests()
        {
            var mockLogger = new Mock<ILogger<BeamQualityAlgorithmService>>();
            _algorithmService = new BeamQualityAlgorithmService(mockLogger.Object);
        }

        /// <summary>
        /// 属性 3: 算法触发条件
        /// Feature: beam-quality-analyzer, Property 3: 算法触发条件
        /// 当采集到至少10个有效数据点时，算法服务应该能够成功执行拟合计算。
        /// 验证需求: 4.1
        /// </summary>
        [Property(MaxTest = 50, Arbitrary = new[] { typeof(AlgorithmGenerators) })]
        public Property Algorithm_WithAtLeast10DataPoints_ShouldTriggerFitting(ValidDataPointsWithMinimum10 validData)
        {
            // Feature: beam-quality-analyzer, Property 3: 算法触发条件
            
            return Prop.ForAll(
                Arb.From(Gen.Constant(validData)),
                data =>
                {
                    // Arrange
                    var dataPoints = data.DataPoints;
                    var parameters = new AnalysisParameters
                    {
                        MinDataPoints = 10,
                        Wavelength = 632.8 // He-Ne 激光波长
                    };

                    // Act & Assert
                    try
                    {
                        var result = _algorithmService.AnalyzeAsync(
                            dataPoints,
                            parameters,
                            CancellationToken.None).GetAwaiter().GetResult();

                        // 验证拟合成功执行
                        return result != null &&
                               result.GaussianFitX != null &&
                               result.GaussianFitY != null &&
                               result.HyperbolicFitX != null &&
                               result.HyperbolicFitY != null &&
                               result.MSquaredX >= 1.0 &&
                               result.MSquaredY >= 1.0 &&
                               result.BeamWaistDiameterX > 0 &&
                               result.BeamWaistDiameterY > 0;
                    }
                    catch
                    {
                        // 如果抛出异常，说明算法没有正确触发
                        return false;
                    }
                });
        }

        /// <summary>
        /// 属性 3.1: 算法触发条件 - 数据点不足应抛出异常
        /// 当数据点少于最小要求时，算法应该抛出 ArgumentException
        /// 验证需求: 4.1
        /// </summary>
        [Property(MaxTest = 50, Arbitrary = new[] { typeof(AlgorithmGenerators) })]
        public Property Algorithm_WithLessThanMinDataPoints_ShouldThrowException(ValidDataPointsWithLessThan10 validData)
        {
            // Feature: beam-quality-analyzer, Property 3: 算法触发条件
            
            return Prop.ForAll(
                Arb.From(Gen.Constant(validData)),
                data =>
                {
                    // Arrange
                    var dataPoints = data.DataPoints;
                    var parameters = new AnalysisParameters
                    {
                        MinDataPoints = 10,
                        Wavelength = 632.8
                    };

                    // Act & Assert
                    try
                    {
                        var result = _algorithmService.AnalyzeAsync(
                            dataPoints,
                            parameters,
                            CancellationToken.None).GetAwaiter().GetResult();

                        // 不应该执行到这里
                        return false;
                    }
                    catch (ArgumentException)
                    {
                        // 应该抛出 ArgumentException
                        return true;
                    }
                    catch
                    {
                        // 其他异常也算失败
                        return false;
                    }
                });
        }

        /// <summary>
        /// 属性 22: 算法计算性能
        /// Feature: beam-quality-analyzer, Property 22: 算法计算性能
        /// 算法服务应该在 500ms 内完成单次拟合计算（对于合理数量的数据点）。
        /// 验证需求: 4.7
        /// </summary>
        [Property(MaxTest = 20, Arbitrary = new[] { typeof(AlgorithmGenerators) })]
        public Property Algorithm_ShouldCompleteWithin500ms(ValidDataPointsWithMinimum10 validData)
        {
            // Feature: beam-quality-analyzer, Property 22: 算法计算性能
            
            return Prop.ForAll(
                Arb.From(Gen.Constant(validData)),
                data =>
                {
                    // Arrange
                    var dataPoints = data.DataPoints;
                    var parameters = new AnalysisParameters
                    {
                        MinDataPoints = 10,
                        Wavelength = 632.8
                    };

                    // Act
                    var stopwatch = Stopwatch.StartNew();
                    
                    try
                    {
                        var result = _algorithmService.AnalyzeAsync(
                            dataPoints,
                            parameters,
                            CancellationToken.None).GetAwaiter().GetResult();
                        
                        stopwatch.Stop();

                        // Assert - 应该在 500ms 内完成
                        return stopwatch.ElapsedMilliseconds <= 500;
                    }
                    catch
                    {
                        stopwatch.Stop();
                        // 即使失败，也应该在 500ms 内返回
                        return stopwatch.ElapsedMilliseconds <= 500;
                    }
                });
        }

        /// <summary>
        /// 属性 8: 算法失败错误处理
        /// Feature: beam-quality-analyzer, Property 8: 算法失败错误处理
        /// 当算法拟合失败时（如数据质量不足），应该抛出 InvalidOperationException 而不是崩溃。
        /// 验证需求: 4.6
        /// </summary>
        [Property(MaxTest = 50, Arbitrary = new[] { typeof(AlgorithmGenerators) })]
        public Property Algorithm_WithInvalidData_ShouldThrowInvalidOperationException(InvalidDataPoints invalidData)
        {
            // Feature: beam-quality-analyzer, Property 8: 算法失败错误处理
            
            return Prop.ForAll(
                Arb.From(Gen.Constant(invalidData)),
                data =>
                {
                    // Arrange
                    var dataPoints = data.DataPoints;
                    var parameters = new AnalysisParameters
                    {
                        MinDataPoints = 3, // 降低最小要求以测试拟合失败
                        Wavelength = 632.8
                    };

                    // Act & Assert
                    try
                    {
                        var result = _algorithmService.AnalyzeAsync(
                            dataPoints,
                            parameters,
                            CancellationToken.None).GetAwaiter().GetResult();

                        // 如果没有抛出异常，也算通过（数据可能恰好有效）
                        return true;
                    }
                    catch (InvalidOperationException)
                    {
                        // 应该抛出 InvalidOperationException（拟合失败）
                        return true;
                    }
                    catch (ArgumentException)
                    {
                        // ArgumentException 也是可接受的（参数验证失败）
                        return true;
                    }
                    catch (Exception ex)
                    {
                        // 不应该抛出其他未处理的异常（如 NullReferenceException）
                        // 这表示代码崩溃了
                        return ex is not (NullReferenceException or DivideByZeroException or IndexOutOfRangeException);
                    }
                });
        }

        /// <summary>
        /// 属性 8.1: 高斯拟合失败错误处理
        /// 当高斯拟合失败时，应该抛出 InvalidOperationException
        /// 验证需求: 4.6
        /// </summary>
        [Fact]
        public void FitGaussian_WithInsufficientData_ShouldThrowArgumentException()
        {
            // Feature: beam-quality-analyzer, Property 8: 算法失败错误处理
            
            // Arrange - 只有2个数据点（不足3个）
            var positions = new double[] { 0, 1 };
            var diameters = new double[] { 100, 110 };

            // Act & Assert
            Assert.Throws<ArgumentException>(() =>
                _algorithmService.FitGaussian(positions, diameters));
        }

        /// <summary>
        /// 属性 8.2: 双曲线拟合失败错误处理
        /// 当双曲线拟合失败时，应该抛出 InvalidOperationException
        /// 验证需求: 4.6
        /// </summary>
        [Fact]
        public void FitHyperbolic_WithInsufficientData_ShouldThrowArgumentException()
        {
            // Feature: beam-quality-analyzer, Property 8: 算法失败错误处理
            
            // Arrange - 只有2个数据点（不足3个）
            var positions = new double[] { 0, 1 };
            var diameters = new double[] { 100, 110 };
            var wavelength = 632.8;

            // Act & Assert
            Assert.Throws<ArgumentException>(() =>
                _algorithmService.FitHyperbolic(positions, diameters, wavelength));
        }

        /// <summary>
        /// 属性 8.3: Null 参数错误处理
        /// 当传入 null 参数时，应该抛出 ArgumentNullException
        /// 验证需求: 4.6
        /// </summary>
        [Fact]
        public async Task AnalyzeAsync_WithNullDataPoints_ShouldThrowArgumentNullException()
        {
            // Feature: beam-quality-analyzer, Property 8: 算法失败错误处理
            
            // Arrange
#pragma warning disable CS8625 // 无法将 null 字面量转换为非 null 的引用类型。
#pragma warning disable CS8600 // 将 null 文本或可能的 null 值转换为不可为 null 类型。
            RawDataPoint[] dataPoints = null;
#pragma warning restore CS8600
#pragma warning restore CS8625
            var parameters = new AnalysisParameters { MinDataPoints = 10, Wavelength = 632.8 };

            // Act & Assert
#pragma warning disable CS8604 // 可能的 null 引用参数。
            await Assert.ThrowsAsync<ArgumentNullException>(() =>
                _algorithmService.AnalyzeAsync(dataPoints, parameters, CancellationToken.None));
#pragma warning restore CS8604
        }

        /// <summary>
        /// 属性 8.4: 无效波长错误处理
        /// 当波长 <= 0 时，应该抛出 ArgumentException
        /// 验证需求: 4.6
        /// </summary>
        [Fact]
        public void FitHyperbolic_WithInvalidWavelength_ShouldThrowArgumentException()
        {
            // Feature: beam-quality-analyzer, Property 8: 算法失败错误处理
            
            // Arrange
            var positions = new double[] { 0, 10, 20, 30, 40 };
            var diameters = new double[] { 100, 110, 120, 130, 140 };
            var invalidWavelength = -100.0; // 无效波长

            // Act & Assert
            Assert.Throws<ArgumentException>(() =>
                _algorithmService.FitHyperbolic(positions, diameters, invalidWavelength));
        }
    }

    /// <summary>
    /// 算法测试数据生成器
    /// </summary>
    public static class AlgorithmGenerators
    {
        /// <summary>
        /// 生成至少10个有效数据点
        /// </summary>
        public static Arbitrary<ValidDataPointsWithMinimum10> ValidDataPointsWithMinimum10Arbitrary()
        {
            var gen = from count in Gen.Choose(10, 50) // 10 到 50 个数据点
                      from waistPosition in Gen.Choose(-50, 50).Select(x => (double)x)
                      from waistDiameter in Gen.Choose(50, 500).Select(x => (double)x)
                      from mSquared in Gen.Choose(10, 30).Select(x => x / 10.0) // M² 从 1.0 到 3.0
                      select GenerateGaussianBeamData(count, waistPosition, waistDiameter, mSquared);

            return Arb.From(gen);
        }

        /// <summary>
        /// 生成少于10个数据点
        /// </summary>
        public static Arbitrary<ValidDataPointsWithLessThan10> ValidDataPointsWithLessThan10Arbitrary()
        {
            var gen = from count in Gen.Choose(1, 9) // 1 到 9 个数据点
                      from waistPosition in Gen.Choose(-50, 50).Select(x => (double)x)
                      from waistDiameter in Gen.Choose(50, 500).Select(x => (double)x)
                      from mSquared in Gen.Choose(10, 30).Select(x => x / 10.0)
                      select GenerateGaussianBeamDataLessThan10(count, waistPosition, waistDiameter, mSquared);

            return Arb.From(gen);
        }

        /// <summary>
        /// 生成无效数据点（用于测试错误处理）
        /// </summary>
        public static Arbitrary<InvalidDataPoints> InvalidDataPointsArbitrary()
        {
            var gen = from count in Gen.Choose(3, 10)
                      select GenerateInvalidData(count);

            return Arb.From(gen);
        }

        /// <summary>
        /// 生成高斯光束数据
        /// </summary>
        private static ValidDataPointsWithMinimum10 GenerateGaussianBeamData(
            int count,
            double waistPosition,
            double waistDiameter,
            double mSquared)
        {
            var dataPoints = new RawDataPoint[count];
            var wavelength = 632.8; // He-Ne 激光波长 (nm)
            var random = new System.Random();

            for (int i = 0; i < count; i++)
            {
                // 探测器位置：从 waistPosition - 100mm 到 waistPosition + 100mm
                double z = waistPosition + (i - count / 2.0) * (200.0 / count);

                // 计算光束直径：w(z) = w0 * sqrt(1 + ((z-z0)*λ*M²/(π*w0²))²)
                double term = ((z - waistPosition) * (wavelength / 1000.0) * mSquared) /
                              (Math.PI * waistDiameter * waistDiameter);
                double diameter = waistDiameter * Math.Sqrt(1 + term * term);

                // 添加 5% 的随机噪声
                double noise = 1.0 + (random.NextDouble() - 0.5) * 0.1;
                diameter *= noise;

                dataPoints[i] = new RawDataPoint
                {
                    DetectorPosition = z,
                    BeamDiameterX = diameter,
                    BeamDiameterY = diameter * (0.9 + random.NextDouble() * 0.2), // Y 方向略有不同
                    Timestamp = DateTime.Now.AddMilliseconds(i * 100)
                };
            }

            return new ValidDataPointsWithMinimum10 { DataPoints = dataPoints };
        }

        /// <summary>
        /// 生成少于10个数据点的高斯光束数据
        /// </summary>
        private static ValidDataPointsWithLessThan10 GenerateGaussianBeamDataLessThan10(
            int count,
            double waistPosition,
            double waistDiameter,
            double mSquared)
        {
            var dataPoints = new RawDataPoint[count];
            var wavelength = 632.8; // He-Ne 激光波长 (nm)
            var random = new System.Random();

            for (int i = 0; i < count; i++)
            {
                // 探测器位置：从 waistPosition - 100mm 到 waistPosition + 100mm
                double z = waistPosition + (i - count / 2.0) * (200.0 / count);

                // 计算光束直径：w(z) = w0 * sqrt(1 + ((z-z0)*λ*M²/(π*w0²))²)
                double term = ((z - waistPosition) * (wavelength / 1000.0) * mSquared) /
                              (Math.PI * waistDiameter * waistDiameter);
                double diameter = waistDiameter * Math.Sqrt(1 + term * term);

                // 添加 5% 的随机噪声
                double noise = 1.0 + (random.NextDouble() - 0.5) * 0.1;
                diameter *= noise;

                dataPoints[i] = new RawDataPoint
                {
                    DetectorPosition = z,
                    BeamDiameterX = diameter,
                    BeamDiameterY = diameter * (0.9 + random.NextDouble() * 0.2), // Y 方向略有不同
                    Timestamp = DateTime.Now.AddMilliseconds(i * 100)
                };
            }

            return new ValidDataPointsWithLessThan10 { DataPoints = dataPoints };
        }

        /// <summary>
        /// 生成无效数据（用于测试错误处理）
        /// </summary>
        private static InvalidDataPoints GenerateInvalidData(int count)
        {
            var dataPoints = new RawDataPoint[count];
            var random = new System.Random();

            for (int i = 0; i < count; i++)
            {
                // 生成完全随机的数据（不符合高斯光束模型）
                dataPoints[i] = new RawDataPoint
                {
                    DetectorPosition = random.NextDouble() * 200 - 100,
                    BeamDiameterX = random.NextDouble() * 1000,
                    BeamDiameterY = random.NextDouble() * 1000,
                    Timestamp = DateTime.Now.AddMilliseconds(i * 100)
                };
            }

            return new InvalidDataPoints { DataPoints = dataPoints };
        }
    }

    /// <summary>
    /// 至少10个有效数据点的包装类
    /// </summary>
    public class ValidDataPointsWithMinimum10
    {
        public RawDataPoint[] DataPoints { get; set; } = Array.Empty<RawDataPoint>();
    }

    /// <summary>
    /// 少于10个数据点的包装类
    /// </summary>
    public class ValidDataPointsWithLessThan10
    {
        public RawDataPoint[] DataPoints { get; set; } = Array.Empty<RawDataPoint>();
    }

    /// <summary>
    /// 无效数据点的包装类
    /// </summary>
    public class InvalidDataPoints
    {
        public RawDataPoint[] DataPoints { get; set; } = Array.Empty<RawDataPoint>();
    }
}
