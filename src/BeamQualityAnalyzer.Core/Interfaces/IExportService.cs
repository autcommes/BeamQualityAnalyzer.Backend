namespace BeamQualityAnalyzer.Core.Interfaces;

/// <summary>
/// 导出服务接口，负责截图和PDF报告生成
/// </summary>
public interface IExportService
{
    /// <summary>
    /// 生成截图（注意：实际截图需要在客户端完成，此方法生成文件名和路径）
    /// </summary>
    /// <param name="outputDirectory">输出目录</param>
    /// <returns>生成的文件路径</returns>
    Task<string> GenerateScreenshotPathAsync(string outputDirectory);
    
    /// <summary>
    /// 生成PDF报告
    /// </summary>
    /// <param name="result">光束分析结果</param>
    /// <param name="options">报告选项</param>
    /// <param name="outputDirectory">输出目录</param>
    /// <returns>生成的PDF文件路径</returns>
    Task<string> GenerateReportAsync(
        Models.BeamAnalysisResult result,
        ReportOptions options,
        string outputDirectory);
}

/// <summary>
/// 报告选项
/// </summary>
public class ReportOptions
{
    /// <summary>
    /// 报告标题
    /// </summary>
    public string Title { get; set; } = "光束质量分析报告";
    
    /// <summary>
    /// 设备信息
    /// </summary>
    public string DeviceInfo { get; set; } = "虚拟光束轮廓仪";
    
    /// <summary>
    /// 操作员姓名
    /// </summary>
    public string OperatorName { get; set; } = "";
    
    /// <summary>
    /// 备注
    /// </summary>
    public string Notes { get; set; } = "";
    
    /// <summary>
    /// 是否包含2D光斑图
    /// </summary>
    public bool Include2DSpotImage { get; set; } = true;
    
    /// <summary>
    /// 是否包含3D能量分布图
    /// </summary>
    public bool Include3DEnergyDistribution { get; set; } = true;
    
    /// <summary>
    /// 是否包含原始数据表格
    /// </summary>
    public bool IncludeRawDataTable { get; set; } = false;
}
