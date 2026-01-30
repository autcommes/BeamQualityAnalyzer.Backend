using Xunit;
using Moq;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using BeamQualityAnalyzer.Core.Interfaces;
using BeamQualityAnalyzer.Core.Models;
using BeamQualityAnalyzer.Server.Hubs;
using BeamQualityAnalyzer.Server.Services;
using BeamQualityAnalyzer.Contracts.Messages;

namespace BeamQualityAnalyzer.UnitTests;

/// <summary>
/// Hub 事件桥接服务测试
/// </summary>
public class HubEventBridgeTests
{
    private readonly Mock<IHubContext<BeamAnalyzerHub>> _mockHubContext;
    private readonly Mock<IDataAcquisitionService> _mockDataAcquisitionService;
    private readonly Mock<IAlgorithmService> _mockAlgorithmService;
    private readonly Mock<ILogger<HubEventBridge>> _mockLogger;
    private readonly Mock<IHubClients> _mockClients;
    private readonly Mock<IClientProxy> _mockClientProxy;
    
    public HubEventBridgeTests()
    {
        _mockHubContext = new Mock<IHubContext<BeamAnalyzerHub>>();
        _mockDataAcquisitionService = new Mock<IDataAcquisitionService>();
        _mockAlgorithmService = new Mock<IAlgorithmService>();
        _mockLogger = new Mock<ILogger<HubEventBridge>>();
        _mockClients = new Mock<IHubClients>();
        _mockClientProxy = new Mock<IClientProxy>();
        
        // 设置 Hub Context 返回 Clients
        _mockHubContext.Setup(h => h.Clients).Returns(_mockClients.Object);
        
        // 设置 Clients 返回 ClientProxy
        _mockClients.Setup(c => c.Group(It.IsAny<string>())).Returns(_mockClientProxy.Object);
        _mockClients.Setup(c => c.All).Returns(_mockClientProxy.Object);
    }
    
    [Fact]
    public async Task StartAsync_ShouldSubscribeToServiceEvents()
    {
        // Arrange
        var bridge = new HubEventBridge(
            _mockHubContext.Object,
            _mockDataAcquisitionService.Object,
            _mockAlgorithmService.Object,
            _mockLogger.Object);
        
        // Act
        await bridge.StartAsync(CancellationToken.None);
        
        // Assert
        // 验证事件订阅（通过触发事件来验证）
        var eventArgs = new RawDataReceivedEventArgs
        {
            DataPoints = new[]
            {
                new RawDataPoint
                {
                    DetectorPosition = 0,
                    BeamDiameterX = 100,
                    BeamDiameterY = 105,
                    Timestamp = DateTime.Now
                }
            },
            Timestamp = DateTime.Now
        };
        
        // 触发事件
        _mockDataAcquisitionService.Raise(
            s => s.RawDataReceived += null,
            _mockDataAcquisitionService.Object,
            eventArgs);
        
        // 等待异步处理
        await Task.Delay(100);
        
        // 验证消息被推送到客户端
        _mockClientProxy.Verify(
            c => c.SendCoreAsync(
                "OnRawDataReceived",
                It.Is<object[]>(args => 
                    args.Length == 1 && 
                    args[0] != null &&
                    args[0].GetType() == typeof(RawDataReceivedMessage)),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
    
    [Fact]
    public async Task StopAsync_ShouldUnsubscribeFromServiceEvents()
    {
        // Arrange
        var bridge = new HubEventBridge(
            _mockHubContext.Object,
            _mockDataAcquisitionService.Object,
            _mockAlgorithmService.Object,
            _mockLogger.Object);
        
        await bridge.StartAsync(CancellationToken.None);
        
        // Act
        await bridge.StopAsync(CancellationToken.None);
        
        // Assert
        // 触发事件，验证不再处理
        var eventArgs = new RawDataReceivedEventArgs
        {
            DataPoints = new[]
            {
                new RawDataPoint
                {
                    DetectorPosition = 0,
                    BeamDiameterX = 100,
                    BeamDiameterY = 105,
                    Timestamp = DateTime.Now
                }
            },
            Timestamp = DateTime.Now
        };
        
        _mockDataAcquisitionService.Raise(
            s => s.RawDataReceived += null,
            _mockDataAcquisitionService.Object,
            eventArgs);
        
        await Task.Delay(100);
        
        // 验证消息没有被推送（因为已经取消订阅）
        _mockClientProxy.Verify(
            c => c.SendCoreAsync(
                "OnRawDataReceived",
                It.IsAny<object[]>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }
    
    [Fact]
    public async Task OnDeviceStatusChanged_ShouldBroadcastToAllClients()
    {
        // Arrange
        var bridge = new HubEventBridge(
            _mockHubContext.Object,
            _mockDataAcquisitionService.Object,
            _mockAlgorithmService.Object,
            _mockLogger.Object);
        
        await bridge.StartAsync(CancellationToken.None);
        
        // Act
        var eventArgs = new DeviceStatusChangedEventArgs
        {
            Status = DeviceStatus.Acquiring,
            Message = "数据采集中",
            Timestamp = DateTime.Now
        };
        
        _mockDataAcquisitionService.Raise(
            s => s.DeviceStatusChanged += null,
            _mockDataAcquisitionService.Object,
            eventArgs);
        
        await Task.Delay(100);
        
        // Assert
        // 验证设备状态消息被推送
        _mockClientProxy.Verify(
            c => c.SendCoreAsync(
                "OnDeviceStatusChanged",
                It.Is<object[]>(args => 
                    args.Length == 1 && 
                    args[0] != null &&
                    args[0].GetType() == typeof(DeviceStatusMessage) &&
                    ((DeviceStatusMessage)args[0]).Status == "Acquiring"),
                It.IsAny<CancellationToken>()),
            Times.Once);
        
        // 验证采集状态消息被推送
        _mockClientProxy.Verify(
            c => c.SendCoreAsync(
                "OnAcquisitionStatusChanged",
                It.Is<object[]>(args => 
                    args.Length == 1 && 
                    args[0] != null &&
                    args[0].GetType() == typeof(AcquisitionStatusMessage) &&
                    ((AcquisitionStatusMessage)args[0]).IsAcquiring == true),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
    
    [Fact]
    public async Task OnRawDataReceived_WithEnoughData_ShouldTriggerAnalysis()
    {
        // Arrange
        var mockResult = new BeamAnalysisResult
        {
            MeasurementTime = DateTime.Now,
            MSquaredX = 1.2,
            MSquaredY = 1.3,
            MSquaredGlobal = 1.25,
            BeamWaistPositionX = 0,
            BeamWaistPositionY = 0,
            BeamWaistDiameterX = 50,
            BeamWaistDiameterY = 55,
            PeakPositionX = 0,
            PeakPositionY = 0,
            GaussianFitX = new GaussianFitResult
            {
                Amplitude = 100,
                Mean = 0,
                StandardDeviation = 10,
                Offset = 0,
                RSquared = 0.95,
                FittedCurve = new double[10]
            },
            GaussianFitY = new GaussianFitResult
            {
                Amplitude = 105,
                Mean = 0,
                StandardDeviation = 11,
                Offset = 0,
                RSquared = 0.96,
                FittedCurve = new double[10]
            },
            HyperbolicFitX = new HyperbolicFitResult
            {
                WaistDiameter = 50,
                WaistPosition = 0,
                Wavelength = 632.8,
                MSquared = 1.2,
                RSquared = 0.97,
                FittedCurve = new double[10]
            },
            HyperbolicFitY = new HyperbolicFitResult
            {
                WaistDiameter = 55,
                WaistPosition = 0,
                Wavelength = 632.8,
                MSquared = 1.3,
                RSquared = 0.98,
                FittedCurve = new double[10]
            }
        };
        
        _mockAlgorithmService
            .Setup(a => a.AnalyzeAsync(
                It.IsAny<RawDataPoint[]>(),
                It.IsAny<AnalysisParameters>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockResult);
        
        var bridge = new HubEventBridge(
            _mockHubContext.Object,
            _mockDataAcquisitionService.Object,
            _mockAlgorithmService.Object,
            _mockLogger.Object);
        
        await bridge.StartAsync(CancellationToken.None);
        
        // Act - 发送10个数据点
        for (int i = 0; i < 10; i++)
        {
            var eventArgs = new RawDataReceivedEventArgs
            {
                DataPoints = new[]
                {
                    new RawDataPoint
                    {
                        DetectorPosition = i,
                        BeamDiameterX = 100 + i,
                        BeamDiameterY = 105 + i,
                        Timestamp = DateTime.Now
                    }
                },
                Timestamp = DateTime.Now
            };
            
            _mockDataAcquisitionService.Raise(
                s => s.RawDataReceived += null,
                _mockDataAcquisitionService.Object,
                eventArgs);
        }
        
        // 等待异步分析完成
        await Task.Delay(500);
        
        // Assert
        // 验证算法服务被调用
        _mockAlgorithmService.Verify(
            a => a.AnalyzeAsync(
                It.Is<RawDataPoint[]>(points => points.Length == 10),
                It.IsAny<AnalysisParameters>(),
                It.IsAny<CancellationToken>()),
            Times.AtLeastOnce);
        
        // 验证计算完成消息被推送
        _mockClientProxy.Verify(
            c => c.SendCoreAsync(
                "OnCalculationCompleted",
                It.Is<object[]>(args => 
                    args.Length == 1 && 
                    args[0] != null &&
                    args[0].GetType() == typeof(CalculationCompletedMessage)),
                It.IsAny<CancellationToken>()),
            Times.AtLeastOnce);
    }
}
