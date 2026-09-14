param(
    [string]$UnityEditor = 'C:/Program Files/Unity/Hub/Editor/6000.6.0f1/Editor',
    [string]$DotnetRoot = 'C:/Program Files/dotnet'
)
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path $PSScriptRoot -Parent
$outputDir = Join-Path $projectRoot 'Temp/ArenaVerification'
New-Item -ItemType Directory -Force -Path $outputDir | Out-Null
$sdkVersion = Get-ChildItem "$DotnetRoot/sdk" -Directory | Sort-Object { [version]$_.Name } -Descending | Select-Object -First 1
$referenceVersion = Get-ChildItem "$DotnetRoot/packs/Microsoft.NETCore.App.Ref" -Directory | Sort-Object { [version]$_.Name } -Descending | Select-Object -First 1
$frameworkDir = Get-ChildItem (Join-Path $referenceVersion.FullName 'ref') -Directory | Select-Object -First 1
$references = @(Get-ChildItem $frameworkDir.FullName -Filter '*.dll' | ForEach-Object { '/reference:' + $_.FullName })
$unityReferences = @(Get-ChildItem "$UnityEditor/Data/Managed/UnityEngine" -Filter 'UnityEngine*.dll' | ForEach-Object { '/reference:' + $_.FullName })
$sources = @(Get-ChildItem "$projectRoot/Assets/Scripts/DigitalArena" -Filter '*.cs' | ForEach-Object { $_.FullName })
$compilerPath = Join-Path $sdkVersion.FullName 'Roslyn/bincore/csc.dll'
$assemblyPath = Join-Path $outputDir 'ArenaValidation.dll'
& "$DotnetRoot/dotnet.exe" $compilerPath /nologo /target:exe /define:ARENA_HEADLESS /nostdlib+ "/out:$assemblyPath" @references @unityReferences @sources "$projectRoot/Assets/Editor/DigitalArenaValidation.cs"
if ($LASTEXITCODE -ne 0) { throw 'Compilation failed.' }
$editorReferences = @(Get-ChildItem "$UnityEditor/Data/Managed/UnityEngine" -Filter 'UnityEditor*.dll' | ForEach-Object { '/reference:' + $_.FullName })
$editorSources = @(Get-ChildItem "$projectRoot/Assets/Editor" -Filter '*.cs' | ForEach-Object { $_.FullName })
$editorAssemblyPath = Join-Path $outputDir 'ArenaEditorValidation.dll'
$editorResponsePath = Join-Path $outputDir 'editor-compile.rsp'
$editorArguments = @('/nologo','/target:library','/define:UNITY_EDITOR','/nostdlib+',"/out:$editorAssemblyPath") + $references + $unityReferences + $editorReferences + $sources + $editorSources
$editorArguments | ForEach-Object { '"' + $_ + '"' } | Set-Content -LiteralPath $editorResponsePath -Encoding utf8
& "$DotnetRoot/dotnet.exe" $compilerPath "@$editorResponsePath"
if ($LASTEXITCODE -ne 0) { throw 'Editor compilation failed.' }
$runtimeVersion = $referenceVersion.Name
$runtimeConfig = @{ runtimeOptions = @{ tfm = $frameworkDir.Name; framework = @{ name = 'Microsoft.NETCore.App'; version = $runtimeVersion } } } | ConvertTo-Json -Depth 5
Set-Content -LiteralPath (Join-Path $outputDir 'ArenaValidation.runtimeconfig.json') -Value $runtimeConfig -Encoding utf8
& "$DotnetRoot/dotnet.exe" $assemblyPath
if ($LASTEXITCODE -ne 0) { throw 'Rules validation failed.' }
