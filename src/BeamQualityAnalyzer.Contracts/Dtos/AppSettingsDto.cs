namespace BeamQualityAnalyzer.Contracts.Dtos;

/// <summary>
/// 应用配置 DTO
/// </summary>
public class AppSettingsDto
{
    /// <summary>
    /// 设备配置
    /// </summary>
    public DeviceSettingsDto? Device { get; set; }
    
    /// <summary>
    /// 算法配置
    /// </summary>
    public AlgorithmSettingsDto? Algorithm { get; set; }
    
    /// <summary>
    /// 导出配置
    /// </summary>
    public ExportSettingsDto? Export { get; set; }
    
    /// <summary>
    /// 数据库配置
    /// </summary>
    public DatabaseSettingsDto? Database { get; set; }
    
    /// <summary>
    /// UI配置
    /// </summary>
    public UISettingsDto? UI { get; set; }
}

/// <summary>
/// 设备配置 DTO
/// </summary>
public class DeviceSettingsDto
{
    /// <summary>
    /// 连接类型（Serial, USB, Network, Virtual）
    /// </summary>
    public string ConnectionType { get; set; } = "Virtual";
    
    /// <summary>
    /// 端口名称
    /// </summary>
    public string? PortName { get; set; }
    
    /// <summary>
    /// 波特率
    /// </summary>
    public int BaudRate { get; set; } = 115200;
    
    /// <summary>
    /// 采集频率 (Hz)
    /// </summary>
    public int AcquisitionFrequency { get; set; } = 10;
}

/// <summary>
/// 算法配置 DTO
/// </summary>
public class AlgorithmSettingsDto
{
    /// <summary>
    /// 默认波长 (nm)
    /// </summary>
    public double DefaultWavelength { get; set; } = 632.8;
    
    /// <summary>
    /// 最小数据点数
    /// </summary>
    public int MinDataPoints { get; set; } = 10;
    
    /// <summary>
    /// 拟合容差
    /// </summary>
    public double FitTolerance { get; set; } = 0.001;
}

/// <summary>
/// 导出配置 DTO
/// </summary>
public class ExportSettingsDto
{
    /// <summary>
    /// 截图目录
    /// </summary>
    public string ScreenshotDirectory { get; set; } = "Screenshots";
    
    /// <summary>
    /// 报告目录
    /// </summary>
    public string ReportDirectory { get; set; } = "Reports";
    
    /// <summary>
    /// 图像格式（PNG, JPEG）
    /// </summary>
    public string ImageFormat { get; set; } = "PNG";
}

/// <summary>
/// 数据库配置 DTO
/// </summary>
public class DatabaseSettingsDto
{
    /// <summary>
    /// 数据库类型（SQLite, MySQL, SqlServer）
    /// </summary>
    public string DatabaseType { get; set; } = "SQLite";
    
    /// <summary>
    /// 连接字符串
    /// </summary>
    public string ConnectionString { get; set; } = "Data Source=beam_analyzer.db";
    
    /// <summary>
    /// 自动备份
    /// </summary>
    public bool AutoBackup { get; set; } = true;
    
    /// <summary>
    /// 命令超时（秒）
    /// </summary>
    public int CommandTimeout { get; set; } = 30;
    
    /// <summary>
    /// 启用失败重试
    /// </summary>
    public bool EnableRetryOnFailure { get; set; } = true;
    
    /// <summary>
    /// 最大重试次数
    /// </summary>
    public int MaxRetryCount { get; set; } = 3;
}

/// <summary>
/// UI配置 DTO
/// </summary>
public class UISettingsDto
{
    /// <summary>
    /// 主题（Dark, Light）
    /// </summary>
    public string Theme { get; set; } = "Dark";
    
    /// <summary>
    /// 图表刷新间隔 (ms)
    /// </summary>
    public double ChartRefreshInterval { get; set; } = 200;
    
    /// <summary>
    /// 3D可视化刷新间隔 (ms)
    /// </summary>
    public double Visualization3DRefreshInterval { get; set; } = 300;
}
