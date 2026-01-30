namespace BeamQualityAnalyzer.Contracts.Messages;

/// <summary>
/// 计算完成消息
/// </summary>
public class CalculationCompletedMessage
{
    /// <summary>
    /// X 方向 M² 因子
    /// </summary>
    public double MSquaredX { get; set; }
    
    /// <summary>
    /// Y 方向 M² 因子
    /// </summary>
    public double MSquaredY { get; set; }
    
    /// <summary>
    /// 全局 M² 因子
    /// </summary>
    public double MSquaredGlobal { get; set; }
    
    /// <summary>
    /// X 方向腰斑位置 (mm)
    /// </summary>
    public double BeamWaistPositionX { get; set; }
    
    /// <summary>
    /// Y 方向腰斑位置 (mm)
    /// </summary>
    public double BeamWaistPositionY { get; set; }
    
    /// <summary>
    /// X 方向腰斑直径 (μm)
    /// </summary>
    public double BeamWaistDiameterX { get; set; }
    
    /// <summary>
    /// Y 方向腰斑直径 (μm)
    /// </summary>
    public double BeamWaistDiameterY { get; set; }
    
    /// <summary>
    /// X 方向峰值位置
    /// </summary>
    public double PeakPositionX { get; set; }
    
    /// <summary>
    /// Y 方向峰值位置
    /// </summary>
    public double PeakPositionY { get; set; }
    
    /// <summary>
    /// X 方向高斯拟合结果
    /// </summary>
    public GaussianFitResultDto? GaussianFitX { get; set; }
    
    /// <summary>
    /// Y 方向高斯拟合结果
    /// </summary>
    public GaussianFitResultDto? GaussianFitY { get; set; }
    
    /// <summary>
    /// X 方向双曲线拟合结果
    /// </summary>
    public HyperbolicFitResultDto? HyperbolicFitX { get; set; }
    
    /// <summary>
    /// Y 方向双曲线拟合结果
    /// </summary>
    public HyperbolicFitResultDto? HyperbolicFitY { get; set; }
    
    /// <summary>
    /// 完成时间戳
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// 高斯拟合结果 DTO
/// </summary>
public class GaussianFitResultDto
{
    /// <summary>
    /// 振幅
    /// </summary>
    public double Amplitude { get; set; }
    
    /// <summary>
    /// 均值（中心位置）
    /// </summary>
    public double Mean { get; set; }
    
    /// <summary>
    /// 标准差
    /// </summary>
    public double StandardDeviation { get; set; }
    
    /// <summary>
    /// 偏移量
    /// </summary>
    public double Offset { get; set; }
    
    /// <summary>
    /// 拟合优度 R²
    /// </summary>
    public double RSquared { get; set; }
    
    /// <summary>
    /// 拟合曲线数据点
    /// </summary>
    public double[]? FittedCurve { get; set; }
}

/// <summary>
/// 双曲线拟合结果 DTO
/// </summary>
public class HyperbolicFitResultDto
{
    /// <summary>
    /// 腰斑直径 w0 (μm)
    /// </summary>
    public double WaistDiameter { get; set; }
    
    /// <summary>
    /// 腰斑位置 z0 (mm)
    /// </summary>
    public double WaistPosition { get; set; }
    
    /// <summary>
    /// 波长 λ (nm)
    /// </summary>
    public double Wavelength { get; set; }
    
    /// <summary>
    /// M² 因子
    /// </summary>
    public double MSquared { get; set; }
    
    /// <summary>
    /// 拟合优度 R²
    /// </summary>
    public double RSquared { get; set; }
    
    /// <summary>
    /// 拟合曲线数据点
    /// </summary>
    public double[]? FittedCurve { get; set; }
}

/// <summary>
/// 光束分析结果 DTO（完整版本，用于数据库存储和报告生成）
/// </summary>
public class BeamAnalysisResultDto
{
    /// <summary>
    /// X 方向 M² 因子
    /// </summary>
    public double MSquaredX { get; set; }
    
    /// <summary>
    /// Y 方向 M² 因子
    /// </summary>
    public double MSquaredY { get; set; }
    
    /// <summary>
    /// 全局 M² 因子
    /// </summary>
    public double MSquaredGlobal { get; set; }
    
    /// <summary>
    /// X 方向腰斑位置 (mm)
    /// </summary>
    public double BeamWaistPositionX { get; set; }
    
    /// <summary>
    /// Y 方向腰斑位置 (mm)
    /// </summary>
    public double BeamWaistPositionY { get; set; }
    
    /// <summary>
    /// X 方向腰斑直径 (μm)
    /// </summary>
    public double BeamWaistDiameterX { get; set; }
    
    /// <summary>
    /// Y 方向腰斑直径 (μm)
    /// </summary>
    public double BeamWaistDiameterY { get; set; }
    
    /// <summary>
    /// X 方向峰值位置
    /// </summary>
    public double PeakPositionX { get; set; }
    
    /// <summary>
    /// Y 方向峰值位置
    /// </summary>
    public double PeakPositionY { get; set; }
    
    /// <summary>
    /// 拟合曲线数据（X方向）
    /// </summary>
    public double[]? FittedCurveX { get; set; }
    
    /// <summary>
    /// 拟合曲线数据（Y方向）
    /// </summary>
    public double[]? FittedCurveY { get; set; }
}

