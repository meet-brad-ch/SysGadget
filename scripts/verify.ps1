# Runs every quality gate, continues past a failed gate, prints one line per gate,
# and exits non-zero if any gate is FAIL or NOT VERIFIED.
# Run on the desk: the tests include the Hardware category (GPU, NVIDIA driver, disks), which CI leaves out.
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$coverageDir = Join-Path $root 'out\coverage'
$results = [ordered]@{}

function Invoke-Gate([string]$Name, [string]$Exe, [scriptblock]$Command) {
    if (-not $Exe) {
        $results[$Name] = 'NOT VERIFIED (tool not found)'
        return
    }
    & $Command $Exe
    $results[$Name] = if ($LASTEXITCODE -eq 0) { 'PASS' } else { 'FAIL' }
}

# Per assembly, the suite that covers it best: Core.Tests covers Core, App.Tests covers App. The values are
# not a union of both suites, so an assembly reached by both is reported at the better of the two.
function Read-Coverage([string]$Folder) {
    $best = @{}
    foreach ($file in Get-ChildItem $Folder -Recurse -Filter 'coverage.cobertura.xml') {
        foreach ($package in ([xml](Get-Content $file.FullName)).coverage.packages.package) {
            $line = [double]$package.'line-rate'
            if (-not $best.ContainsKey($package.name) -or $best[$package.name].Line -lt $line) {
                $best[$package.name] = [pscustomobject]@{ Line = $line; Branch = [double]$package.'branch-rate' }
            }
        }
    }
    return $best
}

$dotnet = (Get-Command dotnet -ErrorAction SilentlyContinue).Source
$python = (Get-Command python -ErrorAction SilentlyContinue).Source

Push-Location $root
try {
    if (Test-Path $coverageDir) { Remove-Item $coverageDir -Recurse -Force }  # no stale values
    Invoke-Gate 'Build' $dotnet { param($t) & $t build SysGadget.slnx -c Release -warnaserror }
    Invoke-Gate 'Unit tests' $dotnet {
        param($t)
        $failed = 0
        foreach ($project in @('tests\SysGadget.Core.Tests', 'tests\SysGadget.App.Tests')) {
            & $t test $project -c Release --no-build --collect 'XPlat Code Coverage' --results-directory $coverageDir
            if ($LASTEXITCODE -ne 0) { $failed++ }
        }
        $global:LASTEXITCODE = $failed
    }
    $results['Integration tests'] = 'N/A (no integration test suite; the window tests run in App.Tests)'
    $results['Lint'] = 'N/A (analysers at latest-recommended and code style run in the build; see Build)'
    Invoke-Gate 'Formatting' $dotnet { param($t) & $t format SysGadget.slnx --verify-no-changes --no-restore }
    $results['Static type check'] = 'N/A (the compiler with nullable analysis; see Build)'
    $results['Static analysis'] = 'N/A (Roslyn analysers at latest-recommended; see Build)'
    Invoke-Gate 'Cyclomatic complexity' $python {
        param($t) & $t -m lizard -l csharp -C 9 -w -x '*\obj\*' -x '*/obj/*' src tests
    }

    $coverage = if (Test-Path $coverageDir) { Read-Coverage $coverageDir } else { @{} }
    if ($coverage.Count -gt 0) {
        $names = $coverage.Keys | Sort-Object
        $results['Line coverage'] = ($names | ForEach-Object { '{0} {1:P1}' -f $_, $coverage[$_].Line }) -join ', '
        $results['Branch coverage'] = ($names | ForEach-Object { '{0} {1:P1}' -f $_, $coverage[$_].Branch }) -join ', '
    } else {
        $results['Line coverage'] = 'NOT VERIFIED (no coverage.cobertura.xml)'
        $results['Branch coverage'] = 'NOT VERIFIED (no coverage.cobertura.xml)'
    }
} finally {
    Pop-Location
}

Write-Output ''
$results.GetEnumerator() | ForEach-Object { '{0,-22} {1}' -f ($_.Key + ':'), $_.Value }
if ($results.Values | Where-Object { $_ -like 'FAIL*' -or $_ -like 'NOT VERIFIED*' }) { exit 1 }
