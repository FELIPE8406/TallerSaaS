# TallerSaaS: Documentación Técnica y Funcional Integral

## 1. Visión General
**TallerSaaS** es una solución empresarial tipo Software as a Service (SaaS) diseñada específicamente para la gestión operativa, administrativa y financiera de talleres automotrices en el mercado colombiano. 

El sistema centraliza el ciclo de vida completo del servicio: desde el agendamiento inicial y la recepción del vehículo, pasando por el control de inventarios y ejecución de mano de obra en bahía, hasta la facturación electrónica y el registro contable automatizado bajo normativa local.

### Valor Agregado
- **Cumplimiento Normativo:** Lógica contable integrada con el Plan Único de Cuentas (PUC) y manejo de impuestos (IVA, Retenfuente, ICA).
- **Eficiencia Operativa:** Automatización de asientos contables y sincronización de inventarios en tiempo real.
- **Arquitectura Multi-tenant:** Aislamiento total de datos para múltiples empresas compartiendo una única infraestructura centralizada.

---

## 2. Arquitectura de Datos y Multi-tenancy

### Estrategia de Aislamiento
El sistema utiliza una arquitectura **Shared Database, Isolated Data** (Base de Datos Compartida, Datos Aislados) mediante un discriminador de `TenantId`.

- **Mecanismo de Resolución:** Un middleware de ASP.NET Core (`TenantMiddleware`) identifica al Tenant activo en cada petición HTTP (vía Claims, Cookie de Sesión o Base de Datos).
- **Aislamiento a Nivel de Datos:** Se implementa mediante **Global Query Filters** en Entity Framework Core. Cada entidad que hereda de una base multi-tenant incluye un filtro automático:
  `builder.Entity<T>().HasQueryFilter(e => e.TenantId == _tenantService.TenantId);`
- **Integridad Referencial:** Todas las tablas core (Clientes, Vehículos, Órdenes, etc.) poseen llaves foráneas indexadas hacia la tabla `Tenants` para asegurar consistencia y rapidez en las consultas.

### Diseño de Base de Datos
- **Motor:** SQL Server.
- **Indexación Estratégica:** Se emplean índices compuestos (ej. `IX_Ordenes_Tenant_Date_State`) que priorizan el `TenantId` como prefijo para optimizar el rendimiento en cargas masivas de múltiples empresas.
- **Tipos de Datos:** Uso de `decimal(12,2)` para transacciones financieras, garantizando precisión en el cálculo de impuestos y bases gravables.

---

## 3. Diccionario de Módulos

| Módulo | Funcionalidades Clave | Propósito Operativo |
| :--- | :--- | :--- |
| **Suscripciones** | Gestión de planes (Básico, Pro, Enterprise), límites de usuarios y pagos. | Administración del ciclo de vida del suscriptor. |
| **Clientes** | Registro de terceros, historial de contactos y vinculación de NIT/Cédula. | Centralización de la base de datos de clientes. |
| **Vehículos** | Registro de placas, marca, modelo, VIN y trazabilidad de eventos. | Gestión del parque automotor atendido. |
| **Inventario** | Control de productos (repuestos) y servicios. Gestión de precios de compra/venta y alertas de stock. | Control de insumos y servicios facturables. |
| **Bodegas** | Transferencia entre bodegas y movimientos de entrada/salida. | Gestión logística multialmacén. |
| **Órdenes de Servicio** | Gestión de estados (Ingresada, En Proceso, Terminada, Entregada). Itemización de mano de obra y repuestos. | **Core del negocio:** Control del flujo de trabajo en el taller. |
| **Contabilidad** | Configuración de PUC, visualización de asientos contables y balance. | Automatización financiera y auditoría. |
| **Agenda** | Citas de taller, gestión de bahías y disponibilidad de mecánicos. | Optimización del tiempo de ingreso y recursos. |
| **Nómina/Comisiones** | Cálculo de comisiones por servicio y gestión de contratos de empleados. | Motivación y pago a la fuerza operativa. |

---

## 4. Lógica Contable y Tributaria (Reglas Colombia)

El sistema automatiza el registro contable mediante un motor de **Inyección de Asientos** sincronizado con los eventos operativos.

### Automatización de Asientos (Débitos/Créditos)
Al facturar una Orden de Servicio, el sistema genera automáticamente el asiento en el **PUC**:

1. **Facturación (Venta):**
   - **DB: 130505 (Clientes):** Valor neto a cobrar.
   - **DB: 135515 (Retenfuente):** Anticipo de impuestos (si aplica).
   - **CR: 413505 (Ingresos Servicios):** Base gravable de servicios.
   - **CR: 417505 (Ingresos Repuestos):** Base gravable de repuestos.
   - **CR: 240801 (IVA Generado):** 19% aplicado a las bases seleccionadas.

2. **Costo de Venta (Salida de Inventario):**
   - **DB: 613505 (Costo de Ventas):** Costo de adquisición del repuesto.
   - **CR: 143505 (Mercancías no fab. por empresa):** Disminución de inventario.

3. **Recaudo (Pago):**
   - **DB: 111005 (Bancos):** Ingreso de caja/banco.
   - **CR: 130505 (Clientes):** Cruce y cancelación de la cuenta por cobrar.

### Reglas Contables Críticas
- **Redondeo:** El sistema aplica redondeo a la cifra entera más cercana según el estándar de la DIAN para reportes contables.
- **Bases Gravables:** Diferenciación en el cálculo de Retención en la Fuente basándose en si el ítem es un servicio o un bien tangible (repuesto).

---

## 5. Escalabilidad y Buenas Prácticas

### Backend (.NET / ASP.NET Core)
- **Capa de Abstracción:** Uso de `IApplicationDbContext` para desacoplar la lógica de negocio de la persistencia directa.
- **Patrón Repository/Service:** Lógica de servicios asíncronos (`async/await`) para maximizar la capacidad de respuesta ante múltiples peticiones concurrentes.
- **Inyección de Dependencias:** Gestión centralizada del ciclo de vida de los servicios (Scoped para el Tenant context).

### Base de Datos & Rendimiento
- **Estrategia de Indexación:** Índices no agrupados compuestos que incluyen el `TenantId` para asegurar que las consultas de una empresa no escaneen datos de otras.
- **Optimización de Consultas:** Uso de proyecciones (`Select(dto => ...)`) y `AsNoTracking()` en operaciones de solo lectura para reducir la presión en memoria del servidor.
- **Seguridad Multitenant:** El uso de Global Query Filters a nivel de DbContext actúa como una red de seguridad (fail-safe) que previene el "Data Leakage" entre empresas incluso ante errores humanos en la escritura de queries.

### Recomendaciones de Evolución
1. **Sharding:** En fases avanzadas, separar a los tenants más pesados en bases de datos independientes.
2. **Caché Distribuida:** Implementar Redis para los catálogos compartidos de vehículos y marcas.
3. **Event-Driven Architecture:** Desacoplar la generación contable mediante mensajería (ej. RabbitMQ) para asegurar la persistencia en picos de tráfico.
