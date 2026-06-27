# DeepAnalysis.ps1 - Deep IL disassembly using Mono.Cecil
$ErrorActionPreference = "Stop"

$cecilPath = "d:\Games\Chrono Ark\ChronoArk_Data\Managed\Mono.Cecil.dll"
Add-Type -Path $cecilPath

$dllPath = "d:\Games\Chrono Ark\ChronoArk_Data\Managed\Assembly-CSharp.dll"
$assembly = [Mono.Cecil.AssemblyDefinition]::ReadAssembly($dllPath)
$module = $assembly.MainModule

$outputFile = "d:\Games\Chrono Ark\ModDevelopment\DEEP_IL_ANALYSIS.txt"
$output = [System.Collections.ArrayList]::new()

function WO($text) {
    $output.Add($text) | Out-Null
    Write-Host $text
}

function DisassembleIL($methodDef, $indent = "") {
    if (-not $methodDef.HasBody) {
        WO "${indent}  (No body)"
        return
    }

    $body = $methodDef.Body

    WO "${indent}  Code size: $($body.CodeSize) bytes"
    WO "${indent}  Locals:"

    if ($body.Variables.Count -gt 0) {
        foreach ($var in $body.Variables) {
            WO "${indent}    [$($var.Index)] $($var.VariableType.FullName)"
        }
    }

    WO "${indent}  Exception handlers:"
    if ($body.ExceptionHandlers.Count -gt 0) {
        foreach ($eh in $body.ExceptionHandlers) {
            WO "${indent}    Try: $($eh.TryStart.Offset)-$($eh.TryEnd.Offset), Handler: $($eh.HandlerStart.Offset), Type: $($eh.HandlerType)"
        }
    }

    WO "${indent}  IL instructions:"
    foreach ($instr in $body.Instructions) {
        $opCode = $instr.OpCode.Name
        $operand = ""

        if ($instr.Operand -ne $null) {
            $ot = $instr.OpCode.OperandType.ToString()
            switch ($ot) {
                "InlineMethod" {
                    $m = $instr.Operand
                    $operand = "$($m.DeclaringType.Name).$($m.Name)"
                }
                "InlineField" {
                    $f = $instr.Operand
                    $operand = "$($f.DeclaringType.Name).$($f.Name)"
                }
                "InlineType" {
                    $t = $instr.Operand
                    $operand = $t.FullName
                }
                "InlineString" {
                    $operand = "STRING: $($instr.Operand)"
                }
                "InlineBrTarget" {
                    $operand = "IL_$($instr.Operand.Offset.ToString('X4'))"
                }
                "ShortInlineBrTarget" {
                    $operand = "IL_$($instr.Operand.Offset.ToString('X4'))"
                }
                "InlineSwitch" {
                    $targets = @()
                    foreach ($t in $instr.Operand) {
                        $targets += "IL_$($t.Offset.ToString('X4'))"
                    }
                    $operand = $targets -join ", "
                }
                "InlineI" { $operand = $instr.Operand }
                "ShortInlineI" { $operand = $instr.Operand }
                "InlineI8" { $operand = $instr.Operand }
                "InlineR" { $operand = $instr.Operand }
                "ShortInlineR" { $operand = $instr.Operand }
                "InlineVar" { $operand = $instr.Operand }
                "ShortInlineVar" { $operand = $instr.Operand }
                "InlineTok" { $operand = $instr.Operand.ToString() }
                default { $operand = $instr.Operand.ToString() }
            }
        }

        WO "${indent}    IL_$($instr.Offset.ToString('X4')): $opCode $operand"
    }
}

$reType = $module.GetType("RE_TheInquisition")
$ssType = $module.GetType("StageSystem")
$tsdType = $module.GetType("TempSaveData")
$sdType = $module.GetType("StoryData")

WO "=========================================="
WO "Chrono Ark Deep IL Analysis Report"
WO "=========================================="
WO ""

# 1. RE_TheInquisition
WO "=========================================="
WO "1. RE_TheInquisition Class"
WO "=========================================="
WO "Namespace: $($reType.Namespace)"
WO "BaseType: $($reType.BaseType.FullName)"
WO ""

WO "--- Fields ---"
foreach ($field in $reType.Fields) {
    WO "  $($field.FieldType.Name) $($field.Name)"
}
WO ""

WO "--- Methods ---"
foreach ($method in $reType.Methods) {
    WO ""
    WO "Method: $($method.Name) (size: $($method.Body.CodeSize) bytes)"
    $params = @()
    foreach ($p in $method.Parameters) {
        $params += "$($p.ParameterType.Name) $($p.Name)"
    }
    WO "  Params: $($params -join ', ')"
    WO "  Return: $($method.ReturnType.Name)"
    DisassembleIL $method "  "
}

# 2. BossEnter coroutine
WO ""
WO "=========================================="
WO "2. StageSystem.BossEnter Coroutine"
WO "=========================================="

$beStateType = $null
foreach ($nested in $ssType.NestedTypes) {
    if ($nested.Name -match "BossEnter.*d__59") {
        $beStateType = $nested
        break
    }
}

if ($beStateType) {
    WO "State machine: $($beStateType.FullName)"
    WO ""
    WO "--- Fields ---"
    foreach ($field in $beStateType.Fields) {
        WO "  $($field.FieldType.Name) $($field.Name)"
    }
    WO ""

    $moveNext = $null
    foreach ($m in $beStateType.Methods) {
        if ($m.Name -eq "MoveNext") {
            $moveNext = $m
            break
        }
    }

    if ($moveNext) {
        WO "--- MoveNext Method (size: $($moveNext.Body.CodeSize) bytes) ---"
        DisassembleIL $moveNext "  "
    }
} else {
    WO "BossEnter state machine not found"
    WO "Available nested types with Boss/Johan:"
    foreach ($nested in $ssType.NestedTypes) {
        if ($nested.Name -match "Boss|Johan") {
            WO "  $($nested.Name)"
        }
    }
}

# 3. Co_JohanQuest coroutine
WO ""
WO "=========================================="
WO "3. StageSystem.Co_JohanQuest Coroutine"
WO "=========================================="

$jqStateType = $null
foreach ($nested in $ssType.NestedTypes) {
    if ($nested.Name -match "Co_JohanQuest.*d__60") {
        $jqStateType = $nested
        break
    }
}

if ($jqStateType) {
    WO "State machine: $($jqStateType.FullName)"
    WO ""
    WO "--- Fields ---"
    foreach ($field in $jqStateType.Fields) {
        WO "  $($field.FieldType.Name) $($field.Name)"
    }
    WO ""

    $moveNext = $null
    foreach ($m in $jqStateType.Methods) {
        if ($m.Name -eq "MoveNext") {
            $moveNext = $m
            break
        }
    }

    if ($moveNext) {
        WO "--- MoveNext Method (size: $($moveNext.Body.CodeSize) bytes) ---"
        DisassembleIL $moveNext "  "
    }
}

# 4. Co_JohanQuestBattleAfter coroutine
WO ""
WO "=========================================="
WO "4. StageSystem.Co_JohanQuestBattleAfter Coroutine"
WO "=========================================="

$jqbaStateType = $null
foreach ($nested in $ssType.NestedTypes) {
    if ($nested.Name -match "Co_JohanQuestBattleAfter.*d__62") {
        $jqbaStateType = $nested
        break
    }
}

if ($jqbaStateType) {
    WO "State machine: $($jqbaStateType.FullName)"
    WO ""
    WO "--- Fields ---"
    foreach ($field in $jqbaStateType.Fields) {
        WO "  $($field.FieldType.Name) $($field.Name)"
    }
    WO ""

    $moveNext = $null
    foreach ($m in $jqbaStateType.Methods) {
        if ($m.Name -eq "MoveNext") {
            $moveNext = $m
            break
        }
    }

    if ($moveNext) {
        WO "--- MoveNext Method (size: $($moveNext.Body.CodeSize) bytes) ---"
        DisassembleIL $moveNext "  "
    }
}

# 5. TempSaveData key fields
WO ""
WO "=========================================="
WO "5. TempSaveData Key Fields"
WO "=========================================="
foreach ($field in $tsdType.Fields) {
    if ($field.Name -match "Inquisition|Boss|Queue|Johan|Quest|Sword") {
        WO "  $($field.FieldType.Name) $($field.Name)"
    }
}

# 6. StoryData key fields
WO ""
WO "=========================================="
WO "6. StoryData Key Fields"
WO "=========================================="
foreach ($field in $sdType.Fields) {
    if ($field.Name -match "Johan|Boss|Quest|Progress") {
        WO "  $($field.FieldType.Name) $($field.Name)"
    }
}

# 7. BossEnterFunc
WO ""
WO "=========================================="
WO "7. StageSystem.BossEnterFunc"
WO "=========================================="

$befMethod = $null
foreach ($m in $ssType.Methods) {
    if ($m.Name -eq "BossEnterFunc") {
        $befMethod = $m
        break
    }
}

if ($befMethod) {
    WO "Method: BossEnterFunc (size: $($befMethod.Body.CodeSize) bytes)"
    DisassembleIL $befMethod "  "
}

# 8. BossKeyReturn
WO ""
WO "=========================================="
WO "8. StageSystem.BossKeyReturn"
WO "=========================================="

$bkrMethod = $null
foreach ($m in $ssType.Methods) {
    if ($m.Name -eq "BossKeyReturn") {
        $bkrMethod = $m
        break
    }
}

if ($bkrMethod) {
    WO "Method: BossKeyReturn (size: $($bkrMethod.Body.CodeSize) bytes)"
    DisassembleIL $bkrMethod "  "
}

WO ""
WO "=========================================="
WO "Analysis Complete"
WO "=========================================="

$output -join "`r`n" | Out-File -FilePath $outputFile -Encoding UTF8
Write-Host ""
Write-Host "Report saved to: $outputFile"

$assembly.Dispose()
