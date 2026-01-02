# PriceLists.Maui

## Qué hace este PR
- Importa archivos `.xlsx`, pide el nombre de la lista (por defecto el nombre del archivo) y persiste cada importación como una lista independiente en SQLite (PriceList + PriceItems).
- Muestra las listas importadas en `ListsPage` y permite navegar a `ListDetailPage` para buscar y ver los productos (Código, Descripción, Precio) de cada lista.
- Mantiene un flujo opcional de preview de hasta 20 filas antes de guardar, reutilizando el importador de Excel.

## Cómo correr
1. Restaurar paquetes y compilar la solución:
   ```bash
   dotnet build PriceLists.Maui.sln
   ```
2. Ejecutar en un emulador/dispositivo Android (requiere SDK/entorno de MAUI configurado):
   ```bash
   dotnet build -t:Run -f net9.0-android PriceLists.Maui/PriceLists.Maui.csproj
   ```

## Base de datos SQLite
- El archivo se crea automáticamente (migraciones en runtime) en:
  ```
  {AppDataDirectory}/pricelists.db
  ```
  donde `AppDataDirectory` es provisto por `Microsoft.Maui.Storage.FileSystem`.

## Migraciones (opcional)
Para generar/actualizar migraciones desde la raíz del repo:
```bash
dotnet ef migrations add InitialSqlite -p PriceLists.Infrastructure/PriceLists.Infrastructure.csproj -s PriceLists.Maui/PriceLists.Maui.csproj
dotnet ef database update -p PriceLists.Infrastructure/PriceLists.Infrastructure.csproj -s PriceLists.Maui/PriceLists.Maui.csproj
```
