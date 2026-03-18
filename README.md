# 🔧 TallerSaaS

> **Sistema SaaS de Gestión para Talleres Automotrices**
> Multi-tenant • ASP.NET Core 9 • SQL Server

[![.NET](https://img.shields.io/badge/.NET-9.0-512BD4?style=flat-square&logo=dotnet)](https://dotnet.microsoft.com/)
[![C#](https://img.shields.io/badge/C%23-13-239120?style=flat-square&logo=csharp)](https://learn.microsoft.com/dotnet/csharp/)
[![EF Core](https://img.shields.io/badge/EF_Core-9.0-512BD4?style=flat-square)](https://learn.microsoft.com/ef/core/)
[![SQL Server](https://img.shields.io/badge/SQL_Server-2019%2B-CC2927?style=flat-square&logo=microsoftsqlserver)](https://www.microsoft.com/sql-server)
[![License](https://img.shields.io/badge/license-Proprietary-red?style=flat-square)]()

---

## 📋 Descripción

**TallerSaaS** es una plataforma web multi-tenant diseñada para la gestión integral de talleres de servicio automotriz. Permite a múltiples talleres (tenants) operar de forma aislada sobre una sola instalación, administrando su ciclo operativo completo: desde la recepción del vehículo hasta la facturación, trazabilidad, inventario y exportación de reportes.

**Problema que resuelve:** Elimina el uso de hojas de Excel dispersas y software de escritorio desconectado, centralizando en una sola plataforma SaaS toda la información de órdenes, clientes, vehículos, pagos e inventario de piezas.

---

## 🛠️ Stack Tecnológico

| Capa | Tecnología | Versión |
|------|-----------|---------|
| **Runtime** | .NET / C# | 9.0 |
| **Framework Web** | ASP.NET Core MVC | 9.0 |
| **ORM** | Entity Framework Core | 9.0.2 |
| **Base de datos** | Microsoft SQL Server | 2019+ |
| **Autenticación** | ASP.NET Core Identity | 9.0.2 |
| **Frontend** | Razor Views + Bootstrap 5 | — |
| **Iconografía** | Bootstrap Icons | CDN |
| **Generación Excel** | ClosedXML | 0.104.2 |
| **Generación PDF** | QuestPDF (Community) | 2024.10.4 |
| **Sesiones** | Distributed Memory Cache | built-in |

---

## 🏗️ Arquitectura

El proyecto sigue **Clean Architecture** dividido en 4 proyectos de clase:

```
TallerSaaS/
└── src/
    ├── TallerSaaS.Domain/          # Entidades de negocio, enums, contratos de dominio
    │   ├── Entities/               # Cliente, Vehiculo, Orden, Factura, Tenant, Bodega, …
    │   ├── Enums/                  # EstadoOrden, TipoMovimiento, …
    │   └── Interfaces/             # IRepository<T>
    │
    ├── TallerSaaS.Application/     # Lógica de aplicación (sin dependencias de infraestructura)
    │   ├── DTOs/                   # ReporteFilter, TimeZoneHelper, ViewModels
    │   ├── Interfaces/             # IApplicationDbContext, IExportStrategy
    │   └── Services/               # ReporteService, OrdenService, FacturaService, …
    │       └── Exporters/          # CsvExportStrategy, TxtExportStrategy, PdfExportStrategy
    │
    ├── TallerSaaS.Infrastructure/  # Implementaciones concretas
    │   ├── Data/                   # ApplicationDbContext, Seed
    │   ├── Migrations/             # EF Core Migrations
    │   ├── Middleware/             # TenantMiddleware
    │   ├── Repositories/           # GenericRepository<T>
    │   └── Services/               # CurrentTenantService, TenantClaimsFactory
    │
    ├── TallerSaaS.Shared/          # Helpers cross-cutting (TenantClaimTypes, ModelBinders)
    │
    └── TallerSaaS.Web/             # Capa de presentación ASP.NET Core MVC
        ├── Controllers/            # 11 controladores (Auth, Dashboard, Clientes, …)
        └── Views/                  # Razor Views por módulo
```

### Patrón Multi-tenant
El aislamiento de datos se implementa mediante **`TenantMiddleware`** que inyecta el `TenantId` en el contexto HTTP tras la autenticación. El `ApplicationDbContext` filtra automáticamente los datos por `TenantId` en cada consulta.

### Patrón Estrategia — Exportaciones
El módulo de reportes usa el **Strategy Pattern** (`IExportStrategy`) para generar CSV, TXT o XLSX sin duplicar lógica de negocio. El controlador delega al strategy correcto según el parámetro `formato`.

---

## ✨ Características Principales

### 👥 Gestión de Clientes y Vehículos
- CRUD completo de clientes con soporte de múltiples vehículos por cliente.
- Registro de VIN, placa, año, marca, modelo y kilometraje.

### 📋 Órdenes de Trabajo
- Creación de órdenes con ítems detallados (descripción, cantidad, precio unitario).
- Cálculo automático de subtotal, descuento, IVA (19%) y total en COP.
- Estados de orden: `Recibido → En Diagnóstico → En Reparación → Listo → Entregado`.
- Historial de trazabilidad por evento.

### 🧾 Facturación
- Generación de facturas consolidadas agrupando múltiples órdenes.
- Numeración automática correlativa por tenant.
- Registro de pagos parciales o totales.

### 📦 Inventario y Bodega
- Gestión de productos con stock mínimo y alertas.
- Movimientos de inventario (entrada / salida / ajuste) con trazabilidad.
- Soporte de múltiples bodegas por tenant.

### 📊 Reportes y Exportaciones
| Reporte | Excel | CSV | TXT |
|---------|-------|-----|-----|
| Órdenes de trabajo | ✅ | ✅ | ✅ |
| Facturas | ✅ | ✅ | ✅ |
| Clientes y vehículos | ✅ | ✅ | ✅ |
| PDF por orden/factura | ✅ | — | — |

- Filtros por periodo: **Trimestral, Semestral, Anual, Personalizado**.
- Excel se sirve como `Content-Disposition: inline` → apertura en nueva pestaña sin descarga forzada.
- Zona horaria correcta UTC-5 (Colombia) en todos los filtros de fecha.

### 🔐 Autenticación y Roles
| Rol | Acceso |
|-----|--------|
| `SuperAdmin` | Administración global de tenants y usuarios |
| `Admin` | Gestión completa de un tenant (clientes, órdenes, reportes, inventario) |
| `Mecanico` | Visualización y actualización de órdenes asignadas |

### 🏢 SuperAdmin Console
- Registro y administración de nuevos tenants (talleres).
- Panel ejecutivo con métricas globales.

---

## ⚙️ Instalación y Configuración

### Pre-requisitos

| Herramienta | Versión mínima |
|------------|----------------|
| .NET SDK | 9.0 |
| SQL Server | 2019 (Express válido) |
| Git | cualquier |

### 1 — Clonar el repositorio

```bash
git clone https://github.com/<org>/TallerSaaS.git
cd TallerSaaS
```

### 2 — Configurar la cadena de conexión

Edita `src/TallerSaaS.Web/appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=TallerSaaS;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true"
  },
  "SuperAdmin": {
    "Email": "superadmin@tallersaas.com",
    "Password": "SuperAdmin@2025!"
  }
}
```

> **Nota:** Para desarrollo local con usuario/contraseña SQL usa:
> ```
> Server=localhost;Database=TallerSaaS;User Id=sa;Password=<pwd>;TrustServerCertificate=True
> ```

Para sobreescribir credenciales sin modificar el archivo, usa `User Secrets`:

```bash
cd src/TallerSaaS.Web
dotnet user-secrets set "SuperAdmin:Password" "MiPasswordSeguro@123"
```

### 3 — Aplicar migraciones y seed inicial

```bash
# Desde la raíz del repositorio
dotnet ef database update --project src/TallerSaaS.Infrastructure --startup-project src/TallerSaaS.Web
```

El seed crea automáticamente los roles (`SuperAdmin`, `Admin`, `Mecanico`) y el usuario SuperAdmin definido en `appsettings.json`.

### 4 — Ejecutar la aplicación

```bash
cd src/TallerSaaS.Web
dotnet run
```

La aplicación estará disponible en `https://localhost:5001` y `http://localhost:5000`.

### 5 — (Opcional) Build de producción

```bash
dotnet publish src/TallerSaaS.Web -c Release -o ./publish
```

---

## 🗺️ Rutas Principales (MVC Routes)

### Módulos de negocio

| Módulo | Ruta Base | Acceso |
|--------|-----------|--------|
| Dashboard | `GET /Dashboard` | Admin, SuperAdmin |
| Clientes | `GET/POST /Clientes` | Admin, SuperAdmin |
| Vehículos | `GET/POST /Vehiculos` | Admin, SuperAdmin |
| Órdenes | `GET/POST /Ordenes` | Admin, Mecánico, SuperAdmin |
| Facturas | `GET/POST /Facturas` | Admin, SuperAdmin |
| Inventario | `GET/POST /Inventario` | Admin, SuperAdmin |
| Bodega | `GET/POST /Bodega` | Admin, SuperAdmin |
| Trazabilidad | `GET /Trazabilidad` | Admin, SuperAdmin |

### Módulo de Reportes

| Endpoint | Parámetros | Descripción |
|----------|-----------|-------------|
| `GET /Reportes` | — | Dashboard de exportaciones |
| `GET /Reportes/ExportarOrdenes` | `formato`, `periodo`, `desde`, `hasta` | Excel / CSV / TXT de órdenes |
| `GET /Reportes/ExportarFacturas` | `formato`, `periodo`, `desde`, `hasta` | Excel / CSV / TXT de facturas |
| `GET /Reportes/ExportarClientesVehiculos` | `formato`, `periodo`, `desde`, `hasta` | Excel / CSV / TXT de clientes |
| `GET /Reportes/FacturaPdf/{ordenId}` | `ordenId` (GUID) | PDF Apple-style de una orden |

**Valores válidos para `periodo`:** `trimestral` · `semestral` · `anual` · `personalizado`
**Valores válidos para `formato`:** `excel` · `csv` · `txt`

### Autenticación

| Endpoint | Método | Descripción |
|----------|--------|-------------|
| `/Account/Login` | GET / POST | Inicio de sesión |
| `/Account/Logout` | POST | Cierre de sesión |
| `/Account/AccessDenied` | GET | Acceso denegado |

### SuperAdmin

| Endpoint | Método | Descripción |
|----------|--------|-------------|
| `/SuperAdmin` | GET | Panel ejecutivo global |
| `/SuperAdmin/NuevoTenant` | GET / POST | Registro de nuevo taller |

---

## 🗄️ Modelo de Datos (Entidades principales)

```
Tenant ─┬─► ApplicationUser (Identity)
        ├─► Cliente ──► Vehiculo ──► Orden ──► ItemOrden
        │                                 └──► EventoTrazabilidad
        ├─► Factura ◄───────────────── Orden (N:M)
        │        └──► Pago
        └─► Bodega ──► ProductoInventario ──► MovimientoInventario
```

---

## 🤝 Contribuir

1. Haz fork del repositorio.
2. Crea una rama: `git checkout -b feature/nombre-feature`.
3. Haz commit de tus cambios: `git commit -m 'feat: descripción'`.
4. Abre un Pull Request hacia `main` con descripción detallada.

### Convenciones de commits

```
feat:     Nueva funcionalidad
fix:      Corrección de bug
refactor: Refactorización sin cambio de comportamiento
docs:     Cambios en documentación
chore:    Mantenimiento (dependencias, configs)
```

---

## 📄 Licencia

Proyecto propietario. Todos los derechos reservados © 2026.
