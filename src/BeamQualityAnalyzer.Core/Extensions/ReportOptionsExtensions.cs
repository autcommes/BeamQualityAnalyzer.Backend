using BeamQualityAnalyzer.Core.Interfaces;

namespace BeamQualityAnalyzer.Core.Extensions;

/// <summary>
/// ReportOptions 扩展方法
/// </summary>
public static class ReportOptionsExtensions
{
    /// <summary>
    /// 从 DTO 转换为领域模型
    /// </summary>
    public static ReportOptions ToReportOptions(this Contracts.Dtos.ReportOptionsDto dto)
    {
        if (dto == null)
            throw new ArgumentNullException(nameof(dto));

        return new ReportOptions
        {
            Title = dto.Title,
            DeviceInfo = "虚拟光束轮廓仪", // TODO: 从配置获取
            OperatorName = dto.OperatorName ?? "",
            Notes = dto.Notes ?? "",
            Include2DSpotImage = dto.Include2DSpot,
            Include3DEnergyDistribution = dto.Include3DEnergy,
            IncludeRawDataTable = dto.IncludeRawData
        };
    }
}
