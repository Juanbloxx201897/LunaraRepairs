# Endpoints principales

- `GET /api/clients` (opcionalmente `?cedula=...` o `?search=...`)
- `GET /api/clients/{id}`
- `GET /api/clients/{id}/repairs`
- `GET /api/devices`
- `POST /api/devices`
- `GET /api/spareparts`
- `POST /api/spareparts`
- `GET /api/technicians`
- `GET /api/repairs`
- `GET /api/repairs/{id}`
- `POST /api/repairs`
- `POST /api/repairs/register` (cliente existente o nuevo, equipo y reparación en una sola operación)
- `PUT /api/repairs/{id}/diagnosis`
- `PUT /api/repairs/{id}/status`
- `PUT /api/repairs/{id}/work`
- `GET /api/repairs/stats`

Los repuestos reciben un código de identificación único generado por el servidor.
El formulario sugiere las marcas y categorías ya registradas y permite agregar nuevas.
La cédula identifica de manera única a cada cliente; el historial de reparaciones se consulta por el ID del cliente.
Cada reparación nueva se registra con un cliente, un equipo y un teléfono con indicativo internacional para clientes nuevos (por ejemplo, `+57`). El CSV de prueba crea una reparación previa completada para cada cliente incluido.

En desarrollo, Swagger está habilitado para explorar los endpoints.
