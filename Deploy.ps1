# Deploy.ps1 - 部署Mod到游戏目录
# 使用方法: .\Deploy.ps1 [-ModName MyFirstMod]

param(
    [string]$ModName = "MyFirstMod"
)

$ErrorActionPreference = "Stop"C:\Users\cmz\AppData\LocalLow\AlFine\ChronoArk\MyFirstMod_RunLog.txt
$scriptPath = Split-Path -Parent $MyInvocation.MyCommand.Path
$modSourcePath = Join-Path $scriptPath $ModName

# 游戏的Mod目录
$gamePath = Split-Path -Parent $scriptPath
$gameModPath = Join-Path $gamePath "ChronoArk_Data\StreamingAssets\Mod"
$modDestPath = Join-Path $gameModPath $ModName

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  Chrono Ark Mod 部署脚本" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Mod名称: $ModName"
Write-Host ""

# 检查源目录
if (-not (Test-Path $modSourcePath)) {
    Write-Host "错误: 未找到Mod源目录 $modSourcePath" -ForegroundColor Red
    exit 1
}

# 检查是否已编译
$assembliesDir = Join-Path $modSourcePath "Assemblies"
$modDll = Join-Path $assembliesDir "$ModName.dll"
if (-not (Test-Path $modDll)) {
    Write-Host "错误: 未找到编译后的DLL，请先运行 .\Build.ps1" -ForegroundColor Red
    exit 1
}

# 创建目标目录
if (-not (Test-Path $gameModPath)) {
    New-Item -ItemType Directory -Path $gameModPath -Force | Out-Null
}

if (Test-Path $modDestPath) {
    Write-Host "清理旧的Mod文件..." -ForegroundColor Yellow
    Remove-Item $modDestPath -Recurse -Force
}

New-Item -ItemType Directory -Path $modDestPath -Force | Out-Null
$destAssembliesPath = Join-Path $modDestPath "Assemblies"
New-Item -ItemType Directory -Path $destAssembliesPath -Force | Out-Null

# 复制Assemblies
Write-Host "复制Assemblies..." -ForegroundColor Yellow
Get-ChildItem $assembliesDir -Filter "*.dll" | ForEach-Object {
    Copy-Item $_.FullName $destAssembliesPath -Force
    Write-Host "  已复制: $($_.Name)" -ForegroundColor Green
}
Get-ChildItem $assembliesDir -Filter "*.pdb" -ErrorAction SilentlyContinue | ForEach-Object {
    Copy-Item $_.FullName $destAssembliesPath -Force
    Write-Host "  已复制: $($_.Name)" -ForegroundColor Green
}

# 复制ChronoArkMod.json
$jsonFile = Join-Path $modSourcePath "ChronoArkMod.json"
if (Test-Path $jsonFile) {
    Copy-Item $jsonFile $modDestPath -Force
    Write-Host "已复制: ChronoArkMod.json" -ForegroundColor Green
}

# 复制Assets目录（如果存在）
$assetsDir = Join-Path $modSourcePath "Assets"
if (Test-Path $assetsDir) {
    $destAssetsPath = Join-Path $modDestPath "Assets"
    Copy-Item $assetsDir $destAssetsPath -Recurse -Force
    Write-Host "已复制: Assets目录" -ForegroundColor Green
}

# 复制gdata目录（如果存在）
$gdataDir = Join-Path $modSourcePath "gdata"
if (Test-Path $gdataDir) {
    $destGdataPath = Join-Path $modDestPath "gdata"
    Copy-Item $gdataDir $destGdataPath -Recurse -Force
    Write-Host "已复制: gdata目录" -ForegroundColor Green
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  部署完成!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "部署位置: $modDestPath"
Write-Host ""
Write-Host "现在可以启动游戏并在Mod菜单中启用 $ModName" -ForegroundColor Yellow
