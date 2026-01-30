using BeamQualityAnalyzer.Core.Models;

namespace BeamQualityAnalyzer.Core.Interfaces;

/// <summary>
/// 数据采集服务接口，负责从设备或文件采集原始光束数据
/// </summary>
public interface IDataAcquisitionService
{
    /// <summary>
    /// 启动数据采集
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>异步任务</returns>
    Task StartAcquisitionAsync(CancellationToken cancellationToken);
    
    /// <summary>
    /// 停止数据采集
    /// </summary>
    /// <returns>异步任务</returns>
    Task StopAcquisitionAsync();
    
    /// <summary>
    /// 急停 - 立即停止所有数据采集和设备运动
    /// </summary>
    void EmergencyStop();
    
    /// <summary>
    /// 设备复位
    /// </summary>
    /// <returns>异步任务</returns>
    Task ResetDeviceAsync();
    
    /// <summary>
    /// 当前采集状态
    /// </summary>
    bool IsAcquiring { get; }
    
    /// <summary>
    /// 原始数据接收事件
    /// </summary>
    event EventHandler<RawDataReceivedEventArgs>? RawDataReceived;
    
    /// <summary>
    /// 设备状态变化事件
    /// </summary>
    event EventHandler<DeviceStatusChangedEventArgs>? DeviceStatusChanged;
}

/// <summary>
/// 原始数据接收事件参数
/// </summary>
public class RawDataReceivedEventArgs : EventArgs
{
    /// <summary>
    /// 原始数据点数组
    /// </summary>
    public required RawDataPoint[] DataPoints { get; init; }
    
    /// <summary>
    /// 接收时间戳
    /// </summary>
    public DateTime Timestamp { get; init; } = DateTime.Now;
}

/// <summary>
/// 设备状态变化事件参数
/// </summary>
public class DeviceStatusChangedEventArgs : EventArgs
{
    /// <summary>
    /// 设备状态
    /// </summary>
    public required DeviceStatus Status { get; init; }
    
    /// <summary>
    /// 状态消息
    /// </summary>
    public string? Message { get; init; }
    
    /// <summary>
    /// 时间戳
    /// </summary>
    public DateTime Timestamp { get; init; } = DateTime.Now;
}

/// <summary>
/// 设备状态枚举
/// </summary>
public enum DeviceStatus
{
    /// <summary>
    /// 未连接
    /// </summary>
    Disconnected,
    
    /// <summary>
    /// 已连接
    /// </summary>
    Connected,
    
    /// <summary>
    /// 采集中
    /// </summary>
    Acquiring,
    
    /// <summary>
    /// 已停止
    /// </summary>
    Stopped,
    
    /// <summary>
    /// 错误
    /// </summary>
    Error,
    
    /// <summary>
    /// 复位中
    /// </summary>
    Resetting
}
