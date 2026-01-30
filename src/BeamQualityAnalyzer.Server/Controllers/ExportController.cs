using Microsoft.AspNetCore.Mvc;

namespace BeamQualityAnalyzer.Server.Controllers;

/// <summary>
/// 导出控制器 - 提供文件下载端点
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class ExportController : ControllerBase
{
    private readonly ILogger<ExportController> _logger;
    private readonly IConfiguration _configuration;
    
    public ExportController(
        ILogger<ExportController> logger,
        IConfiguration configuration)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    }
    
    /// <summary>
    /// 下载文件
    /// </summary>
    /// <param name="filename">文件名</param>
    /// <returns>文件内容</returns>
    [HttpGet("download/{filename}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> DownloadFile(string filename)
    {
        try
        {
            // 验证文件名，防止路径遍历攻击
            if (string.IsNullOrWhiteSpace(filename) || 
                filename.Contains("..") || 
                filename.Contains("/") || 
                filename.Contains("\\"))
            {
                _logger.LogWarning("非法文件名请求: {Filename}", filename);
                return BadRequest(new { error = "非法文件名" });
            }
            
            // 确定文件类型和目录
            string directory;
            string contentType;
            
            if (filename.EndsWith(".png", StringComparison.OrdinalIgnoreCase) || 
                filename.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) ||
                filename.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase))
            {
                directory = _configuration["Export:ScreenshotDirectory"] ?? "screenshots";
                contentType = "image/png";
            }
            else if (filename.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
            {
                directory = _configuration["Export:ReportDirectory"] ?? "reports";
                contentType = "application/pdf";
            }
            else
            {
                _logger.LogWarning("不支持的文件类型: {Filename}", filename);
                return BadRequest(new { error = "不支持的文件类型" });
            }
            
            // 构建完整文件路径
            var filePath = Path.Combine(directory, filename);
            
            // 检查文件是否存在
            if (!System.IO.File.Exists(filePath))
            {
                _logger.LogWarning("文件不存在: {FilePath}", filePath);
                return NotFound(new { error = "文件不存在" });
            }
            
            // 读取文件内容
            var fileBytes = await System.IO.File.ReadAllBytesAsync(filePath);
            
            _logger.LogInformation("文件下载成功: {Filename}, 大小: {Size} bytes", filename, fileBytes.Length);
            
            // 返回文件
            return File(fileBytes, contentType, filename);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "下载文件失败: {Filename}", filename);
            return StatusCode(StatusCodes.Status500InternalServerError, 
                new { error = "下载文件失败", details = ex.Message });
        }
    }
    
    /// <summary>
    /// 下载截图
    /// </summary>
    /// <param name="id">截图ID</param>
    /// <returns>截图文件</returns>
    [HttpGet("screenshot/{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DownloadScreenshot(string id)
    {
        try
        {
            var directory = _configuration["Export:ScreenshotDirectory"] ?? "screenshots";
            var filename = $"screenshot_{id}.png";
            var filePath = Path.Combine(directory, filename);
            
            if (!System.IO.File.Exists(filePath))
            {
                _logger.LogWarning("截图不存在: {Id}", id);
                return NotFound(new { error = "截图不存在" });
            }
            
            var fileBytes = await System.IO.File.ReadAllBytesAsync(filePath);
            
            _logger.LogInformation("截图下载成功: {Id}", id);
            
            return File(fileBytes, "image/png", filename);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "下载截图失败: {Id}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, 
                new { error = "下载截图失败", details = ex.Message });
        }
    }
    
    /// <summary>
    /// 下载报告
    /// </summary>
    /// <param name="id">报告ID</param>
    /// <returns>报告文件</returns>
    [HttpGet("report/{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DownloadReport(string id)
    {
        try
        {
            var directory = _configuration["Export:ReportDirectory"] ?? "reports";
            var filename = $"report_{id}.pdf";
            var filePath = Path.Combine(directory, filename);
            
            if (!System.IO.File.Exists(filePath))
            {
                _logger.LogWarning("报告不存在: {Id}", id);
                return NotFound(new { error = "报告不存在" });
            }
            
            var fileBytes = await System.IO.File.ReadAllBytesAsync(filePath);
            
            _logger.LogInformation("报告下载成功: {Id}", id);
            
            return File(fileBytes, "application/pdf", filename);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "下载报告失败: {Id}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, 
                new { error = "下载报告失败", details = ex.Message });
        }
    }
}
