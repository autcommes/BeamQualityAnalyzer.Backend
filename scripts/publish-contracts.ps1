# 发布 Contracts NuGet 包脚本

param(
    [string]$Version = "1.0.0",
    [string]$Source = "local",  # local, nuget, or custom URL
    [string]$ApiKey = ""
)

$ErrorActionPreference = "Stop"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "发布 BeamQualityAnalyzer.Contracts 包" -ForegroundColor Cyan
Write-Host "版本: $Version" -ForegroundColor Cyan
Write-Host "目标源: $Source" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan

# 切换到 Contracts 项目目录
$ProjectPath = Join-Path $PSScriptRoot "..\src\BeamQualityAnalyzer.Contracts"
Set-Location $ProjectPath

Write-Host "`n清理旧的构建输出..." -ForegroundColor Yellow
if (Test-Path "bin") {
    Remove-Item -Path "bin" -Recurse -Force
}
if (Test-Path "obj") {
    Remove-Item -Path "obj" -Recurse -Force
}

Write-Host "`n构建项目..." -ForegroundColor Yellow
dotnet build -c Release

Write-Host "`n打包 NuGet 包..." -ForegroundColor Yellow
dotnet pack -c Release -p:PackageVersion=$Version -o .\bin\Release\packages

$PackageFile = Get-ChildItem -Path ".\bin\Release\packages" -Filter "BeamQualityAnalyzer.Contracts.$Version.nupkg" | Select-Object -First 1

if (-not $PackageFile) {
    Write-Host "`n错误: 未找到生成的 NuGet 包" -ForegroundColor Red
    exit 1
}

Write-Host "`n成功生成包: $($PackageFile.FullName)" -ForegroundColor Green

# 发布到指定源
if ($Source -eq "local") {
    # 发布到本地 NuGet 源
    $LocalSource = Join-Path $env:USERPROFILE ".nuget\local-packages"
    
    if (-not (Test-Path $LocalSource)) {
        Write-Host "`n创建本地 NuGet 源目录: $LocalSource" -ForegroundColor Yellow
        New-Item -Path $LocalSource -ItemType Directory -Force | Out-Null
    }
    
    Write-Host "`n复制包到本地源: $LocalSource" -ForegroundColor Yellow
    Copy-Item -Path $PackageFile.FullName -Destination $LocalSource -Force
    
    Write-Host "`n成功发布到本地源!" -ForegroundColor Green
    Write-Host "本地源路径: $LocalSource" -ForegroundColor Cyan
    Write-Host "`n在前端项目中使用以下命令引用:" -ForegroundColor Cyan
    Write-Host "dotnet add package BeamQualityAnalyzer.Contracts --version $Version --source $LocalSource" -ForegroundColor White
}
elseif ($Source -eq "nuget") {
    # 发布到 NuGet.org
    if ([string]::IsNullOrEmpty($ApiKey)) {
        Write-Host "`n错误: 发布到 NuGet.org 需要提供 API Key" -ForegroundColor Red
        Write-Host "使用方式: .\publish-contracts.ps1 -Version $Version -Source nuget -ApiKey YOUR_API_KEY" -ForegroundColor Yellow
        exit 1
    }
    
    Write-Host "`n发布到 NuGet.org..." -ForegroundColor Yellow
    dotnet nuget push $PackageFile.FullName --api-key $ApiKey --source https://api.nuget.org/v3/index.json
    
    Write-Host "`n成功发布到 NuGet.org!" -ForegroundColor Green
}
else {
    # 发布到自定义源
    if ([string]::IsNullOrEmpty($ApiKey)) {
        Write-Host "`n警告: 未提供 API Key，尝试匿名发布" -ForegroundColor Yellow
        dotnet nuget push $PackageFile.FullName --source $Source
    }
    else {
        Write-Host "`n发布到自定义源: $Source" -ForegroundColor Yellow
        dotnet nuget push $PackageFile.FullName --api-key $ApiKey --source $Source
    }
    
    Write-Host "`n成功发布到自定义源!" -ForegroundColor Green
}

Write-Host "`n========================================" -ForegroundColor Cyan
Write-Host "发布完成!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Cyan
