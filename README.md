# Abank · API de usuarios

API REST para gestionar los usuarios de una aplicación y su acceso, con login por teléfono y JWT. Hecha en .NET 10 con Clean Architecture y persistencia con **Dapper** sobre **PostgreSQL**.

```
Backend/        API .NET 10 (Clean Architecture) y sus pruebas
Base de datos/  scripts de creación de objetos y datos iniciales
postman/        colección y entorno, con verificaciones
```

## Probarlo en línea, sin instalar nada

La API está desplegada en **https://abank-api-1nsm.onrender.com**:

| | URL |
|---|---|
| Documentación interactiva (Scalar) | https://abank-api-1nsm.onrender.com/scalar/v1 |
| Especificación OpenAPI | https://abank-api-1nsm.onrender.com/openapi/v1.json |
| Salud | https://abank-api-1nsm.onrender.com/health |

Se entra con el mismo usuario de ejemplo: teléfono `70000001`, contraseña `Abank2026*`.

Está en el plan gratuito de Render: tras 15 minutos sin uso la API se suspende, y **la primera petición puede tardar cerca de un minuto** mientras despierta. Las siguientes responden normal.

La colección de Postman corre contra el despliegue cambiando una variable:

```bash
npx newman run postman/Abank.postman_collection.json -e postman/Abank.postman_environment.json --env-var baseUrl=https://abank-api-1nsm.onrender.com
```

---

## Levantarlo

Requisito: Docker.

```bash
docker compose up --build
```

La primera vez construye las imágenes y tarda unos minutos. Postgres ejecuta los scripts de `Base de datos/` al crear el volumen, así que la API arranca con usuarios de ejemplo.

| Servicio | URL |
|---|---|
| API: documentación interactiva (Scalar) | http://localhost:5080/scalar/v1 |
| API: especificación OpenAPI | http://localhost:5080/openapi/v1.json |
| API: salud | http://localhost:5080/health |
| PostgreSQL | `localhost:5433` · base `abank` · usuario `abank` · contraseña `Abank#Dev2026` |

**Usuario para entrar:** teléfono `70000001`, contraseña `Abank2026*`.

Los demás usuarios de ejemplo (`70000002` a `70000005`) tienen la contraseña `Usuario2026*`. El `70000005` está inactivo, para ver que no puede entrar.

Para empezar de cero (borra la base y vuelve a correr los scripts):

```bash
docker compose down -v
```

Los puertos no son los habituales (5080 para la API y 5433 para Postgres) para no chocar con servicios que ya estén corriendo en la máquina. La contraseña de la base y la llave del JWT se pueden cambiar con las variables `DB_PASSWORD` y `JWT_SIGNING_KEY`.

### Sin Docker

Requiere el **SDK de .NET 10**. Con Postgres corriendo en `localhost:5433` y los scripts de `Base de datos/` ya ejecutados en orden:

```bash
cd Backend
dotnet run --project src/Abank.Api
```

La API queda en http://localhost:5080. La cadena de conexión está en `Backend/src/Abank.Api/appsettings.json`.

Se usa .NET 10 y no 8 porque el soporte de .NET 8 termina en noviembre de 2026; .NET 10 es la versión LTS vigente y cumple el «.NET 8+» del enunciado.

---

## Probarlo

- **Scalar:** todos los endpoints documentados. Haz login, copia el `access_token` y pégalo en *Authentication → Bearer*.
- **Postman:** importa `postman/Abank.postman_collection.json` y `postman/Abank.postman_environment.json`, elige el entorno *Abank · Docker local* y corre la colección completa (*Run collection*). Son 13 peticiones con 29 verificaciones: el login guarda el token y el alta guarda el id que usan las siguientes. También corre por consola:

  ```bash
  npx newman run postman/Abank.postman_collection.json -e postman/Abank.postman_environment.json
  ```

- **Pruebas automáticas** (requieren Docker para las de integración):

  ```bash
  cd Backend
  dotnet test
  ```

  Son 40 unitarias y 21 de integración. Las de integración levantan un PostgreSQL real con Testcontainers y le ejecutan **los mismos scripts** de `Base de datos/`, así que también prueban la secuencia, el CHECK y los triggers.

### Cómo está armada la colección de Postman

- **El token lo pone la colección.** El *Login* guarda el `access_token` en la variable `{{token}}`, y la colección lo manda como `Bearer {{token}}` en todas las peticiones. Para probar una petición suelta, corre primero el *Login*.
- **Algunas van sin token a propósito.** Los tres logins y *Listar sin token* están en *No Auth*; la última existe para comprobar que la API responde 401.
- **El alta usa un teléfono al azar.** *CreateUser* manda `"telefono": "{{telefonoNuevo}}"`, y su script *Pre-request* genera un número nuevo antes de cada envío, para que la colección se pueda correr varias veces sin chocar con «teléfono repetido». Por eso cambiar esa variable no tiene efecto: el script la reemplaza.
- **Para probar tus propios datos**, escríbelos directo en el *Body*, en lugar de la variable. Para ver las validaciones está *CreateUser con datos inválidos*, que espera un 400 con el error de cada campo.
- **Para ver qué se envió de verdad**, abre la consola de Postman (*View → Show Postman Console*, `Ctrl+Alt+C`). Muestra el cuerpo y las cabeceras reales de cada petición, con las variables ya reemplazadas.

---

## Endpoints

| Método | Ruta | Token | Respuesta |
|---|---|---|---|
| POST | `/api/v1/auth/login` | No | 200 con `user`, `access_token`, `token_type`, `expires_in` |
| GET | `/api/v1/users` | Sí | 200 con un arreglo; el total va en la cabecera `X-Total-Count` |
| GET | `/api/v1/users/{id_user}` | Sí | 200 con el usuario |
| POST | `/api/v1/users` | Sí | 201 con el usuario y la cabecera `Location` |
| PUT | `/api/v1/users/{id_user}` | Sí | 200 con el usuario actualizado |
| DELETE | `/api/v1/users/{id_user}` | Sí | 200 con el usuario, ya inactivo |

`GET /api/v1/users` acepta parámetros opcionales: `buscar` (en nombres, apellidos, email y teléfono), `estado` (`A` o `I`), `pagina` (desde 1) y `tamano` (de 1 a 100; por defecto 50). Sin parámetros responde como el ejemplo del enunciado.

Los errores salen en formato **ProblemDetails (RFC 9457)**, con un `errorCode` estable y el `traceId` de la petición, el mismo que aparece en los logs. Las validaciones devuelven **todos** los campos con problema de una vez:

```json
{
  "title": "Validation.Failed",
  "status": 400,
  "errors": {
    "telefono": ["El teléfono debe tener entre 7 y 15 dígitos, con un '+' opcional al inicio."],
    "password": ["La contraseña debe tener entre 8 y 64 caracteres."]
  },
  "errorCode": "Validation.Failed",
  "traceId": "00-20c20037630f30c7a8b4c6db9b4c5ea5-e714f0f90e9a9a2a-00"
}
```

| Código | Cuándo |
|---|---|
| 400 | Datos inválidos, JSON mal formado, id del cuerpo distinto al de la ruta |
| 401 | Sin token, token inválido o vencido, credenciales incorrectas, usuario desactivado después del login |
| 403 | Login de un usuario inactivo |
| 404 | El usuario no existe |
| 409 | Teléfono o email repetidos; intento de desactivar la propia cuenta |
| 429 | Más de 10 intentos de login por minuto desde la misma IP |

---

## Lo que el enunciado dejó a criterio

El enunciado deja varios puntos abiertos y algunos ejemplos se contradicen entre sí. Estas son las decisiones tomadas y su motivo.

| Tema | Decisión | Motivo |
|---|---|---|
| El login de ejemplo **devuelve el hash del password** | No se devuelve, ni en el login ni en ningún otro endpoint | Exponer el hash permite atacarlo sin conexión. En un banco no es negociable |
| `GetUsers` no está marcado como protegido | **Sí** exige token | Listar los datos personales de todos los usuarios sin autenticación sería una fuga. Los demás endpoints de usuarios ya lo exigen |
| El login de ejemplo usa `address` y los demás `direccion` | `direccion` en todos | Un mismo dato con dos nombres obliga al cliente a mapear según el endpoint |
| `DELETE` responde 200 y existe el campo `Estado` | **Baja lógica**: el usuario pasa a `I` y la respuesta lo trae | Conserva el historial y lo que cuelga del usuario. Repetir el DELETE responde lo mismo |
| Un usuario inactivo | No puede iniciar sesión. Si se desactiva con la sesión abierta, su token deja de servir en ese momento | Un JWT vale hasta que vence. La API revisa el estado al validar cada token (una consulta por clave primaria) para que la baja sea inmediata |
| ¿Cómo se reactiva? | `PUT` acepta `estado` opcional (`A` o `I`) | Sin esto, la baja lógica sería definitiva en la práctica |
| ¿Quién crea el primer usuario, si `CreateUser` exige token? | Un administrador en los datos iniciales | Sin él no se podría entrar a la aplicación |
| Contraseña en el `PUT` | Opcional: si no viene, se conserva | Editar un teléfono no debería obligar a cambiar la contraseña |
| Nadie puede desactivarse a sí mismo | 409, por `DELETE` o por `PUT` con `estado: "I"` | Evita que el único administrador se deje sin acceso |
| Login por teléfono | El teléfono es único en la base | Es la credencial: dos usuarios con el mismo teléfono no podrían distinguirse |
| Email | Único sin distinguir mayúsculas, y se guarda en minúsculas | `Ana@x.com` y `ana@x.com` son la misma casilla |
| Contraseña | De 8 a 64 caracteres, con letras y números. Se guarda con **BCrypt** (costo 12) | El formato del ejemplo (`$2b$12$`) es BCrypt. El tope de 64 respeta el límite de 72 bytes de BCrypt. Los ejemplos con `"123"` se rechazan a propósito |
| Fecha de nacimiento | Pasada y de alguien mayor de edad | Son usuarios con acceso a una aplicación bancaria |
| Teléfono | De 7 a 15 dígitos, con `+` opcional; se guarda sin espacios ni guiones | 15 es el máximo de E.164. `7000-0001` y `70000001` son el mismo teléfono |
| Nombres de los campos en el JSON | En minúscula y pegados, como el ejemplo: `fechanacimiento`. En el login, `access_token`, `token_type` y `session_active`, también como el ejemplo | Respetar el contrato publicado. Se agregan `estado`, `fechacreacion` y `fechamodificacion`, que no rompen a un cliente que no los use |
| `GetUsers` paginado | Responde un arreglo, como el ejemplo; el total va en `X-Total-Count` | Paginar en la base sin cambiar la forma de la respuesta |
| Intentos de login | Hasta 10 por minuto por IP | Freno básico contra fuerza bruta |
| Teléfono inexistente vs. contraseña incorrecta | Misma respuesta y mismo tiempo | Si no, se podría averiguar qué teléfonos están registrados. Cuando el teléfono no existe, igual se verifica contra un hash de relleno |

---

## Base de datos

Dos scripts en `Base de datos/`, que corren en orden:

- `01_esquema.sql`: secuencia, tabla, restricciones, índices y triggers.
- `02_datos_iniciales.sql`: el administrador y cuatro usuarios de ejemplo. Los hashes se generan en el mismo script con `crypt(..., gen_salt('bf', 12))`, así no quedan hashes ilegibles en el repositorio.

| Columna | Tipo | Nulo | Notas |
|---|---|---|---|
| `id` | `integer` | No | `DEFAULT nextval('usuarios_id_seq')`: **secuencia** explícita |
| `nombres` | `varchar(100)` | No | |
| `apellidos` | `varchar(100)` | No | |
| `fecha_nacimiento` | `date` | No | Sin hora: es una fecha de calendario |
| `direccion` | `varchar(250)` | No | |
| `password` | `varchar(120)` | No | Hash BCrypt (60 caracteres); el tope deja margen para cambiar de algoritmo |
| `telefono` | `varchar(20)` | No | `UNIQUE` |
| `email` | `varchar(150)` | No | Índice único sobre `lower(email)` |
| `estado` | `varchar(1)` | No | `CHECK (estado IN ('A','I'))`, por defecto `A` |
| `fecha_creacion` | `timestamptz` | No | La pone un **trigger** al insertar |
| `fecha_modificacion` | `timestamptz` | Sí | La pone un **trigger** al actualizar |

- **Nombres en snake_case.** Es la convención de PostgreSQL: con `FechaNacimiento` habría que entrecomillar cada columna en cada consulta.
- **`estado` es NOT NULL**, aunque el enunciado lo deja nullable. Un `CHECK` deja pasar `NULL`, y un usuario sin estado no se sabe si puede entrar.
- **Fechas en `timestamptz`.** Guarda el instante en UTC, sin ambigüedad de zona horaria.
- **Los triggers mandan sobre la aplicación.** Al insertar, `fecha_creacion` se fija aunque la aplicación mande otra cosa. Al actualizar, `fecha_creacion` no se puede reescribir y `fecha_modificacion` solo cambia si de verdad cambió algún valor (`WHEN (OLD.* IS DISTINCT FROM NEW.*)`).
- **Índice en `(apellidos, nombres)`**, que es el orden del listado: la paginación no ordena en memoria.

---

## Arquitectura

```
Backend/
  src/
    Abank.Domain/          entidad User y sus reglas, Result, errores. Sin dependencias
    Abank.Application/     casos de uso (commands y queries), puertos, behavior de logging
    Abank.Infrastructure/  Dapper + Npgsql, BCrypt, emisión de JWT, health check
    Abank.Api/             endpoints, contratos JSON, autenticación, errores, OpenAPI
  tests/
    Abank.UnitTests/         dominio, casos de uso con dobles, BCrypt y JWT
    Abank.IntegrationTests/  la API completa contra PostgreSQL con Testcontainers
```

Las dependencias apuntan hacia adentro: el dominio no conoce a nadie, Application solo al dominio, Infrastructure implementa los puertos de Application, y la Api solo traduce HTTP a casos de uso y conecta las capas.

Recorrido de `POST /api/v1/users`:

1. El endpoint convierte el JSON en `CreateUserCommand` y lo envía por el mediador.
2. El handler pide a `User.Create` que valide **todo** el perfil y la contraseña, y devuelve un `Result` con cada error por campo.
3. Si es válido, el repositorio hace un `INSERT ... RETURNING` con Dapper: la secuencia pone el id y el trigger la fecha, y vuelven en el mismo viaje a la base.
4. Un teléfono o email repetido lo detecta la restricción única de la base, no una consulta previa, así no hay carrera entre dos altas simultáneas. El repositorio traduce la violación (`23505`) a un error de negocio.
5. El endpoint traduce el `Result`: 201 con `Location`, o ProblemDetails con el código que corresponde.

### Decisiones técnicas

| Decisión | Motivo |
|---|---|
| .NET 10 (LTS) | El enunciado pide .NET 8 o superior; la 10 es la LTS vigente |
| Dapper para toda la persistencia | Lo pide el enunciado. El SQL está a la vista y cada consulta trae solo lo necesario |
| Un repositorio para escribir y consultas aparte para leer | El repositorio reconstruye la entidad con sus reglas. Las consultas proyectan directo a lo que se muestra, sin el hash |
| Sin Unit of Work | Cada escritura es una sola sentencia, atómica por sí misma. Una transacción explícita sería código sin uso |
| `NpgsqlDataSource` único | Administra el pool de conexiones de toda la aplicación |
| Conteo y página en un viaje | `QueryMultiple` trae el total y la página en una sola ida a la base |
| Filtros armados según lo que aplica | Un `(@x IS NULL OR ...)` fijo impide que el planificador use los índices. Siempre con parámetros; `%` y `_` del usuario se escapan |
| CQRS con Mediator (martinothamar) | Licencia MIT y código generado al compilar, sin reflexión. MediatR es comercial desde la v13 |
| Result pattern | Los errores de negocio no son excepciones. Un solo punto los traduce a HTTP |
| Validación que junta todos los errores | Un formulario marca todos sus campos de una vez, no uno por intento |
| Minimal APIs agrupadas por recurso | Endpoints delgados: petición → comando → `Result` → HTTP |
| JWT HS256 de 60 minutos | Firmado con la llave de configuración (mínimo 256 bits, se valida al arrancar). Solo lleva el id y el nombre, ningún dato sensible |
| `ILogger` de .NET, sin librerías de logging | Cada caso de uso se registra con su duración y, si falló, con el código de error; cada línea lleva el `TraceId` de su petición |
| Scalar expuesto siempre | Para facilitar la revisión de la prueba. En producción se limitaría a Development |

---

## Despliegue

| Pieza | Servicio |
|---|---|
| API | [Render](https://render.com), Web Service con Docker (`Backend/src/Abank.Api/Dockerfile`), región Virginia |
| Base de datos | [Neon](https://neon.tech), PostgreSQL 17 administrado, misma región. Creada con los mismos scripts de `Base de datos/` |

Render construye y publica la imagen en cada push a `main`. La configuración va en variables de entorno, nunca en el repositorio:

| Variable | Para qué |
|---|---|
| `ConnectionStrings__Abank` | Cadena de conexión a Neon, con SSL obligatorio |
| `Jwt__SigningKey` | Llave del JWT, generada por Render. Sin ella la API no arranca: la llave de desarrollo solo existe en `appsettings.Development.json` |
| `Proxy__ClientIpHeader` | `CF-Connecting-IP`. Ver abajo |
| `PORT` | `8080`, el puerto en que escucha la imagen |

**La IP del cliente detrás de los proxies.** En Render las peticiones pasan por Cloudflare y por un balanceador interno antes de llegar a la API, y ambos cambian de IP entre peticiones. Sin ajuste, el límite de 10 logins por minuto quedaría atado a esas IPs y nunca se activaría. La API toma la IP del cliente de `CF-Connecting-IP`, que escribe Cloudflare: se comprobó que Cloudflare rechaza las peticiones que ya traen esa cabecera, así que un cliente no puede inventarse una IP para saltarse el límite. `X-Forwarded-For` no sirve para esto, porque el cliente puede agregarle valores. En local y en Docker no hay proxy y se usa la IP de la conexión.
