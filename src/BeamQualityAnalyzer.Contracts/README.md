# BeamQualityAnalyzer.Contracts

光束质量分析系统共享契约库，包含前后端通信的 DTO 模型、消息类型和枚举定义。

## 安装

```bash
dotnet add package BeamQualityAnalyzer.Contracts
```

## 使用

此包包含以下内容：

### DTO 模型
- `BeamAnalysisResultDto`: 光束分析结果
- `MeasurementRecordDto`: 测量记录
- `CommandResult`: 命令执行结果
- `RawDataPointDto`: 原始数据点

### 消息类型
- `RawDataReceivedMessage`: 原始数据接收消息
- `CalculationCompletedMessage`: 计算完成消息
- `ErrorMessage`: 错误消息
- `ProgressMessage`: 进度消息
- `DeviceStatusMessage`: 设备状态消息

### 枚举
- `DatabaseType`: 数据库类型（SQLite/MySQL/SqlServer）
- `DeviceStatus`: 设备状态
- `AcquisitionStatus`: 采集状态

## 版本历史

### 1.0.0
- 初始版本
- 基础 DTO 模型和消息类型

## 许可证

MIT License
