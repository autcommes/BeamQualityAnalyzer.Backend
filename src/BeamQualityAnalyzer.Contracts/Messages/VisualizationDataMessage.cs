namespace BeamQualityAnalyzer.Contracts.Messages;

/// <summary>
/// 可视化数据更新消息
/// </summary>
public class VisualizationDataMessage
{
    /// <summary>
    /// 2D光斑强度数据（序列化为JSON）
    /// </summary>
    public string? SpotIntensityDataJson { get; init; }
    
    /// <summary>
    /// 光斑中心X坐标
    /// </summary>
    public double SpotCenterX { get; init; }
    
    /// <summary>
    /// 光斑中心Y坐标
    /// </summary>
    public double SpotCenterY { get; init; }
    
    /// <summary>
    /// 3D能量分布数据（序列化为JSON）
    /// </summary>
    public string? EnergyDistribution3DJson { get; init; }
    
    /// <summary>
    /// 时间戳
    /// </summary>
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}
