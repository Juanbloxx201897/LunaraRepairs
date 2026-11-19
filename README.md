# Lunara Software — Lunara Repairs

MVP académico para administrar reparaciones de dispositivos tecnológicos.

## Tecnologías
- Backend: ASP.NET Core Web API, C#, MVC controllers y Entity Framework Core con persistencia CSV local.
- Frontend: React + Vite + JavaScript + Bootstrap 5.
- Comunicación: API REST / JSON.

> La persistencia actual usa CSV locales para facilitar la ejecución y demostración sin configurar un servidor de base de datos.

## Requisitos
- .NET SDK 8 o superior compatible con el proyecto.
- Node.js LTS y npm.
- Visual Studio Code (opcional).

Comprueba:
```bash
dotnet --version
node --version
npm --version
```

## Persistencia de datos
El backend conserva clientes, dispositivos, órdenes, historial, técnicos y repuestos en archivos CSV dentro de `backend/Lunara.Api/Data/`. La primera ejecución carga los datos de demostración; a partir de ahí, los cambios de la API se guardan automáticamente y se mantienen al reiniciar. No edites esos archivos mientras el backend está en ejecución. Los CSV de datos, cuentas y clientes son locales y están excluidos de Git.

## Ejecutar backend
Desde la raíz:
```bash
cd backend/Lunara.Api
dotnet restore
dotnet run
```
La API mostrará la URL local al iniciar. El frontend usa `http://localhost:5080` por defecto; si ASP.NET Core muestra otro puerto, cambia `VITE_API_URL` en el frontend.

El directorio `data/` se crea automáticamente al arrancar el backend.

Configura las claves del servidor mediante variables de entorno (no las guardes en el repositorio). Define los valores JWT y los secretos de administración:
```text
Jwt__Secret=<secreto de al menos 32 bytes>
Jwt__Issuer=<emisor>
Jwt__Audience=<audiencia>
Admin__Username=admin
Admin__InitialPassword=<contraseña inicial segura, al menos 12 caracteres>
Admin__RecoverySecret=<secreto privado para recuperar el acceso de admin>
Admin__TechnicianCreationSecret=<secreto privado para crear técnicos>
Cors__AllowedOrigins__0=http://localhost:5173
Cors__AllowedOrigins__1=http://127.0.0.1:5173
```
En Desarrollo, si no se configura otra clave, el acceso de demostración del administrador es `admin` / `admin`; al iniciar se restablece esa clave y se invalidan las sesiones anteriores. En otros entornos configura `Admin:InitialPassword` con una clave segura de al menos 12 caracteres. Fuera de Desarrollo, esa clave solo se usa al crear la cuenta por primera vez. Si se pierde, el formulario de inicio permite restablecerla usando `Admin:RecoverySecret`. Guarda los secretos fuera del repositorio.

El acceso presenta perfiles separados de administrador, técnico y cliente. El administrador inicia con el usuario configurado (`admin` por defecto), no con correo. Para técnicos, el usuario se genera con la parte del correo anterior a `@` y se muestra al administrador al crear la cuenta; cada técnico recibe una contraseña inicial propia. Los usuarios de técnico deben ser únicos. En Desarrollo, el perfil de cliente consulta usando solo la cédula y no solicita contraseña; esta modalidad de demostración no es adecuada para producción porque la cédula no es un secreto.

En Desarrollo, la clave secreta para crear accesos de técnicos y cambiar sus contraseñas es `admin`. En otros entornos configura `Admin:TechnicianCreationSecret` mediante una variable de entorno y usa ese valor en el panel.

## Ejecutar frontend
En otra terminal:
```bash
cd frontend/lunara-web
npm install
npm run dev
```
Abre la dirección que indique Vite (normalmente `http://localhost:5173`).

## Perfiles y permisos
- El administrador registra y asigna reparaciones, consulta métricas e ingresos, administra técnicos y gestiona inventario y pedidos de repuestos.
- En Desarrollo, el acceso de demostración del administrador es `admin` / `admin`; en producción define una contraseña segura en el servidor.
- Cada técnico ve solo sus órdenes asignadas y actualiza diagnóstico, trabajo y estado del equipo.
- El cliente consulta sus servicios y el avance del equipo; no aprueba ni rechaza presupuestos. El administrador revisa y aprueba o rechaza cada presupuesto. En Desarrollo, el cliente entra con su cédula, sin contraseña.
- Los permisos se verifican también en la API. Las cuentas usan contraseñas con hash y sesiones JWT; esta aplicación sigue siendo un MVP y requiere revisión de seguridad antes de producción.

## Funciones incluidas
- Dashboard con métricas.
- CRUD básico de clientes y dispositivos.
- Identificación única de clientes por cédula, búsqueda inmediata y consulta de sus reparaciones anteriores.
- Directorio de clientes de prueba y una reparación previa por cliente; el conjunto ampliado es opcional y se puede cargar desde un CSV local `lunara_clientes_prueba.csv`.
- Inventario de repuestos con categorías, marcas sugeridas, código único y control de existencias.
- Lista de pedidos de repuestos vinculados a un proveedor; marcar un pedido como comprado incrementa existencias o crea el repuesto en el inventario.
- Administración de cuentas de técnicos con contraseña secreta de creación, control de acceso y cálculo de carga de trabajo.
- Métrica de ingresos acumulados por órdenes entregadas.
- Gráficos de órdenes por estado y unidades de repuestos por categoría.
- Órdenes de reparación y asignación de técnico.
- Diagnóstico, presupuesto y cambios de estado.
- Portal de cliente con aprobación/rechazo de presupuesto.
- Historial de estados.
- Datos iniciales de ejemplo.

Las cuentas de demostración se pueden cargar desde un CSV local `lunara_cuentas_desarrollo.csv`; ese archivo no se incluye en el repositorio. Si falta, el backend crea el administrador a partir de `Admin:InitialPassword` o usa `admin` / `admin` en Desarrollo. Para producción configura la contraseña inicial mediante una variable de entorno.

## Límites de esta versión
- La persistencia usa archivos CSV y no está diseñada para concurrencia de producción.
- No incluye pagos, integración automática con proveedores, notificaciones por correo ni despliegue. Las compras se registran y actualizan manualmente.
- Para producción se recomienda migrar a una base de datos transaccional, configurar secretos gestionados y completar pruebas y revisión de seguridad.
- Los comandos y el código deben probarse en el entorno donde se ejecuten; no se afirma que estén verificados contra tu servidor SQL.
