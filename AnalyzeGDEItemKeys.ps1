# Analyze GDEItemKeys to find the actual runtime value of Item_Equip_Torch_FanaticBoss
$ErrorActionPreference = "Stop"
$assemblyPath = "d:\Games\Chrono Ark\ChronoArk_Data\Managed\Assembly-CSharp.dll"
$cecilPath = "d:\Games\Chrono Ark\ChronoArk_Data\Managed\Mono.Cecil.dll"

Add-Type -Path $cecilPath
$assembly = [Mono.Cecil.AssemblyDefinition]::ReadAssembly($assemblyPath)
$module = $assembly.MainModule

# Find GDEItemKeys type
$gdeType = $module.Types | Where-Object { $_.Name -eq "GDEItemKeys" } | Select-Object -First 1

if ($gdeType -eq $null) {
    Write-Host "GDEItemKeys NOT FOUND in Assembly-CSharp.dll"
    # Search in other assemblies
    $firstpassPath = "d:\Games\Chrono Ark\ChronoArk_Data\Managed\Assembly-CSharp-firstpass.dll"
    if (Test-Path $firstpassPath) {
        $assembly2 = [Mono.Cecil.AssemblyDefinition]::ReadAssembly($firstpassPath)
        $gdeType = $assembly2.MainModule.Types | Where-Object { $_.Name -eq "GDEItemKeys" } | Select-Object -First 1
        if ($gdeType -ne $null) {
            Write-Host "Found in Assembly-CSharp-firstpass.dll"
        }
    }
}

if ($gdeType -eq $null) {
    Write-Host "GDEItemKeys NOT FOUND anywhere"
    $assembly.Dispose()
    exit
}

Write-Host "Found: $($gdeType.FullName)"
Write-Host "Namespace: $($gdeType.Namespace)"

# Find Item_Equip_Torch_FanaticBoss field
$torchField = $gdeType.Fields | Where-Object { $_.Name -eq "Item_Equip_Torch_FanaticBoss" } | Select-Object -First 1

if ($torchField -eq $null) {
    Write-Host "`nField 'Item_Equip_Torch_FanaticBoss' NOT FOUND"
    Write-Host "`n=== All fields containing 'Torch' or 'Fanatic' ==="
    foreach ($f in $gdeType.Fields) {
        if ($f.Name -match "Torch" -or $f.Name -match "Fanatic") {
            Write-Host "  $($f.Name) : $($f.FieldType.FullName)"
        }
    }
} else {
    Write-Host "`nField found: $($torchField.Name)"
    Write-Host "  Type: $($torchField.FieldType.FullName)"
    Write-Host "  IsStatic: $($torchField.IsStatic)"
    Write-Host "  IsLiteral: $($torchField.IsLiteral)"
    Write-Host "  HasConstant: $($torchField.HasConstant)"
    if ($torchField.HasConstant) {
        Write-Host "  Constant: '$($torchField.Constant)'"
    }
}

# Show static constructor (.cctor) to see how fields are initialized
$cctor = $gdeType.Methods | Where-Object { $_.IsConstructor -and $_.IsStatic } | Select-Object -First 1
if ($cctor -ne $null) {
    Write-Host "`n=== Static Constructor .cctor ==="
    Write-Host "Code size: $($cctor.Body.CodeSize) bytes"
    
    # Find instructions that reference Item_Equip_Torch_FanaticBoss
    Write-Host "`n--- Instructions referencing Item_Equip_Torch_FanaticBoss ---"
    $found = $false
    foreach ($i in $cctor.Body.Instructions) {
        $opStr = $i.ToString()
        if ($opStr -match "Item_Equip_Torch_FanaticBoss") {
            # Print this and next 3 instructions
            Write-Host "  IL_$('{0:X4}' -f $i.Offset): $($i.OpCode.Name) $opStr"
            $found = $true
        }
    }
    if (-not $found) {
        Write-Host "  No direct references found in .cctor"
        Write-Host "`n--- First 30 instructions of .cctor ---"
        $count = 0
        foreach ($i in $cctor.Body.Instructions) {
            if ($count -ge 30) { break }
            $opStr = ""
            if ($i.Operand -ne $null) { $opStr = $i.Operand.ToString() }
            Write-Host "  IL_$('{0:X4}' -f $i.Offset): $($i.OpCode.Name) $opStr"
            $count++
        }
    }
}

# Also analyze ItemBase.GetItem method
Write-Host "`n=== Analyzing ItemBase.GetItem ==="
$itemBaseType = $module.Types | Where-Object { $_.Name -eq "ItemBase" } | Select-Object -First 1
if ($itemBaseType -ne $null) {
    $getItemMethod = $itemBaseType.Methods | Where-Object { $_.Name -eq "GetItem" } | Select-Object -First 1
    if ($getItemMethod -ne $null) {
        Write-Host "Method: $($getItemMethod.FullName)"
        Write-Host "Code size: $($getItemMethod.Body.CodeSize) bytes"
        Write-Host "`nIL Instructions:"
        foreach ($i in $getItemMethod.Body.Instructions) {
            $opStr = ""
            if ($i.Operand -ne $null) { $opStr = $i.Operand.ToString() }
            Write-Host "  IL_$('{0:X4}' -f $i.Offset): $($i.OpCode.Name) $opStr"
        }
    }
}

$assembly.Dispose()
Write-Host "`nDone."
