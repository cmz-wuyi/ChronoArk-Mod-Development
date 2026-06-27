# Analyze RE_TheInquisition.OnlyEvent to see how the game gives Item_Equip_Torch_FanaticBoss
$ErrorActionPreference = "Stop"
$assemblyPath = "d:\Games\Chrono Ark\ChronoArk_Data\Managed\Assembly-CSharp.dll"
$cecilPath = "d:\Games\Chrono Ark\ChronoArk_Data\Managed\Mono.Cecil.dll"

Add-Type -Path $cecilPath
$assembly = [Mono.Cecil.AssemblyDefinition]::ReadAssembly($assemblyPath)
$module = $assembly.MainModule

# Find RE_TheInquisition type
$reType = $module.Types | Where-Object { $_.Name -eq "RE_TheInquisition" } | Select-Object -First 1

if ($reType -eq $null) {
    Write-Host "RE_TheInquisition NOT FOUND"
    $assembly.Dispose()
    exit
}

Write-Host "Found: $($reType.FullName)"
Write-Host "BaseType: $($reType.BaseType.FullName)"

# List all methods
Write-Host "`n=== All Methods ==="
foreach ($m in $reType.Methods) {
    Write-Host "  $($m.Name) (size: $($m.Body.CodeSize) bytes)"
}

# Analyze OnlyEvent method
$onlyEventMethod = $reType.Methods | Where-Object { $_.Name -eq "OnlyEvent" } | Select-Object -First 1
if ($onlyEventMethod -ne $null) {
    Write-Host "`n=== OnlyEvent IL ==="
    Write-Host "Code size: $($onlyEventMethod.Body.CodeSize) bytes"
    Write-Host "Local vars:"
    foreach ($v in $onlyEventMethod.Body.Variables) {
        Write-Host "  [$($v.Index)] $($v.VariableType.FullName)"
    }
    Write-Host "`nIL Instructions:"
    foreach ($i in $onlyEventMethod.Body.Instructions) {
        $operandStr = ""
        if ($i.Operand -ne $null) {
            $operandStr = $i.Operand.ToString()
        }
        Write-Host "  IL_$('{0:X4}' -f $i.Offset): $($i.OpCode.Name) $operandStr"
    }
}

# Also analyze UseButton1 method (which calls OnlyEvent or delays)
$useButton1Method = $reType.Methods | Where-Object { $_.Name -eq "UseButton1" } | Select-Object -First 1
if ($useButton1Method -ne $null) {
    Write-Host "`n=== UseButton1 IL (first 60 instructions) ==="
    $count = 0
    foreach ($i in $useButton1Method.Body.Instructions) {
        if ($count -ge 60) { break }
        $operandStr = ""
        if ($i.Operand -ne $null) {
            $operandStr = $i.Operand.ToString()
        }
        Write-Host "  IL_$('{0:X4}' -f $i.Offset): $($i.OpCode.Name) $operandStr"
        $count++
    }
}

# Find how items are added - search for AddNewItem usage
Write-Host "`n=== Searching for AddNewItem callers ==="
foreach ($t in $module.Types) {
    foreach ($m in $t.Methods) {
        if ($m.Body -eq $null) { continue }
        foreach ($i in $m.Body.Instructions) {
            if ($i.OpCode.Code -eq [Mono.Cecil.Code]::Call -or $i.OpCode.Code -eq [Mono.Cecil.Code]::Callvirt) {
                if ($i.Operand -ne $null -and $i.Operand.ToString() -match "AddNewItem") {
                    Write-Host "  Type: $($t.FullName) | Method: $($m.Name)"
                    Write-Host "    IL: $($i.OpCode.Name) $($i.Operand)"
                }
            }
        }
    }
}

$assembly.Dispose()
Write-Host "`nDone."
