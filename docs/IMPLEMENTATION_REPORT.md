# Implementación de amistades, solicitudes, notificaciones y Battleship

## Alcance entregado

Se mantiene .NET 10. Se agregaron componentes con identificadores en inglés y comentarios en inglés que explican las responsabilidades de clases, contratos, métodos, pantallas y reglas delicadas. La aplicación conserva la separación entre Domain, Application, Persistence, Identity y Web.

El diagnóstico y la matriz iniciales se conservan como referencia histórica. Este reporte describe los cambios posteriores; no declara completados los módulos de cuentas, publicaciones o perfil que desarrollan los compañeros.

Los nuevos servicios de Application trabajan con interfaces y DTOs, sin acceder a DbContext, Identity o HTTP. El proyecto conserva referencias previas a EF/HTTP por contratos y ViewModels legados; su limpieza global debe coordinarse con el responsable de esos módulos.

| Fase | Implementación y evidencia |
| --- | --- |
| 1. Base y contratos | `NuGet.Config` del repositorio corrige el mapeo de paquetes sin cambiar configuración global. Contratos `IUserDirectoryService` e `ICurrentUserService`, adaptadores Identity/Web y mapeos AutoMapper separados. |
| 2. Solicitudes | Elegibilidad, búsquedas, envío, recibidas/enviadas, confirmaciones, aceptación/rechazo/cancelación/ocultación y contador. Aceptación y amistad se guardan juntas. |
| 3. Amistades | Listado alfabético, búsqueda, perfiles autorizados, amigos en común, eliminación lógica/reactivación y consultas de publicaciones visibles con filtros. La interacción con publicaciones se conecta al módulo de los compañeros. |
| 4. Notificaciones | Creación interna por comentarios/respuestas/reacciones, destinatarios correctos, historial, contadores, lectura individual/global y comprobación de contenido. Falta conectar las llamadas de sus servicios de comentarios/reacciones. |
| 5. Battleship: preparación | Modelo, migración, partida única activa por pareja, flotas 2/3/3/4/5, tablero 12×12, dirección, límites, superposición, espera y reanudación. |
| 6. Battleship: combate | Turnos estrictos, ataques únicos, aciertos/fallos, hundimientos, victoria, rendición y abandono de 48 horas. Versiones de formulario y transacciones protegen las acciones concurrentes. |
| 7. Historial e integración | Resultados y estadísticas, tableros finales de solo lectura, menú/contadores, errores seguros y pruebas de dominio, SQL y MVC. La verificación final del proyecto completo requiere los módulos del equipo. |

## Ubicación del código

- `LinkUpPro.Core.Domain/Entities/BattleshipGame.cs`: agregado del juego con reglas de preparación, combate y finalización. `ShipPlacement`, `Attack` y `BoardCoordinate` representan flota, disparos y coordenadas.
- `LinkUpPro.Core.Application/Services/`: servicios `FriendshipService`, `FriendshipRequestService`, `NotificationService` y `BattleshipService`. `SocialUserGuard` comprueba activación y usuario actual.
- `LinkUpPro.Core.Application/Interfaces/`: contratos de los módulos y lectura pública de usuarios.
- `LinkUpPro.Core.Application/Dtos/Social/` y `Dtos/Battleship/`: datos de transferencia sin entidades Identity ni tipos HTTP.
- `LinkUpPro.Infrastructure.Persistence/Transactions/AtomicOperation.cs`: transacciones serializables, rollback y traducción segura de conflictos SQL/concurrencia.
- `LinkUpPro.Infrastructure.Persistence/EntityConfigurations/BattleshipGameEntityConfiguration.cs`: índices únicos, restricciones y token de versión.
- `LinkUpPro.Infrastructure.Identity/Services/UserDirectoryService.cs`: consulta de cuentas públicas por ID, lote y nombre normalizado.
- `LinkUpPro/Controllers/`, `ViewModels/`, `Views/`, `Mappings/` y `Filters/`: presentación, formularios, mapeos y protección de los módulos. Los controladores no acceden al DbContext.
- `LinkUpPro.Tests/`: pruebas de reglas, servicios reales con SQL Server e integración con el pipeline MVC y Razor.

## Decisiones de seguridad y consistencia

- Se considera activa una cuenta con `EmailConfirmed = true`, de acuerdo con el modelo actual. Un bloqueo temporal de login no se trata como una desactivación. Si el equipo agrega otro estado, debe ajustar `UserDirectoryService`.
- Los servicios obtienen el actor mediante `ICurrentUserService`. Los formularios no eligen emisor, atacante, destinatario de notificación, turno, resultado o ganador.
- Los IDs obtenidos de Identity se usan en su forma canónica; cambiar mayúsculas en un ID no elude la prohibición de solicitudes propias ni crea otra pareja.
- Las acciones de los cuatro controladores requieren autenticación, cuenta activa y protección antiforgery para solicitudes que modifican datos. Cada recurso verifica propiedad o participación nuevamente en el servicio.
- Las operaciones relacionadas se ejecutan dentro de `IAtomicOperation`. Una excepción revierte incluso escrituras previas efectuadas por repositorios durante esa operación.
- `BattleshipGame.Version` se usa como token de concurrencia de EF y se envía como versión esperada en formularios. Una solicitud antigua tampoco puede consumir un turno posterior del mismo jugador.
- Se preservan las claves compuestas originales de amistades y reacciones. La solicitud y la amistad siguen siendo registros independientes. Eliminar historial no elimina una amistad.
- Terminar una amistad conserva partidas e interacciones. Se exige amistad para crear una partida, no para continuar una existente.
- La finalización por abandono cuenta desde la asignación del turno actual; se evalúa al listar, entrar, refrescar o actuar. La colocación no vence.
- Solo se proyectan las posiciones de la flota propia. Los ataques rivales se incluyen en resultados después de finalizar y únicamente para participantes.
- Los textos se muestran codificados por Razor. Los videos se convierten a URLs de incrustación únicamente después de comprobar host e identificador de YouTube.
- Los detalles de excepciones no se muestran en los errores de los módulos. Se retiró el logging sensible de ambos contextos y se agregó la acción de error prevista por el host.

Los tokens de concurrencia se comparan al guardar y los conflictos deben manejarse de forma explícita, según la [documentación de EF Core](https://learn.microsoft.com/en-us/ef/core/saving/concurrency). La protección de formularios utiliza los mecanismos de [antiforgery de ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/security/anti-request-forgery?view=aspnetcore-10.0).

## Pruebas y ejecución

Resultado final verificado el 19 de octubre de 2026:

- Compilación de la solución: **0 errores y 0 advertencias**.
- Suite completa: **89 pruebas aprobadas, 0 fallidas y 0 omitidas**.
- EF Core: **sin cambios del modelo pendientes de migración**.
- Las migraciones se aplicaron correctamente en la base SQL aislada usada por las pruebas.

| Suite | Pruebas aprobadas |
| --- | ---: |
| `BattleshipRulesTests` | 27 |
| `BattleshipIntegrationTests` | 13 |
| `FriendshipIntegrationTests` | 11 |
| `NotificationIntegrationTests` | 7 |
| `ModuleWebIntegrationTests` | 19 |
| `MappingAndValidationTests` | 11 |
| `UserDirectoryIntegrationTests` | 1 |
| **Total** | **89** |

Comandos desde la raíz:

```powershell
dotnet restore LinkUpPro.slnx
dotnet build LinkUpPro.slnx --no-restore
dotnet test LinkUpPro.Tests/LinkUpPro.Tests.csproj --no-restore --logger "trx;LogFileName=module-tests.trx" --results-directory TestResults
```

Las pruebas de integración necesitan SQL Server y permisos para crear/eliminar bases de pruebas. Por defecto conectan a la instancia local `.` usando autenticación de Windows. Para otra instancia local o de CI:

```powershell
$env:LINKUPPRO_TEST_CONNECTION = "Server=.;Integrated Security=True;TrustServerCertificate=True;Encrypt=False"
dotnet test LinkUpPro.Tests/LinkUpPro.Tests.csproj --no-restore
```

La cadena anterior se destina a pruebas locales. El fixture reemplaza el nombre de base por `LinkUpProTests_` más un GUID, aplica las migraciones de Persistence e Identity y elimina solo esa base al terminar, verificando previamente su nombre. No usa la base configurada en `appsettings.json` ni reemplaza un servidor SQL ausente por un proveedor InMemory.

La autenticación por cabecera empleada en los tests existe únicamente en el ensamblado de pruebas. La aplicación real sigue usando las cookies de Identity; no se agregó un acceso alternativo ni usuarios ficticios de producción.

Cobertura de comportamientos:

- Geometría, flotas, turnos, repetición de celdas, victoria, rendición y frontera exacta de 48 horas.
- Solicitudes cruzadas simultáneas, aceptación doble, rollback, reactivación y conservación de historial.
- Privacidad, filtros combinados/fechas inclusivas, amigos y cuentas inactivas.
- Destinatarios de notificaciones, ausencia de auto notificaciones, lectura privada y pérdida de acceso al contenido.
- Creación simultánea de partidas, ataques/colocaciones/rendiciones simultáneos y formularios obsoletos.
- Partida completa persistida, resultados, estadísticas y separación de tableros.
- Rutas protegidas, antiforgery real, formularios Razor, entradas inválidas, texto codificado y manipulación de campos de emisor/ganador.
- Perfiles AutoMapper y adaptador real de lectura de Identity.

El archivo `TestResults/module-tests.trx` contiene el resultado de la última ejecución; `TestResults` está ignorado por Git. Estas pruebas HTTP usan TestServer y no constituyen una inspección visual manual en navegador.

## Migración de la aplicación

Se generó `20261020011716_AddBattleship`, su diseñador y el snapshot actualizado. La fecha del nombre es el sello UTC generado por EF.

La migración agrega `BattleshipGames`, `ShipPlacements` y `Attacks` con sus restricciones e índices. Las comprobaciones usan bases aisladas. No se aplicó la migración a la base real del proyecto durante este trabajo.

Para aplicarla cuando el equipo integre su modelo y confirme la conexión local correspondiente:

```powershell
dotnet tool restore
dotnet ef database update --project LinkUpPro.Infrastructure.Persistence --startup-project LinkUpPro --context LinkUpProAppContext
```

Si el equipo necesita fusionar migraciones de otros módulos, debe integrar primero el modelo y snapshot. No borrar migraciones ya aplicadas para resolver diferencias.

## Integración con el trabajo del equipo (24 de octubre de 2026)

Se integró `origin/main` (login/registro, publicaciones, comentarios y reacciones) con estos módulos:

1. **Notificaciones conectadas:** `CommentService.AddAsync` llama `NotifyCommentAsync` y `ReactionService.ToggleReactionAsync` llama `NotifyReactionAsync` al registrar o cambiar una reacción, nunca al eliminarla. La interacción y su notificación se guardan dentro del mismo `IAtomicOperation`.
2. **Detalle de publicación:** `GET /Post/Details?postId={id}` reutiliza `IPostService` para revalidar visibilidad. Las notificaciones redirigen ahí con el ancla `#comment-{id}` o `#comments-{postId}`. Sin acceso se muestra "No posee permisos para visualizar esta publicación."
3. **Publicaciones de amigos con interacción:** Amigos y el perfil del amigo filtran con `IFriendshipService` y muestran cada publicación con la partial compartida `Views/Shared/_PostCard.cshtml` (multimedia, reacciones, comentarios y respuestas del módulo de publicaciones). `IPostService.GetVisiblePostsByIdsAsync` carga las métricas en lote. Las reacciones vuelven a la pantalla de origen mediante `ReturnUrl` local.
4. **Autores de comentarios:** el nombre solo enlaza al perfil cuando existe amistad activa con quien visualiza.
5. **Menú:** visible solo para usuarios autenticados, en el orden del documento, con contadores y "Cerrar sesión" mediante POST con antiforgery (`LoginController.Logout`).
6. **Diseño:** las vistas de Amigos, Solicitudes, Notificaciones y Battleship siguen el patrón de Home/Post (container, tarjetas Bootstrap, `form-label`, botones Cancelar/Aceptar). Battleship usa el tablero compartido `Views/Battleship/_Board.cshtml` y `wwwroot/css/Battleship/Battleship.css`: coordenadas A–L/1–12, íconos de barco, ocupadas en gris, aciertos en rojo y fallos en verde.

## Cuentas y Mi perfil (completado el 24 de octubre de 2026)

- **Servicio de cuentas** (`AccountServiceForWebApp`) corregido: mensaje genérico ante credenciales incorrectas, mensaje de cuenta inactiva, bloqueo de 15 minutos tras cinco intentos, enlaces de activación (24 h) y restablecimiento (1 h) de un solo uso guardados como hash en `AspNetUserTokens`, un nuevo enlace invalida el anterior, reenvío de activación limitado a uno cada cinco minutos y mensajes que no revelan si una cuenta existe. Restablecer la contraseña desbloquea la cuenta e invalida sesiones, pero no la activa.
- Se eliminaron errores graves: reenviar el correo activaba la cuenta sin token, restablecer la contraseña activaba cuentas, el enlace de restablecimiento no incluía `userId` y el registro escribía la contraseña en la consola.
- **Registro**: teléfono de República Dominicana, foto validada por extensión, contenido y tamaño (se borra si el registro falla), reglas de contraseña en el servidor, indicador Débil/Media/Fuerte y botón para mostrar u ocultar contraseñas.
- **Sesión**: la pantalla de login informa "Su sesión finalizó por inactividad" o "Debe iniciar sesión para acceder a esta sección". `[AllowAnonymous]` explícito en cada acción pública y página de acceso denegado.
- **Mi perfil** (`ProfileController`): edita nombre, apellido, teléfono y foto (la anterior se borra solo después de guardar la nueva). Cambio de contraseña opcional que valida la actual, exige los tres campos y una contraseña distinta, guarda todo en una sola actualización, invalida todas las sesiones y redirige al login. Correo y usuario son de solo lectura y el id siempre viene de la sesión.
- **Correo local**: `MailSettings:PickupDirectory` guarda los correos como `.eml` en lugar de enviarlos por SMTP (útil sin credenciales). Si se elimina, se usa SMTP normal.

## Ajustes de arquitectura (25 de octubre de 2026)

- **Imágenes en la capa Shared:** `IImageStorageService` (Application) y `ImageStorageService` (Infrastructure.Share) reemplazan al `FileManager` del proyecto web. Valida extensión, firma, contenido y tamaño, genera el nombre del archivo, guarda de forma asíncrona y nunca borra fuera de `Images` ni la foto por defecto. La ruta física (`wwwroot`) llega desde `Program.cs`.
- **Application sin Entity Framework:** las consultas de `PostService` y `CommentService` pasaron a métodos específicos de `IPostRepository`, `ICommentRepository` e `IReactionRepository` (Domain), implementados en Persistence. Se quitaron los paquetes de EF Core del proyecto Application.
- **Controladores sin `UserManager`:** Home, Post, Comment y Reaction heredan de `SocialController`, que aplica `[Authorize]`, antiforgery, verificación de cuenta activa y errores seguros. El usuario sale de `ICurrentUserService` y los autores de `IUserDirectoryService` a través de `PostFeedBuilder`.
- **Pantallas de error claras:** cualquier resultado fallido (no encontrado, sin permisos, validación o conflicto) muestra `SocialError` con el código HTTP correcto y un mensaje en español, en lugar de una página vacía del navegador. Se corrigió además `Post/Delete`, que devolvía la vista con un modelo incorrecto cuando la eliminación fallaba.

## Ejecución local

```powershell
cd LinkUpPro
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=.;Database=LinkUpProDb;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=True"
dotnet user-secrets set "MailSettings:EmailFrom" "no-reply@linkuppro.local"
dotnet user-secrets set "MailSettings:DisplayName" "LinkUp Pro"
dotnet user-secrets set "MailSettings:SmtpHost" "smtp.gmail.com"
dotnet user-secrets set "MailSettings:SmtpPort" "587"
dotnet user-secrets set "MailSettings:SmtpUser" "<cuenta>"
dotnet user-secrets set "MailSettings:SmtpPass" "<clave de aplicación>"
# Opcional, sin SMTP: guardar los correos en una carpeta
dotnet user-secrets set "MailSettings:PickupDirectory" "<ruta>\LinkUpPro\App_Data\Emails"
cd ..
dotnet tool restore
dotnet ef database update --project LinkUpPro.Infrastructure.Persistence --startup-project LinkUpPro --context LinkUpProAppContext
dotnet ef database update --project LinkUpPro.Infrastructure.Identity --startup-project LinkUpPro --context IdentityContext
dotnet run --project LinkUpPro --launch-profile https
```

Verificación de punta a punta contra la base real (registro, activación, bloqueo, restablecimiento, solicitudes, publicaciones, comentarios, reacciones, notificaciones, Battleship, Mi perfil y cierre de sesión): 69 de 69 comprobaciones correctas. Prueba adicional de publicaciones y comentarios (validaciones, filtros, privacidad, comentarios desactivados, respuestas, edición, eliminación lógica, XSS y pantallas de error): 33 de 33.
