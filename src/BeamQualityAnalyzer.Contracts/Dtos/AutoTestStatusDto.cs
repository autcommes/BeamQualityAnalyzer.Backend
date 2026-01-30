namespace BeamQualityAnalyzer.Contracts.Dtos;

/// <summary>
/// 自动测试状态 DTO
/// </summary>
public class AutoTestStatusDto
{
    /// <summary>
    /// 是否正在运行
    /// </summary>
    public bool IsRunning { get; set; }
    
    /// <summary>
    /// 当前测试循环
    /// </summary>
    public int CurrentCycle { get; set; }
    
    /// <summary>
    /// 总测试循环数
    /// </summary>
    public int TotalCycles { get; set; }
    
    /// <summary>
    /// 当前步骤
    /// </summary>
    public string? CurrentStep { get; set; }
    
    /// <summary>
    /// 进度百分比 (0-100)
    /// </summary>
    public double ProgressPercentage { get; set; }
    
    /// <summary>
    /// 已完成测试数
    /// </summary>
    public int CompletedTests { get; set; }
    
    /// <summary>
    /// 失败测试数
    /// </summary>
    public int FailedTests { get; set; }
    
    /// <summary>
    /// 开始时间
    /// </summary>
    public DateTime? StartTime { get; set; }
    
    /// <summary>
    /// 预计完成时间
    /// </summary>
    public DateTime? EstimatedCompletionTime { get; set; }
}
