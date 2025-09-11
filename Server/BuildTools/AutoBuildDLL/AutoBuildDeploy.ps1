# Script tu dong build va deploy DLL sang Unity
# Su dung: .\AutoBuildDeploy.ps1 [-SkipBuild] [-Verbose] [-ShowConfig]

param(
    [switch]$SkipBuild,
    [switch]$Verbose,
    [switch]$ShowConfig
)

# Load file config
$configPath = Join-Path $PSScriptRoot "Config.ps1"
if (-not (Test-Path $configPath)) {
    Write-Host "[LOI] Khong tim thay file Config.ps1 tai: $configPath" -ForegroundColor Red
    exit 1
}
. $configPath

$SuccessColor = "Green"
$ErrorColor = "Red"
$InfoColor = "Yellow"
$ProcessColor = "Cyan"

function Write-Step { param($Message, $Color = $InfoColor) Write-Host "=> $Message" -ForegroundColor $Color }
function Write-Success { param($Message) Write-Host "[OK] $Message" -ForegroundColor $SuccessColor }
function Write-ErrorMsg { param($Message) Write-Host "[LOI] $Message" -ForegroundColor $ErrorColor }
function Write-Process { param($Message) Write-Host $Message -ForegroundColor $ProcessColor }

function Get-FileHashSafe { param($FilePath) try { if (Test-Path $FilePath) { (Get-FileHash -Path $FilePath -Algorithm SHA256).Hash } else { $null } } catch { $null } }
function Compare-FileHashes { param($SourcePath, $DestPath)
    $sourceHash = Get-FileHashSafe -FilePath $SourcePath
    $destHash = Get-FileHashSafe -FilePath $DestPath
    if ($sourceHash -eq $null) { return "SOURCE_NOT_FOUND" }
    if ($destHash -eq $null) { return "DEST_NOT_FOUND" }
    if ($sourceHash -eq $destHash) { return "IDENTICAL" } else { return "DIFFERENT" }
}

if ($ShowConfig) { Show-ConfigInfo; return }

# Kiem tra duong dan
$configErrors = Test-ConfigPaths
if ($configErrors.Count -gt 0) {
    Write-Host "[LOI] Co loi trong config:" -ForegroundColor Red
    $configErrors | ForEach-Object { Write-Host " - $_" -ForegroundColor Red }
    exit 1
}

try {
    Write-Host "=== AI SMART RECALL AUTO BUILD AND DEPLOY ===" -ForegroundColor Magenta

    if (-not $SkipBuild) {
        Write-Step "Buoc 1: Build solution..."
        if (-not (Test-Path $Global:SolutionPath)) { Write-ErrorMsg "Khong tim thay solution file: $Global:SolutionPath"; exit 1 }
        Write-Process "Dang build solution..."
        dotnet build $Global:SolutionPath --configuration $Global:BuildConfiguration --verbosity minimal
        if ($LASTEXITCODE -ne 0) { Write-ErrorMsg "Build solution that bai!"; exit 1 }
        Write-Success "Build solution thanh cong!"
    } else {
        Write-Step "Bo qua build solution (-SkipBuild)"
    }

    Write-Step "Buoc 2: Chay MemoryPackSerializer.exe..."
    if (-not (Test-Path $Global:MemoryPackBinPath)) { Write-ErrorMsg "Khong tim thay thu muc: $Global:MemoryPackBinPath"; exit 1 }
    $exePath = Join-Path $Global:MemoryPackBinPath $Global:ExeName
    if (-not (Test-Path $exePath)) { Write-ErrorMsg "Khong tim thay file: $exePath"; exit 1 }

    Push-Location $Global:MemoryPackBinPath
    if ($Verbose) { & ".\$($Global:ExeName)" } else { & ".\$($Global:ExeName)" | Out-Null }
    $exeExit = $LASTEXITCODE
    Pop-Location
    if ($exeExit -ne 0) { Write-ErrorMsg "Chay MemoryPackSerializer that bai!"; exit 1 }
    Write-Success "Chay MemoryPackSerializer thanh cong!"

    Write-Step "Buoc 3: Copy cac file DLL sang Unity..."
    if (-not (Test-Path $Global:UnityDLLPath)) { New-Item -ItemType Directory -Path $Global:UnityDLLPath -Force | Out-Null }

    $copyStats = @{ New = 0; Updated = 0; Skipped = 0 }

    foreach ($fileName in $Global:FilesToCopy) {
        $sourcePath = if ($fileName -eq "AISmartRecall.SharedModels.dll") { Join-Path $Global:SharedModelsBinPath $fileName } else { Join-Path $Global:MemoryPackBinPath $fileName }
        $destPath = Join-Path $Global:UnityDLLPath $fileName

        if (-not (Test-Path $sourcePath)) { Write-ErrorMsg "Khong tim thay file nguon: $sourcePath"; continue }
        $compare = Compare-FileHashes -SourcePath $sourcePath -DestPath $destPath
        switch ($compare) {
            "SOURCE_NOT_FOUND" { Write-ErrorMsg "Khong tim thay file nguon: $sourcePath" }
            "DEST_NOT_FOUND" { Copy-Item $sourcePath $destPath -Force; Write-Success "[NEW] $fileName"; $copyStats.New++ }
            "IDENTICAL" { Write-Process "[SKIP] $fileName (khong thay doi)"; $copyStats.Skipped++ }
            "DIFFERENT" { Copy-Item $sourcePath $destPath -Force; Write-Success "[UPDATE] $fileName"; $copyStats.Updated++ }
        }
    }

    Write-Host ""; Write-Host "=== KET QUA ===" -ForegroundColor Magenta
    Write-Host ("File moi: {0}, Cap nhat: {1}, Bo qua: {2}" -f $copyStats.New, $copyStats.Updated, $copyStats.Skipped) -ForegroundColor Cyan
    Write-Success "Hoan thanh quy trinh build va deploy!"
} catch {
    Write-ErrorMsg $_.Exception.Message
    exit 1
}

