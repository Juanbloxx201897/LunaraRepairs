# Endpoints principales

- `POST /api/auth/login` (inicio de sesión por perfil)
- `POST /api/auth/admin/recover` (recuperación del administrador)
- `GET /api/clients` (opcionalmente `?cedula=...` o `?search=...`)
- `GET /api/clients/{id}`
- `GET /api/clients/{id}/repairs`
- `GET /api/devices`
- `POST /api/devices`
- `GET /api/spareparts`
- `POST /api/spareparts`
- `GET /api/spareparts/catalog` (técnicos y administración)
- `GET /api/spareparts/purchases`
- `POST /api/spareparts/purchases`
- `PUT /api/spareparts/purchases/{id}/purchased`
- `GET /api/technicians`
- `POST /api/technicians`
- `PUT /api/technicians/{id}/access`
- `GET /api/repairs`
- `GET /api/repairs/{id}`
- `POST /api/repairs`
- `POST /api/repairs/register` (cliente existente o nuevo, equipo y reparación en una sola operación)
- `PUT /api/repairs/{id}/technician`
- `PUT /api/repairs/{id}/diagnosis`
- `POST /api/repairs/{id}/budget-email`
- `PUT /api/repairs/{id}/status`
- `PUT /api/repairs/{id}/work`
- `GET /api/repairs/stats`
- `GET /api/laborprices` (tarifas activas para técnicos; todas para administradores)
- `GET /api/laborprices/{id}`
- `POST /api/laborprices` (administrador)
- `PUT /api/laborprices/{id}` (administrador)

Los endpoints, excepto el inicio de sesión y la recuperación, requieren una sesión autenticada. Las rutas administrativas y técnicas validan además el rol correspondiente. Los repuestos reciben un código de identificación único generado por el servidor.
El formulario sugiere las marcas y categorías ya registradas y permite agregar nuevas.
La cédula identifica de manera única a cada cliente; el historial de reparaciones se consulta por el ID del cliente.
Cada reparación nueva se registra con un cliente, un equipo y un teléfono con indicativo internacional para clientes nuevos (por ejemplo, `+57`). El CSV de prueba crea una reparación previa completada para cada cliente incluido.
La guía de mano de obra se filtra por equipo, marca y tipo de trabajo. Las tarifas iniciales marcadas `PRUEBA` son valores referenciales para demostración.

En desarrollo, Swagger está habilitado para explorar los endpoints.
