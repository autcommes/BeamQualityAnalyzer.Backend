using BeamQualityAnalyzer.Core.Interfaces;
using BeamQualityAnalyzer.Core.Models;
using Microsoft.Extensions.Logging;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace BeamQualityAnalyzer.Core.Services;

/// <summary>
/// 导出服务实现
/// </summary>
public class ExportService : IExportService
{
    private readonly ILogger<ExportService> _logger;

    public ExportService(ILogger<ExportService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        
        // 配置QuestPDF许可证（社区版）
        QuestPDF.Settings.License = LicenseType.Community;
    }

    /// <summary>
    /// 生成截图文件路径（实际截图在客户端完成）
    /// </summary>
    public Task<string> GenerateScreenshotPathAsync(string outputDirectory)
    {
        try
        {
            // 确保输出目录存在
            if (!Directory.Exists(outputDirectory))
            {
                Directory.CreateDirectory(outputDirectory);
                _logger.LogInformation("创建输出目录: {Directory}", outputDirectory);
            }

            // 生成带时间戳的文件名
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var filename = $"screenshot_{timestamp}.png";
            var filePath = Path.Combine(outputDirectory, filename);

            _logger.LogInformation("生成截图路径: {FilePath}", filePath);
            return Task.FromResult(filePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "生成截图路径失败");
            throw new InvalidOperationException("生成截图路径失败", ex);
        }
    }

    /// <summary>
    /// 生成PDF报告
    /// </summary>
    public async Task<string> GenerateReportAsync(
        BeamAnalysisResult result,
        ReportOptions options,
        string outputDirectory)
    {
        if (result == null)
            throw new ArgumentNullException(nameof(result));
        if (options == null)
            throw new ArgumentNullException(nameof(options));
        if (string.IsNullOrWhiteSpace(outputDirectory))
            throw new ArgumentException("输出目录不能为空", nameof(outputDirectory));

        try
        {
            // 确保输出目录存在
            if (!Directory.Exists(outputDirectory))
            {
                Directory.CreateDirectory(outputDirectory);
                _logger.LogInformation("创建输出目录: {Directory}", outputDirectory);
            }

            // 生成带时间戳的文件名
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var filename = $"report_{timestamp}.pdf";
            var filePath = Path.Combine(outputDirectory, filename);

            _logger.LogInformation("开始生成PDF报告: {FilePath}", filePath);

            // 生成PDF文档
            await Task.Run(() =>
            {
                Document.Create(container =>
                {
                    container.Page(page =>
                    {
                        page.Size(PageSizes.A4);
                        page.Margin(2, Unit.Centimetre);
                        page.PageColor(Colors.White);
                        page.DefaultTextStyle(x => x.FontSize(11).FontFamily("Microsoft YaHei"));

                        page.Header()
                            .Element(c => ComposeHeader(c, options, result));

                        page.Content()
                            .Element(c => ComposeContent(c, result, options));

                        page.Footer()
                            .AlignCenter()
                            .Text(x =>
                            {
                                x.Span("第 ");
                                x.CurrentPageNumber();
                                x.Span(" 页，共 ");
                                x.TotalPages();
                                x.Span(" 页");
                            });
                    });
                })
                .GeneratePdf(filePath);
            });

            _logger.LogInformation("PDF报告生成成功: {FilePath}", filePath);
            return filePath;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "生成PDF报告失败");
            throw new InvalidOperationException("生成PDF报告失败", ex);
        }
    }

    /// <summary>
    /// 组合报告头部
    /// </summary>
    private void ComposeHeader(IContainer container, ReportOptions options, BeamAnalysisResult result)
    {
        container.Column(column =>
        {
            column.Item().BorderBottom(1).PaddingBottom(10).Row(row =>
            {
                row.RelativeItem().Column(col =>
                {
                    col.Item().Text(options.Title)
                        .FontSize(20)
                        .Bold()
                        .FontColor(Colors.Blue.Darken2);

                    col.Item().PaddingTop(5).Text($"测量时间: {result.MeasurementTime:yyyy-MM-dd HH:mm:ss}")
                        .FontSize(10)
                        .FontColor(Colors.Grey.Darken1);
                });

                row.ConstantItem(100).AlignRight().Text($"设备: {options.DeviceInfo}")
                    .FontSize(9)
                    .FontColor(Colors.Grey.Darken1);
            });
        });
    }

    /// <summary>
    /// 组合报告内容
    /// </summary>
    private void ComposeContent(IContainer container, BeamAnalysisResult result, ReportOptions options)
    {
        container.Column(column =>
        {
            // 1. 测量参数表格
            column.Item().PaddingTop(10).Element(c => ComposeParametersTable(c, result));

            // 2. 拟合结果表格
            column.Item().PaddingTop(15).Element(c => ComposeFitResultsTable(c, result));

            // 3. 原始数据表格（可选）
            if (options.IncludeRawDataTable && result.RawData != null && result.RawData.Count > 0)
            {
                column.Item().PageBreak();
                column.Item().PaddingTop(10).Element(c => ComposeRawDataTable(c, result));
            }

            // 4. 备注
            if (!string.IsNullOrWhiteSpace(options.Notes))
            {
                column.Item().PaddingTop(15).Element(c => ComposeNotes(c, options));
            }

            // 5. 签名区
            column.Item().PaddingTop(20).Element(c => ComposeSignatureArea(c, options));
        });
    }

    /// <summary>
    /// 组合测量参数表格
    /// </summary>
    private void ComposeParametersTable(IContainer container, BeamAnalysisResult result)
    {
        container.Column(column =>
        {
            column.Item().Text("测量参数")
                .FontSize(14)
                .Bold()
                .FontColor(Colors.Blue.Darken1);

            column.Item().PaddingTop(5).Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.ConstantColumn(150);
                    columns.RelativeColumn(1);
                    columns.RelativeColumn(1);
                    columns.RelativeColumn(1);
                });

                // 表头
                table.Header(header =>
                {
                    header.Cell().Element(CellStyle).Text("参数名称").Bold();
                    header.Cell().Element(CellStyle).Text("全局值").Bold();
                    header.Cell().Element(CellStyle).Text("X方向").Bold();
                    header.Cell().Element(CellStyle).Text("Y方向").Bold();
                });

                // M² 因子
                table.Cell().Element(CellStyle).Text("M² 因子");
                table.Cell().Element(CellStyle).Text(FormatNumber(result.MSquaredGlobal));
                table.Cell().Element(CellStyle).Text(FormatNumber(result.MSquaredX));
                table.Cell().Element(CellStyle).Text(FormatNumber(result.MSquaredY));

                // 腰斑位置
                table.Cell().Element(CellStyle).Text("腰斑位置 (mm)");
                table.Cell().Element(CellStyle).Text("-");
                table.Cell().Element(CellStyle).Text(FormatNumber(result.BeamWaistPositionX));
                table.Cell().Element(CellStyle).Text(FormatNumber(result.BeamWaistPositionY));

                // 腰斑直径
                table.Cell().Element(CellStyle).Text("腰斑直径 (μm)");
                table.Cell().Element(CellStyle).Text("-");
                table.Cell().Element(CellStyle).Text(FormatNumber(result.BeamWaistDiameterX));
                table.Cell().Element(CellStyle).Text(FormatNumber(result.BeamWaistDiameterY));

                // 峰值位置
                table.Cell().Element(CellStyle).Text("峰值位置 (mm)");
                table.Cell().Element(CellStyle).Text("-");
                table.Cell().Element(CellStyle).Text(FormatNumber(result.PeakPositionX));
                table.Cell().Element(CellStyle).Text(FormatNumber(result.PeakPositionY));
            });
        });
    }

    /// <summary>
    /// 组合拟合结果表格
    /// </summary>
    private void ComposeFitResultsTable(IContainer container, BeamAnalysisResult result)
    {
        container.Column(column =>
        {
            column.Item().Text("拟合结果")
                .FontSize(14)
                .Bold()
                .FontColor(Colors.Blue.Darken1);

            column.Item().PaddingTop(5).Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.ConstantColumn(150);
                    columns.RelativeColumn(1);
                    columns.RelativeColumn(1);
                });

                // 表头
                table.Header(header =>
                {
                    header.Cell().Element(CellStyle).Text("拟合类型").Bold();
                    header.Cell().Element(CellStyle).Text("X方向 R²").Bold();
                    header.Cell().Element(CellStyle).Text("Y方向 R²").Bold();
                });

                // 高斯拟合
                table.Cell().Element(CellStyle).Text("高斯拟合");
                table.Cell().Element(CellStyle).Text(result.GaussianFitX != null ? FormatNumber(result.GaussianFitX.RSquared) : "N/A");
                table.Cell().Element(CellStyle).Text(result.GaussianFitY != null ? FormatNumber(result.GaussianFitY.RSquared) : "N/A");

                // 双曲线拟合
                table.Cell().Element(CellStyle).Text("双曲线拟合");
                table.Cell().Element(CellStyle).Text(result.HyperbolicFitX != null ? FormatNumber(result.HyperbolicFitX.RSquared) : "N/A");
                table.Cell().Element(CellStyle).Text(result.HyperbolicFitY != null ? FormatNumber(result.HyperbolicFitY.RSquared) : "N/A");
            });
        });
    }

    /// <summary>
    /// 组合原始数据表格
    /// </summary>
    private void ComposeRawDataTable(IContainer container, BeamAnalysisResult result)
    {
        container.Column(column =>
        {
            column.Item().Text("原始数据")
                .FontSize(14)
                .Bold()
                .FontColor(Colors.Blue.Darken1);

            column.Item().PaddingTop(5).Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(1);
                    columns.RelativeColumn(1);
                    columns.RelativeColumn(1);
                    columns.RelativeColumn(1);
                });

                // 表头
                table.Header(header =>
                {
                    header.Cell().Element(CellStyle).Text("序号").Bold();
                    header.Cell().Element(CellStyle).Text("探测器位置 (mm)").Bold();
                    header.Cell().Element(CellStyle).Text("光束直径X (μm)").Bold();
                    header.Cell().Element(CellStyle).Text("光束直径Y (μm)").Bold();
                });

                // 数据行（最多显示50行）
                var dataToShow = result.RawData.Take(50).ToList();
                for (int i = 0; i < dataToShow.Count; i++)
                {
                    var point = dataToShow[i];
                    table.Cell().Element(CellStyle).Text((i + 1).ToString());
                    table.Cell().Element(CellStyle).Text(FormatNumber(point.DetectorPosition));
                    table.Cell().Element(CellStyle).Text(FormatNumber(point.BeamDiameterX));
                    table.Cell().Element(CellStyle).Text(FormatNumber(point.BeamDiameterY));
                }

                if (result.RawData.Count > 50)
                {
                    table.Cell().ColumnSpan(4).Element(CellStyle)
                        .Text($"... 共 {result.RawData.Count} 个数据点，仅显示前50个")
                        .FontColor(Colors.Grey.Darken1)
                        .Italic();
                }
            });
        });
    }

    /// <summary>
    /// 组合备注区域
    /// </summary>
    private void ComposeNotes(IContainer container, ReportOptions options)
    {
        container.Column(column =>
        {
            column.Item().Text("备注")
                .FontSize(12)
                .Bold()
                .FontColor(Colors.Blue.Darken1);

            column.Item().PaddingTop(5)
                .Border(1)
                .BorderColor(Colors.Grey.Lighten2)
                .Padding(10)
                .Text(options.Notes)
                .FontSize(10);
        });
    }

    /// <summary>
    /// 组合签名区域
    /// </summary>
    private void ComposeSignatureArea(IContainer container, ReportOptions options)
    {
        container.Row(row =>
        {
            row.RelativeItem().Column(column =>
            {
                column.Item().Text("操作员:")
                    .FontSize(10);
                column.Item().PaddingTop(5).Text(options.OperatorName)
                    .FontSize(10)
                    .Bold();
            });

            row.RelativeItem().Column(column =>
            {
                column.Item().Text("审核:")
                    .FontSize(10);
                column.Item().PaddingTop(5).BorderBottom(1).Height(20);
            });

            row.RelativeItem().Column(column =>
            {
                column.Item().Text("日期:")
                    .FontSize(10);
                column.Item().PaddingTop(5).Text(DateTime.Now.ToString("yyyy-MM-dd"))
                    .FontSize(10);
            });
        });
    }

    /// <summary>
    /// 单元格样式
    /// </summary>
    private IContainer CellStyle(IContainer container)
    {
        return container
            .Border(1)
            .BorderColor(Colors.Grey.Lighten2)
            .Padding(5)
            .AlignCenter()
            .AlignMiddle();
    }

    /// <summary>
    /// 格式化数字（保留4位有效数字）
    /// </summary>
    private string FormatNumber(double value)
    {
        if (double.IsNaN(value) || double.IsInfinity(value))
            return "N/A";

        // 保留4位有效数字
        if (Math.Abs(value) < 0.0001)
            return value.ToString("E3");
        else if (Math.Abs(value) < 1)
            return value.ToString("F4");
        else if (Math.Abs(value) < 10)
            return value.ToString("F3");
        else if (Math.Abs(value) < 100)
            return value.ToString("F2");
        else if (Math.Abs(value) < 1000)
            return value.ToString("F1");
        else
            return value.ToString("F0");
    }
}
