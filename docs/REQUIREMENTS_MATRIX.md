# LinkUp Pro: matriz de requisitos

Fecha: 19 de octubre de 2026.

Esta matriz conserva la revisión inicial, anterior a los cambios de código. Los estados no representan la implementación actual. Consultar [IMPLEMENTATION_REPORT.md](IMPLEMENTATION_REPORT.md) para la evidencia posterior y las dependencias de integración; no se asignan automáticamente puntos a los 151 criterios por haber agregado pruebas.

Actualización del usuario: mantener .NET 10. El criterio R134 conserva el estado de diferencia frente al PDF, pero el cambio a .NET 9 queda fuera del plan. El alcance activo es amistades, solicitudes, notificaciones y Battleship; ver [TEAM_MODULE_PLAN.md](TEAM_MODULE_PLAN.md). Las etapas de esta matriz remiten al plan general original y no al nuevo reparto del equipo.

Fuente: `D:/Downloads/Evaluacion mini proyecto LinkUp Pro.pdf` (6 páginas). Se conserva el orden de sus 151 criterios; cada uno vale 10 puntos. Los IDs R001–R151 son identificadores de seguimiento agregados para este análisis.

Plan y evidencia de arquitectura: [PROJECT_ANALYSIS.md](PROJECT_ANALYSIS.md).

## Cómo interpretar los estados

- **Base parcial:** existe modelo, configuración, contrato o implementación común útil. No significa que el criterio esté cumplido ni asigna puntos.
- **Pendiente:** no se encontró el comportamiento específico implementado e integrado. Puede existir infraestructura común aprovechable.
- **Desajuste:** existe una implementación que difiere de un requisito expreso.
- **Cerrado:** requeriría evidencia de cumplimiento; ninguno se marca así en esta revisión.

El build de la solución falló por resolución de paquetes (NU1100). Solo Domain compiló. No se probaron flujos en navegador ni se aplicaron migraciones. Todos los criterios siguen pendientes de cierre o verificación.

## Conteo

| Estado de revisión | Criterios |
| --- | ---: |
| Base parcial | 43 |
| Pendiente | 107 |
| Desajuste | 1 |
| Cerrado con evidencia completa | 0 |
| **Total** | **151** |

Los ceros impresos en la columna de puntos del PDF son parte de la plantilla de evaluación; no son una nota otorgada al proyecto. Esta clasificación es cualitativa y no permite convertir los estados en un porcentaje de implementación.

## Funcionalidades generales (5 criterios)

| ID | Criterio de la rúbrica | Página de rúbrica | Estado | Evidencia / trabajo restante | Etapa propuesta |
| --- | --- | ---: | --- | --- | --- |
| R001 | Menú principal con todas las opciones requeridas | 1 | Pendiente | No se encontró una implementación específica de este comportamiento; falta construir e integrar su flujo y comprobarlo. | 9; protección desde 2 |
| R002 | Navegación correcta entre todos los módulos | 1 | Pendiente | No se encontró una implementación específica de este comportamiento; falta construir e integrar su flujo y comprobarlo. | 9; protección desde 2 |
| R003 | Uso consistente del layout general de la aplicación | 1 | Base parcial | Existe Views/Shared/_Layout.cshtml de plantilla; falta integrar los módulos. | 9; protección desde 2 |
| R004 | Mensajes de validación, confirmación y acceso denegado claros | 1 | Base parcial | Hay mensajes de DataAnnotations en ViewModels de usuario, sin pantallas conectadas ni mensajes completos del PDF. | 9; protección desde 2 |
| R005 | Protección del acceso directo por URL y validación de la sesión activa | 1 | Pendiente | No se encontró una implementación específica de este comportamiento; falta construir e integrar su flujo y comprobarlo. | 9; protección desde 2 |

## Login, registro y recuperación (18 criterios)

| ID | Criterio de la rúbrica | Página de rúbrica | Estado | Evidencia / trabajo restante | Etapa propuesta |
| --- | --- | ---: | --- | --- | --- |
| R006 | Pantalla de inicio de sesión con usuario, contraseña y Mantener sesión iniciada | 1 | Base parcial | LoginViewModel contiene los tres campos; no existe pantalla ni servicio de login. | 2 |
| R007 | Redirección al Home cuando un usuario autenticado intenta acceder al login | 1 | Pendiente | No se encontró una implementación específica de este comportamiento; falta construir e integrar su flujo y comprobarlo. | 2 |
| R008 | Control para mostrar y ocultar los campos de contraseña | 1 | Pendiente | No se encontró una implementación específica de este comportamiento; falta construir e integrar su flujo y comprobarlo. | 2 |
| R009 | Validación de credenciales con mensaje genérico para datos incorrectos | 1 | Pendiente | No se encontró una implementación específica de este comportamiento; falta construir e integrar su flujo y comprobarlo. | 2 |
| R010 | Bloqueo del acceso a cuentas que todavía no han sido activadas | 1 | Base parcial | Identity exige RequireConfirmedEmail; falta autenticar y mostrar el rechazo especificado. | 2 |
| R011 | Bloqueo temporal después de cinco intentos fallidos durante 15 minutos | 1 | Base parcial | Identity configura cinco intentos y 15 minutos; falta ejecutar y verificar el flujo de intentos. | 2 |
| R012 | Funcionamiento correcto de Mantener sesión iniciada por un máximo de siete días | 1 | Base parcial | La cookie contempla siete días si IsPersistent; LoginDto no transporta RememberMe y no hay autenticación. | 2 |
| R013 | Cierre de sesión después de 30 minutos de inactividad cuando no se mantiene la sesión | 1 | Base parcial | La cookie configura 30 minutos y SlidingExpiration; faltan flujo y mensaje por inactividad. | 2 |
| R014 | Registro con todos los campos personales y credenciales requeridos | 1 | Base parcial | RegisterUserViewModel aporta campos iniciales; teléfono es opcional y faltan controlador, servicio y vista. | 2 |
| R015 | Validación del teléfono con formato de República Dominicana | 1 | Pendiente | No se encontró una implementación específica de este comportamiento; falta construir e integrar su flujo y comprobarlo. | 2 |
| R016 | Validación única de nombre de usuario y correo sin distinguir mayúsculas | 1 | Base parcial | Identity tiene correo único e índice normalizado; faltan registro, validaciones y mensajes de duplicidad. | 2 |
| R017 | Validación de foto de perfil, formato permitido y tamaño máximo | 1 | Pendiente | No se encontró una implementación específica de este comportamiento; falta construir e integrar su flujo y comprobarlo. | 2 |
| R018 | Indicador visual y validación de fortaleza de contraseña | 1 | Base parcial | Identity configura los cinco criterios de contraseña; falta indicador visual y flujo de registro. | 2 |
| R019 | Creación de la cuenta en estado inactivo y envío del correo de activación | 1 | Pendiente | No se encontró una implementación específica de este comportamiento; falta construir e integrar su flujo y comprobarlo. | 2 |
| R020 | Activación de cuenta mediante token válido, vigente y de un solo uso | 1 | Base parcial | Existe proveedor de confirmación con duración de 24 horas; falta activación y consumo/invalidez de tokens. | 2 |
| R021 | Reenvío del correo de activación con espera mínima de cinco minutos | 1 | Pendiente | No se encontró una implementación específica de este comportamiento; falta construir e integrar su flujo y comprobarlo. | 2 |
| R022 | Solicitud y envío del enlace para restablecer la contraseña sin revelar cuentas | 1 | Pendiente | No se encontró una implementación específica de este comportamiento; falta construir e integrar su flujo y comprobarlo. | 2 |
| R023 | Restablecimiento correcto de contraseña, desbloqueo e invalidación de sesiones anteriores | 1 | Pendiente | No se encontró una implementación específica de este comportamiento; falta construir e integrar su flujo y comprobarlo. | 2 |

## Publicaciones e interacciones (21 criterios)

| ID | Criterio de la rúbrica | Página de rúbrica | Estado | Evidencia / trabajo restante | Etapa propuesta |
| --- | --- | ---: | --- | --- | --- |
| R024 | Listado de publicaciones propias ordenado de la más reciente a la más antigua | 1 | Pendiente | No se encontró una implementación específica de este comportamiento; falta construir e integrar su flujo y comprobarlo. | 4 |
| R025 | Visualización completa de autor, fecha, contenido, multimedia, privacidad e interacciones | 1 | Pendiente | No se encontró una implementación específica de este comportamiento; falta construir e integrar su flujo y comprobarlo. | 4 |
| R026 | Buscador de publicaciones propias por contenido | 2 | Pendiente | No se encontró una implementación específica de este comportamiento; falta construir e integrar su flujo y comprobarlo. | 4 |
| R027 | Filtros combinables por tipo, fechas y estado de edición | 2 | Pendiente | No se encontró una implementación específica de este comportamiento; falta construir e integrar su flujo y comprobarlo. | 4 |
| R028 | Creación de publicación con texto y selección de Imagen o Video de YouTube | 2 | Pendiente | No se encontró una implementación específica de este comportamiento; falta construir e integrar su flujo y comprobarlo. | 4 |
| R029 | Validación de que cada publicación tenga texto y exactamente un contenido multimedia | 2 | Base parcial | PostEntityConfiguration comprueba exclusión imagen/YouTube; falta validación completa de creación y contenido textual. | 4 |
| R030 | Comportamiento condicional correcto de los campos Imagen y Enlace de YouTube | 2 | Pendiente | No se encontró una implementación específica de este comportamiento; falta construir e integrar su flujo y comprobarlo. | 4 |
| R031 | Validación del texto, archivo de imagen y enlace de YouTube | 2 | Pendiente | No se encontró una implementación específica de este comportamiento; falta construir e integrar su flujo y comprobarlo. | 4 |
| R032 | Reproducción incrustada de videos de YouTube dentro de la aplicación | 2 | Pendiente | No se encontró una implementación específica de este comportamiento; falta construir e integrar su flujo y comprobarlo. | 4 |
| R033 | Manejo correcto de privacidad Solo amigos y Solo yo | 2 | Base parcial | PostPrivacy y su restricción SQL representan FriendsOnly/OnlyMe; no hay autorización ni consultas funcionales. | 4 |
| R034 | Configuración de Permitir comentarios con valor activo por defecto | 2 | Base parcial | La configuración SQL establece AllowComments por defecto en true; faltan formulario y servicio. | 4 |
| R035 | Edición de publicaciones únicamente por su autor | 2 | Pendiente | No se encontró una implementación específica de este comportamiento; falta construir e integrar su flujo y comprobarlo. | 4 |
| R036 | Cambio correcto entre imagen y video sin conservar el contenido anterior | 2 | Pendiente | No se encontró una implementación específica de este comportamiento; falta construir e integrar su flujo y comprobarlo. | 4 |
| R037 | Cambio de privacidad conservando comentarios, respuestas y reacciones | 2 | Pendiente | No se encontró una implementación específica de este comportamiento; falta construir e integrar su flujo y comprobarlo. | 4 |
| R038 | Activación y desactivación de comentarios sin eliminar interacciones anteriores | 2 | Pendiente | No se encontró una implementación específica de este comportamiento; falta construir e integrar su flujo y comprobarlo. | 4 |
| R039 | Eliminación lógica de publicaciones y retiro de todos los listados | 2 | Base parcial | Post tiene IsDeleted; el DeleteAsync genérico elimina físicamente y no hay consultas que apliquen la regla. | 4 |
| R040 | Creación y visualización de comentarios con sus validaciones | 2 | Pendiente | No se encontró una implementación específica de este comportamiento; falta construir e integrar su flujo y comprobarlo. | 5 |
| R041 | Creación de respuestas y mantenimiento de conversaciones anidadas | 2 | Base parcial | Comment tiene ParentCommentId, ParentComment y Replies; falta crear, validar y presentar conversaciones. | 5 |
| R042 | Edición y eliminación de comentarios y respuestas únicamente por su autor | 2 | Pendiente | No se encontró una implementación específica de este comportamiento; falta construir e integrar su flujo y comprobarlo. | 5 |
| R043 | Conservación del hilo mediante el mensaje Este comentario fue eliminado | 2 | Pendiente | No se encontró una implementación específica de este comportamiento; falta construir e integrar su flujo y comprobarlo. | 5 |
| R044 | Registro, cambio y eliminación de reacciones Me gusta y No me gusta | 2 | Base parcial | Reaction tiene clave compuesta usuario/publicación y enum Like/Dislike; faltan acciones y contadores. | 5 |

## Notificaciones (6 criterios)

| ID | Criterio de la rúbrica | Página de rúbrica | Estado | Evidencia / trabajo restante | Etapa propuesta |
| --- | --- | ---: | --- | --- | --- |
| R045 | Generación de notificaciones por comentarios, respuestas y reacciones de otros usuarios | 2 | Pendiente | No se encontró una implementación específica de este comportamiento; falta construir e integrar su flujo y comprobarlo. | 5 |
| R046 | Asignación correcta del destinatario según el tipo de interacción | 2 | Pendiente | No se encontró una implementación específica de este comportamiento; falta construir e integrar su flujo y comprobarlo. | 5 |
| R047 | Listado de notificaciones ordenado de la más reciente a la más antigua | 2 | Pendiente | No se encontró una implementación específica de este comportamiento; falta construir e integrar su flujo y comprobarlo. | 5 |
| R048 | Acceso al contenido relacionado respetando existencia, amistad y privacidad | 2 | Pendiente | No se encontró una implementación específica de este comportamiento; falta construir e integrar su flujo y comprobarlo. | 5 |
| R049 | Manejo de estados Leída y No leída y opción Marcar todas como leídas | 2 | Base parcial | Notification tiene IsRead; faltan marcado individual/global y sección de notificaciones. | 5 |
| R050 | Contador de notificaciones no leídas actualizado al cargar o recargar la pantalla | 2 | Pendiente | No se encontró una implementación específica de este comportamiento; falta construir e integrar su flujo y comprobarlo. | 5 |

## Amigos (15 criterios)

| ID | Criterio de la rúbrica | Página de rúbrica | Estado | Evidencia / trabajo restante | Etapa propuesta |
| --- | --- | ---: | --- | --- | --- |
| R051 | Resumen correcto de amigos activos y publicaciones disponibles | 2 | Pendiente | No se encontró una implementación específica de este comportamiento; falta construir e integrar su flujo y comprobarlo. | 4 |
| R052 | Listado de publicaciones Solo amigos ordenado de la más reciente a la más antigua | 2 | Pendiente | No se encontró una implementación específica de este comportamiento; falta construir e integrar su flujo y comprobarlo. | 4 |
| R053 | Buscador y filtros combinables de publicaciones de amigos | 2 | Pendiente | No se encontró una implementación específica de este comportamiento; falta construir e integrar su flujo y comprobarlo. | 4 |
| R054 | Validación de privacidad y amistad vigente antes de mostrar o interactuar | 3 | Pendiente | No se encontró una implementación específica de este comportamiento; falta construir e integrar su flujo y comprobarlo. | 4 |
| R055 | Comentarios, respuestas y reacciones sobre publicaciones de amigos | 3 | Pendiente | No se encontró una implementación específica de este comportamiento; falta construir e integrar su flujo y comprobarlo. | 5 |
| R056 | Respeto de la configuración de comentarios desactivados | 3 | Pendiente | No se encontró una implementación específica de este comportamiento; falta construir e integrar su flujo y comprobarlo. | 5 |
| R057 | Reproducción de videos de YouTube en publicaciones de amigos | 3 | Pendiente | No se encontró una implementación específica de este comportamiento; falta construir e integrar su flujo y comprobarlo. | 4 |
| R058 | Perfil del amigo con datos y publicaciones visibles correspondientes | 3 | Pendiente | No se encontró una implementación específica de este comportamiento; falta construir e integrar su flujo y comprobarlo. | 4 |
| R059 | Listado de amigos ordenado alfabéticamente con sus datos y acciones | 3 | Pendiente | No se encontró una implementación específica de este comportamiento; falta construir e integrar su flujo y comprobarlo. | 3 |
| R060 | Buscador de amigos por nombre, apellido o nombre de usuario | 3 | Pendiente | No se encontró una implementación específica de este comportamiento; falta construir e integrar su flujo y comprobarlo. | 3 |
| R061 | Cálculo y listado correcto de amigos en común | 3 | Pendiente | No se encontró una implementación específica de este comportamiento; falta construir e integrar su flujo y comprobarlo. | 3 |
| R062 | Manejo de estados Activa y Eliminada para la relación de amistad | 3 | Base parcial | FriendshipStatus y restricción SQL definen Active/Removed; faltan transiciones mediante servicio. | 3 |
| R063 | Eliminación lógica y bidireccional de una amistad con confirmación | 3 | Pendiente | No se encontró una implementación específica de este comportamiento; falta construir e integrar su flujo y comprobarlo. | 3 |
| R064 | Conservación de publicaciones, interacciones, notificaciones y partidas anteriores | 3 | Pendiente | No se encontró una implementación específica de este comportamiento; falta construir e integrar su flujo y comprobarlo. | 3–5 y 8 |
| R065 | Continuación de partidas existentes y bloqueo de nuevas partidas después de eliminar la amistad | 3 | Pendiente | No se encontró una implementación específica de este comportamiento; falta construir e integrar su flujo y comprobarlo. | 8 |

## Solicitudes de amistad (17 criterios)

| ID | Criterio de la rúbrica | Página de rúbrica | Estado | Evidencia / trabajo restante | Etapa propuesta |
| --- | --- | ---: | --- | --- | --- |
| R066 | Contador de solicitudes recibidas pendientes de respuesta | 3 | Pendiente | No se encontró una implementación específica de este comportamiento; falta construir e integrar su flujo y comprobarlo. | 3 |
| R067 | Manejo correcto de estados En espera, Aceptada, Rechazada y Cancelada | 3 | Base parcial | FriendshipRequestStatus y restricciones SQL definen estados/fechas; no hay ciclo de vida funcional. | 3 |
| R068 | Listado de solicitudes pendientes de responder con información requerida | 3 | Pendiente | No se encontró una implementación específica de este comportamiento; falta construir e integrar su flujo y comprobarlo. | 3 |
| R069 | Cálculo y consulta de amigos en común en las solicitudes | 3 | Pendiente | No se encontró una implementación específica de este comportamiento; falta construir e integrar su flujo y comprobarlo. | 3 |
| R070 | Aceptación de solicitudes con confirmación y validaciones | 3 | Pendiente | No se encontró una implementación específica de este comportamiento; falta construir e integrar su flujo y comprobarlo. | 3 |
| R071 | Creación o reactivación atómica de una amistad al aceptar | 3 | Base parcial | Existen TransactionManager y clave única de amistad; no existe servicio de aceptación atómica. | 3 |
| R072 | Rechazo de solicitudes con confirmación y conservación del historial | 3 | Pendiente | No se encontró una implementación específica de este comportamiento; falta construir e integrar su flujo y comprobarlo. | 3 |
| R073 | Listado de solicitudes enviadas con estado y fecha de respuesta | 3 | Pendiente | No se encontró una implementación específica de este comportamiento; falta construir e integrar su flujo y comprobarlo. | 3 |
| R074 | Cancelación de solicitudes pendientes por su emisor | 3 | Pendiente | No se encontró una implementación específica de este comportamiento; falta construir e integrar su flujo y comprobarlo. | 3 |
| R075 | Eliminación lógica de solicitudes aceptadas o rechazadas del historial | 3 | Base parcial | FriendshipRequest tiene IsVisibleToSender; falta ocultar solo solicitudes aceptadas/rechazadas propias. | 3 |
| R076 | Listado de usuarios elegibles para una nueva solicitud | 3 | Pendiente | No se encontró una implementación específica de este comportamiento; falta construir e integrar su flujo y comprobarlo. | 3 |
| R077 | Buscador de usuarios disponibles por nombre de usuario | 3 | Pendiente | No se encontró una implementación específica de este comportamiento; falta construir e integrar su flujo y comprobarlo. | 3 |
| R078 | Selección de un único usuario mediante radio button | 3 | Pendiente | No se encontró una implementación específica de este comportamiento; falta construir e integrar su flujo y comprobarlo. | 3 |
| R079 | Envío de solicitud con todas las validaciones ejecutadas en el servidor | 3 | Pendiente | No se encontró una implementación específica de este comportamiento; falta construir e integrar su flujo y comprobarlo. | 3 |
| R080 | Prevención de solicitudes pendientes duplicadas en cualquier dirección | 3 | Base parcial | La migración incluye índice único filtrado IX_FriendshipRequests_Pending sobre pareja normalizada; falta manejo y verificación concurrente. | 3 |
| R081 | Separación correcta entre registros de solicitudes y relaciones de amistad | 3 | Base parcial | FriendshipRequest y Friendship son entidades/tablas separadas; falta la integración de sus transiciones. | 3 |
| R082 | Autorización correcta para aceptar, rechazar, cancelar y ocultar solicitudes | 3 | Pendiente | No se encontró una implementación específica de este comportamiento; falta construir e integrar su flujo y comprobarlo. | 3 |

## Battleship (26 criterios)

| ID | Criterio de la rúbrica | Página de rúbrica | Estado | Evidencia / trabajo restante | Etapa propuesta |
| --- | --- | ---: | --- | --- | --- |
| R083 | Pantalla principal con partidas activas e historial de partidas | 4 | Pendiente | No se encontró una implementación específica de este comportamiento; falta construir e integrar su flujo y comprobarlo. | 7 |
| R084 | Listado de partidas activas con oponente, fecha, duración y acciones | 4 | Pendiente | No se encontró una implementación específica de este comportamiento; falta construir e integrar su flujo y comprobarlo. | 7 |
| R085 | Rendición desde una partida activa con confirmación y ganador correcto | 4 | Pendiente | No se encontró una implementación específica de este comportamiento; falta construir e integrar su flujo y comprobarlo. | 8 |
| R086 | Listado de amigos activos disponibles para iniciar una partida | 4 | Pendiente | No se encontró una implementación específica de este comportamiento; falta construir e integrar su flujo y comprobarlo. | 7 |
| R087 | Exclusión de amigos con los que ya exista una partida activa | 4 | Pendiente | No se encontró una implementación específica de este comportamiento; falta construir e integrar su flujo y comprobarlo. | 7 |
| R088 | Buscador, selección y validación del oponente | 4 | Pendiente | No se encontró una implementación específica de este comportamiento; falta construir e integrar su flujo y comprobarlo. | 7 |
| R089 | Creación correcta de la partida y acceso de ambos jugadores | 4 | Pendiente | No se encontró una implementación específica de este comportamiento; falta construir e integrar su flujo y comprobarlo. | 7 |
| R090 | Creación de cinco barcos por jugador con tamaños 2, 3, 3, 4 y 5 | 4 | Pendiente | No se encontró una implementación específica de este comportamiento; falta construir e integrar su flujo y comprobarlo. | 7 |
| R091 | Listado exclusivo de barcos todavía no posicionados | 4 | Pendiente | No se encontró una implementación específica de este comportamiento; falta construir e integrar su flujo y comprobarlo. | 7 |
| R092 | Tablero de posicionamiento de 12x12 y selección de celda inicial | 4 | Pendiente | No se encontró una implementación específica de este comportamiento; falta construir e integrar su flujo y comprobarlo. | 7 |
| R093 | Selección de dirección arriba, abajo, izquierda o derecha | 4 | Pendiente | No se encontró una implementación específica de este comportamiento; falta construir e integrar su flujo y comprobarlo. | 7 |
| R094 | Validación para impedir que un barco quede fuera del tablero | 4 | Pendiente | No se encontró una implementación específica de este comportamiento; falta construir e integrar su flujo y comprobarlo. | 7 |
| R095 | Validación para impedir superposición entre barcos | 4 | Pendiente | No se encontró una implementación específica de este comportamiento; falta construir e integrar su flujo y comprobarlo. | 7 |
| R096 | Bloqueo de selección de celdas ocupadas | 4 | Pendiente | No se encontró una implementación específica de este comportamiento; falta construir e integrar su flujo y comprobarlo. | 7 |
| R097 | Finalización correcta de la configuración de los cinco barcos | 4 | Pendiente | No se encontró una implementación específica de este comportamiento; falta construir e integrar su flujo y comprobarlo. | 7 |
| R098 | Pantalla de espera cuando el otro jugador no ha terminado su configuración | 4 | Pendiente | No se encontró una implementación específica de este comportamiento; falta construir e integrar su flujo y comprobarlo. | 7 |
| R099 | Inicio de la fase de ataque por el usuario que creó la partida | 4 | Pendiente | No se encontró una implementación específica de este comportamiento; falta construir e integrar su flujo y comprobarlo. | 8 |
| R100 | Tablero de ataque con aciertos rojos y fallos verdes | 4 | Pendiente | No se encontró una implementación específica de este comportamiento; falta construir e integrar su flujo y comprobarlo. | 8 |
| R101 | Alternancia estricta de turnos después de cada ataque | 4 | Pendiente | No se encontró una implementación específica de este comportamiento; falta construir e integrar su flujo y comprobarlo. | 8 |
| R102 | Bloqueo de ataques fuera de turno o sobre una celda atacada anteriormente | 4 | Pendiente | No se encontró una implementación específica de este comportamiento; falta construir e integrar su flujo y comprobarlo. | 8 |
| R103 | Detección de barcos hundidos y finalización al hundir toda la flota | 4 | Pendiente | No se encontró una implementación específica de este comportamiento; falta construir e integrar su flujo y comprobarlo. | 8 |
| R104 | Actualización manual mediante Refrescar pantalla | 4 | Pendiente | No se encontró una implementación específica de este comportamiento; falta construir e integrar su flujo y comprobarlo. | 8 |
| R105 | Visualización del tablero propio sin revelar el tablero del oponente | 4 | Pendiente | No se encontró una implementación específica de este comportamiento; falta construir e integrar su flujo y comprobarlo. | 8 |
| R106 | Finalización por abandono después de 48 horas sin un ataque válido | 4 | Pendiente | No se encontró una implementación específica de este comportamiento; falta construir e integrar su flujo y comprobarlo. | 8 |
| R107 | Historial con fechas, duración, resultado, ganador y resumen estadístico | 4 | Pendiente | No se encontró una implementación específica de este comportamiento; falta construir e integrar su flujo y comprobarlo. | 8 |
| R108 | Consulta de tableros finales propios y del oponente en modo de solo lectura | 4 | Pendiente | No se encontró una implementación específica de este comportamiento; falta construir e integrar su flujo y comprobarlo. | 8 |

## Mi perfil (11 criterios)

| ID | Criterio de la rúbrica | Página de rúbrica | Estado | Evidencia / trabajo restante | Etapa propuesta |
| --- | --- | ---: | --- | --- | --- |
| R109 | Visualización de datos personales, correo y nombre de usuario | 4 | Pendiente | No se encontró una implementación específica de este comportamiento; falta construir e integrar su flujo y comprobarlo. | 6 |
| R110 | Carga de valores actuales y campos de contraseña vacíos | 4 | Pendiente | No se encontró una implementación específica de este comportamiento; falta construir e integrar su flujo y comprobarlo. | 6 |
| R111 | Edición y validación de nombre, apellido y teléfono | 5 | Pendiente | No se encontró una implementación específica de este comportamiento; falta construir e integrar su flujo y comprobarlo. | 6 |
| R112 | Actualización opcional y segura de la foto de perfil | 5 | Pendiente | No se encontró una implementación específica de este comportamiento; falta construir e integrar su flujo y comprobarlo. | 6 |
| R113 | Conservación de la foto anterior cuando no se selecciona una nueva | 5 | Pendiente | No se encontró una implementación específica de este comportamiento; falta construir e integrar su flujo y comprobarlo. | 6 |
| R114 | Cambio opcional de contraseña validando la contraseña actual | 5 | Pendiente | No se encontró una implementación específica de este comportamiento; falta construir e integrar su flujo y comprobarlo. | 6 |
| R115 | Mostrar u ocultar contraseñas e indicador de fortaleza | 5 | Pendiente | No se encontró una implementación específica de este comportamiento; falta construir e integrar su flujo y comprobarlo. | 6 |
| R116 | Invalidación de sesiones y redirección al login después de cambiar la contraseña | 5 | Pendiente | No se encontró una implementación específica de este comportamiento; falta construir e integrar su flujo y comprobarlo. | 6 |
| R117 | Protección de campos no editables como usuario, correo y estado | 5 | Pendiente | No se encontró una implementación específica de este comportamiento; falta construir e integrar su flujo y comprobarlo. | 6 |
| R118 | Actualización consistente de los datos del perfil en los demás módulos | 5 | Pendiente | No se encontró una implementación específica de este comportamiento; falta construir e integrar su flujo y comprobarlo. | 6 |
| R119 | Autorización para que cada usuario modifique únicamente su propio perfil | 5 | Pendiente | No se encontró una implementación específica de este comportamiento; falta construir e integrar su flujo y comprobarlo. | 6 |

## Seguridad funcional (14 criterios)

| ID | Criterio de la rúbrica | Página de rúbrica | Estado | Evidencia / trabajo restante | Etapa propuesta |
| --- | --- | ---: | --- | --- | --- |
| R120 | Protección de rutas internas mediante Authorize y rutas públicas mediante AllowAnonymous | 5 | Pendiente | No se encontró una implementación específica de este comportamiento; falta construir e integrar su flujo y comprobarlo. | En cada módulo; auditoría 9 |
| R121 | Autorización por propiedad, amistad, privacidad y participación en cada recurso | 5 | Pendiente | No se encontró una implementación específica de este comportamiento; falta construir e integrar su flujo y comprobarlo. | En cada módulo; auditoría 9 |
| R122 | Protección contra manipulación de URL, identificadores y campos ocultos | 5 | Pendiente | No se encontró una implementación específica de este comportamiento; falta construir e integrar su flujo y comprobarlo. | En cada módulo; auditoría 9 |
| R123 | Protección CSRF en todas las operaciones que modifican información | 5 | Pendiente | No se encontró una implementación específica de este comportamiento; falta construir e integrar su flujo y comprobarlo. | En cada módulo; auditoría 9 |
| R124 | Uso de ViewModels para evitar asignación indebida de propiedades | 5 | Base parcial | Hay ViewModels iniciales; no están conectados y algunos aceptan campos no permitidos para perfil. | En cada módulo; auditoría 9 |
| R125 | Administración segura de contraseñas mediante ASP.NET Core Identity | 5 | Base parcial | AppUser e Identity están configurados; falta implementar registro, autenticación y actualización segura. | En cada módulo; auditoría 9 |
| R126 | Tokens de activación y restablecimiento con vigencia limitada y un solo uso | 5 | Base parcial | Proveedores con vigencias de 24 horas/una hora; falta uso único e invalidación de emisiones previas. | En cada módulo; auditoría 9 |
| R127 | Protección contra revelación de nombres de usuario registrados | 5 | Pendiente | No se encontró una implementación específica de este comportamiento; falta construir e integrar su flujo y comprobarlo. | En cada módulo; auditoría 9 |
| R128 | Manejo seguro de sesiones, cookies, HTTPS y cierre de sesión | 5 | Base parcial | Cookies con HttpOnly/Secure/SameSite y middleware HTTPS; faltan autenticación, logout y comprobaciones de sesión. | En cada módulo; auditoría 9 |
| R129 | Codificación segura del contenido para impedir HTML y JavaScript malicioso | 5 | Pendiente | No se encontró una implementación específica de este comportamiento; falta construir e integrar su flujo y comprobarlo. | En cada módulo; auditoría 9 |
| R130 | Validación segura de extensiones, contenido, tamaño y nombres de archivos | 5 | Pendiente | No se encontró una implementación específica de este comportamiento; falta construir e integrar su flujo y comprobarlo. | En cada módulo; auditoría 9 |
| R131 | Control de concurrencia para amistades, solicitudes, partidas, barcos y ataques | 5 | Base parcial | Restricciones SQL para amistades/solicitudes/reacciones y TransactionManager; juego y coordinación concurrente pendientes. | En cada módulo; auditoría 9 |
| R132 | Manejo de errores sin revelar información técnica o sensible | 5 | Base parcial | UseExceptionHandler apunta a /Home/Error, pero esa acción no existe; falta respuesta segura y revisión de logging sensible. | En cada módulo; auditoría 9 |
| R133 | Validación de todas las reglas sensibles nuevamente en el servidor | 5 | Pendiente | No se encontró una implementación específica de este comportamiento; falta construir e integrar su flujo y comprobarlo. | En cada módulo; auditoría 9 |

## Reglas técnicas y arquitectura (18 criterios)

| ID | Criterio de la rúbrica | Página de rúbrica | Estado | Evidencia / trabajo restante | Etapa propuesta |
| --- | --- | ---: | --- | --- | --- |
| R134 | Implementación en ASP.NET Core MVC con .NET 9 | 5 | Desajuste | Los cinco proyectos usan net10.0; documento funcional p. 98 y rúbrica exigen .NET 9. | 1 |
| R135 | Uso correcto de Entity Framework Core Code First y migraciones | 5 | Base parcial | Dos contextos, configuraciones y migraciones iniciales; no se aplicaron/verificaron en base de datos durante esta revisión. | 1 y 7 |
| R136 | Implementación correcta de arquitectura Onion y responsabilidades de capas | 5 | Base parcial | Referencias entre proyectos orientadas hacia el núcleo; faltan Shared y aislamiento de dependencias HTTP/EF de Application. | 1–2 |
| R137 | Uso correcto de ViewModels y validaciones | 5 | Base parcial | Existen nueve ViewModels con validaciones iniciales; faltan contratos por módulo, reglas completas y conexión a vistas. | 1; ampliar por módulo |
| R138 | Uso correcto de DTOs para transferencia de información | 5 | Base parcial | Existen once DTOs, concentrados en cuentas/correo; faltan DTOs de los módulos y coherencia de los contratos existentes. | 1; ampliar por módulo |
| R139 | Uso de AutoMapper entre entidades, DTOs y ViewModels | 6 | Base parcial | AutoMapper registrado y usado por GenericService; hay cero perfiles de mapeo. | 1; ampliar por módulo |
| R140 | Uso de repositorios genéricos para operaciones comunes de acceso a datos | 6 | Base parcial | GenericRepository implementado y registrado; falta verificar integración y usar operaciones específicas para reglas de negocio. | 1; ampliar por módulo |
| R141 | Uso de servicios genéricos y servicios específicos para reglas de negocio | 6 | Base parcial | GenericService implementado; faltan servicios de negocio y su registro en Application. | 1; ampliar por módulo |
| R142 | Controladores sin lógica de negocio compleja ni acceso directo al DbContext | 6 | Base parcial | HomeController no accede a DbContext ni contiene lógica compleja, pero aún no consume servicios funcionales. | En cada módulo |
| R143 | Vistas sin acceso directo a datos y basadas en ViewModels | 6 | Pendiente | No se encontró una implementación específica de este comportamiento; falta construir e integrar su flujo y comprobarlo. | En cada módulo |
| R144 | Uso de ASP.NET Core Identity para usuarios, contraseñas, tokens y sesiones | 6 | Base parcial | IdentityContext, AppUser, proveedores y configuración inicial creados; AccountServiceForWebApp está vacío. | 2 |
| R145 | Uso de la capa Shared para correo electrónico y almacenamiento de imágenes | 6 | Pendiente | No se encontró una implementación específica de este comportamiento; falta construir e integrar su flujo y comprobarlo. | 2 |
| R146 | Configuración externa y protección de credenciales y cadenas de conexión | 6 | Base parcial | La conexión se obtiene de IConfiguration y existe configuración del host; faltan configuración de correo/archivos y verificación integral de secretos. | 1–2 |
| R147 | Consistencia de operaciones relacionadas y validaciones en el servidor | 6 | Base parcial | TransactionManager implementado para LinkUpProAppContext; no hay casos de uso que coordinen cambios ni compensación de archivos. | 3, 6–8 |
| R148 | Interfaz clara, organizada y responsiva con Bootstrap u otro framework CSS | 6 | Base parcial | Existe layout Bootstrap de plantilla; faltan pantallas funcionales y verificación responsiva. | En cada módulo; revisión 9 |
| R149 | Tablero de Battleship 12x12 visualmente comprensible | 6 | Pendiente | No se encontró una implementación específica de este comportamiento; falta construir e integrar su flujo y comprobarlo. | 7–8 |
| R150 | Operaciones asíncronas para acceso a datos, correo y archivos | 6 | Base parcial | Repositorios y gestor de transacciones usan operaciones asíncronas; faltan operaciones de correo/archivos y casos de uso. | En cada módulo |
| R151 | Manejo consistente de fechas y errores sin revelar información sensible | 6 | Pendiente | No se encontró una implementación específica de este comportamiento; falta construir e integrar su flujo y comprobarlo. | 1; ampliar por módulo |

