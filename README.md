# PriceLists.Maui

## Qué hace este PR
- Permite seleccionar un archivo `.xlsx` desde la tablet usando el `FilePicker`.
- Usa un servicio de infraestructura basado en ClosedXML para detectar la fila de encabezado y mapear columnas (código, descripción, precio).
- Muestra un preview de hasta 20 filas, incluyendo metadatos de la hoja detectada, sin persistir datos en SQLite.

## Cómo correr
1. Restaurar paquetes y compilar la solución:
   ```bash
   dotnet build PriceLists.Maui.sln
   ```
2. Ejecutar en un emulador/dispositivo Android (requiere SDK/entorno de MAUI configurado):
   ```bash
   dotnet build -t:Run -f net9.0-android PriceLists.Maui/PriceLists.Maui.csproj
   ```
