﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿# Build.ps1 - 编译Mod项目
# 使用方法: .\Build.ps1 [-Configuration Release|Debug]

param(
    [string]$Configuration = "Release",
    [string]$ModName = "MyFirstMod"
)

$ErrorActionPreference = "Stop"
$scriptPath = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectPath = Join-Path $scriptPath $ModName
$projectFile = Join-Path $projectPath "$ModName.csproj"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  Chrono Ark Mod 构建脚本" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Mod名称: $ModName"
Write-Host "配置: $Configuration"
Write-Host ""

# 查找MSBuild
$msbuildPaths = @(
    "C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe",
    "C:\Program Files\Microsoft Visual Studio\2022\Professional\MSBuild\Current\Bin\MSBuild.exe",
    "C:\Program Files\Microsoft Visual Studio\2022\Enterprise\MSBuild\Current\Bin\MSBuild.exe",
    "C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\MSBuild.exe",
    "C:\Program Files (x86)\Microsoft Visual Studio\2019\Community\MSBuild\Current\Bin\MSBuild.exe",
    "C:\Program Files (x86)\Microsoft Visual Studio\2019\BuildTools\MSBuild\Current\Bin\MSBuild.exe",
    "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\MSBuild.exe",
    "C:\Windows\Microsoft.NET\Framework\v4.0.30319\MSBuild.exe"
)

$msbuild = $null
foreach ($path in $msbuildPaths) {
    if (Test-Path $path) {
        $msbuild = $path
        break
    }
}

if (-not $msbuild) {
    Write-Host "错误: 未找到MSBuild.exe" -ForegroundColor Red
    Write-Host "请安装Visual Studio或.NET Framework SDK" -ForegroundColor Red
    exit 1
}

Write-Host "使用MSBuild: $msbuild" -ForegroundColor Green
Write-Host ""

# 检查项目文件
if (-not (Test-Path $projectFile)) {
    Write-Host "错误: 未找到项目文件 $projectFile" -ForegroundColor Red
    exit 1
}

# 编译项目
Write-Host "正在编译 $ModName ..." -ForegroundColor Yellow
& $msbuild $projectFile /p:Configuration=$Configuration /t:Rebuild /v:minimal

if ($LASTEXITCODE -ne 0) {
    Write-Host ""
    Write-Host "编译失败!" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "编译成功!" -ForegroundColor Green

# 复制输出文件
$outputDir = Join-Path $projectPath "bin\$Configuration"
$assembliesDir = Join-Path $projectPath "Assemblies"

# 清空Assemblies目录
if (Test-Path $assembliesDir) {
    Get-ChildItem $assembliesDir -Filter "*.dll" | Remove-Item -Force
    Get-ChildItem $assembliesDir -Filter "*.pdb" | Remove-Item -Force
}

# 复制mod的DLL和0Harmony.dll
$dllsToCopy = @("$ModName.dll", "0Harmony.dll")
foreach ($dll in $dllsToCopy) {
    $src = Join-Path $outputDir $dll
    if (Test-Path $src) {
        Copy-Item $src $assembliesDir -Force
        Write-Host "已复制: $dll" -ForegroundColor Green
    }
}

# 复制pdb文件（用于调试）
if ($Configuration -eq "Debug") {
    $pdbFile = Join-Path $outputDir "$ModName.pdb"
    if (Test-Path $pdbFile) {
        Copy-Item $pdbFile $assembliesDir -Force
        Write-Host "已复制: $ModName.pdb (调试符号)" -ForegroundColor Green
    }
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  构建完成!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "输出目录: $assembliesDir"
Write-Host ""
Write-Host "下一步: 运行 .\Deploy.ps1 部署到游戏" -ForegroundColor Yellow
