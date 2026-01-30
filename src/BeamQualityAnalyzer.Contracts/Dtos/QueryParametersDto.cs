namespace BeamQualityAnalyzer.Contracts.Dtos;

/// <summary>
/// 查询参数 DTO
/// </summary>
public class QueryParametersDto
{
    /// <summary>
    /// 开始时间
    /// </summary>
    public DateTime? StartTime { get; set; }
    
    /// <summary>
    /// 结束时间
    /// </summary>
    public DateTime? EndTime { get; set; }
    
    /// <summary>
    /// 页索引（从0开始）
    /// </summary>
    public int PageIndex { get; set; } = 0;
    
    /// <summary>
    /// 页大小
    /// </summary>
    public int PageSize { get; set; } = 20;
    
    /// <summary>
    /// 状态过滤
    /// </summary>
    public string? StatusFilter { get; set; }
}
