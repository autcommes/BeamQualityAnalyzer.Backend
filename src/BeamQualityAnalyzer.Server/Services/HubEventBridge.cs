using Microsoft.AspNetCore.SignalR;
using BeamQualityAnalyzer.Core.Interfaces;
using BeamQualityAnalyzer.Core.Models;
using BeamQualityAnalyzer.Server.Hubs;
using BeamQualityAnalyzer.Contracts.Messages;

namespace BeamQualityAnalyzer.Server.Services;

/// <summary>
/// Hub 事件桥接服务
/// 负责将服务层事件转发到 SignalR Hub，实现服务器到客户端的实时推送
/// </summary>
public class HubEventBridge : IHostedService
{
    private readonly IHubContext<BeamAnalyzerHub> _hubContext;
    private readonly IDataAcquisitionService _dataAcquisitionService;
    private readonly IAlgorithmService _algorithmService;
    private readonly ILogger<HubEventBridge> _logger;
    
    // 用于存储最新的原始数据，供算法服务使用
    private readonly List<RawDataPoint> _currentSessionData;
    private readonly object _dataLock = new object();
    
    public HubEventBridge(
        IHubContext<BeamAnalyzerHub> hubContext,
        IDataAcquisitionService dataAcquisitionService,
        IAlgorithmService algorithmService,
        ILogger<HubEventBridge> logger)
    {
        _hubContext = hubContext ?? throw new ArgumentNullException(nameof(hubContext));
        _dataAcquisitionService = dataAcquisitionService ?? throw new ArgumentNullException(nameof(dataAcquisitionService));
        _algorithmService = algorithmService ?? throw new ArgumentNullException(nameof(algorithmService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        
        _currentSessionData = new List<RawDataPoint>();
    }
    
    /// <summary>
    /// 启动服务，订阅服务层事件
    /// </summary>
    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("启动 Hub 事件桥接服务");
        
        // 订阅数据采集服务事件
        _dataAcquisitionService.RawDataReceived += OnRawDataReceived;
        _dataAcquisitionService.DeviceStatusChanged += OnDeviceStatusChanged;
        
        return Task.CompletedTask;
    }
    
    /// <summary>
    /// 停止服务，取消订阅事件
    /// </summary>
    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("停止 Hub 事件桥接服务");
        
        // 取消订阅事件
        _dataAcquisitionService.RawDataReceived -= OnRawDataReceived;
        _dataAcquisitionService.DeviceStatusChanged -= OnDeviceStatusChanged;
        
        return Task.CompletedTask;
    }
    
    /// <summary>
    /// 处理原始数据接收事件
    /// </summary>
    private async void OnRawDataReceived(object? sender, RawDataReceivedEventArgs e)
    {
        try
        {
            _logger.LogDebug("接收到 {Count} 个原始数据点", e.DataPoints.Length);
            
            // 将数据添加到当前会话
            lock (_dataLock)
            {
                _currentSessionData.AddRange(e.DataPoints);
            }
            
            // 构造消息
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
            
            // 推送到订阅了数据流的客户端
            await _hubContext.Clients.Group("DataStream").SendAsync("OnRawDataReceived", message);
            
            // 如果数据点足够，触发算法计算
            int dataPointCount;
            lock (_dataLock)
            {
                dataPointCount = _currentSessionData.Count;
            }
            
            if (dataPointCount >= 10 && dataPointCount % 5 == 0) // 每5个点计算一次
            {
                _ = Task.Run(async () => await TriggerAnalysisAsync());
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "处理原始数据接收事件时发生错误");
            await BroadcastErrorAsync("数据处理错误", ex.Message);
        }
    }
    
    /// <summary>
    /// 处理设备状态变化事件
    /// </summary>
    private async void OnDeviceStatusChanged(object? sender, DeviceStatusChangedEventArgs e)
    {
        try
        {
            _logger.LogInformation("设备状态变更: {Status} - {Message}", e.Status, e.Message);
            
            // 构造消息
            var message = new DeviceStatusMessage
            {
                Status = e.Status.ToString(),
                Message = e.Message ?? string.Empty,
                Timestamp = e.Timestamp
            };
            
            // 推送到所有客户端
            await _hubContext.Clients.All.SendAsync("OnDeviceStatusChanged", message);
            
            // 如果采集状态变化，同时推送采集状态消息
            if (e.Status == DeviceStatus.Acquiring || e.Status == DeviceStatus.Stopped)
            {
                var acquisitionMessage = new AcquisitionStatusMessage
                {
                    IsAcquiring = e.Status == DeviceStatus.Acquiring,
                    DataPointCount = 0,
                    Frequency = 10.0
                };
                
                await _hubContext.Clients.All.SendAsync("OnAcquisitionStatusChanged", acquisitionMessage);
                
                // 如果停止采集，清空当前会话数据
                if (e.Status == DeviceStatus.Stopped)
                {
                    lock (_dataLock)
                    {
                        _currentSessionData.Clear();
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "处理设备状态变化事件时发生错误");
        }
    }
    
    /// <summary>
    /// 触发算法分析
    /// </summary>
    private async Task TriggerAnalysisAsync()
    {
        try
        {
            RawDataPoint[] dataPoints;
            lock (_dataLock)
            {
                dataPoints = _currentSessionData.ToArray();
            }
            
            if (dataPoints.Length < 10)
            {
                _logger.LogDebug("数据点不足，跳过分析");
                return;
            }
            
            _logger.LogInformation("开始算法分析，数据点数: {Count}", dataPoints.Length);
            
            // 推送进度消息
            await BroadcastProgressAsync("算法计算", 0, "开始计算...");
            
            // 执行分析
            var parameters = new AnalysisParameters
            {
                Wavelength = 632.8, // He-Ne 激光波长
                MinDataPoints = 10,
                FitTolerance = 0.001
            };
            
            var result = await _algorithmService.AnalyzeAsync(dataPoints, parameters, CancellationToken.None);
            
            // 推送进度消息
            await BroadcastProgressAsync("算法计算", 100, "计算完成");
            
            // 构造计算完成消息
            var message = new CalculationCompletedMessage
            {
                MSquaredX = result.MSquaredX,
                MSquaredY = result.MSquaredY,
                MSquaredGlobal = result.MSquaredGlobal,
                BeamWaistPositionX = result.BeamWaistPositionX,
                BeamWaistPositionY = result.BeamWaistPositionY,
                BeamWaistDiameterX = result.BeamWaistDiameterX,
                BeamWaistDiameterY = result.BeamWaistDiameterY,
                PeakPositionX = result.PeakPositionX,
                PeakPositionY = result.PeakPositionY,
                GaussianFitX = new GaussianFitResultDto
                {
                    Amplitude = result.GaussianFitX?.Amplitude ?? 0,
                    Mean = result.GaussianFitX?.Mean ?? 0,
                    StandardDeviation = result.GaussianFitX?.StandardDeviation ?? 0,
                    Offset = result.GaussianFitX?.Offset ?? 0,
                    RSquared = result.GaussianFitX?.RSquared ?? 0,
                    FittedCurve = result.GaussianFitX?.FittedCurve
                },
                GaussianFitY = new GaussianFitResultDto
                {
                    Amplitude = result.GaussianFitY?.Amplitude ?? 0,
                    Mean = result.GaussianFitY?.Mean ?? 0,
                    StandardDeviation = result.GaussianFitY?.StandardDeviation ?? 0,
                    Offset = result.GaussianFitY?.Offset ?? 0,
                    RSquared = result.GaussianFitY?.RSquared ?? 0,
                    FittedCurve = result.GaussianFitY?.FittedCurve
                },
                HyperbolicFitX = new HyperbolicFitResultDto
                {
                    WaistDiameter = result.HyperbolicFitX?.WaistDiameter ?? 0,
                    WaistPosition = result.HyperbolicFitX?.WaistPosition ?? 0,
                    Wavelength = result.HyperbolicFitX?.Wavelength ?? 0,
                    MSquared = result.HyperbolicFitX?.MSquared ?? 0,
                    RSquared = result.HyperbolicFitX?.RSquared ?? 0,
                    FittedCurve = result.HyperbolicFitX?.FittedCurve
                },
                HyperbolicFitY = new HyperbolicFitResultDto
                {
                    WaistDiameter = result.HyperbolicFitY?.WaistDiameter ?? 0,
                    WaistPosition = result.HyperbolicFitY?.WaistPosition ?? 0,
                    Wavelength = result.HyperbolicFitY?.Wavelength ?? 0,
                    MSquared = result.HyperbolicFitY?.MSquared ?? 0,
                    RSquared = result.HyperbolicFitY?.RSquared ?? 0,
                    FittedCurve = result.HyperbolicFitY?.FittedCurve
                },
                Timestamp = DateTime.Now
            };
            
            // 推送到所有客户端
            await _hubContext.Clients.All.SendAsync("OnCalculationCompleted", message);
            
            _logger.LogInformation("算法分析完成并推送到客户端");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "算法分析失败");
            await BroadcastErrorAsync("算法计算失败", ex.Message);
        }
    }
    
    /// <summary>
    /// 广播错误消息到所有客户端
    /// </summary>
    private async Task BroadcastErrorAsync(string title, string message)
    {
        var errorMessage = new ErrorMessage
        {
            Title = title,
            Message = message,
            Timestamp = DateTime.Now
        };
        
        await _hubContext.Clients.All.SendAsync("OnErrorOccurred", errorMessage);
    }
    
    /// <summary>
    /// 广播进度消息到所有客户端
    /// </summary>
    private async Task BroadcastProgressAsync(string operation, int percentage, string message)
    {
        var progressMessage = new ProgressMessage
        {
            Operation = operation,
            Percentage = percentage,
            Message = message,
            Timestamp = DateTime.Now
        };
        
        await _hubContext.Clients.All.SendAsync("OnProgressUpdated", progressMessage);
    }
}
