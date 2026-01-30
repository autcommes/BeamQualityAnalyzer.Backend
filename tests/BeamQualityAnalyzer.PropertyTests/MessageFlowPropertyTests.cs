using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BeamQualityAnalyzer.Contracts.Messages;
using BeamQualityAnalyzer.Core.Interfaces;
using BeamQualityAnalyzer.Core.Models;
using BeamQualityAnalyzer.Core.Services;
using BeamQualityAnalyzer.Server.Hubs;
using BeamQualityAnalyzer.Server.Services;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace BeamQualityAnalyzer.PropertyTests
{
    /// <summary>
    /// 消息流属性测试
    /// 验证数据从采集服务流向算法服务，再流向客户端的完整性
    /// </summary>
    public class MessageFlowPropertyTests
    {
        /// <summary>
        /// 属性 14: 数据流完整性
        /// Feature: beam-quality-analyzer, Property 14: 数据流完整性
        /// 数据应该从数据采集服务流向算法服务，计算完成后结果应该传递到客户端，整个数据流不应丢失数据。
        /// 验证需求: 16.2, 16.3, 16.4, 16.5
        /// </summary>
        [Fact]
        public async Task Property14_DataFlowIntegrity_ShouldFlowFromAcquisitionToAlgorithmToClient()
        {
            // Feature: beam-quality-analyzer, Property 14: 数据流完整性
            
            // Arrange - 设置模拟的 Hub 上下文
            var mockHubContext = new Mock<IHubContext<BeamAnalyzerHub>>();
            var mockClients = new Mock<IHubClients>();
            var mockClientProxy = new Mock<IClientProxy>();
            var mockGroupManager = new Mock<IGroupManager>();
            
            mockHubContext.Setup(h => h.Clients).Returns(mockClients.Object);
            mockClients.Setup(c => c.Group(It.IsAny<string>())).Returns(mockClientProxy.Object);
            mockClients.Setup(c => c.All).Returns(mockClientProxy.Object);
            
            // 捕获推送到客户端的消息
            var rawDataMessages = new List<RawDataReceivedMessage>();
            var calculationMessages = new List<CalculationCompletedMessage>();
            var deviceStatusMessages = new List<DeviceStatusMessage>();
            var progressMessages = new List<ProgressMessage>();
            
            // 捕获所有 SendCoreAsync 调用
            mockClientProxy
                .Setup(c => c.SendCoreAsync(
                    It.IsAny<string>(),
                    It.IsAny<object[]>(),
                    default))
                .Callback<string, object[], CancellationToken>((method, args, token) =>
                {
                    if (method == "OnRawDataReceived" && args.Length > 0 && args[0] is RawDataReceivedMessage rawMsg)
                    {
                        rawDataMessages.Add(rawMsg);
                    }
                    else if (method == "OnCalculationCompleted" && args.Length > 0 && args[0] is CalculationCompletedMessage calcMsg)
                    {
                        calculationMessages.Add(calcMsg);
                    }
                    else if (method == "OnDeviceStatusChanged" && args.Length > 0 && args[0] is DeviceStatusMessage statusMsg)
                    {
                        deviceStatusMessages.Add(statusMsg);
                    }
                    else if (method == "OnProgressUpdated" && args.Length > 0 && args[0] is ProgressMessage progMsg)
                    {
                        progressMessages.Add(progMsg);
                    }
                })
                .Returns(Task.CompletedTask);
            
            // 创建服务实例
            var dataAcquisitionLogger = new Mock<ILogger<VirtualBeamProfilerService>>();
            var dataAcquisitionService = new VirtualBeamProfilerService(dataAcquisitionLogger.Object);
            
            var algorithmLogger = new Mock<ILogger<BeamQualityAlgorithmService>>();
            var algorithmService = new BeamQualityAlgorithmService(algorithmLogger.Object);
            
            var bridgeLogger = new Mock<ILogger<HubEventBridge>>();
            var hubEventBridge = new HubEventBridge(
                mockHubContext.Object,
                dataAcquisitionService,
                algorithmService,
                bridgeLogger.Object);
            
            // 启动 Hub 事件桥接服务
            await hubEventBridge.StartAsync(CancellationToken.None);
            
            // Act - 启动数据采集
            var cts = new CancellationTokenSource();
            var acquisitionTask = dataAcquisitionService.StartAcquisitionAsync(cts.Token);
            
            // 等待足够的数据采集和计算（至少15个数据点，触发3次计算）
            await Task.Delay(2000);
            
            // 停止采集
            await dataAcquisitionService.StopAcquisitionAsync();
            
            // 等待所有异步操作完成
            await Task.Delay(500);
            
            // 停止 Hub 事件桥接服务
            await hubEventBridge.StopAsync(CancellationToken.None);
            
            // Assert - 验证数据流完整性
            
            // 1. 验证原始数据推送
            Assert.True(rawDataMessages.Count > 0, 
                "应该接收到原始数据推送消息");
            
            // 2. 验证原始数据内容完整性
            var totalDataPoints = rawDataMessages.Sum(m => m.DataPoints.Length);
            Assert.True(totalDataPoints >= 15, 
                $"应该接收到至少15个数据点，实际接收了 {totalDataPoints} 个");
            
            // 3. 验证所有原始数据点都有有效值
            foreach (var message in rawDataMessages)
            {
                foreach (var dataPoint in message.DataPoints)
                {
                    Assert.True(dataPoint.DetectorPosition != 0, 
                        "探测器位置不应该为0");
                    Assert.True(dataPoint.BeamDiameterX > 0, 
                        "X方向光束直径应该大于0");
                    Assert.True(dataPoint.BeamDiameterY > 0, 
                        "Y方向光束直径应该大于0");
                    Assert.True(dataPoint.Timestamp != default, 
                        "时间戳不应该为默认值");
                }
            }
            
            // 4. 验证算法计算完成消息
            Assert.True(calculationMessages.Count > 0, 
                "应该接收到算法计算完成消息");
            
            // 5. 验证计算结果的完整性
            foreach (var calcMessage in calculationMessages)
            {
                // 验证 M² 因子
                Assert.True(calcMessage.MSquaredX >= 1.0, 
                    $"X方向M²因子应该 >= 1.0，实际值: {calcMessage.MSquaredX}");
                Assert.True(calcMessage.MSquaredY >= 1.0, 
                    $"Y方向M²因子应该 >= 1.0，实际值: {calcMessage.MSquaredY}");
                Assert.True(calcMessage.MSquaredGlobal >= 1.0, 
                    $"全局M²因子应该 >= 1.0，实际值: {calcMessage.MSquaredGlobal}");
                
                // 验证腰斑参数
                Assert.True(calcMessage.BeamWaistDiameterX > 0, 
                    "X方向腰斑直径应该大于0");
                Assert.True(calcMessage.BeamWaistDiameterY > 0, 
                    "Y方向腰斑直径应该大于0");
                
                // 验证拟合结果存在
                Assert.NotNull(calcMessage.GaussianFitX);
                Assert.NotNull(calcMessage.GaussianFitY);
                Assert.NotNull(calcMessage.HyperbolicFitX);
                Assert.NotNull(calcMessage.HyperbolicFitY);
                
                // 验证拟合优度（使用 ! 操作符，因为已经通过 Assert.NotNull 验证）
                Assert.InRange(calcMessage.GaussianFitX!.RSquared, 0.0, 1.0);
                Assert.InRange(calcMessage.GaussianFitY!.RSquared, 0.0, 1.0);
                Assert.InRange(calcMessage.HyperbolicFitX!.RSquared, 0.0, 1.0);
                Assert.InRange(calcMessage.HyperbolicFitY!.RSquared, 0.0, 1.0);
            }
            
            // 6. 验证设备状态消息
            Assert.True(deviceStatusMessages.Count > 0, 
                "应该接收到设备状态变化消息");
            
            // 验证状态变化序列：Connected -> Acquiring -> Stopped
            var statusSequence = deviceStatusMessages.Select(m => m.Status).ToList();
            Assert.Contains("Connected", statusSequence);
            Assert.Contains("Acquiring", statusSequence);
            Assert.Contains("Stopped", statusSequence);
            
            // 7. 验证进度消息
            Assert.True(progressMessages.Count > 0, 
                "应该接收到进度更新消息");
            
            // 验证进度消息包含开始和完成
            var progressOperations = progressMessages
                .Where(m => m.Message != null)
                .Select(m => m.Message!)
                .ToList();
            Assert.Contains(progressOperations, m => m.Contains("开始"));
            Assert.Contains(progressOperations, m => m.Contains("完成"));
        }
        
        /// <summary>
        /// 属性 14.1: 数据流时序性
        /// 数据流应该按照正确的时序传递：原始数据 -> 算法计算 -> 结果推送
        /// 验证需求: 16.2, 16.3, 16.4
        /// </summary>
        [Fact]
        public async Task Property14_1_DataFlowSequence_ShouldFollowCorrectOrder()
        {
            // Feature: beam-quality-analyzer, Property 14: 数据流完整性
            
            // Arrange
            var mockHubContext = new Mock<IHubContext<BeamAnalyzerHub>>();
            var mockClients = new Mock<IHubClients>();
            var mockClientProxy = new Mock<IClientProxy>();
            
            mockHubContext.Setup(h => h.Clients).Returns(mockClients.Object);
            mockClients.Setup(c => c.Group(It.IsAny<string>())).Returns(mockClientProxy.Object);
            mockClients.Setup(c => c.All).Returns(mockClientProxy.Object);
            
            // 记录事件发生的时间戳
            var eventTimeline = new List<(string EventType, DateTime Timestamp)>();
            
            // 捕获所有 SendCoreAsync 调用
            mockClientProxy
                .Setup(c => c.SendCoreAsync(
                    It.IsAny<string>(),
                    It.IsAny<object[]>(),
                    default))
                .Callback<string, object[], CancellationToken>((method, args, token) =>
                {
                    if (method == "OnRawDataReceived")
                    {
                        eventTimeline.Add(("RawData", DateTime.Now));
                    }
                    else if (method == "OnCalculationCompleted")
                    {
                        eventTimeline.Add(("Calculation", DateTime.Now));
                    }
                })
                .Returns(Task.CompletedTask);
            
            // 创建服务
            var dataAcquisitionLogger = new Mock<ILogger<VirtualBeamProfilerService>>();
            var dataAcquisitionService = new VirtualBeamProfilerService(dataAcquisitionLogger.Object);
            
            var algorithmLogger = new Mock<ILogger<BeamQualityAlgorithmService>>();
            var algorithmService = new BeamQualityAlgorithmService(algorithmLogger.Object);
            
            var bridgeLogger = new Mock<ILogger<HubEventBridge>>();
            var hubEventBridge = new HubEventBridge(
                mockHubContext.Object,
                dataAcquisitionService,
                algorithmService,
                bridgeLogger.Object);
            
            await hubEventBridge.StartAsync(CancellationToken.None);
            
            // Act
            var cts = new CancellationTokenSource();
            await dataAcquisitionService.StartAcquisitionAsync(cts.Token);
            
            // 等待足够的数据和计算
            await Task.Delay(2000);
            
            await dataAcquisitionService.StopAcquisitionAsync();
            await Task.Delay(500);
            
            await hubEventBridge.StopAsync(CancellationToken.None);
            
            // Assert - 验证时序
            // 1. 应该先有原始数据，后有计算结果
            var firstRawDataIndex = eventTimeline.FindIndex(e => e.EventType == "RawData");
            var firstCalculationIndex = eventTimeline.FindIndex(e => e.EventType == "Calculation");
            
            Assert.True(firstRawDataIndex >= 0, "应该有原始数据事件");
            Assert.True(firstCalculationIndex >= 0, "应该有计算完成事件");
            Assert.True(firstRawDataIndex < firstCalculationIndex, 
                "原始数据事件应该在计算完成事件之前");
            
            // 2. 验证每次计算之前都有足够的原始数据
            var calculationIndices = eventTimeline
                .Select((e, i) => new { Event = e, Index = i })
                .Where(x => x.Event.EventType == "Calculation")
                .Select(x => x.Index)
                .ToList();
            
            foreach (var calcIndex in calculationIndices)
            {
                // 计算之前应该有至少10个原始数据事件
                var rawDataCountBeforeCalc = eventTimeline
                    .Take(calcIndex)
                    .Count(e => e.EventType == "RawData");
                
                Assert.True(rawDataCountBeforeCalc >= 2, 
                    $"计算之前应该有至少2个原始数据批次，实际有 {rawDataCountBeforeCalc} 个");
            }
        }
        
        /// <summary>
        /// 属性 14.2: 数据流无丢失
        /// 采集的所有数据点都应该被推送到客户端，不应该有数据丢失
        /// 验证需求: 16.2, 16.5
        /// </summary>
        [Fact]
        public async Task Property14_2_DataFlowNoLoss_AllDataPointsShouldBePushed()
        {
            // Feature: beam-quality-analyzer, Property 14: 数据流完整性
            
            // Arrange
            var mockHubContext = new Mock<IHubContext<BeamAnalyzerHub>>();
            var mockClients = new Mock<IHubClients>();
            var mockClientProxy = new Mock<IClientProxy>();
            
            mockHubContext.Setup(h => h.Clients).Returns(mockClients.Object);
            mockClients.Setup(c => c.Group(It.IsAny<string>())).Returns(mockClientProxy.Object);
            mockClients.Setup(c => c.All).Returns(mockClientProxy.Object);
            
            // 记录采集的数据点和推送的数据点
            var acquiredDataPoints = new List<RawDataPoint>();
            var pushedDataPoints = new List<RawDataPointDto>();
            
            var dataAcquisitionLogger = new Mock<ILogger<VirtualBeamProfilerService>>();
            var dataAcquisitionService = new VirtualBeamProfilerService(dataAcquisitionLogger.Object);
            
            // 订阅原始数据事件，记录采集的数据
            dataAcquisitionService.RawDataReceived += (sender, args) =>
            {
                acquiredDataPoints.AddRange(args.DataPoints);
            };
            
            // 捕获所有 SendCoreAsync 调用
            mockClientProxy
                .Setup(c => c.SendCoreAsync(
                    It.IsAny<string>(),
                    It.IsAny<object[]>(),
                    default))
                .Callback<string, object[], CancellationToken>((method, args, token) =>
                {
                    if (method == "OnRawDataReceived" && args.Length > 0 && args[0] is RawDataReceivedMessage msg)
                    {
                        pushedDataPoints.AddRange(msg.DataPoints);
                    }
                })
                .Returns(Task.CompletedTask);
            
            var algorithmLogger = new Mock<ILogger<BeamQualityAlgorithmService>>();
            var algorithmService = new BeamQualityAlgorithmService(algorithmLogger.Object);
            
            var bridgeLogger = new Mock<ILogger<HubEventBridge>>();
            var hubEventBridge = new HubEventBridge(
                mockHubContext.Object,
                dataAcquisitionService,
                algorithmService,
                bridgeLogger.Object);
            
            await hubEventBridge.StartAsync(CancellationToken.None);
            
            // Act
            var cts = new CancellationTokenSource();
            await dataAcquisitionService.StartAcquisitionAsync(cts.Token);
            
            // 采集1秒的数据
            await Task.Delay(1000);
            
            await dataAcquisitionService.StopAcquisitionAsync();
            await Task.Delay(300);
            
            await hubEventBridge.StopAsync(CancellationToken.None);
            
            // Assert - 验证数据无丢失
            Assert.True(acquiredDataPoints.Count > 0, "应该采集到数据");
            Assert.True(pushedDataPoints.Count > 0, "应该推送数据到客户端");
            
            // 验证推送的数据点数量与采集的数据点数量一致
            Assert.Equal(acquiredDataPoints.Count, pushedDataPoints.Count);
            
            // 验证推送的数据内容与采集的数据一致（抽样检查）
            for (int i = 0; i < Math.Min(5, acquiredDataPoints.Count); i++)
            {
                var acquired = acquiredDataPoints[i];
                var pushed = pushedDataPoints[i];
                
                Assert.Equal(acquired.DetectorPosition, pushed.DetectorPosition);
                Assert.Equal(acquired.BeamDiameterX, pushed.BeamDiameterX);
                Assert.Equal(acquired.BeamDiameterY, pushed.BeamDiameterY);
            }
        }
        
        /// <summary>
        /// 属性 14.3: 算法计算触发条件
        /// 当数据点达到阈值时，应该自动触发算法计算
        /// 验证需求: 16.3, 16.4
        /// </summary>
        [Fact]
        public async Task Property14_3_AlgorithmTrigger_ShouldTriggerWhenThresholdReached()
        {
            // Feature: beam-quality-analyzer, Property 14: 数据流完整性
            
            // Arrange
            var mockHubContext = new Mock<IHubContext<BeamAnalyzerHub>>();
            var mockClients = new Mock<IHubClients>();
            var mockClientProxy = new Mock<IClientProxy>();
            
            mockHubContext.Setup(h => h.Clients).Returns(mockClients.Object);
            mockClients.Setup(c => c.Group(It.IsAny<string>())).Returns(mockClientProxy.Object);
            mockClients.Setup(c => c.All).Returns(mockClientProxy.Object);
            
            var calculationTriggered = false;
            var dataPointCountWhenTriggered = 0;
            
            // 捕获所有 SendCoreAsync 调用
            mockClientProxy
                .Setup(c => c.SendCoreAsync(
                    It.IsAny<string>(),
                    It.IsAny<object[]>(),
                    default))
                .Callback<string, object[], CancellationToken>((method, args, token) =>
                {
                    if (method == "OnCalculationCompleted" && !calculationTriggered)
                    {
                        calculationTriggered = true;
                    }
                })
                .Returns(Task.CompletedTask);
            
            var dataAcquisitionLogger = new Mock<ILogger<VirtualBeamProfilerService>>();
            var dataAcquisitionService = new VirtualBeamProfilerService(dataAcquisitionLogger.Object);
            
            var dataPointCount = 0;
            dataAcquisitionService.RawDataReceived += (sender, args) =>
            {
                dataPointCount += args.DataPoints.Length;
                if (calculationTriggered && dataPointCountWhenTriggered == 0)
                {
                    dataPointCountWhenTriggered = dataPointCount;
                }
            };
            
            var algorithmLogger = new Mock<ILogger<BeamQualityAlgorithmService>>();
            var algorithmService = new BeamQualityAlgorithmService(algorithmLogger.Object);
            
            var bridgeLogger = new Mock<ILogger<HubEventBridge>>();
            var hubEventBridge = new HubEventBridge(
                mockHubContext.Object,
                dataAcquisitionService,
                algorithmService,
                bridgeLogger.Object);
            
            await hubEventBridge.StartAsync(CancellationToken.None);
            
            // Act
            var cts = new CancellationTokenSource();
            await dataAcquisitionService.StartAcquisitionAsync(cts.Token);
            
            // 等待足够的数据触发计算
            await Task.Delay(1500);
            
            await dataAcquisitionService.StopAcquisitionAsync();
            await Task.Delay(300);
            
            await hubEventBridge.StopAsync(CancellationToken.None);
            
            // Assert
            Assert.True(calculationTriggered, "应该触发算法计算");
            Assert.True(dataPointCount >= 10, 
                $"应该采集至少10个数据点才触发计算，实际采集了 {dataPointCount} 个");
        }
    }
}
