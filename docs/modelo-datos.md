# Modelo de datos

- Client (cédula única) 1:N Device.
- Client 1:N RepairOrder.
- Device 1:N RepairOrder (en este MVP se valida que el dispositivo pertenezca al cliente seleccionado).
- Technician 1:N RepairOrder opcional.
- RepairOrder 1:N RepairStatusHistory.

La cédula del cliente es única y su teléfono se guarda con indicativo internacional. El registro de un cliente nuevo crea su equipo y su primera RepairOrder en una sola solicitud; por eso el directorio no permite crear clientes sin reparación.

La entidad RepairOrder contiene diagnóstico, presupuesto, trabajo realizado y estado actual. El historial conserva los cambios de estado, y las órdenes asociadas a cada Client forman el historial de reparaciones de esa persona.
