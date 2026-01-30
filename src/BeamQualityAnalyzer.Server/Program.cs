using Serilog;
using BeamQualityAnalyzer.Core.Interfaces;
using BeamQualityAnalyzer.Core.Services;
using BeamQualityAnalyzer.Data.Interfaces;
using BeamQualityAnalyzer.Data.Services;
using BeamQualityAnalyzer.Data.DbContext;
using BeamQualityAnalyzer.Data.Enums;
using BeamQualityAnalyzer.Data.Factories;
using BeamQualityAnalyzer.Server.Hubs;
using BeamQualityAnalyzer.Server.Services;
using Microsoft.EntityFrameworkCore;

// 配置 Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(new ConfigurationBuilder()
        .AddJsonFile("appsettings.json")
        .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"}.json", optional: true)
        .Build())
    .CreateLogger();

try
{
    Log.Information("启动光束质量分析系统后端服务");

    var builder = WebApplication.CreateBuilder(args);

    // 使用 Serilog 作为日志提供程序
    builder.Host.UseSerilog();

    // 配置 CORS
    builder.Services.AddCors(options =>
    {
        options.AddDefaultPolicy(policy =>
        {
            policy.SetIsOriginAllowed(_ => true) // 开发环境允许所有来源
                  .AllowAnyMethod()
                  .AllowAnyHeader()
                  .AllowCredentials();
        });
    });

    // 添加控制器（测试环境跳过以避免 OpenAPI 冲突）
    if (!builder.Environment.IsEnvironment("Testing"))
    {
        builder.Services.AddControllers();
    }

    // 配置 SignalR
    builder.Services.AddSignalR();

    // 配置 Swagger/OpenAPI（仅用于健康检查端点，测试环境禁用）
    if (!builder.Environment.IsEnvironment("Testing"))
    {
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();
    }

    // 配置依赖注入容器
    
    // 注册数据库服务
    var databaseType = builder.Configuration.GetValue<DatabaseType>("Database:DatabaseType");
    var connectionString = builder.Configuration.GetValue<string>("Database:ConnectionString");
    
    builder.Services.AddSingleton<IDatabaseService>(sp =>
    {
        var loggerFactory = sp.GetRequiredService<ILoggerFactory>();
        
        // 根据数据库类型创建对应的 Logger
        Microsoft.Extensions.Logging.ILogger logger = databaseType switch
        {
            DatabaseType.SQLite => loggerFactory.CreateLogger<SqliteDatabaseService>(),
            DatabaseType.MySQL => loggerFactory.CreateLogger<MySqlDatabaseService>(),
            DatabaseType.SqlServer => loggerFactory.CreateLogger<SqlServerDatabaseService>(),
            _ => loggerFactory.CreateLogger<IDatabaseService>()
        };
        
        return DatabaseServiceFactory.Create(databaseType, connectionString!, logger);
    });
    
    // 注册核心服务
    builder.Services.AddSingleton<IDataAcquisitionService, VirtualBeamProfilerService>();
    builder.Services.AddSingleton<IAlgorithmService, BeamQualityAlgorithmService>();
    builder.Services.AddSingleton<IExportService, ExportService>();
    builder.Services.AddSingleton<IAutoTestService, AutoTestService>();
    
    // 注册 Hub 事件桥接服务（作为 Hosted Service）
    builder.Services.AddHostedService<HubEventBridge>();
    
    Log.Information("服务注册完成");

    var app = builder.Build();

    // 初始化数据库
    try
    {
        var databaseService = app.Services.GetRequiredService<IDatabaseService>();
        await databaseService.InitializeDatabaseAsync();
        Log.Information("数据库初始化成功");
    }
    catch (Exception ex)
    {
        Log.Warning(ex, "数据库初始化失败，将在首次使用时重试");
    }

    // 配置 HTTP 请求管道
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "光束质量分析系统 API v1");
            c.RoutePrefix = "swagger";
        });
    }

    // 使用 Serilog 请求日志
    app.UseSerilogRequestLogging();

    // 配置静态文件服务（用于文件下载）
    var screenshotDirectory = builder.Configuration["Export:ScreenshotDirectory"] ?? "screenshots";
    var reportDirectory = builder.Configuration["Export:ReportDirectory"] ?? "reports";
    
    // 确保目录存在
    if (!Directory.Exists(screenshotDirectory))
    {
        Directory.CreateDirectory(screenshotDirectory);
    }
    if (!Directory.Exists(reportDirectory))
    {
        Directory.CreateDirectory(reportDirectory);
    }
    
    // 使用 CORS
    app.UseCors();
    
    // 启用 WebSocket（SignalR 需要）
    app.UseWebSockets();

    app.UseRouting();

    // 映射控制器（测试环境跳过）
    if (!app.Environment.IsEnvironment("Testing"))
    {
        app.MapControllers();
    }

    // 映射 SignalR Hub
    app.MapHub<BeamAnalyzerHub>("/beamAnalyzerHub");

    // 健康检查端点（测试环境跳过以避免 OpenAPI 冲突）
    if (!app.Environment.IsEnvironment("Testing"))
    {
        app.MapGet("/api/health", () => Results.Ok(new
        {
            status = "healthy",
            timestamp = DateTime.UtcNow,
            version = "1.0.0"
        }))
        .WithName("HealthCheck")
        .WithTags("Health");

        // 版本信息端点
        app.MapGet("/api/version", () => Results.Ok(new
        {
            version = "1.0.0",
            buildDate = DateTime.UtcNow.ToString("yyyy-MM-dd"),
            environment = app.Environment.EnvironmentName
        }))
        .WithName("GetVersion")
        .WithTags("Health");
    }

    var urls = builder.Configuration["Urls"] ?? "http://localhost:5000";
    Log.Information("后端服务启动成功，监听地址: {Urls}", urls);

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "后端服务启动失败");
    throw;
}
finally
{
    Log.CloseAndFlush();
}

// 使 Program 类可被测试访问
public partial class Program { }
