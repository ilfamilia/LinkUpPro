# LinkUp Pro

Aplicación web académica desarrollada con **ASP.NET Core MVC (.NET 10)**, **Entity Framework Core (Code First)**, **ASP.NET Core Identity**, **SQL Server** y arquitectura Onion. Incluye red social, solicitudes de amistad, notificaciones y Battleship.

## Integrantes (sección 9)
- Israel Liranzo Familia (2025-2026)
- Luis Junior Arias Mejia (2024-1657)
- Isael Fermin Capellan Valdez (2023-1364)

## Instrucciones para ejecutar el proyecto (profesor)

### 1. Indicar su propia cadena de conexión y credenciales de correo

En Visual Studio:

1. Haga clic derecho sobre el **proyecto web `LinkUpPro`**
2. Seleccione **Administrar secretos de usuario** (*Manage User Secrets*).
3. Se abrirá `secrets.json`. Use esta estructura y reemplace los valores de ejemplo con los suyos:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\MSSQLLocalDB;Database=LinkUpProAcademicDemo;Trusted_Connection=True;TrustServerCertificate=True"
  },
  "MailSettings": {
    "EmailFrom": "correo@ejemplo.com",
    "SmtpHost": "servidor.smtp.ejemplo.com",
    "SmtpPort": 587,
    "SmtpUser": "usuario.smtp@ejemplo.com",
    "SmtpPass": "CONTRASENA_O_CLAVE_DE_APLICACION",
    "DisplayName": "LinkUp Pro"
  }
}
```

### 2. Crear las tablas de la base de datos

Abra **paquetes NuGet → Consola del Administrador de paquetes** y ejecute estas **dos** instrucciones:

```powershell
Update-Database -Context IdentityContext -Project LinkUpPro.Infrastructure.Identity -StartupProject LinkUpPro
Update-Database -Context LinkUpProAppContext -Project LinkUpPro.Infrastructure.Persistence -StartupProject LinkUpPro
```
