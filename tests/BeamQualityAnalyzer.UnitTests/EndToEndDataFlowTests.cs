using Xunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.SignalR;
using BeamQualityAnalyzer.Core.Interfaces;
using BeamQualityAnalyzer.Core.Services;
using BeamQualityAnalyzer.Server.Hubs;
using BeamQualityAnalyzer.Server.Services;
using BeamQualityAnalyzer.Contracts.Messages;

namespace BeamQualityAnalyzer.UnitTests;

/// <summary>
/// 端到端数据流测试
/// 验证属性 14: 数据流完整性 - 数据应从采集服务流向客户端
/// </summary>
public class EndToEndDataFlowTests
{
    [Fact]
    public async Task DataFlow_FromAcquisitionToHub_ShouldBeComplete()
    {
        // Arrange - 设置服务容器
        var services = new ServiceCollection();
        
        // 添加日志
        services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Debug));
        
        // 添加核心服务
        services.AddSingleton<IDataAcquisitionService, VirtualBeamProfilerService>();
        services.AddSingleton<IAlgorithmService, BeamQualityAlgorithmService>();
        
        // 添加 SignalR（用于测试）
        services.AddSignalR();
        
        var serviceProvider = services.BuildServiceProvider();
        
        var dataAcquisitionService = serviceProvider.GetRequiredService<IDataAcquisitionService>();
        var algorithmService = serviceProvider.GetRequiredService<IAlgorithmService>();
        
        // 用于收集事件的列表
        var rawDataMessages = new List<RawDataReceivedMessage>();
        var deviceStatusMessages = new List<DeviceStatusMessage>();
        
        // 订阅事件
        dataAcquisitionService.RawDataReceived += (sender, e) =>
        {
            var message = new RawDataReceivedMessage
            {
                DataPoints = e.DataPoints.Select(dp => new RawDataPointDto
                {
                    DetectorPosition = dp.DetectorPosition,
                    BeamDiameterX = dp.BeamDiameterX,
                    BeamDiameterY = dp.BeamDiameterY,
                    Timestamp = dp.Timestamp
                }).ToArray(),
                Timestamp = e.Timestamp
            };
            rawDataMessages.Add(message);
        };
        
        dataAcquisitionService.DeviceStatusChanged += (sender, e) =>
        {
            var message = new DeviceStatusMessage
            {
                Status = e.Status.ToString(),
                Message = e.Message ?? string.Empty,
                Timestamp = e.Timestamp
            };
            deviceStatusMessages.Add(message);
        };
        
        // Act - 启动数据采集
        await dataAcquisitionService.StartAcquisitionAsync(CancellationToken.None);
        
        // 等待采集完成（虚拟服务会采集50个点，每个点100ms，总共约5秒）
        await Task.Delay(6000);
        
        // 停止采集
        await dataAcquisitionService.StopAcquisitionAsync();
        
        // Assert - 验证数据流完整性
        
        // 1. 验证接收到原始数据
        Assert.NotEmpty(rawDataMessages);
        Assert.True(rawDataMessages.Count >= 40, $"应该接收到至少40个数据点，实际: {rawDataMessages.Count}");
        
        // 2. 验证设备状态变化
        Assert.NotEmpty(deviceStatusMessages);
        Assert.Contains(deviceStatusMessages, m => m.Status == "Connected");
        Assert.Contains(deviceStatusMessages, m => m.Status == "Acquiring");
        Assert.Contains(deviceStatusMessages, m => m.Status == "Stopped");
        
        // 3. 验证数据点的连续性
        var allDataPoints = rawDataMessages.SelectMany(m => m.DataPoints).ToList();
        Assert.True(allDataPoints.Count >= 40, $"总数据点应该至少40个，实际: {allDataPoints.Count}");
        
        // 4. 验证数据点的有效性
        foreach (var dataPoint in allDataPoints)
        {
            Assert.True(dataPoint.BeamDiameterX > 0, "光束直径X应该大于0");
            Assert.True(dataPoint.BeamDiameterY > 0, "光束直径Y应该大于0");
        }
        
        // 5. 验证可以执行算法分析
        var parameters = new Core.Models.AnalysisParameters
        {
            Wavelength = 632.8,
            MinDataPoints = 10,
            FitTolerance = 0.001
        };
        
        var rawDataPoints = allDataPoints.Select(dp => new Core.Models.RawDataPoint
        {
            DetectorPosition = dp.DetectorPosition,
            BeamDiameterX = dp.BeamDiameterX,
            BeamDiameterY = dp.BeamDiameterY,
            Timestamp = dp.Timestamp
        }).ToArray();
        
        var analysisResult = await algorithmService.AnalyzeAsync(
            rawDataPoints,
            parameters,
            CancellationToken.None);
        
        // 6. 验证分析结果
        Assert.NotNull(analysisResult);
        Assert.True(analysisResult.MSquaredX >= 1.0, $"M²(X) 应该 >= 1.0，实际: {analysisResult.MSquaredX}");
        Assert.True(analysisResult.MSquaredY >= 1.0, $"M²(Y) 应该 >= 1.0，实际: {analysisResult.MSquaredY}");
        Assert.True(analysisResult.MSquaredGlobal >= 1.0, $"M²(Global) 应该 >= 1.0，实际: {analysisResult.MSquaredGlobal}");
        Assert.True(analysisResult.BeamWaistDiameterX > 0, "腰斑直径X应该大于0");
        Assert.True(analysisResult.BeamWaistDiameterY > 0, "腰斑直径Y应该大于0");
    }
    
    [Fact]
    public async Task EmergencyStop_ShouldImmediatelyStopDataFlow()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Warning));
        services.AddSingleton<IDataAcquisitionService, VirtualBeamProfilerService>();
        
        var serviceProvider = services.BuildServiceProvider();
        var dataAcquisitionService = serviceProvider.GetRequiredService<IDataAcquisitionService>();
        
        var dataPointCount = 0;
        dataAcquisitionService.RawDataReceived += (sender, e) =>
        {
            dataPointCount += e.DataPoints.Length;
        };
        
        // Act
        await dataAcquisitionService.StartAcquisitionAsync(CancellationToken.None);
        await Task.Delay(500); // 等待一些数据
        
        var countBeforeStop = dataPointCount;
        
        // 执行急停
        dataAcquisitionService.EmergencyStop();
        
        await Task.Delay(1000); // 等待确认停止
        
        var countAfterStop = dataPointCount;
        
        // Assert
        Assert.True(countBeforeStop > 0, "急停前应该有数据");
        Assert.Equal(countBeforeStop, countAfterStop); // 急停后不应该再有新数据
    }
}
