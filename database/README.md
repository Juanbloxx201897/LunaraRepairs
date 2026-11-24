# Base de datos

Lunara usa SQLite y Entity Framework Core. La base local se crea y migra automáticamente al iniciar la API en `backend/Lunara.Api/data/lunara.db`; puedes cambiar la ruta mediante `Database__Path`. Los datos CSV heredados solo se importan si la base aún no contiene datos.

## Aplicar migraciones manualmente

Desde la raíz del proyecto:

```bash
dotnet ef database update \
  --project backend/Lunara.Api/Lunara.Api.csproj \
  --startup-project backend/Lunara.Api/Lunara.Api.csproj
```

Al modificar entidades, crea una migración nueva; no vuelvas a generar la migración inicial:

```bash
dotnet ef migrations add NombreDescriptivo \
  --project backend/Lunara.Api/Lunara.Api.csproj \
  --startup-project backend/Lunara.Api/Lunara.Api.csproj
```

Para explorar la base en VS Code, usa la extensión SQLite y abre `backend/Lunara.Api/data/lunara.db`. Las bases locales y credenciales están excluidas de Git; conserva una copia de seguridad antes de reemplazar o borrar datos.
