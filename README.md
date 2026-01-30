# 光束质量分析系统 - 后端服务

## 概述

光束质量分析系统后端服务，基于 ASP.NET Core 9.0 + SignalR 构建，提供实时数据采集、算法计算和数据管理功能。

## 项目结构

```
BeamQualityAnalyzer.Backend/
├─ src/
│   ├─ BeamQualityAnalyzer.Server/          # ASP.NET Core Web API + SignalR
│   ├─ BeamQualityAnalyzer.Core/            # 核心业务逻辑
│   ├─ BeamQualityAnalyzer.Data/            # 数据访问层
│   └─ BeamQualityAnalyzer.Contracts/       # 共享契约（发布为 NuGet 包）
├─ tests/
│   ├─ BeamQualityAnalyzer.UnitTests/       # 单元测试
│   └─ BeamQualityAnalyzer.PropertyTests/   # 属性测试
├─ docs/                                    # 文档
├─ scripts/                                 # 部署脚本
├─ Dockerfile                               # Docker 镜像构建文件
└─ BeamQualityAnalyzer.Backend.sln          # 解决方案文件
```

## 技术栈

- **.NET 9.0**: 运行时框架
- **ASP.NET Core**: Web API 框架
- **SignalR**: 实时双向通信
- **Entity Framework Core 9.0**: ORM 框架
- **SQLite/MySQL/SQL Server**: 数据库支持
- **Serilog**: 日志记录
- **Swagger/OpenAPI**: API 文档

## 快速开始

### 前置要求

- .NET 9.0 SDK
- Visual Studio 2022 或 VS Code

### 本地开发

1. 克隆仓库：
```bash
git clone https://github.com/yourorg/BeamQualityAnalyzer.Backend.git
cd BeamQualityAnalyzer.Backend
```

2. 还原依赖：
```bash
dotnet restore
```

3. 运行服务：
```bash
dotnet run --project src/BeamQualityAnalyzer.Server
```

4. 访问 Swagger 文档：
```
http://localhost:5000/swagger
```

### Docker 部署

1. 构建镜像：
```bash
docker build -t beam-analyzer-server:1.0.0 .
```

2. 运行容器：
```bash
docker run -d -p 5000:5000 --name beam-analyzer beam-analyzer-server:1.0.0
```

## 配置说明

### appsettings.json

```json
{
  "Urls": "http://0.0.0.0:5000",
  "AllowedOrigins": ["http://localhost:*"],
  "Database": {
    "DatabaseType": "SQLite",
    "ConnectionString": "Data Source=beam_analyzer.db"
  },
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information"
    }
  }
}
```

### 环境变量

- `ASPNETCORE_ENVIRONMENT`: 环境名称（Development/Production）
- `ASPNETCORE_URLS`: 监听地址

## API 端点

### 健康检查

- `GET /api/health`: 健康检查
- `GET /api/version`: 版本信息

### SignalR Hub

- `/beamAnalyzerHub`: 主要通信 Hub（待实现）

## 开发指南

### 添加新服务

1. 在 `BeamQualityAnalyzer.Core` 中定义接口
2. 实现服务类
3. 在 `Program.cs` 中注册服务

### 数据库迁移

```bash
dotnet ef migrations add InitialCreate --project src/BeamQualityAnalyzer.Data
dotnet ef database update --project src/BeamQualityAnalyzer.Server
```

## 测试

运行所有测试：
```bash
dotnet test
```

运行单元测试：
```bash
dotnet test tests/BeamQualityAnalyzer.UnitTests
```

运行属性测试：
```bash
dotnet test tests/BeamQualityAnalyzer.PropertyTests
```

## 日志

日志文件位置：`logs/beam-analyzer-{Date}.log`

## 许可证

MIT License

## 联系方式

- 项目主页: https://github.com/yourorg/BeamQualityAnalyzer.Backend
- 问题反馈: https://github.com/yourorg/BeamQualityAnalyzer.Backend/issues
