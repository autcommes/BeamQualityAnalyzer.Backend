namespace BeamQualityAnalyzer.Contracts.Dtos;

/// <summary>
/// 设备状态 DTO
/// </summary>
public class DeviceStatusDto
{
    /// <summary>
    /// 设备状态
    /// </summary>
    public required string Status { get; set; }
    
    /// <summary>
    /// 设备名称
    /// </summary>
    public string? DeviceName { get; set; }
    
    /// <summary>
    /// 设备型号
    /// </summary>
    public string? DeviceModel { get; set; }
    
    /// <summary>
    /// 是否已连接
    /// </summary>
    public bool IsConnected { get; set; }
    
    /// <summary>
    /// 状态消息
    /// </summary>
    public string? Message { get; set; }
    
    /// <summary>
    /// 最后更新时间
    /// </summary>
    public DateTime LastUpdateTime { get; set; } = DateTime.UtcNow;
}
