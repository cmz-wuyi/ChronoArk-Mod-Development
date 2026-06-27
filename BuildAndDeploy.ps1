﻿# BuildAndDeploy.ps1 - 一键构建并部署
# 使用方法: .\BuildAndDeploy.ps1

param(
    [string]$Configuration = "Release",
    [string]$ModName = "MyFirstMod"
)

$ErrorActionPreference = "Stop"
$scriptPath = Split-Path -Parent $MyInvocation.MyCommand.Path

Write-Host "一键构建并部署 $ModName ..." -ForegroundColor Cyan
Write-Host ""

# 构建并部署
& (Join-Path $scriptPath "Build.ps1") -Configuration $Configuration -ModName $ModName
if ($LASTEXITCODE -ne 0) { exit 1 }

Write-Host ""
& (Join-Path $scriptPath "Deploy.ps1") -ModName $ModName
if ($LASTEXITCODE -ne 0) { exit 1 }

Write-Host ""
Write-Host "全部完成! 可以启动游戏测试Mod了。" -ForegroundColor Green
