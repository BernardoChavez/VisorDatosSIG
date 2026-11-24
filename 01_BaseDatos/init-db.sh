#!/usr/bin/env bash
set -e

echo "=========================================================="
echo ">> Verificando disponibilidad de SQL Server..."
echo "=========================================================="

SQLCMD="/opt/mssql-tools18/bin/sqlcmd"
if [ ! -f "$SQLCMD" ]; then
    SQLCMD="/opt/mssql-tools/bin/sqlcmd"
fi

for i in {1..60}; do
    if $SQLCMD -S sqlserver -U sa -P "${MSSQL_SA_PASSWORD}" -C -Q "SELECT 1" > /dev/null 2>&1; then
        echo ">> SQL Server está listo y respondiendo."
        break
    fi
    echo ">> Esperando que SQL Server termine de iniciar (intento $i/60)..."
    sleep 2
done

# Verificar si la base de datos ya fue creada
DB_EXISTE=$($SQLCMD -S sqlserver -U sa -P "${MSSQL_SA_PASSWORD}" -C -h -1 -Q "SET NOCOUNT ON; SELECT COUNT(*) FROM sys.databases WHERE name='VisorDatosSIG_BD'" 2>/dev/null | tr -d '[:space:]')

if [ "$DB_EXISTE" = "1" ]; then
    echo ">> La base de datos 'VisorDatosSIG_BD' ya existe. Omitiendo inicialización."
else
    echo ">> Inicializando base de datos 'VisorDatosSIG_BD'..."
    
    echo ">> [1/4] Ejecutando 01_CrearBD.sql..."
    $SQLCMD -S sqlserver -U sa -P "${MSSQL_SA_PASSWORD}" -C -i /scripts/01_CrearBD.sql
    
    echo ">> [2/4] Ejecutando 02_Objetos.sql..."
    $SQLCMD -S sqlserver -U sa -P "${MSSQL_SA_PASSWORD}" -C -i /scripts/02_Objetos.sql
    
    echo ">> [3/4] Ejecutando 03_Seguridad.sql..."
    $SQLCMD -S sqlserver -U sa -P "${MSSQL_SA_PASSWORD}" -C -i /scripts/03_Seguridad.sql
    
    echo ">> [4/5] Ejecutando 04_DatosPrueba.sql..."
    $SQLCMD -S sqlserver -U sa -P "${MSSQL_SA_PASSWORD}" -C -i /scripts/04_DatosPrueba.sql
    
    echo ">> [5/5] Ejecutando 05_ModuloAguaCortesReconexiones.sql..."
    $SQLCMD -S sqlserver -U sa -P "${MSSQL_SA_PASSWORD}" -C -i /scripts/05_ModuloAguaCortesReconexiones.sql
    
    echo "=========================================================="
    echo ">> ¡Base de datos VisorDatosSIG_BD inicializada exitosamente!"
    echo "=========================================================="
fi
