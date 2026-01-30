using BeamQualityAnalyzer.Contracts.Messages;

namespace BeamQualityAnalyzer.Contracts.Dtos;

/// <summary>
/// 测量记录 DTO
/// </summary>
public class MeasurementRecordDto
{
    /// <summary>
    /// 记录ID
    /// </summary>
    public int Id { get; set; }
    
    /// <summary>
    /// 测量时间
    /// </summary>
    public DateTime MeasurementTime { get; set; }
    
    /// <summary>
    /// 设备信息
    /// </summary>
    public string? DeviceInfo { get; set; }
    
    /// <summary>
    /// 状态（Complete, Incomplete, Error）
    /// </summary>
    public string Status { get; set; } = "Complete";
    
    /// <summary>
    /// 备注
    /// </summary>
    public string? Notes { get; set; }
    
    /// <summary>
    /// 原始数据点
    /// </summary>
    public List<RawDataPointDto>? RawDataPoints { get; set; }
    
    /// <summary>
    /// 分析结果
    /// </summary>
    public BeamAnalysisResultDto? AnalysisResult { get; set; }
    
    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
