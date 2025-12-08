Write-Host "🚀 Iniciando Worker Service ETL..." -ForegroundColor Cyan

# Verificar carpetas
if (!(Test-Path "staging")) { mkdir staging }
if (!(Test-Path "logs")) { mkdir logs }
if (!(Test-Path "Data")) { mkdir Data }

# Verificar CSV
if (!(Test-Path "Data/encuestas.csv")) {
    Write-Host "⚠️  Archivo Data/encuestas.csv no encontrado" -ForegroundColor Yellow
    Write-Host "   Por favor cree el archivo con datos de ejemplo" -ForegroundColor Yellow
    exit 1
}

# Ejecutar
cd src/SistemaOpinionesETL.Presentation
dotnet run