namespace BeamQualityAnalyzer.Contracts.Dtos;

/// <summary>
/// 分析参数 DTO
/// </summary>
public class AnalysisParametersDto
{
    /// <summary>
    /// 倍率
    /// </summary>
    public double Magnification { get; set; } = 1.0;
    
    /// <summary>
    /// 86线结果
    /// </summary>
    public double Line86Result { get; set; }
    
    /// <summary>
    /// 二阶拟合结果
    /// </summary>
    public double SecondOrderFitResult { get; set; }
    
    /// <summary>
    /// 波长 (nm)
    /// </summary>
    public double Wavelength { get; set; } = 632.8;
    
    /// <summary>
    /// 最小数据点数
    /// </summary>
    public int MinDataPoints { get; set; } = 10;
    
    /// <summary>
    /// 拟合容差
    /// </summary>
    public double FitTolerance { get; set; } = 0.001;
}
