@echo off
chcp 65001 >nul
echo 正在清理后端构建产物...

echo 清理 bin 目录...
for /d /r . %%d in (bin) do @if exist "%%d" rd /s /q "%%d"

echo 清理 obj 目录...
for /d /r . %%d in (obj) do @if exist "%%d" rd /s /q "%%d"

echo 清理日志文件...
if exist "src\BeamQualityAnalyzer.Server\logs" rd /s /q "src\BeamQualityAnalyzer.Server\logs"

echo 清理数据库文件...
if exist "src\BeamQualityAnalyzer.Server\dev_beam_analyzer.db" del /f /q "src\BeamQualityAnalyzer.Server\dev_beam_analyzer.db"
if exist "src\BeamQualityAnalyzer.Server\dev_beam_analyzer.db-shm" del /f /q "src\BeamQualityAnalyzer.Server\dev_beam_analyzer.db-shm"
if exist "src\BeamQualityAnalyzer.Server\dev_beam_analyzer.db-wal" del /f /q "src\BeamQualityAnalyzer.Server\dev_beam_analyzer.db-wal"

echo 清理报告和截图...
if exist "src\BeamQualityAnalyzer.Server\reports" rd /s /q "src\BeamQualityAnalyzer.Server\reports"
if exist "src\BeamQualityAnalyzer.Server\screenshots" rd /s /q "src\BeamQualityAnalyzer.Server\screenshots"

echo 清理 NuGet 包缓存...
for /d /r . %%d in (packages) do @if exist "%%d" rd /s /q "%%d"

echo 后端清理完成！
pause
