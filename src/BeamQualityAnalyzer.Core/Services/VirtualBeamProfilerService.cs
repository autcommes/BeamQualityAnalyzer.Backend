using BeamQualityAnalyzer.Core.Interfaces;
using BeamQualityAnalyzer.Core.Models;
using Microsoft.Extensions.Logging;

namespace BeamQualityAnalyzer.Core.Services;

/// <summary>
/// 虚拟光束轮廓测量服务，用于模拟真实的光束测量数据
/// </summary>
public class VirtualBeamProfilerService : IDataAcquisitionService
{
    private readonly ILogger<VirtualBeamProfilerService> _logger;
    private readonly Random _random;
    private CancellationTokenSource? _acquisitionCts;
    private Task? _acquisitionTask;
    private bool _isAcquiring;
    private DeviceStatus _currentStatus;
    
    // 模拟参数
    private const double WaistDiameterX = 50.0;  // μm
    private const double WaistDiameterY = 55.0;  // μm
    private const double WaistPositionX = 0.0;   // mm
    private const double WaistPositionY = 0.0;   // mm
    private const double Wavelength = 632.8;     // nm (He-Ne激光)
    private const double MSquared = 1.2;         // 接近理想光束
    private const int AcquisitionFrequency = 10; // Hz
    private const double NoiseAmplitude = 2.0;   // μm
    
    public VirtualBeamProfilerService(ILogger<VirtualBeamProfilerService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _random = new Random();
        _currentStatus = DeviceStatus.Disconnected;
    }
    
    /// <inheritdoc/>
    public bool IsAcquiring => _isAcquiring;
    
    /// <inheritdoc/>
    public event EventHandler<RawDataReceivedEventArgs>? RawDataReceived;
    
    /// <inheritdoc/>
    public event EventHandler<DeviceStatusChangedEventArgs>? DeviceStatusChanged;
    
    /// <inheritdoc/>
    public async Task StartAcquisitionAsync(CancellationToken cancellationToken)
    {
        if (_isAcquiring)
        {
            _logger.LogWarning("数据采集已在运行中");
            return;
        }
        
        _logger.LogInformation("启动虚拟数据采集");
        
        // 模拟设备连接
        UpdateDeviceStatus(DeviceStatus.Connected, "设备已连接");
        await Task.Delay(100, cancellationToken); // 模拟连接延迟
        
        _isAcquiring = true;
        UpdateDeviceStatus(DeviceStatus.Acquiring, "数据采集中");
        
        // 创建采集任务的取消令牌源
        _acquisitionCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        
        // 启动异步数据采集循环
        _acquisitionTask = Task.Run(async () => await AcquisitionLoopAsync(_acquisitionCts.Token), _acquisitionCts.Token);
    }
    
    /// <inheritdoc/>
    public async Task StopAcquisitionAsync()
    {
        if (!_isAcquiring)
        {
            _logger.LogWarning("数据采集未运行");
            return;
        }
        
        _logger.LogInformation("停止虚拟数据采集");
        
        // 取消采集任务
        _acquisitionCts?.Cancel();
        
        // 等待采集任务完成
        if (_acquisitionTask != null)
        {
            try
            {
                await _acquisitionTask;
            }
            catch (OperationCanceledException)
            {
                // 预期的取消异常
            }
        }
        
        _isAcquiring = false;
        UpdateDeviceStatus(DeviceStatus.Stopped, "数据采集已停止");
        
        // 清理资源
        _acquisitionCts?.Dispose();
        _acquisitionCts = null;
        _acquisitionTask = null;
    }
    
    /// <inheritdoc/>
    public void EmergencyStop()
    {
        _logger.LogWarning("执行急停");
        
        // 立即取消采集
        _acquisitionCts?.Cancel();
        _isAcquiring = false;
        
        UpdateDeviceStatus(DeviceStatus.Stopped, "急停已执行");
    }
    
    /// <inheritdoc/>
    public async Task ResetDeviceAsync()
    {
        _logger.LogInformation("执行设备复位");
        
        UpdateDeviceStatus(DeviceStatus.Resetting, "设备复位中");
        
        // 模拟复位延迟
        await Task.Delay(500);
        
        UpdateDeviceStatus(DeviceStatus.Connected, "设备复位完成");
    }
    
    /// <summary>
    /// 异步数据采集循环（10Hz频率）
    /// </summary>
    private async Task AcquisitionLoopAsync(CancellationToken cancellationToken)
    {
        int dataPointIndex = 0;
        const int totalPoints = 50; // 总共采集50个位置点
        const int delayMs = 1000 / AcquisitionFrequency; // 100ms (10Hz)
        
        try
        {
            while (!cancellationToken.IsCancellationRequested && dataPointIndex < totalPoints)
            {
                // 生成单个数据点
                var dataPoint = GenerateSingleDataPoint(dataPointIndex, totalPoints);
                
                // 触发数据接收事件
                RawDataReceived?.Invoke(this, new RawDataReceivedEventArgs
                {
                    DataPoints = new[] { dataPoint },
                    Timestamp = DateTime.Now
                });
                
                _logger.LogDebug("采集数据点 {Index}/{Total}: 位置={Position:F2}mm, X={DiameterX:F2}μm, Y={DiameterY:F2}μm",
                    dataPointIndex + 1, totalPoints, dataPoint.DetectorPosition, dataPoint.BeamDiameterX, dataPoint.BeamDiameterY);
                
                dataPointIndex++;
                
                // 等待下一个采集周期
                await Task.Delay(delayMs, cancellationToken);
            }
            
            _logger.LogInformation("数据采集完成，共采集 {Count} 个数据点", dataPointIndex);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("数据采集被取消");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "数据采集过程中发生错误");
            UpdateDeviceStatus(DeviceStatus.Error, $"采集错误: {ex.Message}");
        }
    }
    
    /// <summary>
    /// 生成单个数据点
    /// </summary>
    private RawDataPoint GenerateSingleDataPoint(int index, int totalPoints)
    {
        // 探测器位置范围：-50mm 到 +50mm
        double z = -50.0 + index * (100.0 / (totalPoints - 1));
        
        // 使用双曲线公式计算光束直径
        // w(z) = w0 * sqrt(1 + ((z-z0)*λ*M²/(π*w0²))²)
        double diameterX = CalculateBeamDiameter(z, WaistDiameterX, WaistPositionX, Wavelength, MSquared);
        double diameterY = CalculateBeamDiameter(z, WaistDiameterY, WaistPositionY, Wavelength, MSquared);
        
        // 添加随机噪声模拟真实测量
        diameterX += (_random.NextDouble() * 2.0 - 1.0) * NoiseAmplitude;
        diameterY += (_random.NextDouble() * 2.0 - 1.0) * NoiseAmplitude;
        
        // 确保直径为正数
        diameterX = Math.Max(diameterX, 1.0);
        diameterY = Math.Max(diameterY, 1.0);
        
        return new RawDataPoint
        {
            DetectorPosition = z,
            BeamDiameterX = diameterX,
            BeamDiameterY = diameterY,
            IntensityMatrix = GenerateGaussianIntensityMatrix(diameterX, diameterY),
            Timestamp = DateTime.Now
        };
    }
    
    /// <summary>
    /// 计算光束直径（双曲线公式）
    /// </summary>
    /// <param name="z">探测器位置 (mm)</param>
    /// <param name="w0">腰斑直径 (μm)</param>
    /// <param name="z0">腰斑位置 (mm)</param>
    /// <param name="lambda">波长 (nm)</param>
    /// <param name="mSquared">M²因子</param>
    /// <returns>光束直径 (μm)</returns>
    private double CalculateBeamDiameter(double z, double w0, double z0, double lambda, double mSquared)
    {
        // 转换单位：λ从nm转换为μm
        double lambdaMicrons = lambda / 1000.0;
        
        // 双曲线公式: w(z) = w0 * sqrt(1 + ((z-z0)*λ*M²/(π*w0²))²)
        double term = ((z - z0) * lambdaMicrons * mSquared) / (Math.PI * w0 * w0);
        double diameter = w0 * Math.Sqrt(1 + term * term);
        
        return diameter;
    }
    
    /// <summary>
    /// 生成2D高斯强度矩阵（用于光斑可视化）
    /// </summary>
    /// <param name="diameterX">X方向光束直径 (μm)</param>
    /// <param name="diameterY">Y方向光束直径 (μm)</param>
    /// <returns>强度矩阵</returns>
    private double[,] GenerateGaussianIntensityMatrix(double diameterX, double diameterY)
    {
        const int size = 100;
        var matrix = new double[size, size];
        double centerX = size / 2.0;
        double centerY = size / 2.0;
        
        // 高斯分布的标准差约为直径的1/4
        double sigmaX = diameterX / 4.0;
        double sigmaY = diameterY / 4.0;
        
        for (int i = 0; i < size; i++)
        {
            for (int j = 0; j < size; j++)
            {
                double dx = (i - centerX) / sigmaX;
                double dy = (j - centerY) / sigmaY;
                
                // 2D高斯函数: exp(-(dx²+dy²)/2)
                matrix[i, j] = Math.Exp(-(dx * dx + dy * dy) / 2.0);
                
                // 添加少量噪声
                matrix[i, j] += (_random.NextDouble() - 0.5) * 0.05;
                matrix[i, j] = Math.Max(0, Math.Min(1, matrix[i, j])); // 限制在[0,1]范围
            }
        }
        
        return matrix;
    }
    
    /// <summary>
    /// 更新设备状态并触发事件
    /// </summary>
    private void UpdateDeviceStatus(DeviceStatus status, string? message = null)
    {
        _currentStatus = status;
        
        DeviceStatusChanged?.Invoke(this, new DeviceStatusChangedEventArgs
        {
            Status = status,
            Message = message,
            Timestamp = DateTime.Now
        });
        
        _logger.LogInformation("设备状态变更: {Status} - {Message}", status, message ?? "");
    }
}
