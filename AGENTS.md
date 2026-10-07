# Contexto del proyecto: OnkyoIn

## 1. Objetivo
- Qué es: integración/automatización para receptores Onkyo (control vía red).
- Para qué sirve: permite al usuario controlar su dispositivo Onkyo remotamente desde una página web.
- Estado actual: en desarrollo.

## 2. Rol
- Actúa como programador que escribe código de C# de manera fácil de entender.

## 3. Alcance e hitos
- Crear una página web que controle cualquier dispositivo Onkyo compatible. Funcionalidades iniciales:
  1. Descubrir la IP del dispositivo en la red donde se ejecute la aplicación.
  2. Enviar el comando de encendido al dispositivo.
  3. Subir el volumen del dispositivo.
  4. Bajar el volumen del dispositivo.
- Deben ser 2 proyectos separados: uno para la página web y otro para la aplicación de control del dispositivo.
- Estructurar el proyecto con el patrón Domain-Driven Design (DDD).
- Tests para probar todo.

## 4. Stack y estructura
- Lenguaje(s): C#
- Framework(s): .NET 10 (`net10.0`), ASP.NET Core Web API (con controladores).
- Librería de control: [Onkyo.eISCP](https://github.com/lolochristen/Onkyo.eISCP) — se integra como **submódulo Git** en `libs/Onkyo.eISCP` y se referencia como proyecto (no está en NuGet).
- Frontend: React 19 + Vite + TypeScript en `src/OnkyoIn.Web/ClientApp`.
- Gestor de paquetes: dotnet (backend), npm (frontend).
- Solución: `OnkyoIn.slnx` (formato nuevo de .NET 10).
- Proyectos:
  - `src/OnkyoIn.Control` — **Web API** que habla con el dispositivo (puerto 60128 vía eISCP).
  - `src/OnkyoIn.Web` — **Web API + SPA React**; llama a Control por HTTP.
  - `tests/OnkyoIn.Control.Tests` — tests de Control.
  - `tests/OnkyoIn.Web.Tests` — tests de Web.
  - `libs/Onkyo.eISCP` — submódulo de la librería de terceros.
- Capas DDD (carpetas dentro de cada proyecto):
  - `Domain/` — entidades y puertos (interfaces).
  - `Application/` — casos de uso (services) y DTOs.
  - `Infrastructure/` — adaptadores (eISCP en Control, HttpClient en Web).
  - `Controllers/` — capa de presentación HTTP.
- Comunicación: **API HTTP** entre Web y Control.

## 5. Cómo ejecutar
- Compilar la solución: `dotnet build OnkyoIn.slnx`
- Ejecutar los tests: `dotnet test OnkyoIn.slnx`
- Backend Control: `dotnet run --project src/OnkyoIn.Control` (https://localhost:7066)
- Backend Web: `dotnet run --project src/OnkyoIn.Web` (https://localhost:7261)
- Frontend (SPA): `cd src/OnkyoIn.Web/ClientApp` y `npm install` (una vez) + `npm run dev` (proxye /api a Web).
- Compilar el frontend: `npm run build` dentro de `ClientApp`.

## 6. Convenciones
- Estilo: camelCase.
- Idiomas: código en inglés, comentarios en inglés.
- Ramas/commits: se deben hacer commits en https://github.com/Mex1jac/OnkyoTo
- No subir: archivos con datos de conexiones o credenciales.

## 7. Reglas para ti (el agente)
- Siempre: añade/actualiza tests al cambiar la lógica.
- Nunca: no modifiques credenciales ni archivos `.env`.
- Verificación antes de terminar: corre `dotnet build` (y `dotnet test`) y muéstrame la salida.
- Si algo es ambiguo: pregúntame antes de asumir.

## 8. Tareas / backlog
| # | Tarea | Prioridad | Estado | Notas |
|---|-------|-----------|--------|-------|
| 1 | Definir solución y estructura DDD de los 2 proyectos | Alta | Hecho | `OnkyoIn.Web`, `OnkyoIn.Control` |
| 2 | Descubrimiento de la IP del dispositivo en la red | Alta | Hecho | `ISCPConnection.DiscoverAsync` (UDP) |
| 3 | Enviar comando de encendido | Alta | Hecho | `PWR01` vía Onkyo.eISCP |
| 4 | Subir volumen | Media | Hecho | `MVLUP` |
| 5 | Bajar volumen | Media | Hecho | `MVLDOWN` |
| 6 | Tests de las funcionalidades anteriores | Alta | Hecho | 27 tests en xUnit |
| 7 | Manejo de errores/estados en la UI (feedback al usuario) | Media | Hecho | ProblemDetails + excepciones de dominio |
| 8 | Consultar estado del dispositivo (encendido, volumen actual) | Media | Hecho | `GET /api/device/state` |
| 9 | Configurar appsettings por entorno y CORS estricto | Baja | Hecho | `Cors:AllowedOrigins` por entorno |
| 10 | Integrar el submódulo al clonar (`git submodule update --init`) | Media | Hecho | Documentado en README |
| 11 | Revisar porque el comando power on enciende el aparato con exito pero devuelve error de time out | Alta | Hecho | Timeout subido a 6s + verificación de estado |


## 9. Notas y decisiones
- Se usa **DDD en carpetas** dentro de 2 proyectos (no un proyecto por capa) para mantenerlo simple, según decisión del usuario.
- La librería `Onkyo.eISCP` **no está publicada en NuGet**; se incorpora como submódulo Git y referencia de proyecto.
- El frontend es una **SPA React** servida aparte por Vite en desarrollo; en producción se puede publicar en `wwwroot`.
- El puerto eISCP por defecto es **60128** (TCP), y el descubrimiento es por **UDP broadcast** (`ECNQSTN`).
- **Configuración por entorno:** CORS se controla con la sección `Cors:AllowedOrigins` en `appsettings.json` (vacío por defecto) y `appsettings.Development.json` (orígenes locales: Vite `5173`, Web `7261/5241`). En entornos distintos de Development, si no hay orígenes configurados, la app **falla al arrancar** en lugar de permitir todo. La URL de Control (`ControlApi:BaseUrl`) es obligatoria en Web. Los valores pueden sobreescribirse por variables de entorno, p. ej. `Cors__AllowedOrigins__0`.
- **Manejo de errores:** se usan excepciones de dominio (`DeviceNotFoundException`, `DeviceCommunicationException` en Control; `ControlApiException` en Web) traducidas a **ProblemDetails** estándar por un `IExceptionHandler` en cada API. El SPA lee `detail` del ProblemDetails para mostrar mensajes claros.
- **Códigos HTTP:** 400 petición inválida, 404 dispositivo no encontrado, 502 error de comunicación con el dispositivo / servicio inalcanzable, 504 timeout, 500 inesperado.
- **Endpoints de la API:** `GET /api/device/discover`, `GET /api/device/state?ipAddress=...`, `POST /api/device/power-on`, `POST /api/device/volume/up`, `POST /api/device/volume/down`.
