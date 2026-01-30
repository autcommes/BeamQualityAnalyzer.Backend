# 光束质量分析系统 - 后端服务

## 概述

光束质量分析系统后端服务，基于 ASP.NET Core 8.0 + SignalR 构建，提供实时数据采集、光束质量算法计算和数据管理功能。

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

- **.NET 8.0**: 运行时框架
- **ASP.NET Core 8.0**: Web API 框架
- **SignalR**: 实时双向通信
- **Entity Framework Core 8.0**: ORM 框架
- **SQLite/MySQL/SQL Server**: 数据库支持（带故障转移）
- **Serilog**: 结构化日志记录
- **Swagger/OpenAPI**: API 文档
- **FsCheck**: 属性测试框架

## 快速开始

### 前置要求

- .NET 8.0 SDK
- Visual Studio 2022 或 VS Code
- （可选）MySQL 或 SQL Server 数据库

### 本地开发

1. 克隆仓库：
```bash
git clone https://github.com/autcommes/BeamQualityAnalyzer.Backend.git
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

- `/beamAnalyzerHub`: 主要通信 Hub
  - `StartDataAcquisitionAsync()`: 开始数据采集
  - `StopDataAcquisitionAsync()`: 停止数据采集
  - `StartAutoTestAsync()`: 开始自动测试
  - `StopAutoTestAsync()`: 停止自动测试
  - `UpdateAnalysisParametersAsync()`: 更新分析参数
  - `QueryMeasurementsAsync()`: 查询历史测量记录
  - `ExportReportAsync()`: 导出报告
  - `SubscribeToDataStreamAsync()`: 订阅实时数据流

### Hub 事件（服务器推送）

- `OnRawDataReceived`: 原始数据接收事件
- `OnCalculationCompleted`: 计算完成事件
- `OnVisualizationDataReady`: 可视化数据就绪事件
- `OnAcquisitionStatusChanged`: 采集状态变化事件
- `OnAutoTestStatusChanged`: 自动测试状态变化事件
- `OnDeviceStatusChanged`: 设备状态变化事件

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

## 核心功能

### 数据采集
- 虚拟光束轮廓仪模拟（用于开发测试）
- 实时数据流推送
- 支持连续采集和单次采集

### 算法计算
- 高斯拟合算法
- 双曲线拟合算法
- 光束质量因子 M² 计算
- 束腰位置和半径计算

### 数据管理
- 多数据库支持（SQLite/MySQL/SQL Server）
- 数据库故障自动转移
- 历史数据查询和导出
- JSON 文件备份

### 自动测试
- 可配置测试序列
- 自动数据采集和分析
- 测试报告生成

## 联系方式

- 项目主页: https://github.com/autcommes/BeamQualityAnalyzer.Backend
- 问题反馈: https://github.com/autcommes/BeamQualityAnalyzer.Backend/issues
