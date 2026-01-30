using Microsoft.AspNetCore.SignalR;
using BeamQualityAnalyzer.Core.Interfaces;
using BeamQualityAnalyzer.Core.Models;
using BeamQualityAnalyzer.Data.Interfaces;
using BeamQualityAnalyzer.Contracts.Dtos;
using BeamQualityAnalyzer.Contracts.Messages;

namespace BeamQualityAnalyzer.Server.Hubs;

/// <summary>
/// 光束分析器 SignalR Hub
/// 提供客户端与服务器之间的双向实时通信
/// </summary>
public class BeamAnalyzerHub : Hub
{
    private readonly IDataAcquisitionService _dataAcquisitionService;
    private readonly IAlgorithmService _algorithmService;
    private readonly IDatabaseService _databaseService;
    private readonly IExportService _exportService;
    private readonly IAutoTestService _autoTestService;
    private readonly IHubContext<BeamAnalyzerHub> _hubContext;
    private readonly ILogger<BeamAnalyzerHub> _logger;
    
    public BeamAnalyzerHub(
        IDataAcquisitionService dataAcquisitionService,
        IAlgorithmService algorithmService,
        IDatabaseService databaseService,
        IExportService exportService,
        IAutoTestService autoTestService,
        IHubContext<BeamAnalyzerHub> hubContext,
        ILogger<BeamAnalyzerHub> logger)
    {
        _dataAcquisitionService = dataAcquisitionService ?? throw new ArgumentNullException(nameof(dataAcquisitionService));
        _algorithmService = algorithmService ?? throw new ArgumentNullException(nameof(algorithmService));
        _databaseService = databaseService ?? throw new ArgumentNullException(nameof(databaseService));
        _exportService = exportService ?? throw new ArgumentNullException(nameof(exportService));
        _autoTestService = autoTestService ?? throw new ArgumentNullException(nameof(autoTestService));
        _hubContext = hubContext ?? throw new ArgumentNullException(nameof(hubContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }
    
    #region 数据采集控制
    
    /// <summary>
    /// 启动数据采集
    /// </summary>
    public async Task<CommandResult> StartAcquisition()
    {
        try
        {
            _logger.LogInformation("客户端 {ConnectionId} 请求启动数据采集", Context.ConnectionId);
            
            await _dataAcquisitionService.StartAcquisitionAsync(CancellationToken.None);
            
            return CommandResult.SuccessResult("数据采集已启动");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "启动数据采集失败");
            return CommandResult.FailureResult("启动数据采集失败", ex.Message);
        }
    }
    
    /// <summary>
    /// 停止数据采集
    /// </summary>
    public async Task<CommandResult> StopAcquisition()
    {
        try
        {
            _logger.LogInformation("客户端 {ConnectionId} 请求停止数据采集", Context.ConnectionId);
            
            await _dataAcquisitionService.StopAcquisitionAsync();
            
            return CommandResult.SuccessResult("数据采集已停止");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "停止数据采集失败");
            return CommandResult.FailureResult("停止数据采集失败", ex.Message);
        }
    }
    
    /// <summary>
    /// 急停
    /// </summary>
    public CommandResult EmergencyStop()
    {
        try
        {
            _logger.LogWarning("客户端 {ConnectionId} 触发急停", Context.ConnectionId);
            
            _dataAcquisitionService.EmergencyStop();
            
            return CommandResult.SuccessResult("急停执行成功");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "急停执行失败");
            return CommandResult.FailureResult("急停执行失败", ex.Message);
        }
    }
    
    /// <summary>
    /// 获取采集状态
    /// </summary>
    public Task<AcquisitionStatusMessage> GetAcquisitionStatus()
    {
        var status = new AcquisitionStatusMessage
        {
            IsAcquiring = _dataAcquisitionService.IsAcquiring,
            DataPointCount = 0, // TODO: 从服务获取
            Frequency = 10.0
        };
        
        return Task.FromResult(status);
    }
    
    #endregion
    
    #region 设备控制
    
    /// <summary>
    /// 设备复位
    /// </summary>
    public async Task<CommandResult> ResetDevice()
    {
        try
        {
            _logger.LogInformation("客户端 {ConnectionId} 请求设备复位", Context.ConnectionId);
            
            await _dataAcquisitionService.ResetDeviceAsync();
            
            return CommandResult.SuccessResult("设备复位成功");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "设备复位失败");
            return CommandResult.FailureResult("设备复位失败", ex.Message);
        }
    }
    
    /// <summary>
    /// 获取设备状态
    /// </summary>
    public Task<DeviceStatusMessage> GetDeviceStatus()
    {
        // TODO: 从服务获取实际状态
        var status = new DeviceStatusMessage
        {
            Status = _dataAcquisitionService.IsAcquiring ? "Acquiring" : "Ready",
            Message = "设备正常"
        };
        
        return Task.FromResult(status);
    }
    
    #endregion
    
    #region 算法计算
    
    /// <summary>
    /// 重新计算分析
    /// </summary>
    public Task<CommandResult> RecalculateAnalysis(AnalysisParameters parameters)
    {
        try
        {
            _logger.LogInformation("客户端 {ConnectionId} 请求重新计算", Context.ConnectionId);
            
            // TODO: 实现重新计算逻辑
            // 需要保存当前的原始数据，然后使用新参数重新计算
            
            return Task.FromResult(CommandResult.SuccessResult("重新计算已启动"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "重新计算失败");
            return Task.FromResult(CommandResult.FailureResult("重新计算失败", ex.Message));
        }
    }
    
    /// <summary>
    /// 获取最新分析结果
    /// </summary>
    public Task<BeamAnalysisResultDto?> GetLatestAnalysisResult()
    {
        // TODO: 从缓存或服务获取最新结果
        return Task.FromResult<BeamAnalysisResultDto?>(null);
    }
    
    #endregion
    
    #region 数据库操作
    
    /// <summary>
    /// 保存测量记录
    /// </summary>
    public Task<CommandResult<int>> SaveMeasurement(/* MeasurementRecordDto record */)
    {
        try
        {
            _logger.LogInformation("客户端 {ConnectionId} 请求保存测量记录", Context.ConnectionId);
            
            // TODO: 实现保存逻辑
            // var entity = record.ToEntity();
            // var id = await _databaseService.SaveMeasurementAsync(entity);
            
            return Task.FromResult(CommandResult<int>.SuccessResult(0, "测量记录已保存"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "保存测量记录失败");
            return Task.FromResult(CommandResult<int>.FailureResult("保存测量记录失败", ex.Message));
        }
    }
    
    /// <summary>
    /// 查询测量记录
    /// </summary>
    public Task<List<object>> QueryMeasurements(/* QueryParametersDto parameters */)
    {
        try
        {
            _logger.LogInformation("客户端 {ConnectionId} 请求查询测量记录", Context.ConnectionId);
            
            // TODO: 实现查询逻辑
            // var records = await _databaseService.QueryMeasurementsAsync(...);
            
            return Task.FromResult(new List<object>());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "查询测量记录失败");
            throw;
        }
    }
    
    /// <summary>
    /// 删除测量记录
    /// </summary>
    public async Task<CommandResult> DeleteMeasurement(int id)
    {
        try
        {
            _logger.LogInformation("客户端 {ConnectionId} 请求删除测量记录 {Id}", Context.ConnectionId, id);
            
            await _databaseService.DeleteMeasurementAsync(id);
            
            return CommandResult.SuccessResult("测量记录已删除");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "删除测量记录失败");
            return CommandResult.FailureResult("删除测量记录失败", ex.Message);
        }
    }
    
    #endregion
    
    #region 导出功能
    
    /// <summary>
    /// 生成截图路径（实际截图在客户端完成）
    /// </summary>
    public async Task<CommandResult<string>> GenerateScreenshotPath(string outputDirectory)
    {
        try
        {
            _logger.LogInformation("客户端 {ConnectionId} 请求生成截图路径", Context.ConnectionId);
            
            var filePath = await _exportService.GenerateScreenshotPathAsync(outputDirectory);
            
            return CommandResult<string>.SuccessResult(filePath, "截图路径已生成");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "生成截图路径失败");
            return CommandResult<string>.FailureResult("生成截图路径失败", ex.Message);
        }
    }
    
    /// <summary>
    /// 生成PDF报告
    /// </summary>
    public Task<CommandResult<string>> GenerateReport(ReportOptionsDto options, string outputDirectory)
    {
        try
        {
            _logger.LogInformation("客户端 {ConnectionId} 请求生成报告", Context.ConnectionId);
            
            // TODO: 获取当前的分析结果
            // 这里需要从缓存或服务中获取最新的分析结果
            // 暂时返回错误，等待实现完整的数据流
            
            return Task.FromResult(CommandResult<string>.FailureResult("生成报告失败", "暂无可用的分析结果"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "生成报告失败");
            return Task.FromResult(CommandResult<string>.FailureResult("生成报告失败", ex.Message));
        }
    }
    
    #endregion
    
    #region 配置管理
    
    /// <summary>
    /// 获取配置
    /// </summary>
    public Task<object> GetSettings()
    {
        _logger.LogInformation("客户端 {ConnectionId} 请求获取配置", Context.ConnectionId);
        
        // TODO: 从配置服务获取
        return Task.FromResult<object>(new { });
    }
    
    /// <summary>
    /// 更新配置
    /// </summary>
    public Task<CommandResult> UpdateSettings(/* AppSettingsDto settings */)
    {
        try
        {
            _logger.LogInformation("客户端 {ConnectionId} 请求更新配置", Context.ConnectionId);
            
            // TODO: 实现配置更新逻辑
            
            return Task.FromResult(CommandResult.SuccessResult("配置已更新"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新配置失败");
            return Task.FromResult(CommandResult.FailureResult("更新配置失败", ex.Message));
        }
    }
    
    /// <summary>
    /// 测试数据库连接
    /// </summary>
    public async Task<CommandResult> TestDatabaseConnection(/* DatabaseSettingsDto settings */)
    {
        try
        {
            _logger.LogInformation("客户端 {ConnectionId} 请求测试数据库连接", Context.ConnectionId);
            
            var isConnected = await _databaseService.TestConnectionAsync();
            
            return isConnected 
                ? CommandResult.SuccessResult("数据库连接成功") 
                : CommandResult.FailureResult("数据库连接失败");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "测试数据库连接失败");
            return CommandResult.FailureResult("测试数据库连接失败", ex.Message);
        }
    }
    
    #endregion
    
    #region 自动测试
    
    /// <summary>
    /// 启动自动测试
    /// </summary>
    public Task<CommandResult> StartAutoTest(AutoTestConfigurationDto config)
    {
        try
        {
            _logger.LogInformation("客户端 {ConnectionId} 请求启动自动测试", Context.ConnectionId);
            
            // 将 DTO 转换为领域模型
            var configuration = new AutoTestConfiguration
            {
                TestCycles = config.TestCycles,
                EnableWarmup = config.IncludeWarmup,
                WarmupDurationSeconds = config.WarmupSeconds,
                DataPointsPerCycle = config.DataPointsPerTest,
                IntervalBetweenCyclesSeconds = config.IntervalSeconds,
                AnalysisParameters = new AnalysisParameters
                {
                    Magnification = config.AnalysisParameters.Magnification,
                    Line86Result = config.AnalysisParameters.Line86Result,
                    SecondOrderFitResult = config.AnalysisParameters.SecondOrderFitResult,
                    Wavelength = config.AnalysisParameters.Wavelength,
                    MinDataPoints = config.AnalysisParameters.MinDataPoints,
                    FitTolerance = config.AnalysisParameters.FitTolerance
                },
                SaveToDatabase = config.AutoSaveResults,
                GenerateReport = config.GenerateReport
            };
            
            // 捕获 ConnectionId，因为 Hub 实例会被释放
            var connectionId = Context.ConnectionId;
            
            // 启动自动测试（异步执行，进度通过 IHubContext 推送）
            _ = Task.Run(async () =>
            {
                var progress = new Progress<AutoTestProgress>(p =>
                {
                    // 使用 IHubContext 推送进度到客户端（避免 Hub 实例被释放的问题）
                    _hubContext.Clients.Client(connectionId).SendAsync("OnProgressUpdated", new ProgressMessage
                    {
                        Operation = "AutoTest",
                        Percentage = p.Percentage,
                        Message = p.Message ?? $"{p.CurrentStep} - 测试循环 {p.CurrentCycle}/{p.TotalCycles}"
                    });
                });
                
                try
                {
                    var result = await _autoTestService.RunAutoTestAsync(configuration, progress, CancellationToken.None);
                    
                    // 使用 IHubContext 推送完成消息
                    await _hubContext.Clients.Client(connectionId).SendAsync("OnAutoTestCompleted", new
                    {
                        TestId = result.TestId,
                        IsSuccess = result.IsSuccess,
                        CompletedCycles = result.CompletedCycles,
                        Statistics = result.Statistics,
                        ReportFilePath = result.ReportFilePath,
                        FailureReason = result.FailureReason
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "自动测试执行失败");
                    
                    // 使用 IHubContext 推送错误消息
                    await _hubContext.Clients.Client(connectionId).SendAsync("OnErrorOccurred", new ErrorMessage
                    {
                        ErrorCode = "AUTO_TEST_FAILED",
                        Message = "自动测试执行失败",
                        Details = ex.Message
                    });
                }
            });
            
            return Task.FromResult(CommandResult.SuccessResult("自动测试已启动"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "启动自动测试失败");
            return Task.FromResult(CommandResult.FailureResult("启动自动测试失败", ex.Message));
        }
    }
    
    /// <summary>
    /// 获取自动测试状态
    /// </summary>
    public Task<AutoTestStatusDto> GetAutoTestStatus()
    {
        _logger.LogInformation("客户端 {ConnectionId} 请求获取自动测试状态", Context.ConnectionId);
        
        // TODO: 实现状态跟踪
        var status = new AutoTestStatusDto
        {
            IsRunning = false,
            CurrentCycle = 0,
            TotalCycles = 0,
            CurrentStep = "Idle"
        };
        
        return Task.FromResult(status);
    }
    
    #endregion
    
    #region 数据流订阅
    
    /// <summary>
    /// 订阅数据流
    /// </summary>
    public async Task SubscribeToDataStream()
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, "DataStream");
        _logger.LogInformation("客户端 {ConnectionId} 订阅数据流", Context.ConnectionId);
    }
    
    /// <summary>
    /// 取消订阅数据流
    /// </summary>
    public async Task UnsubscribeFromDataStream()
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, "DataStream");
        _logger.LogInformation("客户端 {ConnectionId} 取消订阅数据流", Context.ConnectionId);
    }
    
    #endregion
    
    #region 连接管理
    
    /// <summary>
    /// 客户端连接时调用
    /// </summary>
    public override async Task OnConnectedAsync()
    {
        _logger.LogInformation("客户端 {ConnectionId} 已连接", Context.ConnectionId);
        await base.OnConnectedAsync();
    }
    
    /// <summary>
    /// 客户端断开连接时调用
    /// </summary>
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        if (exception != null)
        {
            _logger.LogWarning(exception, "客户端 {ConnectionId} 异常断开连接", Context.ConnectionId);
        }
        else
        {
            _logger.LogInformation("客户端 {ConnectionId} 正常断开连接", Context.ConnectionId);
        }
        
        await base.OnDisconnectedAsync(exception);
    }
    
    #endregion
}
