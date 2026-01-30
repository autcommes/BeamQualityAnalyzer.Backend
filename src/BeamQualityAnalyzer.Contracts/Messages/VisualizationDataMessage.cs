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
    /// 3D能量分布数据（序列化为JSON）- 已废弃，使用EnergyDistribution3DXJson和EnergyDistribution3DYJson
    /// </summary>
    [Obsolete("使用 EnergyDistribution3DXJson 和 EnergyDistribution3DYJson 代替")]
    public string? EnergyDistribution3DJson { get; init; }
    
    /// <summary>
    /// X方向3D能量分布数据（序列化为JSON）
    /// </summary>
    public string? EnergyDistribution3DXJson { get; init; }
    
    /// <summary>
    /// Y方向3D能量分布数据（序列化为JSON）
    /// </summary>
    public string? EnergyDistribution3DYJson { get; init; }
    
    /// <summary>
    /// 时间戳
    /// </summary>
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}
