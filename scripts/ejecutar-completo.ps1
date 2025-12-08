# ========================================
# Script de Ejecución Completa del Sistema ETL
# ========================================

Write-Host "=========================================" -ForegroundColor Cyan
Write-Host "  SISTEMA OPINIONES ETL - EJECUCIÓN" -ForegroundColor Cyan
Write-Host "=========================================" -ForegroundColor Cyan

# Verificar .NET SDK
Write-Host "`n📦 Verificando .NET SDK..." -ForegroundColor Yellow
try {
    $dotnetVersion = dotnet --version
    Write-Host "  ✓ .NET SDK $dotnetVersion instalado" -ForegroundColor Green
} catch {
    Write-Host "  ❌ .NET SDK no encontrado. Por favor instale .NET 8 SDK" -ForegroundColor Red
    exit 1
}

# Crear carpetas necesarias
Write-Host "`n📁 Creando carpetas necesarias..." -ForegroundColor Yellow
$carpetas = @("Data", "staging", "staging/procesados", "logs")
foreach ($carpeta in $carpetas) {
    if (!(Test-Path $carpeta)) {
        New-Item -ItemType Directory -Path $carpeta -Force | Out-Null
        Write-Host "  ✓ $carpeta creada" -ForegroundColor Green
    } else {
        Write-Host "  ✓ $carpeta ya existe" -ForegroundColor Gray
    }
}

# Verificar archivo CSV
Write-Host "`n📄 Verificando archivos de datos..." -ForegroundColor Yellow
if (!(Test-Path "Data/encuestas.csv")) {
    Write-Host "  ⚠️  Data/encuestas.csv no encontrado" -ForegroundColor Yellow
    Write-Host "     Creando archivo de ejemplo..." -ForegroundColor Yellow
    
    $csvContent = @"
ProductId,CustomerId,CreatedAt,CommentText,Rating,Source
PROD001,CUST001,2024-12-01,Excelente producto muy satisfecho,5,Encuesta Interna
PROD002,CUST002,2024-12-01,Buena calidad pero caro,4,Encuesta Interna
PROD003,CUST003,2024-12-02,No cumplió expectativas,2,Encuesta Interna
"@
    
    Set-Content -Path "Data/encuestas.csv" -Value $csvContent
    Write-Host "  ✓ Archivo de ejemplo creado" -ForegroundColor Green
} else {
    Write-Host "  ✓ Data/encuestas.csv encontrado" -ForegroundColor Green
}

# Restaurar paquetes
Write-Host "`n🔄 Restaurando paquetes NuGet..." -ForegroundColor Yellow
dotnet restore
if ($LASTEXITCODE -ne 0) {
    Write-Host "  ❌ Error restaurando paquetes" -ForegroundColor Red
    exit 1
}
Write-Host "  ✓ Paquetes restaurados" -ForegroundColor Green

# Compilar solución
Write-Host "`n🔨 Compilando solución..." -ForegroundColor Yellow
dotnet build --configuration Release --no-restore
if ($LASTEXITCODE -ne 0) {
    Write-Host "  ❌ Error compilando solución" -ForegroundColor Red
    exit 1
}
Write-Host "  ✓ Solución compilada exitosamente" -ForegroundColor Green

# Menú de opciones
Write-Host "`n=========================================" -ForegroundColor Cyan
Write-Host "  ¿QUÉ DESEA EJECUTAR?" -ForegroundColor Cyan
Write-Host "=========================================" -ForegroundColor Cyan
Write-Host "  1. Worker Service (ETL en segundo plano)" -ForegroundColor White
Write-Host "  2. API REST (Monitoreo y control)" -ForegroundColor White
Write-Host "  3. Ambos (Worker + API en paralelo)" -ForegroundColor White
Write-Host "  4. Solo compilar (sin ejecutar)" -ForegroundColor White
Write-Host "  5. Salir" -ForegroundColor White
Write-Host "=========================================" -ForegroundColor Cyan

$opcion = Read-Host "`nSeleccione una opción (1-5)"

switch ($opcion) {
    "1" {
        Write-Host "`n▶️  Iniciando Worker Service..." -ForegroundColor Green
        Write-Host "   Presione Ctrl+C para detener" -ForegroundColor Gray
        Set-Location "src/SistemaOpinionesETL.Presentation"
        dotnet run
    }
    "2" {
        Write-Host "`n▶️  Iniciando API REST..." -ForegroundColor Green
        Write-Host "   API: https://localhost:7001" -ForegroundColor Yellow
        Write-Host "   Swagger: https://localhost:7001/swagger" -ForegroundColor Yellow
        Write-Host "   Presione Ctrl+C para detener" -ForegroundColor Gray
        Set-Location "src/SistemaOpinionesETL.API"
        dotnet run
    }
    "3" {
        Write-Host "`n▶️  Iniciando Worker + API..." -ForegroundColor Green
        
        # Iniciar Worker en segundo plano
        $workerJob = Start-Job -ScriptBlock {
            Set-Location $using:PWD
            Set-Location "src/SistemaOpinionesETL.Presentation"
            dotnet run
        }
        
        Start-Sleep -Seconds 3
        
        # Iniciar API en segundo plano
        $apiJob = Start-Job -ScriptBlock {
            Set-Location $using:PWD
            Set-Location "src/SistemaOpinionesETL.API"
            dotnet run
        }
        
        Write-Host "`n✅ Servicios iniciados:" -ForegroundColor Green
        Write-Host "   🔧 Worker ETL: Procesando cada 5 minutos" -ForegroundColor Yellow
        Write-Host "   🌐 API REST: https://localhost:7001" -ForegroundColor Yellow
        Write-Host "   📖 Swagger: https://localhost:7001/swagger" -ForegroundColor Yellow
        Write-Host "`n   Presione cualquier tecla para detener ambos servicios..." -ForegroundColor Gray
        
        $null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
        
        Write-Host "`n🛑 Deteniendo servicios..." -ForegroundColor Red
        Stop-Job -Job $workerJob, $apiJob
        Remove-Job -Job $workerJob, $apiJob
        Write-Host "   ✓ Servicios detenidos" -ForegroundColor Green
    }
    "4" {
        Write-Host "`n✅ Compilación completada. No se ejecutó ningún servicio." -ForegroundColor Green
    }
    "5" {
        Write-Host "`n👋 Saliendo..." -ForegroundColor Cyan
        exit 0
    }
    default {
        Write-Host "`n❌ Opción inválida" -ForegroundColor Red
    }
}

Write-Host "`n=========================================" -ForegroundColor Cyan
Write-Host "  Ejecución finalizada" -ForegroundColor Cyan
Write-Host "=========================================" -ForegroundColor Cyan