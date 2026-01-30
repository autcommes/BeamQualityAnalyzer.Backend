namespace BeamQualityAnalyzer.Contracts.Dtos;

/// <summary>
/// 报告选项 DTO
/// </summary>
public class ReportOptionsDto
{
    /// <summary>
    /// 报告标题
    /// </summary>
    public string Title { get; set; } = "光束质量分析报告";
    
    /// <summary>
    /// 包含原始数据
    /// </summary>
    public bool IncludeRawData { get; set; } = true;
    
    /// <summary>
    /// 包含曲线图
    /// </summary>
    public bool IncludeCharts { get; set; } = true;
    
    /// <summary>
    /// 包含2D光斑图
    /// </summary>
    public bool Include2DSpot { get; set; } = true;
    
    /// <summary>
    /// 包含3D能量分布图
    /// </summary>
    public bool Include3DEnergy { get; set; } = true;
    
    /// <summary>
    /// 备注
    /// </summary>
    public string? Notes { get; set; }
    
    /// <summary>
    /// 操作员姓名
    /// </summary>
    public string? OperatorName { get; set; }
}
