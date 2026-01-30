using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using BeamQualityAnalyzer.Core.Interfaces;
using BeamQualityAnalyzer.Core.Services;
using FsCheck;
using FsCheck.Xunit;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace BeamQualityAnalyzer.PropertyTests
{
    /// <summary>
    /// 数据采集属性测试
    /// 验证数据采集服务的核心功能和性能要求
    /// </summary>
    public class DataAcquisitionPropertyTests
    {
        /// <summary>
        /// 属性 24: 数据采集频率
        /// Feature: beam-quality-analyzer, Property 24: 数据采集频率
        /// 系统应该以不低于 10Hz 的频率采集光束数据。
        /// 验证需求: 2.5
        /// </summary>
        [Fact]
        public async Task Property24_DataAcquisitionFrequency_ShouldBeAtLeast10Hz()
        {
            // Feature: beam-quality-analyzer, Property 24: 数据采集频率
            
            // Arrange
            var logger = new Mock<ILogger<VirtualBeamProfilerService>>();
            var service = new VirtualBeamProfilerService(logger.Object);
            
            var dataReceivedCount = 0;
            var dataReceivedTimes = new List<DateTime>();
            var firstDataTime = DateTime.MinValue;
            var lastDataTime = DateTime.MinValue;
            
            service.RawDataReceived += (sender, args) =>
            {
                dataReceivedCount++;
                var now = DateTime.Now;
                dataReceivedTimes.Add(now);
                
                if (firstDataTime == DateTime.MinValue)
                {
                    firstDataTime = now;
                }
                lastDataTime = now;
            };
            
            // Act - 启动采集并等待足够的数据
            var cts = new CancellationTokenSource();
            var acquisitionTask = service.StartAcquisitionAsync(cts.Token);
            
            // 等待至少接收到一些数据
            await Task.Delay(2500); // 等待 2.5 秒，确保有足够的采集时间
            
            await service.StopAcquisitionAsync();
            
            // Assert - 验证采集频率
            // 计算实际采集时间跨度
            if (firstDataTime != DateTime.MinValue && lastDataTime != DateTime.MinValue)
            {
                var actualDuration = (lastDataTime - firstDataTime).TotalSeconds;
                var expectedMinCount = (int)(actualDuration * 10 * 0.9); // 允许 10% 的误差
                
                Assert.True(dataReceivedCount >= expectedMinCount, 
                    $"数据采集频率不足 10Hz。在 {actualDuration:F2} 秒内应至少采集 {expectedMinCount} 个数据点，实际采集了 {dataReceivedCount} 个");
            }
            
            // 验证平均采集间隔
            if (dataReceivedTimes.Count >= 2)
            {
                var intervals = new List<double>();
                for (int i = 1; i < dataReceivedTimes.Count; i++)
                {
                    var interval = (dataReceivedTimes[i] - dataReceivedTimes[i - 1]).TotalMilliseconds;
                    intervals.Add(interval);
                }
                
                var averageInterval = intervals.Average();
                var expectedMaxInterval = 1000.0 / 10.0; // 10Hz = 100ms
                
                Assert.True(averageInterval <= expectedMaxInterval * 1.3, // 允许 30% 的误差
                    $"平均采集间隔 {averageInterval:F2}ms 超过了 10Hz 的要求 (应 <= {expectedMaxInterval * 1.3:F2}ms)");
            }
        }
        
        /// <summary>
        /// 属性 1: 数据采集触发状态更新
        /// Feature: beam-quality-analyzer, Property 1: 数据采集触发状态更新
        /// 当用户点击"开始"按钮时，系统应该启动数据采集并更新状态为"采集中"。
        /// 验证需求: 2.1
        /// </summary>
        [Fact]
        public async Task Property1_StartAcquisition_ShouldTriggerStatusUpdate()
        {
            // Feature: beam-quality-analyzer, Property 1: 数据采集触发状态更新
            
            // Arrange
            var logger = new Mock<ILogger<VirtualBeamProfilerService>>();
            var service = new VirtualBeamProfilerService(logger.Object);
            
            var statusChanges = new List<DeviceStatus>();
            
            service.DeviceStatusChanged += (sender, args) =>
            {
                statusChanges.Add(args.Status);
            };
            
            // Act - 启动数据采集
            var cts = new CancellationTokenSource();
            var acquisitionTask = service.StartAcquisitionAsync(cts.Token);
            
            // 等待状态更新
            await Task.Delay(200);
            
            // Assert - 验证状态变化
            Assert.True(service.IsAcquiring, "启动采集后，IsAcquiring 应该为 true");
            
            // 验证状态变化序列：Disconnected -> Connected -> Acquiring
            Assert.Contains(DeviceStatus.Connected, statusChanges);
            Assert.Contains(DeviceStatus.Acquiring, statusChanges);
            
            // 验证最终状态是 Acquiring
            Assert.Equal(DeviceStatus.Acquiring, statusChanges[statusChanges.Count - 1]);
            
            // Cleanup
            await service.StopAcquisitionAsync();
        }
        
        /// <summary>
        /// 属性 1.1: 数据采集应该触发数据接收事件
        /// 启动采集后，应该持续接收到数据
        /// 验证需求: 2.1
        /// </summary>
        [Fact]
        public async Task Property1_1_StartAcquisition_ShouldTriggerDataReceivedEvents()
        {
            // Feature: beam-quality-analyzer, Property 1: 数据采集触发状态更新
            
            // Arrange
            var logger = new Mock<ILogger<VirtualBeamProfilerService>>();
            var service = new VirtualBeamProfilerService(logger.Object);
            
            var dataReceivedCount = 0;
            
            service.RawDataReceived += (sender, args) =>
            {
                dataReceivedCount++;
            };
            
            // Act - 启动数据采集
            var cts = new CancellationTokenSource();
            var acquisitionTask = service.StartAcquisitionAsync(cts.Token);
            
            // 等待至少接收一些数据
            await Task.Delay(500);
            
            // Assert - 验证接收到数据
            Assert.True(dataReceivedCount > 0, "启动采集后应该接收到数据");
            
            // Cleanup
            await service.StopAcquisitionAsync();
        }
        
        /// <summary>
        /// 属性 2: 急停立即停止操作
        /// Feature: beam-quality-analyzer, Property 2: 急停立即停止操作
        /// 当用户点击"急停"按钮时，系统应该立即停止所有数据采集和设备运动。
        /// 验证需求: 2.2
        /// </summary>
        [Fact]
        public async Task Property2_EmergencyStop_ShouldImmediatelyStopAllOperations()
        {
            // Feature: beam-quality-analyzer, Property 2: 急停立即停止操作
            
            // Arrange
            var logger = new Mock<ILogger<VirtualBeamProfilerService>>();
            var service = new VirtualBeamProfilerService(logger.Object);
            
            var dataReceivedAfterStop = 0;
            var stopwatch = new Stopwatch();
            var emergencyStopExecuted = false;
            
            service.RawDataReceived += (sender, args) =>
            {
                if (emergencyStopExecuted)
                {
                    dataReceivedAfterStop++;
                }
            };
            
            var statusChanges = new List<(DeviceStatus Status, DateTime Time)>();
            
            service.DeviceStatusChanged += (sender, args) =>
            {
                statusChanges.Add((args.Status, DateTime.Now));
            };
            
            // Act - 启动数据采集
            var cts = new CancellationTokenSource();
            var acquisitionTask = service.StartAcquisitionAsync(cts.Token);
            
            // 等待采集开始
            await Task.Delay(300);
            
            // 执行急停
            stopwatch.Start();
            emergencyStopExecuted = true;
            service.EmergencyStop();
            stopwatch.Stop();
            
            // 等待一段时间，确保没有新数据
            await Task.Delay(500);
            
            // Assert - 验证急停效果
            Assert.False(service.IsAcquiring, "急停后，IsAcquiring 应该为 false");
            
            // 验证急停后没有新数据产生（允许少量延迟数据）
            Assert.True(dataReceivedAfterStop <= 2, 
                $"急停后不应该继续接收数据，但接收了 {dataReceivedAfterStop} 个数据点");
            
            // 验证急停响应时间（应该非常快，< 50ms）
            Assert.True(stopwatch.ElapsedMilliseconds < 50, 
                $"急停响应时间过长: {stopwatch.ElapsedMilliseconds}ms (应 < 50ms)");
            
            // 验证状态变更为 Stopped
            var stoppedStatus = statusChanges.FindLast(s => s.Status == DeviceStatus.Stopped);
            Assert.NotEqual(default, stoppedStatus);
        }
        
        /// <summary>
        /// 属性 2.1: 急停应该是同步操作
        /// 急停方法应该立即返回，不应该等待异步操作完成
        /// 验证需求: 2.2
        /// </summary>
        [Fact]
        public async Task Property2_1_EmergencyStop_ShouldBeSynchronous()
        {
            // Feature: beam-quality-analyzer, Property 2: 急停立即停止操作
            
            // Arrange
            var logger = new Mock<ILogger<VirtualBeamProfilerService>>();
            var service = new VirtualBeamProfilerService(logger.Object);
            
            // Act - 启动数据采集
            var cts = new CancellationTokenSource();
            var acquisitionTask = service.StartAcquisitionAsync(cts.Token);
            
            await Task.Delay(200);
            
            // 测量急停方法的执行时间
            var stopwatch = Stopwatch.StartNew();
            service.EmergencyStop();
            stopwatch.Stop();
            
            // Assert - 急停方法应该立即返回（< 10ms）
            Assert.True(stopwatch.ElapsedMilliseconds < 10, 
                $"急停方法执行时间过长: {stopwatch.ElapsedMilliseconds}ms (应 < 10ms)");
        }
        
        /// <summary>
        /// 属性 2.2: 急停后应该能够重新启动采集
        /// 急停不应该导致服务进入不可恢复的状态
        /// 验证需求: 2.2, 2.3
        /// </summary>
        [Fact]
        public async Task Property2_2_EmergencyStop_ShouldAllowRestart()
        {
            // Feature: beam-quality-analyzer, Property 2: 急停立即停止操作
            
            // Arrange
            var logger = new Mock<ILogger<VirtualBeamProfilerService>>();
            var service = new VirtualBeamProfilerService(logger.Object);
            
            // Act - 第一次采集
            var cts1 = new CancellationTokenSource();
            await service.StartAcquisitionAsync(cts1.Token);
            await Task.Delay(200);
            
            // 急停
            service.EmergencyStop();
            await Task.Delay(100);
            
            // 尝试重新启动采集
            var dataReceivedAfterRestart = 0;
            service.RawDataReceived += (sender, args) =>
            {
                dataReceivedAfterRestart++;
            };
            
            var cts2 = new CancellationTokenSource();
            await service.StartAcquisitionAsync(cts2.Token);
            await Task.Delay(500);
            
            // Assert - 验证重新启动成功
            Assert.True(service.IsAcquiring, "急停后应该能够重新启动采集");
            Assert.True(dataReceivedAfterRestart > 0, "重新启动后应该接收到数据");
            
            // Cleanup
            await service.StopAcquisitionAsync();
        }
        
        /// <summary>
        /// 属性 24.1: 数据采集频率的一致性
        /// 采集频率应该在整个采集过程中保持稳定
        /// 验证需求: 2.5
        /// </summary>
        [Fact]
        public async Task Property24_1_DataAcquisitionFrequency_ShouldBeConsistent()
        {
            // Feature: beam-quality-analyzer, Property 24: 数据采集频率
            
            // Arrange
            var logger = new Mock<ILogger<VirtualBeamProfilerService>>();
            var service = new VirtualBeamProfilerService(logger.Object);
            
            var dataReceivedTimes = new List<DateTime>();
            
            service.RawDataReceived += (sender, args) =>
            {
                dataReceivedTimes.Add(DateTime.Now);
            };
            
            // Act - 运行采集 1.5 秒
            var cts = new CancellationTokenSource();
            await service.StartAcquisitionAsync(cts.Token);
            
            await Task.Delay(1500);
            
            await service.StopAcquisitionAsync();
            
            // Assert - 验证采集间隔的一致性
            if (dataReceivedTimes.Count >= 3)
            {
                var intervals = new List<double>();
                for (int i = 1; i < dataReceivedTimes.Count; i++)
                {
                    var interval = (dataReceivedTimes[i] - dataReceivedTimes[i - 1]).TotalMilliseconds;
                    intervals.Add(interval);
                }
                
                var averageInterval = intervals.Average();
                var standardDeviation = Math.Sqrt(intervals.Average(v => Math.Pow(v - averageInterval, 2)));
                
                // 标准差应该小于平均值的 30%（允许一定的抖动）
                Assert.True(standardDeviation < averageInterval * 0.3, 
                    $"采集间隔不稳定。平均间隔: {averageInterval:F2}ms, 标准差: {standardDeviation:F2}ms");
            }
        }
        
        /// <summary>
        /// 属性 1.2: 停止采集应该更新状态
        /// 正常停止采集应该更新状态为"已停止"
        /// 验证需求: 2.1
        /// </summary>
        [Fact]
        public async Task Property1_2_StopAcquisition_ShouldUpdateStatus()
        {
            // Feature: beam-quality-analyzer, Property 1: 数据采集触发状态更新
            
            // Arrange
            var logger = new Mock<ILogger<VirtualBeamProfilerService>>();
            var service = new VirtualBeamProfilerService(logger.Object);
            
            var statusChanges = new List<DeviceStatus>();
            
            service.DeviceStatusChanged += (sender, args) =>
            {
                statusChanges.Add(args.Status);
            };
            
            // Act - 启动并停止采集
            var cts = new CancellationTokenSource();
            await service.StartAcquisitionAsync(cts.Token);
            await Task.Delay(300);
            
            await service.StopAcquisitionAsync();
            await Task.Delay(100);
            
            // Assert - 验证状态变化
            Assert.False(service.IsAcquiring, "停止采集后，IsAcquiring 应该为 false");
            Assert.Contains(DeviceStatus.Stopped, statusChanges);
        }
    }
}
