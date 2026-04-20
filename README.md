# 🔧 TallerSaaS

> **Sistema SaaS de Gestión para Talleres Automotrices**
> Multi-tenant • ASP.NET Core 6 • SQL Server

[![.NET](https://img.shields.io/badge/.NET-6.0-512BD4?style=flat-square&logo=dotnet)](https://dotnet.microsoft.com/)
[![C#](https://img.shields.io/badge/C%23-10-239120?style=flat-square&logo=csharp)](https://learn.microsoft.com/dotnet/csharp/)
[![EF Core](https://img.shields.io/badge/EF_Core-6.0-512BD4?style=flat-square)](https://learn.microsoft.com/ef/core/)
[![SQL Server](https://img.shields.io/badge/SQL_Server-2019%2B-CC2927?style=flat-square&logo=microsoftsqlserver)](https://www.microsoft.com/sql-server)
[![License](https://img.shields.io/badge/license-Proprietary-red?style=flat-square)]()

---

## 📋 Descripción

**TallerSaaS** es una plataforma web multi-tenant diseñada para la gestión integral de talleres de servicio automotriz. Permite a múltiples talleres (tenants) operar de forma aislada sobre una sola instalación, administrando su ciclo operativo completo: desde la recepción del vehículo hasta la facturación, trazabilidad, inventario y exportación de reportes.

**Estado Actual:**
- Aplicación desplegada y estable.
- Sistema de autenticación y roles (`SuperAdmin`, `Admin`, `Mecanico`) totalmente funcional.
- Plataforma lista para demostraciones y pruebas con clientes reales.

---

## 🌐 URL del Sistema

El sistema se encuentra desplegado en el siguiente enlace de producción:
👉 **[https://geardash.runasp.net](https://geardash.runasp.net)**

### Acceso Demo
El acceso a la plataforma demo para revisiones comerciales o técnicas está disponible bajo solicitud. 
> **Nota:** Por motivos de seguridad, las credenciales no se publican en este repositorio. Por favor, contacte al desarrollador para obtener una cuenta de prueba.

---

## ✨ Características Principales

- **Gestión de Clientes y Vehículos:** CRUD completo con soporte multi-vehículo.
- **Órdenes de Trabajo:** Ciclo de vida completo desde recepción hasta entrega con cálculo automático de costos e IVA.
- **Control de Empleados:** Gestión de nómina, contratos y asignación de tareas.
- **Inventario y Bodega:** Control de stock, movimientos y alertas de existencias.
- **Contabilidad y Facturación:** Generación de facturas, registro de pagos y automatización de asientos contables.
- **Reportes y Exportación:** Generación de reportes en Excel, CSV, TXT y PDF (Apple-style).

---

## 🛠️ Stack Tecnológico

| Capa | Tecnología | Versión |
|------|-----------|---------|
| **Runtime** | .NET / C# | 6.0 |
| **Framework Web** | ASP.NET Core MVC | 6.0 |
| **ORM** | Entity Framework Core | 6.0.36 |
| **Base de datos** | Microsoft SQL Server | 2019+ |
| **Autenticación** | ASP.NET Core Identity | 6.0 |
| **Generación Excel** | ClosedXML | 0.102.1 |
| **Generación PDF** | QuestPDF (Community) | 2024.10.4 |

---

## 🏗️ Arquitectura (Clean Architecture)

El proyecto está estructurado para facilitar la escalabilidad y mantenibilidad:

- **TallerSaaS.Domain:** Entidades de negocio centrales.
- **TallerSaaS.Application:** Lógica de negocio y servicios.
- **TallerSaaS.Infrastructure:** Acceso a datos, migraciones y middleware multi-tenant.
- **TallerSaaS.Web:** Interfaz de usuario (MVC) y controladores.

---

## ⚙️ Instrucciones de Despliegue

Para realizar un despliegue manual en servidores IIS o similares:

```bash
# 1. Limpiar versiones anteriores
dotnet clean

# 2. Compilar el proyecto
dotnet build

# 3. Publicar en modo Release (Framework-Dependent para MonsterASP)
dotnet publish -c Release -o ./publish
```

### Advertencias Críticas
- **Seguridad:** NUNCA incluyas credenciales (usuarios, contraseñas, connection strings) en repositorios públicos.
- **Configuración:** Asegúrate de configurar correctamente el archivo `appsettings.json` en el servidor con los datos de la base de datos de producción.

---

## 📄 Licencia

Proyecto propietario. Todos los derechos reservados © 2026.
