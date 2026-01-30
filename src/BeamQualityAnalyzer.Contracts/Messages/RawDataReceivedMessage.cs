namespace BeamQualityAnalyzer.Contracts.Messages;

/// <summary>
/// 原始数据接收消息（10Hz频率推送）
/// </summary>
public class RawDataReceivedMessage
{
    /// <summary>
    /// 数据点数组
    /// </summary>
    public required RawDataPointDto[] DataPoints { get; init; }
    
    /// <summary>
    /// 接收时间戳
    /// </summary>
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    
    /// <summary>
    /// 数据点数量
    /// </summary>
    public int Count => DataPoints?.Length ?? 0;
}

/// <summary>
/// 原始数据点 DTO
/// </summary>
public class RawDataPointDto
{
    /// <summary>
    /// 探测器位置 (mm)
    /// </summary>
    public double DetectorPosition { get; set; }
    
    /// <summary>
    /// X 方向光束直径 (μm)
    /// </summary>
    public double BeamDiameterX { get; set; }
    
    /// <summary>
    /// Y 方向光束直径 (μm)
    /// </summary>
    public double BeamDiameterY { get; set; }
    
    /// <summary>
    /// 时间戳
    /// </summary>
    public DateTime Timestamp { get; set; }
}
