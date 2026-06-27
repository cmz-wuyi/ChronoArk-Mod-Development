# Deep analysis of ModInfo, ModAssemblyInfo, ModManager classes
$ErrorActionPreference = "Stop"
$assemblyPath = "d:\Games\Chrono Ark\ChronoArk_Data\Managed\Assembly-CSharp.dll"
$cecilPath = "d:\Games\Chrono Ark\ChronoArk_Data\Managed\Mono.Cecil.dll"

Add-Type -Path $cecilPath
$assembly = [Mono.Cecil.AssemblyDefinition]::ReadAssembly($assemblyPath)
$module = $assembly.MainModule

# Target classes to analyze
$targetClasses = @(
    "ChronoArkMod.ModData.ModInfo",
    "ChronoArkMod.ModData.ModAssemblyInfo",
    "ChronoArkMod.ModManager",
    "ChronoArkMod.Plugin.ChronoArkPlugin",
    "ChronoArkMod.Plugin.PluginConfigAttribute",
    "ChronoArkMod.ModData.ModDataInfo"
)

foreach ($fullName in $targetClasses) {
    Write-Host "`n========================================"
    Write-Host "Analyzing: $fullName"
    Write-Host "========================================"
    
    $type = $module.Types | Where-Object { $_.FullName -eq $fullName } | Select-Object -First 1
    
    if ($type -eq $null) {
        Write-Host "  NOT FOUND"
        continue
    }
    
    Write-Host "BaseType: $($type.BaseType.FullName)"
    Write-Host "IsAbstract: $($type.IsAbstract)"
    Write-Host "IsInterface: $($type.IsInterface)"
    
    Write-Host "`n--- Fields ---"
    foreach ($f in $type.Fields) {
        $access = ""
        if ($f.IsPublic) { $access += "public " }
        if ($f.IsPrivate) { $access += "private " }
        if ($f.IsStatic) { $access += "static " }
        Write-Host "  $access$($f.Name) : $($f.FieldType.FullName)"
    }
    
    Write-Host "`n--- Properties ---"
    foreach ($p in $type.Properties) {
        Write-Host "  $($p.Name) : $($p.PropertyType.FullName)"
        if ($p.GetMethod -ne $null) {
            $body = $p.GetMethod.Body
            if ($body -ne $null) {
                $instr = $body.Instructions
                if ($instr.Count -le 8) {
                    Write-Host "    Getter IL:"
                    foreach ($i in $instr) {
                        Write-Host "      $($i.OpCode.Name) $($i.Operand)"
                    }
                }
            }
        }
    }
    
    Write-Host "`n--- Constructors ---"
    foreach ($c in $type.Methods | Where-Object { $_.IsConstructor }) {
        $params = ($c.Parameters | ForEach-Object { "$($_.ParameterType.Name) $($_.Name)" }) -join ", "
        Write-Host "  .ctor($params)"
    }
    
    Write-Host "`n--- Custom Attributes ---"
    foreach ($attr in $type.CustomAttributes) {
        Write-Host "  [$($attr.AttributeType.FullName)]"
        if ($attr.ConstructorArguments.Count -gt 0) {
            $args = ($attr.ConstructorArguments | ForEach-Object { "$($_.Value)" }) -join ", "
            Write-Host "    Args: $args"
        }
    }
    
    Write-Host "`n--- Methods (non-trivial) ---"
    foreach ($m in $type.Methods | Where-Object { -not $_.IsConstructor -and -not $_.IsAbstract }) {
        $params = ($m.Parameters | ForEach-Object { "$($_.ParameterType.Name) $($_.Name)" }) -join ", "
        Write-Host "  $($m.Name)($params)"
    }
}

$assembly.Dispose()
Write-Host "`nDone."
