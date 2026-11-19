[CmdletBinding()]
param(
    [string] $WindowsMetadata,
    [string] $OutputDirectory = (Join-Path $PSScriptRoot 'dist')
)

$ErrorActionPreference = 'Stop'
$framework = Join-Path $env:windir 'Microsoft.NET\Framework64\v4.0.30319'
$compiler = Join-Path $framework 'csc.exe'
if (-not (Test-Path -LiteralPath $compiler)) {
    throw 'The .NET Framework 4.x compiler could not be found.'
}

if (-not $WindowsMetadata) {
    $sdk = Join-Path ${env:ProgramFiles(x86)} 'Windows Kits\10\UnionMetadata'
    if (Test-Path -LiteralPath $sdk) {
        $WindowsMetadata = Get-ChildItem -LiteralPath $sdk -Recurse -Filter Windows.winmd -File |
            Where-Object { $_.Directory.Name -match '^10\.\d+\.\d+\.\d+$' } |
            Sort-Object { [version] $_.Directory.Name } -Descending |
            Select-Object -First 1 -ExpandProperty FullName
    }
}
if (-not $WindowsMetadata -or -not (Test-Path -LiteralPath $WindowsMetadata)) {
    throw 'Install the Windows 10 or 11 SDK, or provide -WindowsMetadata with the path to Windows.winmd.'
}

$output = (New-Item -ItemType Directory -Path $OutputDirectory -Force).FullName
$exe = Join-Path $output 'ChargeTray.exe'
$manifest = Join-Path $PSScriptRoot 'src\app.manifest'
$sources = Get-ChildItem -LiteralPath (Join-Path $PSScriptRoot 'src') -Filter '*.cs' -File |
    Select-Object -ExpandProperty FullName
$references = @('System.Runtime.dll', 'System.Runtime.WindowsRuntime.dll',
    'System.Runtime.InteropServices.WindowsRuntime.dll') |
    ForEach-Object { '/reference:' + (Join-Path $framework $_) }

& $compiler /nologo /codepage:65001 /target:winexe /platform:x64 /optimize+ "/out:$exe" `
    "/win32manifest:$manifest" "/reference:$WindowsMetadata" @references @sources
if ($LASTEXITCODE -ne 0) { throw "Compilation failed with exit code $LASTEXITCODE." }
Write-Output $exe
