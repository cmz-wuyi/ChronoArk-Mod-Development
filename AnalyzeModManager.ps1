# Analyze ModManager and ModInfo classes in Assembly-CSharp.dll
# Using English-only to avoid encoding issues

$ErrorActionPreference = "Stop"
$assemblyPath = "d:\Games\Chrono Ark\ChronoArk_Data\Managed\Assembly-CSharp.dll"
$cecilPath = "d:\Games\Chrono Ark\ChronoArk_Data\Managed\Mono.Cecil.dll"

Write-Host "Loading Mono.Cecil from: $cecilPath"
Add-Type -Path $cecilPath

Write-Host "Loading assembly: $assemblyPath"
$assembly = [Mono.Cecil.AssemblyDefinition]::ReadAssembly($assemblyPath)
$module = $assembly.MainModule

Write-Host "`n=== Searching for classes with Mod-related names ==="
$modTypes = $module.Types | Where-Object {
    $_.Name -match "Mod" -and $_.Namespace -notmatch "Unity"
} | Select-Object -First 30

foreach ($t in $modTypes) {
    Write-Host "`nType: $($t.FullName)"
    foreach ($f in $t.Fields) {
        Write-Host "  Field: $($f.Name) : $($f.FieldType.Name)"
    }
    foreach ($p in $t.Properties) {
        Write-Host "  Prop: $($p.Name) : $($p.PropertyType.Name)"
    }
}

Write-Host "`n=== Searching for classes with Assemblies field ==="
foreach ($t in $module.Types) {
    foreach ($f in $t.Fields) {
        if ($f.Name -match "Assembl" -or $f.Name -match "EntryDll" -or $f.Name -match "ModAssembly" -or $f.Name -match "UseMod") {
            Write-Host "Type: $($t.FullName) | Field: $($f.Name) : $($f.FieldType.Name)"
        }
    }
    foreach ($p in $t.Properties) {
        if ($p.Name -match "Assembl" -or $p.Name -match "EntryDll" -or $p.Name -match "ModAssembly" -or $p.Name -match "UseMod") {
            Write-Host "Type: $($t.FullName) | Prop: $($p.Name) : $($p.PropertyType.Name)"
        }
    }
}

Write-Host "`n=== Searching for JSON-like Mod data classes (fields: Name, Author, Version) ==="
foreach ($t in $module.Types) {
    $hasName = $false
    $hasAuthor = $false
    $hasVersion = $false
    foreach ($m in $t.Members) {
        if ($m.Name -eq "Name") { $hasName = $true }
        if ($m.Name -eq "Author") { $hasAuthor = $true }
        if ($m.Name -eq "Version") { $hasVersion = $true }
    }
    if ($hasName -and $hasAuthor -and $hasVersion) {
        Write-Host "`nCandidate: $($t.FullName)"
        foreach ($f in $t.Fields) {
            Write-Host "  Field: $($f.Name) : $($f.FieldType.Name)"
        }
        foreach ($p in $t.Properties) {
            Write-Host "  Prop: $($p.Name) : $($p.PropertyType.Name)"
        }
        # Show methods that mention "Plugin" or "Initialize"
        foreach ($m in $t.Methods) {
            if ($m.Name -match "Plugin" -or $m.Name -match "Init" -or $m.Name -match "Load") {
                Write-Host "  Method: $($m.Name)()"
            }
        }
    }
}

$assembly.Dispose()
Write-Host "`nDone."
