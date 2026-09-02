# Cloudflare deploy (tek seferlik / güncelleme)

param(
    [switch]$SkipLogin
)

$ErrorActionPreference = "Stop"
Set-Location $PSScriptRoot

if (-not $SkipLogin) {
    npx wrangler whoami | Out-Null
    if ($LASTEXITCODE -ne 0) {
        npx wrangler login
    }
}

if (-not (Test-Path ".upload-api-key.local")) {
    $apiKey = -join ((48..57) + (65..90) + (97..122) | Get-Random -Count 48 | ForEach-Object { [char]$_ })
    Set-Content -Path ".upload-api-key.local" -Value $apiKey -NoNewline
    Write-Host "Yeni upload API key oluşturuldu: .upload-api-key.local"
}

$apiKey = Get-Content ".upload-api-key.local" -Raw
$apiKey.Trim() | npx wrangler secret put UPLOAD_API_KEY
npx wrangler deploy

$workerUrl = "https://rck-is-takibi-api.rck-istakibi.workers.dev/api/data"
Write-Host ""
Write-Host "Worker URL: $workerUrl"
Write-Host "Upload key dosyası: $PSScriptRoot\.upload-api-key.local"
Write-Host "Bu key'i uygulama Ayarlar > Cloudflare Upload API Key alanına yapıştırın."
