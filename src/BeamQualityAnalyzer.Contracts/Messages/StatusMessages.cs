namespace BeamQualityAnalyzer.Contracts.Messages;

/// <summary>
/// 设备状态消息
/// </summary>
public class DeviceStatusMessage
{
    /// <summary>
    /// 设备状态
    /// </summary>
    public required string Status { get; init; }
    
    /// <summary>
    /// 状态消息
    /// </summary>
    public string? Message { get; init; }
    
    /// <summary>
    /// 时间戳
    /// </summary>
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// 采集状态消息
/// </summary>
public class AcquisitionStatusMessage
{
    /// <summary>
    /// 是否正在采集
    /// </summary>
    public bool IsAcquiring { get; init; }
    
    /// <summary>
    /// 已采集数据点数量
    /// </summary>
    public int DataPointCount { get; init; }
    
    /// <summary>
    /// 采集频率 (Hz)
    /// </summary>
    public double Frequency { get; init; }
    
    /// <summary>
    /// 时间戳
    /// </summary>
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// 错误消息
/// </summary>
public class ErrorMessage
{
    /// <summary>
    /// 错误标题
    /// </summary>
    public string? Title { get; init; }
    
    /// <summary>
    /// 错误代码
    /// </summary>
    public string? ErrorCode { get; init; }
    
    /// <summary>
    /// 错误消息
    /// </summary>
    public required string Message { get; init; }
    
    /// <summary>
    /// 错误详情
    /// </summary>
    public string? Details { get; init; }
    
    /// <summary>
    /// 错误级别
    /// </summary>
    public string Level { get; init; } = "Error";
    
    /// <summary>
    /// 时间戳
    /// </summary>
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// 进度消息
/// </summary>
public class ProgressMessage
{
    /// <summary>
    /// 操作名称
    /// </summary>
    public required string Operation { get; init; }
    
    /// <summary>
    /// 进度百分比 (0-100)
    /// </summary>
    public double Percentage { get; init; }
    
    /// <summary>
    /// 进度消息
    /// </summary>
    public string? Message { get; init; }
    
    /// <summary>
    /// 时间戳
    /// </summary>
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// 日志消息
/// </summary>
public class LogMessage
{
    /// <summary>
    /// 日志级别
    /// </summary>
    public required string Level { get; init; }
    
    /// <summary>
    /// 日志消息
    /// </summary>
    public required string Message { get; init; }
    
    /// <summary>
    /// 时间戳
    /// </summary>
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}
