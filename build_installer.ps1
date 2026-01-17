# Build Release Script for Honor Bridge

Write-Host "Building Honor Bridge (Self-Contained)..." -ForegroundColor Cyan

# 1. Clean
dotnet clean
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

# 2. Publish (Self-Contained, Single File)
# Uses the WinX64 profile we created
dotnet publish src/HonorBridge.Client.Wpf/HonorBridge.Client.Wpf.csproj /p:PublishProfile=WinX64
if ($LASTEXITCODE -ne 0) { 
    Write-Error "Build Failed!"
    exit $LASTEXITCODE 
}

Write-Host "Build Complete!" -ForegroundColor Green

# 3. Check for Inno Setup
$iscc = "C:\Program Files (x86)\Inno Setup 6\ISCC.exe"
if (Test-Path $iscc) {
    Write-Host "Compiling Installer..." -ForegroundColor Cyan
    & $iscc "Installer\setup.iss"
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "Installer Created Successfully!" -ForegroundColor Green
        Write-Host "File is located in: Installer\Output\HonorBridgeSetup_v1.0.exe"
    }
} else {
    Write-Warning "Inno Setup Compiler (ISCC.exe) not found at default location."
    Write-Host "To create the installable setup.exe:"
    Write-Host "1. Download and Install Inno Setup: https://jrsoftware.org/isdl.php"
    Write-Host "2. Double click 'Installer\setup.iss' to open it."
    Write-Host "3. Click Compile."
}

Write-Host "Press any key to exit..."
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
