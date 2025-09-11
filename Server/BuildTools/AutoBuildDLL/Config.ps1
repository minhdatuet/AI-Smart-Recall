# ==============================================================================
# FILE CONFIG CHO TOOL AUTO BUILD DEPLOY - AI SMART RECALL
# Chi can sua duong dan DLL cua Unity project, cac duong dan khac tu dong tim
# ==============================================================================

# ================================SUA TAI DAY===================================
#🡇🡇🡇🡇🡇🡇🡇🡇🡇🡇🡇🡇🡇🡇🡇🡇🡇🡇🡇🡇🡇🡇🡇🡇🡇🡇🡇🡇🡇🡇🡇🡇🡇🡇🡇🡇🡇🡇🡇🡇🡇🡇🡇🡇🡇🡇🡇🡇🡇🡇

# Duong dan den thu muc DLL cua Unity project (noi se copy file den)
$Global:UnityDLLPath = "D:\AI-Smart-Recall\AI-Smart-Recall\Assets\DLL"

#🡅🡅🡅🡅🡅🡅🡅🡅🡅🡅🡅🡅🡅🡅🡅🡅🡅🡅🡅🡅🡅🡅🡅🡅🡅🡅🡅🡅🡅🡅🡅🡅🡅🡅🡅🡅🡅🡅🡅🡅🡅🡅🡅🡅🡅🡅🡅🡅🡅🡅

# ==============================================================================
# DUOI NAY LA CAC CONFIG DUOC TIM TU DONG (KHONG CAN SUA)  
# ==============================================================================

# Tu dong tim duong dan project (dua tren vi tri hien tai)
$Global:ProjectRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)

# Tu dong tim file solution (.sln)
$Global:SolutionPath = Get-ChildItem -Path $Global:ProjectRoot -Filter "*.sln" | Select-Object -First 1 | ForEach-Object { $_.FullName }

# Tu dong tim thu muc bin/Release cua MemoryPackSerializer
$Global:MemoryPackBinPath = Join-Path $Global:ProjectRoot "MemoryPackSerializer\bin\Release\netstandard2.1"

# Tu dong tim thu muc SharedModels bin/Release
$Global:SharedModelsBinPath = Join-Path $Global:ProjectRoot "AISmartRecall.SharedModels\bin\Release\netstandard2.1"

# Danh sach cac file DLL can copy
$Global:FilesToCopy = @(
    "AISmartRecall.SharedModels.dll",
    "MemoryPackSerializer.dll"
)

# ==============================================================================
# CAC TUY CHON KHAC
# ==============================================================================

# Ten file exe can chay (thong thuong khong can sua)
$Global:ExeName = "MemoryPackSerializer.exe"

# Che do build (Release hoac Debug)
$Global:BuildConfiguration = "Release"

# Target framework
$Global:TargetFramework = "netstandard2.1"

# ==============================================================================
# HAM KIEM TRA DUONG DAN
# ==============================================================================

function Test-ConfigPaths {
    $errors = @()
    
    Write-Host "=> Kiem tra cac duong dan..." -ForegroundColor Yellow
    
    # Kiem tra solution file
    if (-not (Test-Path $Global:SolutionPath)) {
        $errors += "Solution file khong ton tai: $Global:SolutionPath"
    } else {
        Write-Host "[OK] Solution file: $Global:SolutionPath" -ForegroundColor Green
    }
    
    # Kiem tra thu muc MemoryPackSerializer (chi can kiem tra khi khong build)
    if (-not (Test-Path $Global:MemoryPackBinPath)) {
        Write-Host "[CANH BAO] Thu muc MemoryPackSerializer chua ton tai: $Global:MemoryPackBinPath" -ForegroundColor Yellow
        Write-Host "          (Se duoc tao sau khi build)" -ForegroundColor Yellow
    } else {
        Write-Host "[OK] Thu muc MemoryPackSerializer: $Global:MemoryPackBinPath" -ForegroundColor Green
    }
    
    # Kiem tra thu muc SharedModels
    if (-not (Test-Path $Global:SharedModelsBinPath)) {
        Write-Host "[CANH BAO] Thu muc SharedModels chua ton tai: $Global:SharedModelsBinPath" -ForegroundColor Yellow
        Write-Host "          (Se duoc tao sau khi build)" -ForegroundColor Yellow
    } else {
        Write-Host "[OK] Thu muc SharedModels: $Global:SharedModelsBinPath" -ForegroundColor Green
    }
    
    # Kiem tra thu muc Unity DLL
    if (-not (Test-Path $Global:UnityDLLPath)) {
        Write-Host "[CANH BAO] Thu muc Unity DLL chua ton tai: $Global:UnityDLLPath" -ForegroundColor Yellow
        Write-Host "          (Se duoc tao tu dong)" -ForegroundColor Yellow
    } else {
        Write-Host "[OK] Thu muc Unity DLL: $Global:UnityDLLPath" -ForegroundColor Green
    }
    
    return $errors
}

# ==============================================================================
# HIEN THI THONG TIN CONFIG
# ==============================================================================

function Show-ConfigInfo {
    Write-Host ""
    Write-Host "=== AI SMART RECALL - THONG TIN CONFIG ===" -ForegroundColor Magenta
    Write-Host "Solution Path       : $Global:SolutionPath" -ForegroundColor Cyan
    Write-Host "MemoryPack Bin Path : $Global:MemoryPackBinPath" -ForegroundColor Cyan
    Write-Host "SharedModels Bin    : $Global:SharedModelsBinPath" -ForegroundColor Cyan
    Write-Host "Unity DLL Path      : $Global:UnityDLLPath" -ForegroundColor Cyan
    Write-Host "Build Config        : $Global:BuildConfiguration" -ForegroundColor Cyan
    Write-Host "Target Framework    : $Global:TargetFramework" -ForegroundColor Cyan
    Write-Host "Files to copy       : $($Global:FilesToCopy -join ', ')" -ForegroundColor Cyan
    Write-Host ""
}
