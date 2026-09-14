$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
$readyRoot = (Resolve-Path -LiteralPath 'C:/Users/User/Desktop/J2_AssetImage/ready').Path
$confirmDir = 'C:/Users/User/Desktop/J2_AssetImage/confirm'
New-Item -ItemType Directory -Path $confirmDir -Force | Out-Null
$confirmRoot = (Resolve-Path -LiteralPath $confirmDir).Path
$manifests = @(Get-ChildItem -LiteralPath (Join-Path $projectRoot 'Assets/Resources/Digimon') -Recurse -Filter manifest.json | Where-Object { $_.Directory.Name -eq 'Source~' })
$pending = @()
foreach ($file in $manifests) {
    $manifest = Get-Content -LiteralPath $file.FullName -Raw | ConvertFrom-Json
    $model = Get-Item -LiteralPath (Join-Path $file.Directory.Parent.FullName ($manifest.name + '_Model.fbx'))
    foreach ($reference in $manifest.references) {
        if (-not (Test-Path -LiteralPath $reference.path)) { continue }
        $sourcePath = (Resolve-Path -LiteralPath $reference.path).Path
        if ((Split-Path -Parent $sourcePath) -ne $readyRoot) { throw 'Reference is outside ready' }
        foreach ($report in @('validation.txt','runtime-validation.txt')) {
            $checked = Get-Item -LiteralPath (Join-Path $file.Directory.FullName $report)
            if ($checked.LastWriteTimeUtc -lt $model.LastWriteTimeUtc) { throw ('Stale validation: ' + $manifest.name) }
            if ((Get-Content -LiteralPath $checked.FullName -Raw) -notmatch ([regex]::Escape($manifest.name) + ': PASS')) { throw 'Validation did not pass' }
        }
        if ((Get-FileHash -LiteralPath $sourcePath -Algorithm SHA256).Hash -ne $reference.sha256) { throw 'Reference changed since revision' }
        $pending += [pscustomobject]@{ Source=$sourcePath; Hash=$reference.sha256; ReportFolder=$file.Directory.FullName }
    }
}
$moved = @()
foreach ($item in $pending) {
    $destination = Join-Path $confirmRoot (Split-Path -Leaf $item.Source)
    $index = 1
    while (Test-Path -LiteralPath $destination) {
        $leaf = [IO.Path]::GetFileNameWithoutExtension($item.Source) + '_' + $item.Hash.Substring(0,8) + '_' + $index + [IO.Path]::GetExtension($item.Source)
        $destination = Join-Path $confirmRoot $leaf
        $index++
    }
    if ([IO.Path]::GetDirectoryName([IO.Path]::GetFullPath($destination)) -ne $confirmRoot) { throw 'Invalid destination' }
    Move-Item -LiteralPath $item.Source -Destination $destination
    $record = [pscustomobject]@{ source=$item.Source; confirmedPath=$destination; sha256=$item.Hash; confirmedAt=(Get-Date -Format o) }
    $record | ConvertTo-Json | Add-Content -LiteralPath (Join-Path $item.ReportFolder 'reference-confirmation.jsonl') -Encoding utf8
    $moved += $destination
}
"Moved verified references to confirm: $($moved.Count)"
