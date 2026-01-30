namespace BeamQualityAnalyzer.Contracts.Dtos;

/// <summary>
/// 自动测试配置 DTO
/// </summary>
public class AutoTestConfigurationDto
{
    /// <summary>
    /// 测试名称
    /// </summary>
    public string TestName { get; set; } = "自动测试";
    
    /// <summary>
    /// 测试循环次数
    /// </summary>
    public int TestCycles { get; set; } = 1;
    
    /// <summary>
    /// 每次测试的数据点数
    /// </summary>
    public int DataPointsPerTest { get; set; } = 20;
    
    /// <summary>
    /// 是否包含预热
    /// </summary>
    public bool IncludeWarmup { get; set; } = false;
    
    /// <summary>
    /// 预热时间（秒）
    /// </summary>
    public int WarmupSeconds { get; set; } = 30;
    
    /// <summary>
    /// 测试间隔（秒）
    /// </summary>
    public int IntervalSeconds { get; set; } = 5;
    
    /// <summary>
    /// 是否自动保存结果
    /// </summary>
    public bool AutoSaveResults { get; set; } = true;
    
    /// <summary>
    /// 是否生成报告
    /// </summary>
    public bool GenerateReport { get; set; } = true;
    
    /// <summary>
    /// 分析参数
    /// </summary>
    public required AnalysisParametersDto AnalysisParameters { get; set; }
}
