@echo off
chcp 65001 >nul
echo 启动光束质量分析系统后端服务...
cd src\BeamQualityAnalyzer.Server
dotnet run
