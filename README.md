# Lunara Software — Lunara Repairs

Proyecto final de POO en la IUE. MVP académico para administrar reparaciones de dispositivos tecnológicos.

## Tecnologías
- Backend: ASP.NET Core Web API, C#, MVC controllers y Entity Framework Core con SQLite.
- Frontend: React + Vite + JavaScript + Bootstrap 5.
- Comunicación: API REST / JSON.

> SQLite guarda los datos en un archivo local; no requiere instalar ni mantener un servidor de base de datos.

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
El backend conserva clientes, dispositivos, órdenes, historial, técnicos, cuentas y repuestos en la base de datos `backend/Lunara.Api/data/lunara.db`. Se crea automáticamente al iniciar, y los cambios permanecen al reiniciar la aplicación. Si encuentra los CSV locales anteriores y la base de datos todavía está vacía, los importa una sola vez; los archivos CSV originales no se modifican. Puedes cambiar la ruta mediante la variable `Database__Path`. Los datos CSV, la base de datos y los archivos con configuración local están excluidos de Git.

Para explorar registros dentro de VS Code, instala la extensión **SQLite** (`alexcvzz.vscode-sqlite`). En la paleta de comandos, ejecuta **SQLite: Open Database** y selecciona `backend/Lunara.Api/data/lunara.db`; las tablas aparecerán en **SQLite Explorer** y podrás usar **Show Table** para ver sus filas. El archivo [consultar-registros.sql3](backend/Lunara.Api/consultar-registros.sql3) incluye consultas de solo lectura para clientes, reparaciones, técnicos, repuestos y pedidos. Ábrelo, ejecuta **SQLite: Use Database** y selecciona la base; después usa **SQLite: Run Query** o `Cmd+Shift+Q`.

## Ejecutar backend
Desde la raíz:
```bash
cd backend/Lunara.Api
dotnet restore
dotnet run
```
La API mostrará la URL local al iniciar. El frontend usa `http://localhost:5080` por defecto; si ASP.NET Core muestra otro puerto, cambia `VITE_API_URL` en el frontend.

El directorio `data/` y la base de datos SQLite se crean automáticamente al arrancar el backend.

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
Email__Smtp__Host=smtp.gmail.com
Email__Smtp__Port=587
Email__Smtp__EnableSsl=true
Email__Smtp__Username=<cuenta-de-gmail>
Email__Smtp__Password=<clave-de-aplicacion-de-google>
Email__Smtp__FromAddress=<cuenta-de-gmail>
Email__Smtp__FromName=Lunara Repairs
```
En Desarrollo, si no se configura otra clave, el acceso de demostración del administrador es `admin` / `admin`; al iniciar se restablece esa clave y se invalidan las sesiones anteriores. En otros entornos configura `Admin:InitialPassword` con una clave segura de al menos 12 caracteres. Fuera de Desarrollo, esa clave solo se usa al crear la cuenta por primera vez. Si se pierde, el formulario de inicio permite restablecerla usando `Admin:RecoverySecret`. Guarda los secretos fuera del repositorio.

El acceso presenta perfiles separados de administrador, técnico y cliente. El administrador inicia con el usuario configurado (`admin` por defecto), no con correo. Para técnicos, el usuario se genera con la parte del correo anterior a `@` y se muestra al administrador al crear la cuenta; cada técnico recibe una contraseña inicial propia. Los usuarios de técnico deben ser únicos. En Desarrollo, el perfil de cliente consulta usando solo la cédula y no solicita contraseña; esta modalidad de demostración no es adecuada para producción porque la cédula no es un secreto.

Para una presentación local en un entorno distinto de Desarrollo, habilita explícitamente el acceso de demostración por cédula con `Client__AllowCedulaOnlyLogin=true`. Déjalo deshabilitado o sin configurar en cualquier entorno público.

En Desarrollo, la clave secreta para crear accesos de técnicos y cambiar sus contraseñas es `admin`. En otros entornos configura `Admin:TechnicianCreationSecret` mediante una variable de entorno y usa ese valor en el panel.

Para enviar presupuestos por Gmail, activa la verificación en dos pasos y crea una clave de aplicación en la cuenta remitente. Configura los valores `Email__Smtp__*` como variables de entorno del backend; no guardes la clave de aplicación en el repositorio. Usa el host `smtp.gmail.com`, puerto `587` y TLS. El presupuesto se envía al correo guardado para el cliente de esa orden e incluye versiones HTML y texto plano, diagnóstico y trabajo realizado. Si el SMTP no está disponible, el presupuesto queda guardado y se puede reenviar desde la orden. Una respuesta SMTP exitosa confirma que Gmail aceptó el mensaje para entrega, pero la recepción final depende del proveedor del destinatario; revisa también Spam o correo no deseado.

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
- El cliente consulta sus servicios, el avance del equipo, el problema reportado, el diagnóstico técnico, el trabajo realizado y las observaciones del historial; puede aprobar o rechazar el presupuesto cuando está pendiente. En Desarrollo, el cliente entra con su cédula, sin contraseña.
- Los permisos se verifican también en la API. Las cuentas usan contraseñas con hash y sesiones JWT; esta aplicación sigue siendo un MVP y requiere revisión de seguridad antes de producción.

## Funciones incluidas
- Dashboard con métricas.
- CRUD básico de clientes y dispositivos.
- Identificación única de clientes por cédula, búsqueda inmediata y consulta de sus reparaciones anteriores.
- Directorio de clientes de prueba y una reparación previa por cliente; el conjunto ampliado es opcional y se puede cargar desde un CSV local `lunara_clientes_prueba.csv`.
- Inventario de repuestos con categorías, marcas sugeridas, código único y control de existencias.
- Lista de pedidos de repuestos vinculados a un proveedor; marcar un pedido como comprado incrementa existencias o crea el repuesto en el inventario.
- Administración de cuentas de técnicos con contraseña secreta de creación, control de acceso y cálculo de carga de trabajo.
- Guía de mano de obra por tipo de equipo, marca y trabajo, editable por el administrador y consultable por el técnico con buscador. Las guías vacías reciben tarifas de demostración etiquetadas `PRUEBA`; son valores referenciales que deben validarse antes de cotizar.
- Métrica de ingresos acumulados por órdenes entregadas.
- Gráficos de órdenes por estado y unidades de repuestos por categoría.
- Guía de precios para técnicos con tarifas de mano de obra, repuestos del inventario, cantidades y total calculado.
- Gráfico mensual de ventas según las órdenes entregadas.
- Envío del presupuesto al correo del cliente por Gmail SMTP, con opción para reenviar.
- Autocompletado de marcas a partir de equipos ya registrados.
- Órdenes de reparación y asignación de técnico.
- Diagnóstico, presupuesto y cambios de estado.
- Portal de cliente con diagnóstico, trabajo realizado, observaciones de seguimiento y aprobación/rechazo del presupuesto.
- Historial de estados.
- Datos iniciales de ejemplo.

Las cuentas de demostración se pueden cargar desde un CSV local `lunara_cuentas_desarrollo.csv`; ese archivo no se incluye en el repositorio. Si falta, el backend crea el administrador a partir de `Admin:InitialPassword` o usa `admin` / `admin` en Desarrollo. Para producción configura la contraseña inicial mediante una variable de entorno.

## Límites de esta versión
- SQLite es adecuada para esta versión local y demostrativa; para un despliegue multiinstancia o alto volumen conviene usar PostgreSQL o SQL Server y configurar copias de seguridad.
- No incluye pagos, integración automática con proveedores ni despliegue. Los correos de presupuesto requieren configurar Gmail SMTP; las compras se registran y actualizan manualmente.
- Para producción configura secretos gestionados y completa pruebas y revisión de seguridad.
- Los comandos y el código deben probarse en el entorno donde se ejecuten.
