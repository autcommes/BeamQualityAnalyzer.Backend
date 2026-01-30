using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BeamQualityAnalyzer.Core.Models;
using BeamQualityAnalyzer.Core.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace BeamQualityAnalyzer.UnitTests
{
    /// <summary>
    /// 算法服务单元测试
    /// </summary>
    public class AlgorithmServiceTests
    {
        private readonly BeamQualityAlgorithmService _service;
        private readonly Mock<ILogger<BeamQualityAlgorithmService>> _mockLogger;

        public AlgorithmServiceTests()
        {
            _mockLogger = new Mock<ILogger<BeamQualityAlgorithmService>>();
            _service = new BeamQualityAlgorithmService(_mockLogger.Object);
        }

        #region 高斯拟合测试

        [Fact]
        public void FitGaussian_WithValidData_ReturnsValidResult()
        {
            // Arrange
            var positions = new[] { -2.0, -1.0, 0.0, 1.0, 2.0 };
            var diameters = new[] { 100.0, 110.0, 120.0, 110.0, 100.0 };

            // Act
            var result = _service.FitGaussian(positions, diameters);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.IsValid());
            Assert.InRange(result.RSquared, 0.0, 1.0);
            Assert.True(result.StandardDeviation > 0);
            Assert.NotNull(result.FittedCurve);
            Assert.Equal(positions.Length, result.FittedCurve.Length);
        }

        [Fact]
        public void FitGaussian_WithNullPositions_ThrowsArgumentNullException()
        {
#pragma warning disable CS8600, CS8604 // 测试中故意传入 null 以验证参数验证
            // Arrange
            double[] positions = null;
            var diameters = new[] { 100.0, 110.0, 120.0 };

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => _service.FitGaussian(positions, diameters));
#pragma warning restore CS8600, CS8604
        }

        [Fact]
        public void FitGaussian_WithNullDiameters_ThrowsArgumentNullException()
        {
#pragma warning disable CS8600, CS8604 // 测试中故意传入 null 以验证参数验证
            // Arrange
            var positions = new[] { 0.0, 1.0, 2.0 };
            double[] diameters = null;

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => _service.FitGaussian(positions, diameters));
#pragma warning restore CS8600, CS8604
        }

        [Fact]
        public void FitGaussian_WithMismatchedArrayLengths_ThrowsArgumentException()
        {
            // Arrange
            var positions = new[] { 0.0, 1.0, 2.0 };
            var diameters = new[] { 100.0, 110.0 };

            // Act & Assert
            Assert.Throws<ArgumentException>(() => _service.FitGaussian(positions, diameters));
        }

        [Fact]
        public void FitGaussian_WithInsufficientData_ThrowsArgumentException()
        {
            // Arrange
            var positions = new[] { 0.0, 1.0 };
            var diameters = new[] { 100.0, 110.0 };

            // Act & Assert
            Assert.Throws<ArgumentException>(() => _service.FitGaussian(positions, diameters));
        }

        #endregion

        #region 双曲线拟合测试

        [Fact]
        public void FitHyperbolic_WithValidData_ReturnsValidResult()
        {
            // Arrange
            var positions = new[] { -50.0, -25.0, 0.0, 25.0, 50.0 };
            var diameters = new[] { 150.0, 100.0, 50.0, 100.0, 150.0 };
            double wavelength = 632.8; // He-Ne 激光

            // Act
            var result = _service.FitHyperbolic(positions, diameters, wavelength);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.IsValid());
            Assert.True(result.WaistDiameter > 0);
            Assert.True(result.MSquared >= 1.0);
            Assert.InRange(result.RSquared, 0.0, 1.0);
            Assert.NotNull(result.FittedCurve);
            Assert.Equal(positions.Length, result.FittedCurve.Length);
        }

        [Fact]
        public void FitHyperbolic_WithNullPositions_ThrowsArgumentNullException()
        {
#pragma warning disable CS8600, CS8604 // 测试中故意传入 null 以验证参数验证
            // Arrange
            double[] positions = null;
            var diameters = new[] { 100.0, 110.0, 120.0 };
            double wavelength = 632.8;

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => _service.FitHyperbolic(positions, diameters, wavelength));
#pragma warning restore CS8600, CS8604
        }

        [Fact]
        public void FitHyperbolic_WithInvalidWavelength_ThrowsArgumentException()
        {
            // Arrange
            var positions = new[] { 0.0, 1.0, 2.0 };
            var diameters = new[] { 100.0, 110.0, 120.0 };
            double wavelength = -632.8; // 负波长

            // Act & Assert
            Assert.Throws<ArgumentException>(() => _service.FitHyperbolic(positions, diameters, wavelength));
        }

        #endregion

        #region M² 因子计算测试

        [Fact]
        public void CalculateMSquared_WithValidFitResult_ReturnsValueGreaterThanOne()
        {
            // Arrange
            var fitResult = new HyperbolicFitResult
            {
                WaistDiameter = 50.0,
                WaistPosition = 0.0,
                Wavelength = 632.8,
                MSquared = 1.2,
                RSquared = 0.95
            };

            // Act
            var mSquared = _service.CalculateMSquared(fitResult);

            // Assert
            Assert.True(mSquared >= 1.0);
            Assert.Equal(fitResult.MSquared, mSquared);
        }

        [Fact]
        public void CalculateMSquared_WithNullFitResult_ThrowsArgumentNullException()
        {
#pragma warning disable CS8600, CS8604 // 测试中故意传入 null 以验证参数验证
            // Arrange
            HyperbolicFitResult fitResult = null;

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => _service.CalculateMSquared(fitResult));
#pragma warning restore CS8600, CS8604
        }

        #endregion

        #region 腰斑参数计算测试

        [Fact]
        public void CalculateBeamWaist_WithValidFitResult_ReturnsPositiveDiameter()
        {
            // Arrange
            var fitResult = new HyperbolicFitResult
            {
                WaistDiameter = 50.0,
                WaistPosition = 10.0,
                Wavelength = 632.8,
                MSquared = 1.2,
                RSquared = 0.95
            };

            // Act
            var (position, diameter) = _service.CalculateBeamWaist(fitResult);

            // Assert
            Assert.True(diameter > 0);
            Assert.Equal(fitResult.WaistPosition, position);
            Assert.Equal(fitResult.WaistDiameter, diameter);
        }

        [Fact]
        public void CalculateBeamWaist_WithNullFitResult_ThrowsArgumentNullException()
        {
#pragma warning disable CS8600, CS8604 // 测试中故意传入 null 以验证参数验证
            // Arrange
            HyperbolicFitResult fitResult = null;

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => _service.CalculateBeamWaist(fitResult));
#pragma warning restore CS8600, CS8604
        }

        #endregion

        #region 完整分析测试

        [Fact]
        public async Task AnalyzeAsync_WithValidData_ReturnsCompleteResult()
        {
            // Arrange
            var dataPoints = GenerateTestDataPoints(15);
            var parameters = new AnalysisParameters
            {
                Wavelength = 632.8,
                MinDataPoints = 10,
                FitTolerance = 0.001
            };

            // Act
            var result = await _service.AnalyzeAsync(dataPoints, parameters, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.IsValid());
            Assert.NotNull(result.GaussianFitX);
            Assert.NotNull(result.GaussianFitY);
            Assert.NotNull(result.HyperbolicFitX);
            Assert.NotNull(result.HyperbolicFitY);
            Assert.True(result.MSquaredX >= 1.0);
            Assert.True(result.MSquaredY >= 1.0);
            Assert.True(result.MSquaredGlobal >= 1.0);
            Assert.True(result.BeamWaistDiameterX > 0);
            Assert.True(result.BeamWaistDiameterY > 0);
        }

        [Fact]
        public async Task AnalyzeAsync_WithNullDataPoints_ThrowsArgumentNullException()
        {
#pragma warning disable CS8600, CS8604 // 测试中故意传入 null 以验证参数验证
            // Arrange
            RawDataPoint[] dataPoints = null;
            var parameters = new AnalysisParameters();

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(
                () => _service.AnalyzeAsync(dataPoints, parameters, CancellationToken.None));
#pragma warning restore CS8600, CS8604
        }

        [Fact]
        public async Task AnalyzeAsync_WithInsufficientData_ThrowsArgumentException()
        {
            // Arrange
            var dataPoints = GenerateTestDataPoints(5);
            var parameters = new AnalysisParameters
            {
                MinDataPoints = 10
            };

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(
                () => _service.AnalyzeAsync(dataPoints, parameters, CancellationToken.None));
        }

        [Fact]
        public async Task AnalyzeAsync_WithCancellation_ThrowsOperationCanceledException()
        {
            // Arrange
            var dataPoints = GenerateTestDataPoints(15);
            var parameters = new AnalysisParameters();
            var cts = new CancellationTokenSource();
            cts.Cancel(); // 立即取消

            // Act & Assert
            await Assert.ThrowsAnyAsync<OperationCanceledException>(
                () => _service.AnalyzeAsync(dataPoints, parameters, cts.Token));
        }

        #endregion

        #region 辅助方法

        /// <summary>
        /// 生成测试数据点
        /// </summary>
        private RawDataPoint[] GenerateTestDataPoints(int count)
        {
            var dataPoints = new RawDataPoint[count];
            double waistDiameterX = 50.0;
            double waistDiameterY = 55.0;
            double wavelength = 632.8;
            double mSquared = 1.2;

            for (int i = 0; i < count; i++)
            {
                double z = -50.0 + i * (100.0 / count);
                
                // 双曲线公式
                double termX = (z * wavelength / 1000.0 * mSquared) / (Math.PI * waistDiameterX * waistDiameterX);
                double termY = (z * wavelength / 1000.0 * mSquared) / (Math.PI * waistDiameterY * waistDiameterY);
                
                double diameterX = waistDiameterX * Math.Sqrt(1 + termX * termX);
                double diameterY = waistDiameterY * Math.Sqrt(1 + termY * termY);

                dataPoints[i] = new RawDataPoint
                {
                    DetectorPosition = z,
                    BeamDiameterX = diameterX,
                    BeamDiameterY = diameterY,
                    Timestamp = DateTime.Now.AddMilliseconds(i * 100)
                };
            }

            return dataPoints;
        }

        #endregion
    }
}
