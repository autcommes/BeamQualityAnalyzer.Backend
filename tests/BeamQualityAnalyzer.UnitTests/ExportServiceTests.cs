using BeamQualityAnalyzer.Core.Interfaces;
using BeamQualityAnalyzer.Core.Models;
using BeamQualityAnalyzer.Core.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace BeamQualityAnalyzer.UnitTests;

/// <summary>
/// 导出服务单元测试
/// </summary>
public class ExportServiceTests : IDisposable
{
    private readonly Mock<ILogger<ExportService>> _mockLogger;
    private readonly IExportService _exportService;
    private readonly string _testOutputDirectory;

    public ExportServiceTests()
    {
        _mockLogger = new Mock<ILogger<ExportService>>();
        _exportService = new ExportService(_mockLogger.Object);
        _testOutputDirectory = Path.Combine(Path.GetTempPath(), "BeamAnalyzerTests", Guid.NewGuid().ToString());
    }

    [Fact]
    public async Task GenerateScreenshotPathAsync_ShouldReturnValidPath()
    {
        // Act
        var filePath = await _exportService.GenerateScreenshotPathAsync(_testOutputDirectory);

        // Assert
        Assert.NotNull(filePath);
        Assert.StartsWith(_testOutputDirectory, filePath);
        Assert.EndsWith(".png", filePath);
        Assert.Contains("screenshot_", filePath);
        Assert.True(Directory.Exists(_testOutputDirectory));
    }

    [Fact]
    public async Task GenerateScreenshotPathAsync_ShouldCreateDirectory_WhenNotExists()
    {
        // Arrange
        var nonExistentDirectory = Path.Combine(_testOutputDirectory, "screenshots");

        // Act
        var filePath = await _exportService.GenerateScreenshotPathAsync(nonExistentDirectory);

        // Assert
        Assert.True(Directory.Exists(nonExistentDirectory));
    }

    [Fact]
    public async Task GenerateReportAsync_ShouldGeneratePdfFile()
    {
        // Arrange
        var result = CreateSampleBeamAnalysisResult();
        var options = new ReportOptions
        {
            Title = "测试报告",
            DeviceInfo = "测试设备",
            OperatorName = "测试操作员",
            Notes = "这是一个测试报告",
            Include2DSpotImage = false,
            Include3DEnergyDistribution = false,
            IncludeRawDataTable = true
        };

        // Act
        var filePath = await _exportService.GenerateReportAsync(result, options, _testOutputDirectory);

        // Assert
        Assert.NotNull(filePath);
        Assert.True(File.Exists(filePath));
        Assert.EndsWith(".pdf", filePath);
        Assert.Contains("report_", filePath);

        // 验证文件大小（PDF文件应该大于0字节）
        var fileInfo = new FileInfo(filePath);
        Assert.True(fileInfo.Length > 0);
    }

    [Fact]
    public async Task GenerateReportAsync_ShouldThrowException_WhenResultIsNull()
    {
        // Arrange
        var options = new ReportOptions();

        // Act & Assert
#pragma warning disable CS8625 // 无法将 null 字面量转换为非 null 的引用类型。
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _exportService.GenerateReportAsync(null, options, _testOutputDirectory));
#pragma warning restore CS8625
    }

    [Fact]
    public async Task GenerateReportAsync_ShouldThrowException_WhenOptionsIsNull()
    {
        // Arrange
        var result = CreateSampleBeamAnalysisResult();

        // Act & Assert
#pragma warning disable CS8625 // 无法将 null 字面量转换为非 null 的引用类型。
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _exportService.GenerateReportAsync(result, null, _testOutputDirectory));
#pragma warning restore CS8625
    }

    [Fact]
    public async Task GenerateReportAsync_ShouldThrowException_WhenOutputDirectoryIsEmpty()
    {
        // Arrange
        var result = CreateSampleBeamAnalysisResult();
        var options = new ReportOptions();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _exportService.GenerateReportAsync(result, options, ""));
    }

    [Fact]
    public async Task GenerateReportAsync_ShouldIncludeAllParameters()
    {
        // Arrange
        var result = CreateSampleBeamAnalysisResult();
        var options = new ReportOptions
        {
            Title = "完整测试报告",
            DeviceInfo = "虚拟光束轮廓仪",
            OperatorName = "张三",
            Notes = "测试备注内容",
            Include2DSpotImage = false,
            Include3DEnergyDistribution = false,
            IncludeRawDataTable = true
        };

        // Act
        var filePath = await _exportService.GenerateReportAsync(result, options, _testOutputDirectory);

        // Assert
        Assert.True(File.Exists(filePath));
        
        // 验证文件大小合理（包含数据的PDF应该更大）
        var fileInfo = new FileInfo(filePath);
        Assert.True(fileInfo.Length > 1000); // 至少1KB
    }

    /// <summary>
    /// 创建示例光束分析结果
    /// </summary>
    private BeamAnalysisResult CreateSampleBeamAnalysisResult()
    {
        var rawData = new List<RawDataPoint>();
        for (int i = 0; i < 20; i++)
        {
            rawData.Add(new RawDataPoint
            {
                DetectorPosition = -50.0 + i * 5.0,
                BeamDiameterX = 100.0 + i * 2.0,
                BeamDiameterY = 105.0 + i * 2.0,
                Timestamp = DateTime.Now.AddSeconds(i)
            });
        }

        return new BeamAnalysisResult
        {
            Id = Guid.NewGuid(),
            MeasurementTime = DateTime.Now,
            RawData = rawData,
            MSquaredX = 1.2,
            MSquaredY = 1.3,
            MSquaredGlobal = 1.25,
            BeamWaistPositionX = 0.0,
            BeamWaistPositionY = 0.0,
            BeamWaistDiameterX = 50.0,
            BeamWaistDiameterY = 55.0,
            PeakPositionX = 0.0,
            PeakPositionY = 0.0,
            GaussianFitX = new GaussianFitResult
            {
                Amplitude = 1.0,
                Mean = 0.0,
                StandardDeviation = 10.0,
                Offset = 0.0,
                RSquared = 0.98,
                FittedCurve = new double[20]
            },
            GaussianFitY = new GaussianFitResult
            {
                Amplitude = 1.0,
                Mean = 0.0,
                StandardDeviation = 10.0,
                Offset = 0.0,
                RSquared = 0.97,
                FittedCurve = new double[20]
            },
            HyperbolicFitX = new HyperbolicFitResult
            {
                WaistDiameter = 50.0,
                WaistPosition = 0.0,
                Wavelength = 632.8,
                MSquared = 1.2,
                RSquared = 0.99,
                FittedCurve = new double[20]
            },
            HyperbolicFitY = new HyperbolicFitResult
            {
                WaistDiameter = 55.0,
                WaistPosition = 0.0,
                Wavelength = 632.8,
                MSquared = 1.3,
                RSquared = 0.98,
                FittedCurve = new double[20]
            }
        };
    }

    public void Dispose()
    {
        // 清理测试目录
        if (Directory.Exists(_testOutputDirectory))
        {
            try
            {
                Directory.Delete(_testOutputDirectory, true);
            }
            catch
            {
                // 忽略清理错误
            }
        }
    }
}
