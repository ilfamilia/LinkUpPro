# Plan de módulos asignados y contratos de integración

Estado actualizado: se implementaron los servicios, persistencia, pantallas y pruebas de estos módulos. El resultado verificable y las integraciones pendientes con los compañeros se detallan en [IMPLEMENTATION_REPORT.md](IMPLEMENTATION_REPORT.md). Las fases siguientes se conservan como guía de alcance, no como una lista de trabajo todavía sin iniciar.

## Decisiones y alcance

- Mantener **.NET 10**, por instrucción del usuario, aunque los PDF indiquen .NET 9.
- Implementar amistades, solicitudes de amistad, notificaciones y el juego completo de Battleship, incluidos sus servicios, persistencia, DTOs, ViewModels, perfiles, controladores, vistas y validaciones.
- Aplicar Onion Architecture, SOLID e identificadores técnicos en inglés. Conservar nombres de dominio existentes útiles y no introducir un cambio global de carpetas o contratos ajenos como requisito para avanzar.
- Los demás módulos serán desarrollados por los compañeros. Como hipótesis de organización, se consideran dependencias externas usuarios/autenticación, publicaciones/comentarios/reacciones, perfil y Shared. Todavía no se ha especificado qué compañero es dueño de cada módulo.
- Los contratos y fases se definieron antes de implementar. La implementación actual y su verificación se registran en IMPLEMENTATION_REPORT.md.

## Cobertura y responsabilidades

Los cuatro grupos seleccionados abarcan **64 criterios funcionales** de la rúbrica: Amigos 15, Solicitudes 17, Notificaciones 6 y Battleship 26. No son 64 funcionalidades independientes: algunos criterios de Amigos dependen de publicaciones/interacciones y deben comprobarse con el módulo de los compañeros. A esto se suman los criterios técnicos y de seguridad aplicables.

| Área | Responsabilidad de este trabajo | Integración con otros módulos |
| --- | --- | --- |
| Solicitudes | Elegibilidad, envío, aceptación, rechazo, cancelación, historial y contador. | Consultar usuarios activos y su información pública; obtener usuario autenticado. |
| Amistades | Relación única, listado/búsqueda, amigos en común, eliminación/reactivación y comprobación de amistad activa. Pantalla de Amigos y perfil de amigo como integración. | El módulo de publicaciones proporciona listados/filtros, interacciones y contenido visible del amigo; cuentas proporciona datos públicos actuales. |
| Notificaciones | Crear según las reglas del PDF, consultar, marcar leídas y contar pendientes. | Comentarios/respuestas/reacciones llaman al servicio. Publicaciones valida el acceso al contenido al navegar. |
| Battleship | Modelo, flotas, tableros, turnos, ataques, rendición, abandono, resultados e historial. | Identidad del jugador y cuenta activa; amistad activa al crear partida. |
| Cuentas y perfil | Consumir contratos estables; no rehacer sus formularios ni autenticación. | Sus responsables corrigen namespaces, identificadores, campos de perfil y estados de cuenta. |
| Menú y composición | Entregar rutas, contadores y registros de los cuatro módulos. | Integrar en el layout y arranque compartidos con el responsable de presentación. |

## Contratos a definir antes de implementar

Los nombres siguientes son propuestas de integración, no componentes ya implementados. Pueden adaptarse al contrato acordado con el equipo manteniendo sus responsabilidades.

### Datos de usuario

Proponer `IUserDirectoryService` con consultas por ID, por lote y búsqueda de usuarios activos. `UserSummaryDto` contendría `Id`, `UserName`, `FirstName`, `LastName`, `ProfilePicturePath` y `IsActive`. Para la consulta por lote conviene aceptar un conjunto de IDs y evitar consultar Identity una vez por cada fila.

La implementación pertenece al módulo de cuentas/Identity. Application consume la interfaz sin conocer `AppUser` ni `UserManager`. Debe acordarse si cuenta activa significa `EmailConfirmed` o un estado adicional; no inferir que el bloqueo temporal de login sea desactivación de cuenta.

No reutilizar `UserDto` obligando a estos módulos a conocer `Role`, correo y teléfono. Sus listados solo necesitan información pública. Mantener todos los IDs de usuario como `string`.

### Usuario actual

Proponer `ICurrentUserService` como acceso a la identidad autenticada, con adaptación en Web. El servidor obtiene el usuario actual; los formularios no eligen emisor de solicitud, propietario, actor de notificación ni atacante. Cada caso de uso verifica además que la cuenta siga activa.

### Amistad como dependencia

`IFriendshipService` debe proporcionar consultas claras como `AreFriendsAsync`, IDs de amigos activos y amigos en común. Publicaciones lo consume para privacidad; Battleship para crear partidas. Una partida existente se autoriza por participantes y estado, incluso si ya no son amigos.

### Publicaciones e interacciones

Acordar con el responsable de publicaciones un contrato para obtener publicaciones visibles de amigos con filtros y consultar su contenido de detalle. Puede ser una operación de su `IPostService` o una interfaz de lectura específica.

Para notificaciones, acordar un contrato de acceso a contenido, por ejemplo `IPostAccessService`, que compruebe existencia, eliminación, privacidad, amistad y relación comentario/publicación. Una notificación nunca concede acceso por sí misma.

Los listados, multimedia y formularios de comentarios/reacciones se integran mediante las vistas/contratos del módulo correspondiente; no crear una segunda implementación de publicaciones dentro de amistades.

### Creación de notificaciones

`INotificationService` recibe llamadas internas después de comentarios, respuestas y registro/cambio de reacciones. Puede ofrecer operaciones específicas para cada interacción. Actor y destinatario deben derivarse de datos de confianza, no de un formulario público de creación.

El autor de la publicación recibe comentarios/reacciones y el autor del comentario o respuesta recibe respuestas. No crear auto notificaciones; quitar reacción no notifica. Las notificaciones exigidas son de interacciones; el PDF no exige agregar notificaciones de solicitudes ni del juego.

Coordinar transacción e inserción de la interacción/notificación con el equipo para no guardar una notificación de una acción fallida. El `TransactionManager` existente solo cubre el contexto de aplicación; no asumir que cubre Identity u otros recursos.

### Archivos y migraciones compartidos

- Los cuatro módulos consumen rutas de fotos existentes; el almacenamiento/carga de fotos permanece en cuentas/perfil/Shared.
- Acordar responsabilidad de `LinkUpProAppContext`, registro de servicios, perfiles y layout porque son archivos compartidos.
- Designar un integrador de migraciones del contexto común para evitar snapshots incompatibles. Cada migración debe corresponder al modelo final integrado.
- Pueden emplearse dobles de prueba para usuarios y publicaciones mientras sus responsables implementan los módulos; no agregar cuentas falsas o bypass de autenticación al flujo real de la aplicación.

## Fases de implementación

### Fase 1. Base compilable y acuerdos del equipo

1. Mantener `net10.0` y resolver restauración/compilación. El diagnóstico previo encontró errores NU1100; no se ha verificado de nuevo en esta actualización documental.
2. Definir los contratos anteriores y la semántica de cuenta activa/usuario autenticado.
3. Fijar convenciones de nombres y corregir en nuestro alcance los namespaces o tipos inconsistentes que impidan integrar.
4. Entregar al responsable de cuentas/perfil la lista de correcciones: namespaces `InvestmentApp`, IDs de usuario `string`, propagación de `RememberMe`, coherencia de nombres y edición de perfil sin correo/usuario/rol ni estado modificables.
5. Definir registro de dependencias, perfiles, errores de negocio, fechas UTC y estrategia de transacciones/concurrencia, sin reestructurar módulos que desarrollan los compañeros.

**Entrega:** solución compilable, contratos consumibles y responsabilidades de archivos/migraciones claras. Los contratos compartidos tienen nombres y tipos estables antes de construir sus consumidores.

### Fase 2. Solicitudes de amistad

1. DTOs/ViewModels/perfiles específicos y `IFriendshipRequestService` con operaciones `Async`.
2. Listado/búsqueda de usuarios elegibles con selección única: activos, distintos del actual, sin amistad activa ni solicitud pendiente en ambas direcciones.
3. Envío con revalidación del estado y manejo del índice único ya existente.
4. Pendientes recibidas, aceptación/rechazo con confirmación y autorización exclusiva del receptor.
5. Enviadas, cancelación exclusiva del emisor e historial visible con ocultación lógica sin cambiar el estado.
6. Aceptación atómica que crea/reactiva una amistad única; contador de recibidas pendientes excluyendo cuentas inactivas.

**Entrega:** ciclo completo con controlador, vistas, servicio y persistencia; pruebas de solicitudes cruzadas y doble aceptación. Atiende R066–R082.

### Fase 3. Amistades y puntos de integración con publicaciones

1. Completar `IFriendshipService` y operaciones específicas del repositorio; mantener la clave compuesta existente y normalizar la pareja de IDs.
2. Listado alfabético de amigos activos, búsqueda por nombre/apellido/usuario y cálculo/listado de amigos en común.
3. Eliminar lógicamente con confirmación y autorización, revocando acceso a recursos sujetos a amistad.
4. Construir resumen de Amigos y perfil del amigo integrando datos públicos y publicaciones visibles del módulo externo, con sus filtros y orden.
5. Integrar conservación de interacciones, notificaciones y partidas; una amistad reactivada restaura acceso al contenido que siga siendo visible.

**Entrega:** relación bidireccional sin duplicados, eliminación/reactivación e integración con publicaciones. Los criterios de publicaciones no se cierran solo con un listado de amigos. Atiende R051–R065; continuación de partidas se verifica al completar Battleship.

### Fase 4. Notificaciones

1. Implementar `INotificationService`, DTOs/ViewModels, mapeos, consultas y controlador/vistas.
2. Crear notificaciones de comentario/respuesta/reacción con destinatario correcto y sin auto notificaciones.
3. Listar por fecha descendente; lectura individual, marcar todas y contador de no leídas.
4. Navegar al contenido con revalidación; conservar notificaciones aunque desaparezca el contenido o se pierda acceso.
5. Conectar llamadas desde comentarios/reacciones desarrollados por los compañeros; protección de acciones de lectura y modificación por destinatario.

**Entrega:** notificaciones persistidas y verificadas con interacciones reales, sin actualización en tiempo real. Atiende R045–R050.

### Fase 5. Battleship: modelo, partidas y colocación

1. Diseñar `BattleshipGame`, `ShipPlacement`, `Attack`, estados, direcciones y coordenadas; configurar relaciones, índices, versión de concurrencia y migraciones integradas.
2. Registrar participantes, creador, fase, turno, fecha de asignación de turno, fechas de inicio/fin, ganador y motivo de finalización.
3. Crear partidas contra amigos activos, con buscador y selección única; impedir más de una activa por pareja incluso con solicitudes simultáneas.
4. Crear cinco barcos identificables por jugador de tamaños 2, 3, 3, 4 y 5; distinguir los dos de tamaño 3.
5. Construir selección de barco pendiente, tablero 12×12, celda y dirección; impedir salida, superposición y doble colocación.
6. Mostrar espera si el oponente no termina; al completar ambos comenzar ataque con el creador. Colocación sin vencimiento automático.

**Entrega:** ambos participantes configuran y reanudan una partida persistida; terceros no acceden ni reciben datos de barcos ajenos. Atiende R083–R084, R086–R099 y parte de los criterios transversales.

### Fase 6. Battleship: combate y finalización

1. Tablero de ataque propio, aciertos rojos, fallos verdes y celdas ya atacadas bloqueadas.
2. Validar participante, fase, cuenta activa, turno y celda en el servidor.
3. Registrar ataque, resultado, hundimiento y siguiente turno atómicamente. Todo ataque válido alterna, incluso un acierto.
4. Detectar victoria por flota hundida; rendición con confirmación y ganador contrario.
5. Finalizar por abandono tras 48 horas desde asignación del turno actual al acceder/refrescar/actuar; impedir doble finalización.
6. Refresco manual y consulta de colocación propia, sin revelar ataque o flota del rival durante la partida.
7. Continuar partidas existentes después de eliminar amistad y bloquear creación de nuevas entre esos usuarios hasta restablecerla.

**Entrega:** partida completa con turnos estrictos, vencimiento, rendición y resultados persistidos. Pruebas concurrentes de ataques/finalización. Atiende R085 y R100–R106, con integración de R065.

### Fase 7. Historial, integración y verificación del alcance

1. Historial con oponente, fechas, duración, resultado/ganador y resumen total/ganadas/perdidas.
2. Resultados de solo lectura con tableros finales de ataque propios y del oponente; colocación propia consultable.
3. Integrar las cuatro secciones en menú, contadores, layout y navegación del proyecto.
4. Verificar `[Authorize]`, cuenta activa, permisos por recurso, POST/antiforgery, validaciones de ViewModels, mapeos seguros y mensajes de error.
5. Ejecutar pruebas significativas sobre base relacional para transacciones, unicidad y concurrencia; comprobar dos sesiones de jugadores y un tercero sin acceso.
6. Validar los 64 criterios de estos grupos y los técnicos/de seguridad aplicables, con evidencia compartida donde dependan de publicaciones o Identity.

**Entrega:** módulos completos e integrados con el trabajo del equipo; R107–R108 y cierre transversal. No declarar terminado un criterio dependiente mientras solo exista un contrato o un doble de prueba.

## Orden de trabajo

Base y contratos → solicitudes → amistades → notificaciones → Battleship: preparación → Battleship: combate → historial e integración.

Las correcciones globales de perfil/autenticación se coordinan con sus responsables. El reporte de implementación distingue lo probado de las integraciones que requieren sus módulos.
