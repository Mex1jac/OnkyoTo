# OnkyoIn

Aplicación para controlar receptores Onkyo (AVR) por red desde una página web.

## ¿Qué hace?

- **Descubrir** la IP del dispositivo Onkyo en la red local.
- **Encender** el dispositivo.
- **Subir** y **bajar** el volumen.
- **Consultar** el estado actual (encendido/apagado y volumen).

## Arquitectura

El proyecto sigue **Domain-Driven Design (DDD)** con las capas `Domain`, `Application`
e `Infrastructure` como carpetas dentro de cada proyecto, y se divide en dos aplicaciones
que se comunican por **API HTTP**.

```
OnkyoIn/
├── OnkyoIn.slnx                  Solución (.NET 10)
├── AGENTS.md                     Contexto del proyecto para asistentes de IA
├── src/
│   ├── OnkyoIn.Control/          Web API que habla con el dispositivo (protocolo eISCP)
│   └── OnkyoIn.Web/              Web API + SPA React que consume a Control
│       └── ClientApp/            Frontend React 19 + Vite + TypeScript
├── tests/
│   ├── OnkyoIn.Control.Tests/    Tests de Control (xUnit)
│   └── OnkyoIn.Web.Tests/        Tests de Web (xUnit)
└── libs/
    └── Onkyo.eISCP/              Submódulo Git (librería de terceros)
```

- **OnkyoIn.Control** ejecuta los comandos reales sobre el dispositivo usando la
  librería [Onkyo.eISCP](https://github.com/lolochristen/Onkyo.eISCP) (TCP puerto `60128`).
- **OnkyoIn.Web** sirve el frontend y reenvía las peticiones a Control por HTTP.

## Requisitos

- [.NET SDK 10](https://dotnet.microsoft.com/download)
- [Node.js 20+](https://nodejs.org/) y npm (para el frontend)
- Un receptor Onkyo compatible en la misma red

## Cómo empezar

### 1. Clonar con el submódulo

La librería `Onkyo.eISCP` se incluye como **submódulo Git**, así que hay que
inicializarla al clonar:

```bash
git clone --recurse-submodules https://github.com/Mex1jac/OnkyoTo.git
```

Si ya clonaste el repositorio sin la opción anterior:

```bash
git submodule update --init --recursive
```

> **Importante:** si `libs/Onkyo.eISCP` está vacío, la solución **no compilará**.
> Ejecuta siempre el comando de arriba después de clonar o al cambiar de rama.

### 2. Compilar y probar el backend

```bash
dotnet build OnkyoIn.slnx
dotnet test OnkyoIn.slnx
```

### 3. Ejecutar las aplicaciones

Hacen falta **tres procesos**. Abre una terminal por cada uno:

```bash
# Terminal 1 — API de control del dispositivo
dotnet run --project src/OnkyoIn.Control
# Escucha en https://localhost:7066 (y http://localhost:5047)

# Terminal 2 — API web
dotnet run --project src/OnkyoIn.Web
# Escucha en https://localhost:7261 (y http://localhost:5241)

# Terminal 3 — frontend (SPA)
cd src/OnkyoIn.Web/ClientApp
npm install
npm run dev
# Vite sirve en http://localhost:5173 y hace proxy de /api a la API web
```

Abre **http://localhost:5173** en el navegador.

## Endpoints de la API

Disponibles tanto en `OnkyoIn.Control` como en `OnkyoIn.Web` (Web los reenvía):

| Método | Ruta | Descripción |
|--------|------|-------------|
| `GET` | `/api/device/discover` | Descubre un dispositivo y devuelve su IP |
| `GET` | `/api/device/state?ipAddress={ip}` | Lee estado (encendido y volumen) |
| `POST` | `/api/device/power-on` | Enciende el dispositivo |
| `POST` | `/api/device/volume/up` | Sube el volumen |
| `POST` | `/api/device/volume/down` | Baja el volumen |

Cuerpo para los `POST`:

```json
{ "ipAddress": "192.168.1.100" }
```

Puedes probarlos con los archivos `*.http` incluidos
(`src/OnkyoIn.Control/OnkyoIn.Control.http`, `src/OnkyoIn.Web/OnkyoIn.Web.http`).

### Códigos de error

| Código | Significado |
|--------|-------------|
| `400` | Petición inválida (p. ej. IP vacía) |
| `404` | No se encontró ningún dispositivo |
| `502` | Error de comunicación con el dispositivo / servicio inalcanzable |
| `504` | El servicio tardó demasiado en responder |
| `500` | Error inesperado |

Los errores se devuelven como [ProblemDetails](https://datatracker.ietf.org/doc/html/rfc7807).

## Configuración

La configuración se toma de `appsettings.json` y se sobreescribe por entorno
(`appsettings.Development.json`, variables de entorno, etc.).

| Clave | Proyecto | Descripción |
|-------|----------|-------------|
| `ControlApi:BaseUrl` | OnkyoIn.Web | URL de la API de Control (obligatoria) |
| `Cors:AllowedOrigins` | Ambos | Orígenes permitidos para CORS |

- En **Development** ya vienen configurados los orígenes locales (Vite `5173`, Web `7261/5241`).
- En **Production** debes indicar los orígenes explícitamente; si no, la aplicación
  **falla al arrancar** en lugar de permitir todo.

Ejemplo con variables de entorno:

```bash
Cors__AllowedOrigins__0=https://mi-dominio.com
ControlApi__BaseUrl=https://control.mi-dominio.com
```

## Notas

- La librería `Onkyo.eISCP` **no está publicada en NuGet**, por eso se integra como
  submódulo Git y referencia de proyecto.
- El descubrimiento usa **UDP broadcast** (`ECNQSTN`) y el control usa **TCP puerto 60128**.
- El submódulo fija una revisión concreta de la librería; para actualizarla:
  `git submodule update --remote libs/Onkyo.eISCP`.
