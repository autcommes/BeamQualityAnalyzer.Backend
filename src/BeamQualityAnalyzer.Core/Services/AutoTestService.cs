using BeamQualityAnalyzer.Core.Interfaces;
using BeamQualityAnalyzer.Core.Models;
using Microsoft.Extensions.Logging;

namespace BeamQualityAnalyzer.Core.Services;

/// <summary>
/// 自动测试服务实现，负责执行自动化测试流程
/// </summary>
public class AutoTestService : IAutoTestService
{
    private readonly IDataAcquisitionService _dataAcquisitionService;
    private readonly IAlgorithmService _algorithmService;
    private readonly ILogger<AutoTestService> _logger;
    
    public AutoTestService(
        IDataAcquisitionService dataAcquisitionService,
        IAlgorithmService algorithmService,
        ILogger<AutoTestService> logger)
    {
        _dataAcquisitionService = dataAcquisitionService ?? throw new ArgumentNullException(nameof(dataAcquisitionService));
        _algorithmService = algorithmService ?? throw new ArgumentNullException(nameof(algorithmService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }
    
    /// <inheritdoc/>
    public async Task<AutoTestResult> RunAutoTestAsync(
        AutoTestConfiguration config,
        IProgress<AutoTestProgress> progress,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(config);
        ArgumentNullException.ThrowIfNull(progress);
        
        _logger.LogInformation("开始自动测试，测试循环数: {TestCycles}", config.TestCycles);
        
        var result = new AutoTestResult
        {
            StartTime = DateTime.Now,
            IsSuccess = false
        };
        
        try
        {
            // 步骤1: 设备复位
            await ExecuteDeviceResetAsync(progress, cancellationToken);
            
            // 步骤2: 预热（可选）
            if (config.EnableWarmup)
            {
                await ExecuteWarmupAsync(config, progress, cancellationToken);
            }
            
            // 步骤3: 执行测试循环
            for (int cycle = 1; cycle <= config.TestCycles; cycle++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                
                _logger.LogInformation("执行测试循环 {Cycle}/{TotalCycles}", cycle, config.TestCycles);
                
                // 报告进度
                ReportProgress(progress, $"测试循环 {cycle}/{config.TestCycles}", 
                    CalculateProgressPercentage(cycle, config.TestCycles, config.EnableWarmup),
                    cycle, config.TestCycles, $"正在执行第 {cycle} 次测量");
                
                // 执行单次测量循环
                var cycleResult = await ExecuteTestCycleAsync(config, cancellationToken);
                
                // 验证结果
                ValidateCycleResult(cycleResult);
                
                result.CycleResults.Add(cycleResult);
                result.CompletedCycles = cycle;
                
                // 循环间隔
                if (cycle < config.TestCycles && config.IntervalBetweenCyclesSeconds > 0)
                {
                    _logger.LogDebug("等待 {Interval} 秒后继续下一次测量", config.IntervalBetweenCyclesSeconds);
                    await Task.Delay(TimeSpan.FromSeconds(config.IntervalBetweenCyclesSeconds), cancellationToken);
                }
            }
            
            // 步骤4: 计算统计数据
            result.Statistics = CalculateStatistics(result.CycleResults);
            
            // 步骤5: 生成测试报告（如果需要）
            if (config.GenerateReport)
            {
                ReportProgress(progress, "生成测试报告", 95, 0, 0, "正在生成测试报告");
                result.ReportFilePath = await GenerateTestReportAsync(result, cancellationToken);
            }
            
            result.IsSuccess = true;
            result.EndTime = DateTime.Now;
            
            _logger.LogInformation("自动测试完成，成功完成 {CompletedCycles} 个测试循环", result.CompletedCycles);
            
            // 报告完成
            ReportProgress(progress, "测试完成", 100, 0, 0, "自动测试已成功完成");
            
            return result;
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("自动测试被取消");
            result.IsSuccess = false;
            result.FailureReason = "测试被用户取消";
            result.EndTime = DateTime.Now;
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "自动测试失败: {Message}", ex.Message);
            result.IsSuccess = false;
            result.FailureReason = ex.Message;
            result.FailedStep = "未知步骤";
            result.EndTime = DateTime.Now;
            return result;
        }
    }
    
    /// <summary>
    /// 执行设备复位
    /// </summary>
    private async Task ExecuteDeviceResetAsync(IProgress<AutoTestProgress> progress, CancellationToken cancellationToken)
    {
        _logger.LogInformation("执行设备复位");
        ReportProgress(progress, "设备复位", 5, 0, 0, "正在复位设备");
        
        try
        {
            await _dataAcquisitionService.ResetDeviceAsync();
            await Task.Delay(1000, cancellationToken); // 等待复位完成
            _logger.LogInformation("设备复位完成");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "设备复位失败");
            throw new InvalidOperationException("设备复位失败", ex);
        }
    }
    
    /// <summary>
    /// 执行预热
    /// </summary>
    private async Task ExecuteWarmupAsync(AutoTestConfiguration config, IProgress<AutoTestProgress> progress, CancellationToken cancellationToken)
    {
        _logger.LogInformation("开始预热，持续时间: {Duration} 秒", config.WarmupDurationSeconds);
        ReportProgress(progress, "设备预热", 10, 0, 0, $"正在预热设备（{config.WarmupDurationSeconds}秒）");
        
        // 启动采集
        await _dataAcquisitionService.StartAcquisitionAsync(cancellationToken);
        
        // 等待预热时间
        await Task.Delay(TimeSpan.FromSeconds(config.WarmupDurationSeconds), cancellationToken);
        
        // 停止采集
        await _dataAcquisitionService.StopAcquisitionAsync();
        
        _logger.LogInformation("预热完成");
    }
    
    /// <summary>
    /// 执行单次测试循环
    /// </summary>
    private async Task<BeamAnalysisResult> ExecuteTestCycleAsync(AutoTestConfiguration config, CancellationToken cancellationToken)
    {
        var collectedData = new List<RawDataPoint>();
        var dataCollectionComplete = new TaskCompletionSource<bool>();
        
        // 订阅数据接收事件
        EventHandler<RawDataReceivedEventArgs> dataHandler = (sender, e) =>
        {
            collectedData.AddRange(e.DataPoints);
            
            if (collectedData.Count >= config.DataPointsPerCycle)
            {
                dataCollectionComplete.TrySetResult(true);
            }
        };
        
        try
        {
            _dataAcquisitionService.RawDataReceived += dataHandler;
            
            // 启动数据采集
            _logger.LogDebug("启动数据采集，目标数据点数: {DataPoints}", config.DataPointsPerCycle);
            await _dataAcquisitionService.StartAcquisitionAsync(cancellationToken);
            
            // 等待数据采集完成或超时
            using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);
            
            await dataCollectionComplete.Task.WaitAsync(linkedCts.Token);
            
            // 停止数据采集
            await _dataAcquisitionService.StopAcquisitionAsync();
            
            _logger.LogDebug("数据采集完成，共采集 {Count} 个数据点", collectedData.Count);
            
            // 执行算法计算
            _logger.LogDebug("开始算法计算");
            var analysisResult = await _algorithmService.AnalyzeAsync(
                collectedData.Take(config.DataPointsPerCycle).ToArray(),
                config.AnalysisParameters,
                cancellationToken);
            
            _logger.LogDebug("算法计算完成，M²X={MSquaredX:F4}, M²Y={MSquaredY:F4}", 
                analysisResult.MSquaredX, analysisResult.MSquaredY);
            
            return analysisResult;
        }
        finally
        {
            _dataAcquisitionService.RawDataReceived -= dataHandler;
        }
    }
    
    /// <summary>
    /// 验证测试循环结果
    /// </summary>
    private void ValidateCycleResult(BeamAnalysisResult result)
    {
        // 验证 M² 因子
        if (result.MSquaredX < 1.0 || result.MSquaredY < 1.0)
        {
            _logger.LogWarning("M² 因子异常: M²X={MSquaredX}, M²Y={MSquaredY}", 
                result.MSquaredX, result.MSquaredY);
            throw new InvalidOperationException($"M² 因子异常: M²X={result.MSquaredX:F4}, M²Y={result.MSquaredY:F4}");
        }
        
        // 验证腰斑直径
        if (result.BeamWaistDiameterX <= 0 || result.BeamWaistDiameterY <= 0)
        {
            _logger.LogWarning("腰斑直径异常: X={DiameterX}, Y={DiameterY}", 
                result.BeamWaistDiameterX, result.BeamWaistDiameterY);
            throw new InvalidOperationException($"腰斑直径异常: X={result.BeamWaistDiameterX:F4}, Y={result.BeamWaistDiameterY:F4}");
        }
        
        _logger.LogDebug("测试循环结果验证通过");
    }
    
    /// <summary>
    /// 计算统计数据
    /// </summary>
    private AutoTestStatistics CalculateStatistics(List<BeamAnalysisResult> results)
    {
        if (results.Count == 0)
        {
            throw new InvalidOperationException("没有可用的测试结果");
        }
        
        _logger.LogInformation("计算统计数据，样本数: {Count}", results.Count);
        
        var mSquaredXValues = results.Select(r => r.MSquaredX).ToArray();
        var mSquaredYValues = results.Select(r => r.MSquaredY).ToArray();
        var waistDiameterXValues = results.Select(r => r.BeamWaistDiameterX).ToArray();
        var waistDiameterYValues = results.Select(r => r.BeamWaistDiameterY).ToArray();
        
        var statistics = new AutoTestStatistics
        {
            MSquaredXAverage = CalculateAverage(mSquaredXValues),
            MSquaredXStdDev = CalculateStandardDeviation(mSquaredXValues),
            MSquaredYAverage = CalculateAverage(mSquaredYValues),
            MSquaredYStdDev = CalculateStandardDeviation(mSquaredYValues),
            BeamWaistDiameterXAverage = CalculateAverage(waistDiameterXValues),
            BeamWaistDiameterXStdDev = CalculateStandardDeviation(waistDiameterXValues),
            BeamWaistDiameterYAverage = CalculateAverage(waistDiameterYValues),
            BeamWaistDiameterYStdDev = CalculateStandardDeviation(waistDiameterYValues)
        };
        
        _logger.LogInformation("统计数据: M²X={MSquaredXAvg:F4}±{MSquaredXStd:F4}, M²Y={MSquaredYAvg:F4}±{MSquaredYStd:F4}",
            statistics.MSquaredXAverage, statistics.MSquaredXStdDev,
            statistics.MSquaredYAverage, statistics.MSquaredYStdDev);
        
        return statistics;
    }
    
    /// <summary>
    /// 计算平均值
    /// </summary>
    private double CalculateAverage(double[] values)
    {
        return values.Average();
    }
    
    /// <summary>
    /// 计算标准差
    /// </summary>
    private double CalculateStandardDeviation(double[] values)
    {
        if (values.Length <= 1)
        {
            return 0.0;
        }
        
        double average = values.Average();
        double sumOfSquares = values.Sum(v => Math.Pow(v - average, 2));
        return Math.Sqrt(sumOfSquares / (values.Length - 1));
    }
    
    /// <summary>
    /// 生成测试报告
    /// </summary>
    private async Task<string> GenerateTestReportAsync(AutoTestResult result, CancellationToken cancellationToken)
    {
        _logger.LogInformation("生成测试报告");
        
        // 创建报告目录
        var reportDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), 
            "BeamAnalyzer", "AutoTestReports");
        Directory.CreateDirectory(reportDirectory);
        
        // 生成报告文件名
        var reportFileName = $"AutoTest_{result.TestId:N}_{result.StartTime:yyyyMMdd_HHmmss}.txt";
        var reportFilePath = Path.Combine(reportDirectory, reportFileName);
        
        // 生成报告内容
        var reportContent = GenerateReportContent(result);
        
        // 写入文件
        await File.WriteAllTextAsync(reportFilePath, reportContent, cancellationToken);
        
        _logger.LogInformation("测试报告已保存: {FilePath}", reportFilePath);
        
        return reportFilePath;
    }
    
    /// <summary>
    /// 生成报告内容
    /// </summary>
    private string GenerateReportContent(AutoTestResult result)
    {
        var sb = new System.Text.StringBuilder();
        
        sb.AppendLine("========================================");
        sb.AppendLine("       光束质量分析系统自动测试报告");
        sb.AppendLine("========================================");
        sb.AppendLine();
        sb.AppendLine($"测试ID: {result.TestId}");
        sb.AppendLine($"开始时间: {result.StartTime:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine($"结束时间: {result.EndTime:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine($"测试时长: {(result.EndTime - result.StartTime).TotalSeconds:F2} 秒");
        sb.AppendLine($"测试状态: {(result.IsSuccess ? "成功" : "失败")}");
        
        if (!result.IsSuccess)
        {
            sb.AppendLine($"失败原因: {result.FailureReason}");
            sb.AppendLine($"失败步骤: {result.FailedStep}");
        }
        
        sb.AppendLine($"完成循环数: {result.CompletedCycles}");
        sb.AppendLine();
        
        if (result.Statistics != null)
        {
            sb.AppendLine("========================================");
            sb.AppendLine("              统计数据");
            sb.AppendLine("========================================");
            sb.AppendLine();
            sb.AppendLine($"M² 因子 X 方向:");
            sb.AppendLine($"  平均值: {result.Statistics.MSquaredXAverage:F4}");
            sb.AppendLine($"  标准差: {result.Statistics.MSquaredXStdDev:F4}");
            sb.AppendLine();
            sb.AppendLine($"M² 因子 Y 方向:");
            sb.AppendLine($"  平均值: {result.Statistics.MSquaredYAverage:F4}");
            sb.AppendLine($"  标准差: {result.Statistics.MSquaredYStdDev:F4}");
            sb.AppendLine();
            sb.AppendLine($"腰斑直径 X 方向:");
            sb.AppendLine($"  平均值: {result.Statistics.BeamWaistDiameterXAverage:F4} μm");
            sb.AppendLine($"  标准差: {result.Statistics.BeamWaistDiameterXStdDev:F4} μm");
            sb.AppendLine();
            sb.AppendLine($"腰斑直径 Y 方向:");
            sb.AppendLine($"  平均值: {result.Statistics.BeamWaistDiameterYAverage:F4} μm");
            sb.AppendLine($"  标准差: {result.Statistics.BeamWaistDiameterYStdDev:F4} μm");
            sb.AppendLine();
        }
        
        if (result.CycleResults.Count > 0)
        {
            sb.AppendLine("========================================");
            sb.AppendLine("            各循环详细结果");
            sb.AppendLine("========================================");
            sb.AppendLine();
            
            for (int i = 0; i < result.CycleResults.Count; i++)
            {
                var cycleResult = result.CycleResults[i];
                sb.AppendLine($"循环 {i + 1}:");
                sb.AppendLine($"  测量时间: {cycleResult.MeasurementTime:yyyy-MM-dd HH:mm:ss}");
                sb.AppendLine($"  M²X: {cycleResult.MSquaredX:F4}");
                sb.AppendLine($"  M²Y: {cycleResult.MSquaredY:F4}");
                sb.AppendLine($"  腰斑直径X: {cycleResult.BeamWaistDiameterX:F4} μm");
                sb.AppendLine($"  腰斑直径Y: {cycleResult.BeamWaistDiameterY:F4} μm");
                sb.AppendLine($"  腰斑位置X: {cycleResult.BeamWaistPositionX:F4} mm");
                sb.AppendLine($"  腰斑位置Y: {cycleResult.BeamWaistPositionY:F4} mm");
                sb.AppendLine();
            }
        }
        
        sb.AppendLine("========================================");
        sb.AppendLine("              报告结束");
        sb.AppendLine("========================================");
        
        return sb.ToString();
    }
    
    /// <summary>
    /// 报告进度
    /// </summary>
    private void ReportProgress(IProgress<AutoTestProgress> progress, string step, int percentage, 
        int currentCycle, int totalCycles, string? message = null)
    {
        progress.Report(new AutoTestProgress
        {
            CurrentStep = step,
            Percentage = percentage,
            CurrentCycle = currentCycle,
            TotalCycles = totalCycles,
            Message = message
        });
    }
    
    /// <summary>
    /// 计算进度百分比
    /// </summary>
    private int CalculateProgressPercentage(int currentCycle, int totalCycles, bool hasWarmup)
    {
        // 预留 15% 给初始化和报告生成
        int basePercentage = hasWarmup ? 15 : 10;
        int cyclePercentage = 80;
        
        double progress = basePercentage + (cyclePercentage * currentCycle / (double)totalCycles);
        return (int)Math.Min(progress, 90);
    }
}
