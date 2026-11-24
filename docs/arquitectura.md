# Arquitectura

- **Views:** páginas y componentes React.
- **Controllers:** controladores ASP.NET Core que exponen API REST.
- **Models:** entidades de dominio.
- **Services:** reglas de negocio del flujo de reparación.
- **Data:** DbContext e inicialización de datos.
- **Persistence:** Entity Framework Core + SQLite.

El frontend no accede directamente a la base de datos: consume la API JSON. La API autentica las sesiones con JWT y aplica permisos por rol; en las operaciones críticas, el servidor valida además la propiedad y el estado de cada recurso.
