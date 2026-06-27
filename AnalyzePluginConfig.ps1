# Analyze Assembly-CSharp-firstpass.dll and ModAssemblyInfo.init()
$ErrorActionPreference = "Stop"
$firstpassPath = "d:\Games\Chrono Ark\ChronoArk_Data\Managed\Assembly-CSharp-firstpass.dll"
$cecilPath = "d:\Games\Chrono Ark\ChronoArk_Data\Managed\Mono.Cecil.dll"

Add-Type -Path $cecilPath

Write-Host "=== Step 1: Search PluginConfigAttribute in Assembly-CSharp-firstpass.dll ==="
$assembly2 = [Mono.Cecil.AssemblyDefinition]::ReadAssembly($firstpassPath)
$module2 = $assembly2.MainModule

foreach ($t in $module2.Types) {
    if ($t.Name -match "PluginConfig" -or $t.Name -match "PluginAttr" -or $t.Name -match "ModPlugin") {
        Write-Host "Found: $($t.FullName)"
        foreach ($f in $t.Fields) { Write-Host "  Field: $($f.Name) : $($f.FieldType.Name)" }
        foreach ($p in $t.Properties) { Write-Host "  Prop: $($p.Name) : $($p.PropertyType.Name)" }
        foreach ($c in $t.Methods | Where-Object { $_.IsConstructor }) {
            $params = ($c.Parameters | ForEach-Object { "$($_.ParameterType.Name) $($_.Name)" }) -join ", "
            Write-Host "  .ctor($params)"
        }
    }
}

Write-Host "`n=== Step 2: List all Plugin-related types in firstpass ==="
foreach ($t in $module2.Types) {
    if ($t.Namespace -match "Plugin" -or $t.Name -match "Plugin") {
        Write-Host "Type: $($t.FullName) (base: $($t.BaseType.FullName))"
    }
}

$assembly2.Dispose()

Write-Host "`n=== Step 3: Search PluginConfigAttribute in Assembly-CSharp.dll ==="
$assemblyPath = "d:\Games\Chrono Ark\ChronoArk_Data\Managed\Assembly-CSharp.dll"
$assembly = [Mono.Cecil.AssemblyDefinition]::ReadAssembly($assemblyPath)
$module = $assembly.MainModule

foreach ($t in $module.Types) {
    if ($t.Name -match "PluginConfig" -or $t.Name -match "PluginAttr") {
        Write-Host "Found in Assembly-CSharp.dll: $($t.FullName)"
    }
}

# Also check nested types
foreach ($t in $module.Types) {
    foreach ($nt in $t.NestedTypes) {
        if ($nt.Name -match "PluginConfig" -or $nt.Name -match "PluginAttr" -or $nt.Name -match "Plugin") {
            Write-Host "Nested type: $($nt.FullName) in $($t.FullName)"
        }
    }
}

Write-Host "`n=== Step 4: Analyze ModAssemblyInfo.init() IL ==="
$modAssemblyType = $module.Types | Where-Object { $_.FullName -eq "ChronoArkMod.ModData.ModAssemblyInfo" } | Select-Object -First 1
if ($modAssemblyType -ne $null) {
    $initMethod = $modAssemblyType.Methods | Where-Object { $_.Name -eq "init" } | Select-Object -First 1
    if ($initMethod -ne $null) {
        Write-Host "Method: init()"
        Write-Host "Body size: $($initMethod.Body.CodeSize) bytes"
        Write-Host "Local vars:"
        foreach ($v in $initMethod.Body.Variables) {
            Write-Host "  [$($v.Index)] $($v.VariableType.FullName)"
        }
        Write-Host "`nIL Instructions:"
        foreach ($i in $initMethod.Body.Instructions) {
            $operandStr = ""
            if ($i.Operand -ne $null) {
                $operandStr = $i.Operand.ToString()
                if ($operandStr.Length -gt 120) { $operandStr = $operandStr.Substring(0, 120) + "..." }
            }
            Write-Host "  IL_$('{0:X4}' -f $i.Offset): $($i.OpCode.Name) $operandStr"
        }
    } else {
        Write-Host "init() method NOT FOUND"
    }
}

Write-Host "`n=== Step 5: Analyze ModManager.LoadMod() IL ==="
$modManagerType = $module.Types | Where-Object { $_.FullName -eq "ChronoArkMod.ModManager" } | Select-Object -First 1
if ($modManagerType -ne $null) {
    $loadMethod = $modManagerType.Methods | Where-Object { $_.Name -eq "LoadMod" } | Select-Object -First 1
    if ($loadMethod -ne $null) {
        Write-Host "Method: LoadMod(ModInfo modInfo)"
        Write-Host "Body size: $($loadMethod.Body.CodeSize) bytes"
        Write-Host "`nIL Instructions (first 80):"
        $count = 0
        foreach ($i in $loadMethod.Body.Instructions) {
            if ($count -ge 80) { break }
            $operandStr = ""
            if ($i.Operand -ne $null) {
                $operandStr = $i.Operand.ToString()
                if ($operandStr.Length -gt 120) { $operandStr = $operandStr.Substring(0, 120) + "..." }
            }
            Write-Host "  IL_$('{0:X4}' -f $i.Offset): $($i.OpCode.Name) $operandStr"
            $count++
        }
    }
}

$assembly.Dispose()
Write-Host "`nDone."
