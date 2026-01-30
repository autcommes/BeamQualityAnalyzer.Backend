using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BeamQualityAnalyzer.Core.Interfaces;
using BeamQualityAnalyzer.Core.Models;
using BeamQualityAnalyzer.Core.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace BeamQualityAnalyzer.PropertyTests
{
    /// <summary>
    /// 自动测试服务属性测试
    /// 验证自动测试服务的工作流完整性和报告生成功能
    /// </summary>
    public class AutoTestServicePropertyTests
    {
        /// <summary>
        /// 属性 34: 自动测试工作流完整性
        /// Feature: beam-quality-analyzer, Property 34: 自动测试工作流完整性
        /// 自动测试应该依次执行所有步骤：设备复位 -> 数据采集 -> 算法计算 -> 结果验证
        /// 验证需求: 12.1, 12.2, 12.4, 12.5
        /// </summary>
        [Fact]
        public async Task Property34_AutoTestWorkflow_ShouldExecuteAllStepsInOrder()
        {
            // Feature: beam-quality-analyzer, Property 34: 自动测试工作流完整性
            
            // Arrange
            var executedSteps = new List<string>();
            var stepTimestamps = new Dictionary<string, DateTime>();
            
            // Mock 数据采集服务
            var mockDataAcquisition = new Mock<IDataAcquisitionService>();
            mockDataAcquisition.Setup(s => s.ResetDeviceAsync())
                .Callback(() =>
                {
                    executedSteps.Add("DeviceReset");
                    stepTimestamps["DeviceReset"] = DateTime.Now;
                })
                .Returns(Task.CompletedTask);
            
            mockDataAcquisition.Setup(s => s.StartAcquisitionAsync(It.IsAny<CancellationToken>()))
                .Callback(() =>
                {
                    executedSteps.Add("StartAcquisition");
                    stepTimestamps["StartAcquisition"] = DateTime.Now;
                })
                .Returns(Task.CompletedTask);
            
            mockDataAcquisition.Setup(s => s.StopAcquisitionAsync())
                .Callback(() =>
                {
                    executedSteps.Add("StopAcquisition");
                    stepTimestamps["StopAcquisition"] = DateTime.Now;
                })
                .Returns(Task.CompletedTask);
            
            mockDataAcquisition.SetupGet(s => s.IsAcquiring).Returns(false);
            
            // 模拟数据接收事件
            mockDataAcquisition
                .Setup(s => s.StartAcquisitionAsync(It.IsAny<CancellationToken>()))
                .Callback<CancellationToken>(async (ct) =>
                {
                    executedSteps.Add("StartAcquisition");
                    stepTimestamps["StartAcquisition"] = DateTime.Now;
                    
                    // 延迟触发数据接收事件
                    await Task.Delay(100, ct);
                    
                    var eventArgs = new RawDataReceivedEventArgs
                    {
                        DataPoints = GenerateTestDataPoints(25).ToArray()
                    };
                    
                    mockDataAcquisition.Raise(
                        s => s.RawDataReceived += null,
                        mockDataAcquisition.Object,
                        eventArgs);
                })
                .Returns(Task.CompletedTask);
            
            // Mock 算法服务
            var mockAlgorithm = new Mock<IAlgorithmService>();
            mockAlgorithm.Setup(s => s.AnalyzeAsync(
                    It.IsAny<RawDataPoint[]>(),
                    It.IsAny<AnalysisParameters>(),
                    It.IsAny<CancellationToken>()))
                .Callback(() =>
                {
                    executedSteps.Add("AlgorithmCalculation");
                    stepTimestamps["AlgorithmCalculation"] = DateTime.Now;
                })
                .ReturnsAsync(new BeamAnalysisResult
                {
                    MeasurementTime = DateTime.Now,
                    MSquaredX = 1.05,
                    MSquaredY = 1.08,
                    BeamWaistDiameterX = 50.0,
                    BeamWaistDiameterY = 52.0,
                    BeamWaistPositionX = 100.0,
                    BeamWaistPositionY = 100.0,
                    PeakPositionX = 100.0,
                    PeakPositionY = 100.0
                });
            
            var logger = new Mock<ILogger<AutoTestService>>();
            var autoTestService = new AutoTestService(
                mockDataAcquisition.Object,
                mockAlgorithm.Object,
                logger.Object);
            
            // 创建测试配置
            var config = new AutoTestConfiguration
            {
                TestCycles = 2,
                EnableWarmup = false, // 禁用预热以简化测试
                DataPointsPerCycle = 20,
                IntervalBetweenCyclesSeconds = 0,
                GenerateReport = false, // 暂不生成报告
                AnalysisParameters = new AnalysisParameters
                {
                    Wavelength = 1064.0,
                    FitTolerance = 0.001
                }
            };
            
            var progressSteps = new List<string>();
            var progress = new Progress<AutoTestProgress>(p =>
            {
                progressSteps.Add(p.CurrentStep);
            });
            
            // Act
            var result = await autoTestService.RunAutoTestAsync(
                config,
                progress,
                CancellationToken.None);
            
            // Assert - 验证工作流步骤完整性
            Assert.True(result.IsSuccess, $"自动测试应该成功完成。失败原因: {result.FailureReason}");
            Assert.Equal(2, result.CompletedCycles);
            
            // 验证关键步骤都被执行
            Assert.Contains("DeviceReset", executedSteps);
            Assert.Contains("StartAcquisition", executedSteps);
            Assert.Contains("AlgorithmCalculation", executedSteps);
            Assert.Contains("StopAcquisition", executedSteps);
            
            // 验证步骤执行顺序
            var resetIndex = executedSteps.IndexOf("DeviceReset");
            var firstAcquisitionIndex = executedSteps.IndexOf("StartAcquisition");
            var firstCalculationIndex = executedSteps.IndexOf("AlgorithmCalculation");
            
            Assert.True(resetIndex < firstAcquisitionIndex,
                "设备复位应该在数据采集之前执行");
            Assert.True(firstAcquisitionIndex < firstCalculationIndex,
                "数据采集应该在算法计算之前执行");
            
            // 验证每个测试循环都执行了采集和计算
            var acquisitionCount = executedSteps.Count(s => s == "StartAcquisition");
            var calculationCount = executedSteps.Count(s => s == "AlgorithmCalculation");
            
            Assert.True(acquisitionCount >= config.TestCycles,
                $"应该执行 {config.TestCycles} 次数据采集，实际执行了 {acquisitionCount} 次");
            Assert.Equal(config.TestCycles, calculationCount);
            
            // 验证进度报告包含关键步骤
            Assert.Contains(progressSteps, s => s.Contains("设备复位") || s.Contains("DeviceReset"));
            Assert.Contains(progressSteps, s => s.Contains("测试循环") || s.Contains("TestCycle"));
            Assert.Contains(progressSteps, s => s.Contains("完成") || s.Contains("Complete"));
            
            // 验证测试结果包含所有循环的数据
            Assert.Equal(config.TestCycles, result.CycleResults.Count);
            
            // 验证每个循环的结果都有效
            foreach (var cycleResult in result.CycleResults)
            {
                Assert.True(cycleResult.MSquaredX >= 1.0, "M²X 应该 >= 1.0");
                Assert.True(cycleResult.MSquaredY >= 1.0, "M²Y 应该 >= 1.0");
                Assert.True(cycleResult.BeamWaistDiameterX > 0, "腰斑直径X 应该 > 0");
                Assert.True(cycleResult.BeamWaistDiameterY > 0, "腰斑直径Y 应该 > 0");
            }
            
            // 验证统计数据已计算
            Assert.NotNull(result.Statistics);
            Assert.True(result.Statistics.MSquaredXAverage >= 1.0);
            Assert.True(result.Statistics.MSquaredYAverage >= 1.0);
        }
        
        /// <summary>
        /// 属性 35: 自动测试报告生成
        /// Feature: beam-quality-analyzer, Property 35: 自动测试报告生成
        /// 自动测试完成后应该生成包含测量数据和统计信息的测试报告
        /// 验证需求: 12.4, 12.5
        /// </summary>
        [Fact]
        public async Task Property35_AutoTestReport_ShouldBeGenerated()
        {
            // Feature: beam-quality-analyzer, Property 35: 自动测试报告生成
            
            // Arrange
            var mockDataAcquisition = new Mock<IDataAcquisitionService>();
            mockDataAcquisition.Setup(s => s.ResetDeviceAsync())
                .Returns(Task.CompletedTask);
            
            mockDataAcquisition.Setup(s => s.StartAcquisitionAsync(It.IsAny<CancellationToken>()))
                .Callback<CancellationToken>(async (ct) =>
                {
                    await Task.Delay(100, ct);
                    
                    var eventArgs = new RawDataReceivedEventArgs
                    {
                        DataPoints = GenerateTestDataPoints(25).ToArray()
                    };
                    
                    mockDataAcquisition.Raise(
                        s => s.RawDataReceived += null,
                        mockDataAcquisition.Object,
                        eventArgs);
                })
                .Returns(Task.CompletedTask);
            
            mockDataAcquisition.Setup(s => s.StopAcquisitionAsync())
                .Returns(Task.CompletedTask);
            
            mockDataAcquisition.SetupGet(s => s.IsAcquiring).Returns(false);
            
            var mockAlgorithm = new Mock<IAlgorithmService>();
            mockAlgorithm.Setup(s => s.AnalyzeAsync(
                    It.IsAny<RawDataPoint[]>(),
                    It.IsAny<AnalysisParameters>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new BeamAnalysisResult
                {
                    MeasurementTime = DateTime.Now,
                    MSquaredX = 1.05,
                    MSquaredY = 1.08,
                    BeamWaistDiameterX = 50.0,
                    BeamWaistDiameterY = 52.0,
                    BeamWaistPositionX = 100.0,
                    BeamWaistPositionY = 100.0,
                    PeakPositionX = 100.0,
                    PeakPositionY = 100.0
                });
            
            var logger = new Mock<ILogger<AutoTestService>>();
            var autoTestService = new AutoTestService(
                mockDataAcquisition.Object,
                mockAlgorithm.Object,
                logger.Object);
            
            // 创建测试配置（启用报告生成）
            var config = new AutoTestConfiguration
            {
                TestCycles = 3,
                EnableWarmup = false,
                DataPointsPerCycle = 20,
                IntervalBetweenCyclesSeconds = 0,
                GenerateReport = true, // 启用报告生成
                AnalysisParameters = new AnalysisParameters
                {
                    Wavelength = 1064.0,
                    FitTolerance = 0.001
                }
            };
            
            var progress = new Progress<AutoTestProgress>();
            
            // Act
            var result = await autoTestService.RunAutoTestAsync(
                config,
                progress,
                CancellationToken.None);
            
            // Assert - 验证报告生成
            Assert.True(result.IsSuccess, $"自动测试应该成功完成。失败原因: {result.FailureReason}");
            Assert.NotNull(result.ReportFilePath);
            Assert.NotEmpty(result.ReportFilePath);
            
            // 验证报告文件存在
            Assert.True(File.Exists(result.ReportFilePath),
                $"报告文件应该存在: {result.ReportFilePath}");
            
            // 验证报告文件不为空
            var fileInfo = new FileInfo(result.ReportFilePath);
            Assert.True(fileInfo.Length > 0, "报告文件不应该为空");
            
            // 读取报告内容并验证
            var reportContent = await File.ReadAllTextAsync(result.ReportFilePath);
            
            // 输出报告内容用于调试
            System.Diagnostics.Debug.WriteLine("=== 报告内容 ===");
            System.Diagnostics.Debug.WriteLine(reportContent);
            System.Diagnostics.Debug.WriteLine("=== 报告内容结束 ===");
            
            // 验证报告包含必需的元素
            Assert.Contains("光束质量分析系统自动测试报告", reportContent);
            Assert.Contains("测试ID", reportContent);
            Assert.Contains("开始时间", reportContent);
            Assert.Contains("结束时间", reportContent);
            Assert.Contains("测试状态", reportContent);
            Assert.Contains("完成循环数", reportContent);
            
            // 验证报告包含统计数据
            Assert.Contains("统计数据", reportContent);
            Assert.Contains("M² 因子 X 方向", reportContent);
            Assert.Contains("M² 因子 Y 方向", reportContent);
            Assert.Contains("腰斑直径 X 方向", reportContent);
            Assert.Contains("腰斑直径 Y 方向", reportContent);
            Assert.Contains("平均值", reportContent);
            Assert.Contains("标准差", reportContent);
            
            // 验证报告包含各循环详细结果
            Assert.Contains("各循环详细结果", reportContent);
            Assert.Contains("循环 1:", reportContent);
            Assert.Contains("循环 2:", reportContent);
            Assert.Contains("循环 3:", reportContent);
            
            // 验证报告包含测试ID（默认GUID格式，带连字符）
            Assert.Contains(result.TestId.ToString(), reportContent);
            
            // 验证报告包含状态信息（成功或失败）
            // 注意：报告中的状态可能是"成功"或"失败"
            Assert.True(reportContent.Contains("成功") || reportContent.Contains("失败"),
                "报告应该包含测试状态（成功或失败）");
            
            // 验证报告包含正确的循环数
            Assert.Contains($"完成循环数: {result.CompletedCycles}", reportContent);
            
            // 验证统计数据在报告中
            if (result.Statistics != null)
            {
                Assert.Contains($"{result.Statistics.MSquaredXAverage:F4}", reportContent);
                Assert.Contains($"{result.Statistics.MSquaredYAverage:F4}", reportContent);
            }
            
            // Cleanup - 删除测试生成的报告文件
            try
            {
                if (File.Exists(result.ReportFilePath))
                {
                    File.Delete(result.ReportFilePath);
                }
            }
            catch
            {
                // 忽略清理错误
            }
        }
        
        /// <summary>
        /// 属性 34.1: 自动测试失败时应该记录失败步骤
        /// 当测试过程中某个步骤失败时，应该记录失败的步骤和原因
        /// 验证需求: 12.5
        /// </summary>
        [Fact]
        public async Task Property34_1_AutoTestWorkflow_ShouldRecordFailedStep()
        {
            // Feature: beam-quality-analyzer, Property 34: 自动测试工作流完整性
            
            // Arrange - 模拟设备复位失败
            var mockDataAcquisition = new Mock<IDataAcquisitionService>();
            mockDataAcquisition.Setup(s => s.ResetDeviceAsync())
                .ThrowsAsync(new InvalidOperationException("设备复位失败：设备未响应"));
            
            var mockAlgorithm = new Mock<IAlgorithmService>();
            var logger = new Mock<ILogger<AutoTestService>>();
            
            var autoTestService = new AutoTestService(
                mockDataAcquisition.Object,
                mockAlgorithm.Object,
                logger.Object);
            
            var config = new AutoTestConfiguration
            {
                TestCycles = 2,
                EnableWarmup = false,
                DataPointsPerCycle = 20,
                IntervalBetweenCyclesSeconds = 0,
                GenerateReport = false,
                AnalysisParameters = new AnalysisParameters
                {
                    Wavelength = 1064.0,
                    FitTolerance = 0.001
                }
            };
            
            var progress = new Progress<AutoTestProgress>();
            
            // Act
            var result = await autoTestService.RunAutoTestAsync(
                config,
                progress,
                CancellationToken.None);
            
            // Assert - 验证失败信息被记录
            Assert.False(result.IsSuccess, "测试应该失败");
            Assert.NotNull(result.FailureReason);
            Assert.NotEmpty(result.FailureReason);
            Assert.Contains("设备复位失败", result.FailureReason);
            
            // 验证完成的循环数为0（因为在第一步就失败了）
            Assert.Equal(0, result.CompletedCycles);
        }
        
        /// <summary>
        /// 属性 35.1: 报告文件名应该包含时间戳
        /// 报告文件名应该包含测试ID和时间戳，确保唯一性
        /// 验证需求: 12.4
        /// </summary>
        [Fact]
        public async Task Property35_1_AutoTestReport_FileNameShouldContainTimestamp()
        {
            // Feature: beam-quality-analyzer, Property 35: 自动测试报告生成
            
            // Arrange
            var mockDataAcquisition = new Mock<IDataAcquisitionService>();
            mockDataAcquisition.Setup(s => s.ResetDeviceAsync())
                .Returns(Task.CompletedTask);
            
            mockDataAcquisition.Setup(s => s.StartAcquisitionAsync(It.IsAny<CancellationToken>()))
                .Callback<CancellationToken>(async (ct) =>
                {
                    await Task.Delay(100, ct);
                    
                    var eventArgs = new RawDataReceivedEventArgs
                    {
                        DataPoints = GenerateTestDataPoints(25).ToArray()
                    };
                    
                    mockDataAcquisition.Raise(
                        s => s.RawDataReceived += null,
                        mockDataAcquisition.Object,
                        eventArgs);
                })
                .Returns(Task.CompletedTask);
            
            mockDataAcquisition.Setup(s => s.StopAcquisitionAsync())
                .Returns(Task.CompletedTask);
            
            mockDataAcquisition.SetupGet(s => s.IsAcquiring).Returns(false);
            
            var mockAlgorithm = new Mock<IAlgorithmService>();
            mockAlgorithm.Setup(s => s.AnalyzeAsync(
                    It.IsAny<RawDataPoint[]>(),
                    It.IsAny<AnalysisParameters>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new BeamAnalysisResult
                {
                    MeasurementTime = DateTime.Now,
                    MSquaredX = 1.05,
                    MSquaredY = 1.08,
                    BeamWaistDiameterX = 50.0,
                    BeamWaistDiameterY = 52.0,
                    BeamWaistPositionX = 100.0,
                    BeamWaistPositionY = 100.0,
                    PeakPositionX = 100.0,
                    PeakPositionY = 100.0
                });
            
            var logger = new Mock<ILogger<AutoTestService>>();
            var autoTestService = new AutoTestService(
                mockDataAcquisition.Object,
                mockAlgorithm.Object,
                logger.Object);
            
            var config = new AutoTestConfiguration
            {
                TestCycles = 1,
                EnableWarmup = false,
                DataPointsPerCycle = 20,
                IntervalBetweenCyclesSeconds = 0,
                GenerateReport = true,
                AnalysisParameters = new AnalysisParameters
                {
                    Wavelength = 1064.0,
                    FitTolerance = 0.001
                }
            };
            
            var progress = new Progress<AutoTestProgress>();
            
            // Act
            var result = await autoTestService.RunAutoTestAsync(
                config,
                progress,
                CancellationToken.None);
            
            // Assert - 验证文件名格式
            Assert.NotNull(result.ReportFilePath);
            
            var fileName = Path.GetFileName(result.ReportFilePath);
            
            // 验证文件名包含 "AutoTest_" 前缀
            Assert.StartsWith("AutoTest_", fileName);
            
            // 验证文件名包含测试ID（GUID格式，无连字符）
            Assert.Contains(result.TestId.ToString("N"), fileName);
            
            // 验证文件名包含时间戳（yyyyMMdd_HHmmss 格式）
            var datePattern = @"\d{8}_\d{6}";
            Assert.Matches(datePattern, fileName);
            
            // 验证文件扩展名为 .txt
            Assert.Equal(".txt", Path.GetExtension(result.ReportFilePath));
            
            // Cleanup
            try
            {
                if (File.Exists(result.ReportFilePath))
                {
                    File.Delete(result.ReportFilePath);
                }
            }
            catch
            {
                // 忽略清理错误
            }
        }
        
        /// <summary>
        /// 生成测试用的数据点
        /// </summary>
        private List<RawDataPoint> GenerateTestDataPoints(int count)
        {
            var dataPoints = new List<RawDataPoint>();
            var random = new System.Random(42); // 固定种子以确保可重复性
            
            for (int i = 0; i < count; i++)
            {
                var position = 50.0 + i * 10.0; // 50mm 到 290mm
                var diameter = 100.0 + position * 0.1 + random.NextDouble() * 5.0;
                
                dataPoints.Add(new RawDataPoint
                {
                    DetectorPosition = position,
                    BeamDiameterX = diameter,
                    BeamDiameterY = diameter * 1.05, // Y方向稍大一些
                    Timestamp = DateTime.Now.AddMilliseconds(i * 100)
                });
            }
            
            return dataPoints;
        }
    }
}
