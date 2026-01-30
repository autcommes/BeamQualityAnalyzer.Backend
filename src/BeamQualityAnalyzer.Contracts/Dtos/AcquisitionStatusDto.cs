namespace BeamQualityAnalyzer.Contracts.Dtos;

/// <summary>
/// 采集状态 DTO
/// </summary>
public class AcquisitionStatusDto
{
    /// <summary>
    /// 是否正在采集
    /// </summary>
    public bool IsAcquiring { get; set; }
    
    /// <summary>
    /// 已采集数据点数量
    /// </summary>
    public int DataPointCount { get; set; }
    
    /// <summary>
    /// 采集频率 (Hz)
    /// </summary>
    public double Frequency { get; set; }
    
    /// <summary>
    /// 采集开始时间
    /// </summary>
    public DateTime? StartTime { get; set; }
    
    /// <summary>
    /// 采集持续时间（秒）
    /// </summary>
    public double? DurationSeconds { get; set; }
}
