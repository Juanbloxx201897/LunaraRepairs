# Base de datos

El esquema se administra con Entity Framework Core. La primera migración se genera y aplica desde `backend/Lunara.Api`:

```bash
dotnet tool install --global dotnet-ef
dotnet ef migrations add InitialCreate
dotnet ef database update
```

La conexión debe apuntar a una instancia SQL Server accesible. No se incluye ninguna contraseña real.
