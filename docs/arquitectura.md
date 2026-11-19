# Arquitectura

- **Views:** páginas y componentes React.
- **Controllers:** controladores ASP.NET Core que exponen API REST.
- **Models:** entidades de dominio.
- **Services:** reglas de negocio del flujo de reparación.
- **Data:** DbContext e inicialización de datos.
- **Persistence:** Entity Framework Core + SQL Server.

El frontend no accede directamente a la base de datos: consume la API JSON. La selección de perfiles es únicamente una función de demostración, no un control de acceso.
