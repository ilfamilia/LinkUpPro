# LinkUp Pro: análisis y plan de ejecución

Fecha del análisis: 19 de octubre de 2026.

Este archivo conserva el diagnóstico anterior a la implementación. Para el estado posterior de amistades, solicitudes, notificaciones y Battleship, consultar [IMPLEMENTATION_REPORT.md](IMPLEMENTATION_REPORT.md).

Actualización de alcance: el usuario indicó mantener **.NET 10** y trabajar en amistades, solicitudes de amistad, notificaciones y Battleship. El plan vigente para este trabajo está en [TEAM_MODULE_PLAN.md](TEAM_MODULE_PLAN.md). Los demás módulos se integrarán con el trabajo de sus compañeros. El diagnóstico global se conserva como referencia del proyecto completo.

## Alcance y fuentes

Esta revisión responde a la solicitud de analizar y planificar antes de escribir código. Solo se agrega documentación; no se modifican clases, proyectos, migraciones ni configuración de la aplicación.

Fuentes revisadas:

- `D:/Downloads/Mini proyecto LinkUp Pro.pdf`: documento funcional, versión 2.0, 100 páginas. Se revisó el texto completo y ejemplos visuales de posicionamiento/tablero.
- `D:/Downloads/Evaluacion mini proyecto LinkUp Pro.pdf`: rúbrica, 6 páginas, 151 criterios de 10 puntos, total de 1510 puntos.
- Los cinco proyectos de `LinkUpPro.slnx`: entidades, contratos, servicios, inyección de dependencias, configuraciones, migraciones, controlador y vistas.

Los PDF son fuentes de requisitos del producto. Sus instrucciones para construirlo no autorizan implementar durante este análisis. Las decisiones de diseño propuestas aquí se distinguen de los requisitos expresos.

## Diagnóstico

Existe una base de dominio y persistencia aprovechable. Todavía no hay un módulo funcional completo conectado desde sus pantallas hasta sus reglas de negocio y almacenamiento. Contar clases o carpetas no permite calcular un porcentaje funcional fiable.

Los 151 criterios necesitan cierre o verificación. Varios tienen código de soporte; otros carecen de implementación específica y el requisito de .NET presenta un desajuste. La clasificación individual está en [REQUIREMENTS_MATRIX.md](REQUIREMENTS_MATRIX.md). Esto no es una calificación académica ni significa que se deba rehacer todo lo existente.

| Grupo de la rúbrica | Criterios | Situación actual |
| --- | ---: | --- |
| Funcionalidades generales | 5 | Layout de plantilla; faltan menú, navegación y protección. |
| Login, registro y recuperación | 18 | Identity configurado y contratos iniciales; servicio de cuentas vacío. |
| Publicaciones, comentarios y reacciones | 21 | Entidades y persistencia; faltan servicios y pantallas. |
| Notificaciones | 6 | Entidad y repositorio; falta el flujo funcional. |
| Amigos | 15 | Relación y estados; falta gestión, consultas y permisos. |
| Solicitudes de amistad | 17 | Modelo y restricciones; falta ejecutar su ciclo de vida. |
| Battleship | 26 | Módulo ausente. |
| Mi perfil | 11 | Modelo y contratos iniciales; falta actualización segura. |
| Seguridad funcional | 14 | Configuración parcial; faltan controles de los casos de uso. |
| Reglas técnicas y arquitectura | 18 | Estructura creada; faltan Shared e integración completa. |
| **Total** | **151** | **Todos requieren cierre o verificación.** |

## Base existente

- Cinco proyectos: `Core.Domain`, `Core.Application`, `Infrastructure.Persistence`, `Infrastructure.Identity` y `LinkUpPro` como presentación MVC.
- Domain no referencia infraestructura. Application referencia Domain. Infraestructura referencia las capas interiores; Web reúne las implementaciones. La dirección entre proyectos es una base compatible con Onion, aunque aún faltan responsabilidades y flujos completos.
- Seis entidades: `Post`, `Comment`, `Reaction`, `Friendship`, `FriendshipRequest` y `Notification`, con sus seis enumeraciones.
- `AppUser` extiende Identity con nombre, apellido, foto y fechas. El teléfono puede usar `PhoneNumber`, heredado de `IdentityUser`; no necesita otra propiedad duplicada.
- `GenericRepository` y seis repositorios específicos registrados. Los específicos todavía solo heredan operaciones comunes.
- Seis configuraciones de entidades y dos contextos: `LinkUpProAppContext` e `IdentityContext`. Ambos tienen migraciones iniciales e historiales de migración separados sobre la conexión configurada.
- Restricciones SQL para una amistad por pareja ordenada, una solicitud pendiente por pareja en cualquier dirección, una reacción por usuario/publicación y exclusión entre imagen y enlace de YouTube.
- `ITransactionManager` y su implementación para el contexto de la aplicación. No cubren automáticamente Identity ni archivos y todavía no son usados por casos de negocio.
- Once DTOs y nueve ViewModels, incluyendo sus bases, concentrados en usuarios/correo.
- Servicio genérico con `IMapper`; AutoMapper registrado, pero cero perfiles.
- `IAccountServiceForWebApp` y una clase `AccountServiceForWebApp` vacía, sin implementar la interfaz ni registrarse como su implementación.
- `IEmailService` sin implementación.
- Identity configura contraseñas fuertes, correo confirmado, bloqueo de cinco intentos/15 minutos, tokens de confirmación de 24 horas y recuperación de una hora.
- Cookies con HttpOnly, Secure, SameSite, 30 minutos y tratamiento de persistencia de hasta siete días. Falta conectarlas y comprobarlas en autenticación real.
- Un `HomeController` que retorna una bienvenida y un layout Bootstrap de plantilla.

## Hallazgos prioritarios

| Prioridad | Evidencia | Acción prevista |
| --- | --- | --- |
| Decisión del usuario | Los cinco `.csproj` usan `net10.0`; el documento funcional p. 98 y la rúbrica p. 5 exigen .NET 9. | Mantener .NET 10 por instrucción expresa del usuario. Se registra la diferencia con la especificación sin planificar un cambio de framework. |
| Alta | El build sin restauración termina con 24 errores NU1100, con mensajes de `PackageSourceMapping` y paquetes no resueltos. | Resolver configuración/restauración y compilar antes de construir módulos. No se atribuye a sintaxis ni se afirma que alguna versión no exista. |
| Alta | Servicio de cuentas vacío y registro de servicios específicos de Application vacío. | Implementar y conectar servicios por módulo. El CRUD genérico no implementa sus reglas. |
| Alta | No existe `Infrastructure.Shared`. | Crear la capa requerida para correo e imágenes, con interfaces en Application. |
| Alta | Home no tiene `[Authorize]`; la ruta inicial es Home y `/Login` no existe. | Construir entrada pública, redirecciones y rutas protegidas. El middleware no reemplaza la autorización. |
| Alta | Battleship no tiene entidades, contratos, persistencia, servicios ni vistas. | Diseñar y construir el módulo completo: 26 criterios funcionales y otros transversales. |
| Alta | `GenericRepository.DeleteAsync` usa `Remove`; el servicio genérico delega esa operación. | Crear operaciones explícitas de eliminación lógica para publicaciones, comentarios, amistades e historial de solicitudes. Una reacción sí puede eliminar su registro. |
| Media | Los ocho ViewModels de usuario conservan namespaces `InvestmentApp...`; `DeleteUserViewModel` usa además `.Asset`. | Unificar namespaces `LinkUpPro...` y revisar contratos ajenos al alcance. |
| Alta | `UpdateUserViewModel` acepta correo, usuario y rol y no tiene contraseña actual. `CreateUserViewModel.Id` es `int`, frente a `string` de Identity. | Separar registro, perfil y cambio de contraseña; permitir solo campos autorizados. |
| Media | `RememberMe` está en `LoginViewModel`, pero no en `LoginDto`. | Propagar la opción hasta autenticación. |
| Media | Hay roles en contratos sin un requisito de administración de roles; el contexto es `IdentityUserContext`. | Retirar campos de rol innecesarios de los flujos públicos. No agregar administración no solicitada. |
| Alta | Faltan validación dominicana de teléfono, imagen real/tamaño y reglas de formularios. | Validar en ViewModels y comprobar invariantes y permisos en servicios. `required` y `[DataType]` no sustituyen validaciones HTTP. |
| Media | AutoMapper no tiene perfiles. | Crear perfiles Entity/DTO y DTO/ViewModel, ignorando campos controlados por el servidor. |
| Media | Application referencia EF Core y HTTP; sus ViewModels usan `IFormFile`. | Como decisión de diseño, mover ViewModels a Web y retirar dependencias que dejen de usarse. |
| Alta | No se implementa uso único/invalidez de tokens anteriores ni reenvío con espera de cinco minutos. | Diseñar seguimiento o versionado de emisión compatible con Identity. La duración configurada solo cubre una parte. |
| Alta | Ambos contextos SQL activan `EnableSensitiveDataLogging()` sin condición. `/Home/Error` está configurada, pero su acción no existe. | Revisar logs y construir manejo seguro de errores. |
| Alta | Hay restricciones para amistad/solicitudes/reacciones; faltan casos concurrentes verificados y control para el juego. | Combinar restricciones, transacciones y control de versión o actualizaciones condicionales según la operación. |

### Sobre `Friendship.cs`

La entidad actual sirve como base de una relación bidireccional: `User1Id`, `User2Id` y `Status`, con estados `Active` y `Removed`. Su configuración tiene clave compuesta y exige ordenar los identificadores.

No hace falta añadir un `Id` artificial ni heredar de `BasicEntity<int>` para cumplir el PDF. Falta que el servicio normalice siempre la pareja de acuerdo con la restricción SQL, consulte ambos participantes, elimine lógicamente y reactive la relación anterior al aceptar una nueva solicitud. Aceptar y crear/reactivar amistad debe ejecutarse como una operación atómica.

### Verificación realizada y sus límites

Se ejecutó `dotnet build LinkUpPro.slnx --no-restore --nologo`. Domain compiló; la solución completa falló con 24 errores NU1100 de resolución de paquetes. El SDK local incluye .NET 9 y .NET 10. No se cambió la configuración NuGet del equipo ni se instalaron paquetes.

No se ejecutaron migraciones ni se modificó una base de datos. Tener migraciones no demuestra que estén aplicadas. Tampoco se verificaron flujos en navegador; no hay proyectos de pruebas en la solución revisada. Los estados de la matriz se basan en revisión estática, no en pruebas funcionales exitosas.

## Arquitectura y convenciones propuestas

La separación siguiente es una decisión de diseño para cumplir Onion y las exigencias de DTOs, ViewModels y Shared:

| Capa | Responsabilidad | Dependencias |
| --- | --- | --- |
| `Core.Domain` | Entidades, enums, invariantes puras y contratos de repositorio existentes. | Sin referencias a los otros proyectos. |
| `Core.Application` | Casos de uso, interfaces, DTOs, permisos y coordinación de transacciones; perfiles Entity/DTO. | Domain y bibliotecas generales necesarias. |
| `Infrastructure.Persistence` | EF Core, consultas, repositorios, configuraciones, migraciones y transacciones del contexto de aplicación. | Capas interiores. |
| `Infrastructure.Identity` | Cuentas mediante Identity, contexto y migraciones de usuarios. | Capas interiores; no implementaciones concretas de Shared. |
| `Infrastructure.Shared` | Correo e imágenes y configuración de sus proveedores. | Contratos de Application. |
| `LinkUpPro` | Controladores, ViewModels, vistas, perfiles DTO/ViewModel y composición de dependencias. | Application; infraestructura para el arranque. |

Flujo: View → ViewModel → Controller → Service → Repository → almacenamiento. El resultado vuelve como DTO y se transforma a ViewModel. El controlador valida `ModelState`, llama servicios y retorna resultados. Los servicios vuelven a comprobar reglas sensibles.

El núcleo no debe incorporar `AppUser`, `DbContext`, `IFormFile` ni tipos de Identity. Web adapta el archivo HTTP a streams/metadatos o contratos de Application para el almacenamiento.

Mantener repositorios y servicios genéricos exigidos para operaciones comunes. Agregar operaciones específicas para consultas y transiciones de negocio. Limitar la exposición de `IQueryable` y las navegaciones por strings mediante consultas específicas; no agregar EF Core a los servicios para compensarlo.

### SOLID aplicado

| Principio | Aplicación concreta |
| --- | --- |
| Responsabilidad única | Separar publicaciones, correo y archivos; las reglas de coordenadas/turnos no pertenecen a controladores o vistas. |
| Abierto/cerrado | Cambiar proveedor de correo o imágenes mediante interfaces e inyección sin reescribir los casos de uso. |
| Sustitución de Liskov | Respetar contratos; expresar eliminación lógica como operación clara, evitando cambiar silenciosamente el significado del CRUD común. |
| Segregación de interfaces | Contratos por módulo. Separar consultas de usuarios y perfil del contrato de cuentas cuando simplifique los consumidores. |
| Inversión de dependencias | Application consume abstracciones implementadas por Persistence, Identity y Shared; Web conecta las implementaciones. |

SOLID no exige una interfaz por cada clase ni capas adicionales sin responsabilidad concreta.

### Nombres e idioma

- Identificadores técnicos en inglés: clases, interfaces, métodos, propiedades, DTOs, ViewModels, enums, namespaces y carpetas. Instrucciones de trabajo en español; mensajes visibles en español según los PDF.
- Tipos, métodos y propiedades en `PascalCase`; parámetros/variables en `camelCase`; interfaces con `I`; métodos asíncronos con `Async`; sufijos `Dto` y `ViewModel`.
- Unificar `FirstName`, `LastName`, `PhoneNumber`, `ProfilePicturePath` y una convención de estado documentada. Evitar mezclar `Name`/`FirstName`, `Phone`/`PhoneNumber` o `isVerified`/`IsVerified` para el mismo concepto.
- Identificadores de usuarios como `string`; conservar claves compuestas cuando correspondan.
- Contratos específicos propuestos: `RegisterUserDto`, `UpdateProfileDto`, `ChangePasswordDto`, `CreatePostDto`, `UpdatePostDto`, `PostFilterDto`, `SendFriendshipRequestDto`, `PlaceShipDto` y `AttackDto`. No son clases ya implementadas.
- El servidor determina usuario actual, autor, fechas, estados derivados, turnos, resultados y ganador.
- Persistir fechas consistentemente, preferiblemente UTC como recomienda el PDF; convertir al mostrar e incluir completo el día final de filtros.

## Reglas sensibles para la implementación

1. Home muestra publicaciones propias. Amigos muestra publicaciones visibles de amigos actuales con cuentas activas. `OnlyMe` solo permite acceso al autor. Revalidar permisos al consultar y actuar, aunque haya un formulario abierto.
2. Publicación: texto obligatorio de hasta 1000 caracteres y exactamente una imagen o video YouTube. Imagen real `.jpg`, `.jpeg`, `.png` o `.webp`, máximo 5 MB. YouTube se incrusta con identificador validado, sin HTML arbitrario.
3. Desactivar comentarios impide crear comentarios/respuestas, pero conserva el hilo y permite editar/eliminar interacciones propias existentes y reaccionar. El acceso a la publicación sigue siendo obligatorio.
4. Un comentario eliminado con respuestas conserva un marcador y la estructura. El padre debe pertenecer a la misma publicación y no formar ciclos.
5. Responder notifica al autor del contenido respondido; comentar/reaccionar al autor de la publicación. Sin auto notificaciones. Quitar reacción no notifica. No se exige tiempo real.
6. Solicitud y amistad son registros diferentes. Rechazar/cancelar conserva historia; ocultar una aceptada no elimina amistad. Aceptar crea/reactiva una sola relación atómicamente.
7. Terminar amistad conserva interacciones y partidas existentes. Se exige amistad para crear una partida nueva, no para continuar la anterior.
8. Perfil permite nombre, apellido, teléfono y foto opcional. Usuario/correo son de consulta. Cambiar contraseña requiere la vigente y los tres campos completos, invalida sesiones y cierra la actual. Los fallos no pueden dejar cambios parciales.
9. Guardar la nueva imagen antes de retirar la anterior. Si falla la persistencia, compensar los archivos creados. Una transacción SQL no revierte el sistema de archivos.
10. Tokens Identity con vigencia, uso único e invalidación de emisiones anteriores; reenvío de activación con espera de cinco minutos. Recuperar contraseña no activa la cuenta; restablecerla con éxito elimina bloqueo e invalida sesiones.
11. Battleship: **12×12**, cinco barcos por jugador de tamaños **2, 3, 3, 4 y 5**, cuatro direcciones, sin salir del tablero ni superponerse. El creador ataca primero; cada ataque válido alterna turno, incluso al acertar.
12. Abandono tras **48 horas desde la asignación del turno actual**, evaluado al entrar al módulo/partida, refrescar o actuar. La colocación no caduca. No se requiere un proceso programado.
13. Durante el juego no se expone la flota rival ni su tablero de ataque. En resultados, los participantes consultan ambos tableros de ataque finales en modo de solo lectura. Aciertos rojos, fallos verdes y actualización manual.

Referencias al documento funcional: cuentas pp. 3–13 y 93–96; publicaciones/interacciones pp. 14–31; amigos pp. 31–47; solicitudes pp. 47–61; Battleship pp. 62–80; perfil pp. 81–88; seguridad pp. 88–98; requisitos técnicos pp. 98–100.

## Plan por etapas

Este es el panorama general del proyecto completo. Para el alcance asignado a este trabajo y sus dependencias con los compañeros, seguir [TEAM_MODULE_PLAN.md](TEAM_MODULE_PLAN.md).

Cada etapa entrega comportamiento revisable. Autorización, validación, fechas y CSRF se implementan con cada módulo; la etapa final comprueba el conjunto. Crear clases sin integrar el flujo no cierra una etapa.

### 1. Base ejecutable y contratos coherentes

- Mantener .NET 10 y referencias compatibles; resolver restauración sin modificar configuración global innecesariamente.
- Compilar, revisar dependencias Onion, corregir namespaces/IDs y ubicar ViewModels en Web.
- Definir casos de uso, mapeos, usuario actual, fechas y errores. Mantener componentes existentes útiles.
- Preparar configuración local y verificar los dos contextos/migraciones sin borrar datos ni migraciones aplicadas sin evaluar su estado.

**Cierre:** restore/build correctos, contratos coherentes y persistencia local reproducible. Atiende principalmente 134–143 y 146.

### 2. Shared y ciclo completo de cuentas

- Crear `Infrastructure.Shared`, implementar `IEmailService` e `IImageStorageService` y registrarlos.
- Implementar `IAccountServiceForWebApp` con Identity y contratos específicos, incluido reenvío y persistencia de sesión.
- Crear controladores, vistas y perfiles de login, registro, confirmación, reenvío y recuperación.
- Completar tokens, espera de reenvío, desbloqueo tras restablecimiento y revocación de sesiones.
- Construir entrada pública/protección interna, mensajes de expiración, visibilidad/fortaleza de contraseñas y logout POST con antiforgery.

**Cierre:** registro con foto → correo → activación → login; bloqueo, reenvío, recuperación y sesiones cumplen sus límites. Criterios 6–23, 125–128 y 144–145, más seguridad asociada.

### 3. Solicitudes y amistades

- Implementar `IFriendshipRequestService`, `IFriendshipService` y consultas de usuarios activos/elegibles.
- Construir envío, aceptación, rechazo, cancelación y ocultación con confirmaciones.
- Normalizar parejas; aceptar con transacción y creación/reactivación de amistad; manejar conflictos concurrentes.
- Crear listado/buscador de amigos, amigos en común, eliminación lógica y contador de solicitudes recibidas.

**Cierre:** ciclo completo entre dos usuarios sin duplicados, cambios ajenos ni confusión entre ocultar historial y terminar amistad. Publicaciones y partidas se integran después. Criterios 59–63 y 66–82.

### 4. Publicaciones y privacidad

- Crear contratos, perfiles e `IPostService` para publicaciones propias y de amigos.
- Implementar crear/editar texto y multimedia, reemplazo de archivos, YouTube y eliminación lógica.
- Aplicar privacidad, propiedad, cuenta activa y amistad en el servidor.
- Construir Home, publicaciones de Amigos y perfil del amigo con filtros combinables y fechas inclusivas.

**Cierre:** cada listado muestra solo lo autorizado; terminar amistad o cambiar privacidad revoca acceso en la siguiente consulta/acción. Criterios 24–39, 51–54 y 57–58.

### 5. Conversaciones, reacciones y notificaciones

- Implementar `ICommentService`, `IReactionService` e `INotificationService` y sus contratos.
- Crear hilos anidados, edición propia, borrado con marcador y reglas de comentarios deshabilitados.
- Registrar/cambiar/quitar una reacción por usuario/publicación.
- Generar destinatarios correctos, listado seguro, lectura individual/global y contador del menú.

**Cierre:** los hilos se conservan, no se edita contenido ajeno y no hay auto notificaciones. Los enlaces revalidan acceso. Criterios 40–50 y 55–56; integración con 64.

### 6. Perfil consistente

- Crear contratos de edición específicos y separar servicio de perfil si beneficia al contrato de cuentas.
- Implementar datos personales, teléfono dominicano, foto opcional y campos de consulta protegidos.
- Coordinar datos/foto/contraseña para evitar cambios parciales; invalidar sesiones y cerrar la actual al cambiar contraseña.
- Consultar datos actuales del autor en los demás módulos.

**Cierre:** sin nueva foto se conserva la existente; fallos no guardan parcialmente; manipular el formulario no cambia correo, usuario o estado. Criterios 109–119.

### 7. Battleship: creación y colocación

- Diseñar como propuesta inicial `BattleshipGame`, `ShipPlacement`, `Attack`, enums de estado/dirección/finalización y un tipo de coordenada. Revisar el modelo antes de migrarlo.
- Definir participantes, creador, fase, turno y fecha de asignación, fechas, ganador y control de concurrencia. Definir unicidad de partida activa por pareja, barco colocado y disparo por jugador/celda.
- Implementar repositorios/servicios/contratos sin exponer datos rivales.
- Construir selección de amigo, creación, acceso de ambos jugadores y exclusión de parejas con partida activa.
- Implementar flotas, selección de barco/celda/dirección, tablero 12×12, límites, solapamiento y espera.

**Cierre:** ambos usuarios configuran flotas válidas en una única partida activa; terceros no acceden. Criterios 83–84, 86–98 y 149.

### 8. Battleship: ataques, finalización e historial

- Empezar con el creador después de completar ambas flotas.
- Ejecutar disparo, acierto/fallo, hundimiento y cambio de turno atómicamente; impedir repetidos y fuera de turno.
- Implementar victoria, rendición y abandono de 48 horas al acceder/actuar; impedir finalizaciones dobles.
- Crear tableros propios, refresco manual, historial, estadísticas y resultados de solo lectura.
- Conservar partidas cuando termina amistad y bloquear nuevas hasta restablecerla.

**Cierre:** partida completa en dos navegadores con turnos estrictos y privacidad; rendición, abandono e historial correctos. Criterios 85, 99–108 y 65.

### 9. Integración y cierre de la rúbrica

- Completar menú, contadores, navegación, estados vacíos, mensajes y diseño responsivo.
- Auditar `[Authorize]`/`[AllowAnonymous]`, permisos de recurso, estado activo, POST/antiforgery, texto seguro e imágenes.
- Verificar restore/build, migraciones y ejecución reproducible; documentar configuración y demostración.
- Ejecutar pruebas dirigidas a permisos, transacciones, sesiones/tokens, archivos y concurrencia. Usar integración relacional para restricciones/transacciones: InMemory no demuestra su funcionamiento SQL.
- Recorrer los 151 criterios, registrar evidencia y cambiar estados solo al cumplirlos.

**Cierre:** criterios con evidencia o una observación explícita; compilación y comprobaciones de flujos completos satisfactorias. Atiende integración general, técnica y de seguridad.

## Pruebas relevantes por incorporar con cada etapa

- Solicitudes cruzadas y doble aceptación simultáneas no dejan duplicados ni estados parciales.
- Una amistad eliminada o privacidad cambiada bloquea acciones desde formularios previamente abiertos.
- Deshabilitar comentarios preserva edición/eliminación propia, hilos y reacciones; borrar el padre conserva respuestas.
- Reenvíos invalidan tokens anteriores; reutilizar/vencer tokens falla; recuperación no activa cuentas inactivas.
- Control del tiempo para comprobar inactividad/persistencia y vencimientos sin esperas reales prolongadas; cambios de contraseña invalidan sesiones previas.
- Fallos en imagen o contraseña no actualizan parcialmente perfil ni eliminan su foto anterior.
- Dos ataques del mismo turno o finalizaciones simultáneas solo admiten una transición válida.
- Abandono calculado desde el turno, no desde creación; colocación sin caducidad.
- Los datos ocultos del oponente no viajan en HTML, DTOs ni respuestas, además de no mostrarse visualmente.

El siguiente trabajo de implementación es la fase 1 del plan acotado en TEAM_MODULE_PLAN.md. Esta actualización solo cambia documentación y no inicia la implementación.
