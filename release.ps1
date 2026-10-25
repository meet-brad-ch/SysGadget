<#
.SYNOPSIS
Releases SysGadget from the desk: tests, installer, tag v<version>, GitHub release with the setup.

.DESCRIPTION
The version comes from Directory.Build.props; raise it there (and commit) before a release. Stops at the
first problem, before anything is tagged or published:
  1. on master, a clean tree, in step with origin/master, the tag and the release do not exist yet;
  2. builds Release (warnings are errors) and runs every test, the Hardware ones included (this machine has
     the GPU and the disks the CI runner has not);
  3. builds the installer (build-installer.ps1);
  4. tags v<version> ("SysGadget <version>", annotated) and pushes the tag;
  5. creates the GitHub release with the setup, its SHA-256 in the notes.
The pushed tag also starts the CI release job; when CI runs again it replaces the setup with its own build.
Needs git, gh (logged in), the .NET SDK and Inno Setup.
#>
$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot
Set-Location $root

function Step([string]$text) { Write-Host "$(Get-Date -Format HH:mm:ss) $text" }
function Run([string]$what, [scriptblock]$command) {
    # git, gh and dotnet write progress and expected answers to stderr; Windows PowerShell 5.1 turns that
    # into a terminating error under 'Stop', so here the exit code alone decides.
    $ErrorActionPreference = 'Continue'
    & $command
    if ($LASTEXITCODE -ne 0) { throw "$what failed with exit code $LASTEXITCODE" }
}

$props = [xml](Get-Content (Join-Path $root 'Directory.Build.props'))
$version = ($props.Project.PropertyGroup | Where-Object { $_.Version }).Version
if ($version -notmatch '^\d+\.\d+\.\d+$') { throw "Directory.Build.props: version must be major.minor.patch; got '$version'" }
$tag = "v$version"

Step "1/5 checks for $tag"
$branch = git rev-parse --abbrev-ref HEAD
if ($branch -ne 'master') { throw "Release from master; this is '$branch'" }
if (git status --porcelain) { throw 'The working tree is not clean: commit or remove the changes first' }
Run 'git fetch' { git fetch --quiet origin master --tags }
if ((git rev-parse HEAD) -ne (git rev-parse origin/master)) { throw 'master is not in step with origin/master: push or pull first' }
if (git tag --list $tag) { throw "Tag $tag exists already: raise the version in Directory.Build.props" }
$releases = Run 'gh release list' { gh release list --limit 1000 --json tagName --jq '.[].tagName' }
if ($releases -contains $tag) { throw "GitHub release $tag exists already" }

Step '2/5 build and every test'
Run 'dotnet build' { dotnet build SysGadget.slnx -c Release }
foreach ($tests in @('tests\SysGadget.Core.Tests', 'tests\SysGadget.App.Tests')) {
    Run "dotnet test $tests" { dotnet test $tests -c Release --no-build }
}

Step '3/5 installer'
& (Join-Path $root 'build-installer.ps1') -Version $version
$setup = Join-Path $root "out\installer\SysGadget-Setup-$version.exe"
$hash = (Get-FileHash -Algorithm SHA256 $setup).Hash.ToLowerInvariant()

Step "4/5 tag $tag"
Run 'git tag' { git tag -a $tag -m "SysGadget $version" }
Run 'git push tag' { git push origin $tag }

Step '5/5 GitHub release'
$notes = "The installer of SysGadget $version (self-contained, no .NET needed).`n`nSHA-256 of SysGadget-Setup-$version.exe: $hash"
Run 'gh release create' { gh release create $tag $setup --title "SysGadget $version" --notes $notes }
Step "released $tag"
