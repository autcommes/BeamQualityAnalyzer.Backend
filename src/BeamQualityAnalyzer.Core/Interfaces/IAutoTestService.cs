using BeamQualityAnalyzer.Core.Models;

namespace BeamQualityAnalyzer.Core.Interfaces;

/// <summary>
/// 自动测试服务接口，负责执行自动化测试流程
/// </summary>
public interface IAutoTestService
{
    /// <summary>
    /// 启动自动测试
    /// </summary>
    /// <param name="config">自动测试配置</param>
    /// <param name="progress">进度报告接口</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>自动测试结果</returns>
    Task<AutoTestResult> RunAutoTestAsync(
        AutoTestConfiguration config,
        IProgress<AutoTestProgress> progress,
        CancellationToken cancellationToken);
}

/// <summary>
/// 自动测试配置
/// </summary>
public class AutoTestConfiguration
{
    /// <summary>
    /// 测试循环次数
    /// </summary>
    public int TestCycles { get; set; } = 3;
    
    /// <summary>
    /// 是否执行预热
    /// </summary>
    public bool EnableWarmup { get; set; } = true;
    
    /// <summary>
    /// 预热时间（秒）
    /// </summary>
    public int WarmupDurationSeconds { get; set; } = 10;
    
    /// <summary>
    /// 每次测量的数据点数量
    /// </summary>
    public int DataPointsPerCycle { get; set; } = 20;
    
    /// <summary>
    /// 测量间隔（秒）
    /// </summary>
    public int IntervalBetweenCyclesSeconds { get; set; } = 5;
    
    /// <summary>
    /// 分析参数
    /// </summary>
    public required AnalysisParameters AnalysisParameters { get; set; }
    
    /// <summary>
    /// 是否保存测试结果到数据库
    /// </summary>
    public bool SaveToDatabase { get; set; } = true;
    
    /// <summary>
    /// 是否生成测试报告
    /// </summary>
    public bool GenerateReport { get; set; } = true;
}

/// <summary>
/// 自动测试进度
/// </summary>
public class AutoTestProgress
{
    /// <summary>
    /// 当前步骤
    /// </summary>
    public required string CurrentStep { get; set; }
    
    /// <summary>
    /// 进度百分比（0-100）
    /// </summary>
    public int Percentage { get; set; }
    
    /// <summary>
    /// 当前循环索引（从1开始）
    /// </summary>
    public int CurrentCycle { get; set; }
    
    /// <summary>
    /// 总循环数
    /// </summary>
    public int TotalCycles { get; set; }
    
    /// <summary>
    /// 进度消息
    /// </summary>
    public string? Message { get; set; }
}

/// <summary>
/// 自动测试结果
/// </summary>
public class AutoTestResult
{
    /// <summary>
    /// 测试ID
    /// </summary>
    public Guid TestId { get; set; } = Guid.NewGuid();
    
    /// <summary>
    /// 测试开始时间
    /// </summary>
    public DateTime StartTime { get; set; }
    
    /// <summary>
    /// 测试结束时间
    /// </summary>
    public DateTime EndTime { get; set; }
    
    /// <summary>
    /// 测试是否成功
    /// </summary>
    public bool IsSuccess { get; set; }
    
    /// <summary>
    /// 失败原因（如果失败）
    /// </summary>
    public string? FailureReason { get; set; }
    
    /// <summary>
    /// 失败步骤（如果失败）
    /// </summary>
    public string? FailedStep { get; set; }
    
    /// <summary>
    /// 完成的测试循环数
    /// </summary>
    public int CompletedCycles { get; set; }
    
    /// <summary>
    /// 每个循环的测量结果
    /// </summary>
    public List<BeamAnalysisResult> CycleResults { get; set; } = new();
    
    /// <summary>
    /// 统计数据
    /// </summary>
    public AutoTestStatistics? Statistics { get; set; }
    
    /// <summary>
    /// 测试报告文件路径（如果生成）
    /// </summary>
    public string? ReportFilePath { get; set; }
}

/// <summary>
/// 自动测试统计数据
/// </summary>
public class AutoTestStatistics
{
    /// <summary>
    /// M²因子X方向平均值
    /// </summary>
    public double MSquaredXAverage { get; set; }
    
    /// <summary>
    /// M²因子X方向标准差
    /// </summary>
    public double MSquaredXStdDev { get; set; }
    
    /// <summary>
    /// M²因子Y方向平均值
    /// </summary>
    public double MSquaredYAverage { get; set; }
    
    /// <summary>
    /// M²因子Y方向标准差
    /// </summary>
    public double MSquaredYStdDev { get; set; }
    
    /// <summary>
    /// 腰斑直径X方向平均值
    /// </summary>
    public double BeamWaistDiameterXAverage { get; set; }
    
    /// <summary>
    /// 腰斑直径X方向标准差
    /// </summary>
    public double BeamWaistDiameterXStdDev { get; set; }
    
    /// <summary>
    /// 腰斑直径Y方向平均值
    /// </summary>
    public double BeamWaistDiameterYAverage { get; set; }
    
    /// <summary>
    /// 腰斑直径Y方向标准差
    /// </summary>
    public double BeamWaistDiameterYStdDev { get; set; }
}
