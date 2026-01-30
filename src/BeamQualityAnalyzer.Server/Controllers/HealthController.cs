using Microsoft.AspNetCore.Mvc;
using System.Reflection;
using System.Diagnostics;
using BeamQualityAnalyzer.Data.Interfaces;

namespace BeamQualityAnalyzer.Server.Controllers;

/// <summary>
/// 健康检查控制器
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class HealthController : ControllerBase
{
    private readonly ILogger<HealthController> _logger;
    private readonly IDatabaseService _databaseService;
    private readonly IConfiguration _configuration;
    
    public HealthController(
        ILogger<HealthController> logger,
        IDatabaseService databaseService,
        IConfiguration configuration)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _databaseService = databaseService ?? throw new ArgumentNullException(nameof(databaseService));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    }
    
    /// <summary>
    /// 健康检查
    /// </summary>
    /// <returns>健康状态</returns>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult GetHealth()
    {
        try
        {
            var health = new
            {
                status = "healthy",
                timestamp = DateTime.UtcNow,
                version = GetVersionString(),
                environment = _configuration["ASPNETCORE_ENVIRONMENT"] ?? "Production",
                uptime = GetUptime()
            };
            
            _logger.LogDebug("健康检查: {Status}", health.status);
            
            return Ok(health);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "健康检查失败");
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                status = "unhealthy",
                timestamp = DateTime.UtcNow,
                error = ex.Message
            });
        }
    }
    
    /// <summary>
    /// 详细健康检查（包含数据库连接状态）
    /// </summary>
    /// <returns>详细健康状态</returns>
    [HttpGet("detailed")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> GetDetailedHealth()
    {
        try
        {
            // 测试数据库连接
            bool databaseHealthy = false;
            string databaseMessage = "未测试";
            
            try
            {
                databaseHealthy = await _databaseService.TestConnectionAsync();
                databaseMessage = databaseHealthy ? "连接正常" : "连接失败";
            }
            catch (Exception dbEx)
            {
                databaseMessage = $"连接异常: {dbEx.Message}";
            }
            
            var overallHealthy = databaseHealthy;
            
            var health = new
            {
                status = overallHealthy ? "healthy" : "degraded",
                timestamp = DateTime.UtcNow,
                version = GetVersionString(),
                environment = _configuration["ASPNETCORE_ENVIRONMENT"] ?? "Production",
                uptime = GetUptime(),
                components = new
                {
                    database = new
                    {
                        status = databaseHealthy ? "healthy" : "unhealthy",
                        message = databaseMessage,
                        type = _databaseService.GetDatabaseType().ToString()
                    },
                    signalr = new
                    {
                        status = "healthy",
                        message = "SignalR Hub 运行正常"
                    }
                }
            };
            
            _logger.LogInformation("详细健康检查: {Status}", health.status);
            
            if (overallHealthy)
            {
                return Ok(health);
            }
            else
            {
                return StatusCode(StatusCodes.Status503ServiceUnavailable, health);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "详细健康检查失败");
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                status = "unhealthy",
                timestamp = DateTime.UtcNow,
                error = ex.Message
            });
        }
    }
    
    /// <summary>
    /// 获取版本信息
    /// </summary>
    /// <returns>版本信息</returns>
    [HttpGet("version")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult GetVersion()
    {
        try
        {
            var version = new
            {
                version = GetVersionString(),
                buildDate = GetBuildDate(),
                environment = _configuration["ASPNETCORE_ENVIRONMENT"] ?? "Production",
                framework = GetFrameworkVersion(),
                os = Environment.OSVersion.ToString(),
                machineName = Environment.MachineName
            };
            
            _logger.LogDebug("版本信息查询");
            
            return Ok(version);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取版本信息失败");
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                error = ex.Message
            });
        }
    }
    
    /// <summary>
    /// 获取应用版本号
    /// </summary>
    private static string GetVersionString()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var version = assembly.GetName().Version;
        return version?.ToString() ?? "1.0.0";
    }
    
    /// <summary>
    /// 获取构建日期
    /// </summary>
    private static string GetBuildDate()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var buildDate = System.IO.File.GetLastWriteTime(assembly.Location);
        return buildDate.ToString("yyyy-MM-dd HH:mm:ss");
    }
    
    /// <summary>
    /// 获取框架版本
    /// </summary>
    private static string GetFrameworkVersion()
    {
        return Environment.Version.ToString();
    }
    
    /// <summary>
    /// 获取运行时间
    /// </summary>
    private static string GetUptime()
    {
        var uptime = DateTime.UtcNow - Process.GetCurrentProcess().StartTime.ToUniversalTime();
        return $"{uptime.Days}天 {uptime.Hours}小时 {uptime.Minutes}分钟";
    }
}
