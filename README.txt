*******************************************************************************
*                                 TallerSaaS                                  *
*******************************************************************************

DESCRIPCIÓN:
Sistema SaaS integral para la gestión de talleres automotrices, permitiendo el 
control total de clientes, vehículos, órdenes de trabajo, inventarios y nómina.

URL DEL SISTEMA:
https://geardash.runasp.net

ACCESO DEMO:
El acceso a la plataforma demo está disponible bajo solicitud. 
Por favor, contacte al desarrollador para obtener credenciales de prueba.

TECNOLOGÍAS PRINCIPALES:
- ASP.NET Core 6 (MVC)
- Entity Framework Core
- SQL Server
- ASP.NET Identity

INSTRUCCIONES DE DESPLIEGUE:
1. dotnet clean
2. dotnet build
3. dotnet publish -c Release -o ./publish
4. Subir el contenido de la carpeta /publish a la raíz del sitio en IIS.

NOTA DE SEGURIDAD:
Asegúrese de configurar correctamente las cadenas de conexión y credenciales de 
SuperAdmin en el archivo appsettings.json del servidor de producción. 
NO comparta archivos con credenciales reales en el repositorio.

-------------------------------------------------------------------------------
GearDash Systems © 2026 - Todos los derechos reservados.
*******************************************************************************
