# Documentación Técnica — SplitTrailersPRO

**Repositorio:** `JGalvanMR/SplitTrailersPRO` (rama `devs`)
**Tipo de proyecto:** Aplicación Android nativa (Xamarin.Android, .NET), proyecto `SplitTrailers.csproj`
**Fuente de verdad:** Código fuente del repositorio clonado. No se dispone de un `context_json` estructurado previo; esta documentación se generó por lectura directa de cada módulo. Donde el código no permite determinar un dato, se indica explícitamente **"No determinable con la información disponible"**.

## Índice de módulos documentados
1. MainActivity.cs — Login / arranque
2. SolicitarPed.cs — Ingreso y gestión de pedidos
3. capturar_split.cs — Captura de etiquetas de split
4. CancelarParcial.cs — Cancelación parcial de captura
5. CancelarSplit.cs — Cancelación de split completo
6. DetalleCaptura.cs — Detalle de orden (hereda de SolicitarPed)
7. ImprimirSplit.cs — Impresión de etiquetas de split
8. detalle_split_cancelar.cs — Detalle de split a cancelar
9. productosolicitar.cs — Solicitud de producto faltante
10. reasignarterminar.cs — Reasignación y cierre de órdenes
11. solicitarreimpresion.cs — Solicitud de reimpresión/reetiquetado
12. Helpers (DialogHelper, ThemeHelper) y Models/Modal (capa de datos)

---

# 1. MainActivity.cs

## 🧭 Propósito
Activity de arranque (`MainLauncher = true`) que funciona como pantalla de login del sistema: valida sincronía horaria con el servidor, gestiona autoactualización de la app, autentica al responsable de captura de split y controla sesión única por dispositivo antes de navegar a `SolicitarPed`.

## ⚙️ Responsabilidades
- Inicializar la vista principal y cargar el spinner de "Responsables" activos (`TB_RESPON_SPLIT`).
- Verificar y ofrecer descarga de actualizaciones de la APK desde un endpoint HTTP interno.
- Validar que la hora del dispositivo coincida con la hora del servidor SQL.
- Autenticar usuario/contraseña contra `TB_RESPON_SPLIT`.
- Controlar sesión única por usuario/dispositivo vía `tb_det_acceso_celulares`.
- Registrar cada ingreso en `TB_REGISTRO_MOVIMIENTOS`.
- Generar/recuperar un identificador único de dispositivo.

## 🔄 Flujo de Funcionamiento
1. `OnCreate`: configura `StrictMode` en modo permisivo, infla el layout, enlaza el botón de login.
2. Consulta `Tb_folio_campo` para obtener `foliocampo`.
3. Llena el spinner de responsables desde `TB_RESPON_SPLIT` (`status = 'A'`).
4. Ejecuta `getData()`: descarga `version.txt`, compara `versionCode` y, si hay versión nueva, ofrece actualizar (`downloadApp()` descarga el APK e inicia instalación).
5. Compara hora del servidor (`GETDATE()`) contra hora local; si difieren en horas completas, bloquea el acceso y cierra la Activity.
6. Configura la `MaterialToolbar` como ActionBar.
7. Al hacer clic en login (`Btnlogin_Click`):
   - Valida selección de responsable, vehículo y contraseña no vacía.
   - Busca coincidencia usuario/contraseña en el DataTable `responsables`.
   - Obtiene IP e identificador de dispositivo.
   - Verifica sesión activa existente en `tb_det_acceso_celulares` (mismo IMEI permite continuar; IMEI distinto bloquea; dispositivo en uso por otro usuario bloquea).
   - Registra sesión y movimiento, navega a `SolicitarPed` pasando responsable, versión e IMEI.

## 📐 Reglas de Negocio

**🔒 Restricciones**
- No se puede usar el sistema si la hora del dispositivo difiere de la del servidor (diferencia en horas ≠ 0).
- Un usuario no puede tener dos sesiones activas en dispositivos distintos.
- Un dispositivo no puede ser usado por un segundo usuario mientras otro tenga sesión activa.

**✅ Validaciones**
- Responsable y vehículo deben estar seleccionados (distintos del placeholder).
- Contraseña no vacía.
- El par usuario/contraseña debe existir en `TB_RESPON_SPLIT`.

**🔁 Agrupaciones**
- Spinner de responsables limitado a `status = 'A'`, ordenado por `NOM_CAPSPLIT`.

**⚙️ Reglas Operativas**
- Todo login exitoso genera auditoría en `TB_REGISTRO_MOVIMIENTOS` (tipo `'E'`, sistema `'SPLITTRAIL'`).
- Actualización de versión comparando `versionCode` numérico contra `version.txt` remoto.
- El identificador de dispositivo se genera una vez y se persiste en SharedPreferences, priorizando `Settings.Secure.AndroidId`.

## 🔗 Dependencias
SQL Server (`Tb_folio_campo`, `TB_RESPON_SPLIT`, `tb_det_acceso_celulares`, `TB_REGISTRO_MOVIMIENTOS`, `TB_DET_PEDIDOS`, `TB_PED_EMBARQUE`); endpoint HTTP interno de actualización; `SplitTrailers.Helpers.DialogHelper`/`ThemeHelper`; `Plugin.DeviceInfo`; `Org.Json`; Activity destino `SolicitarPed`.

## ⚠️ Riesgos Técnicos
- Credenciales SQL hardcodeadas (`sa` / contraseña en texto plano) en el código fuente y en el APK.
- Consultas SQL construidas por concatenación de cadenas (riesgo de inyección SQL) en todos los puntos de acceso a datos.
- Comparación de contraseña en texto plano.
- `StrictMode` en modo permisivo (`PermitAll`), oculta problemas de I/O en el hilo principal.
- Métodos duplicados casi idénticos (`Btnlogin_Click` vs `Btnlogin_ClickLEGACY`) y bloques extensos de código comentado.
- Manejo de excepciones silencioso en `getData()`.
- Descarga de actualización sobre HTTP sin cifrar ni verificación de integridad.
- Estado compartido en variables estáticas de clase, acoplando fuertemente otras Activities.
- `Connect Timeout = 0` en la cadena de conexión activa (riesgo de bloqueo indefinido).

## 🧪 Casos Edge
- Diferencias horarias menores a una hora no bloquean el acceso (el cálculo usa horas completas).
- Falla de red al validar actualización: capturada de forma silenciosa, no bloquea el login.
- Origen del spinner de vehículo no está definido en este archivo — comportamiento no determinable con la información disponible.

## 🧱 Suposiciones Detectadas
- Conectividad constante a la red local/corporativa (IPs internas 192.168.123.x).
- El reloj del servidor SQL es la fuente de verdad horaria.
- El identificador de dispositivo persiste mientras no se reinstale la app o se limpien datos.

## 📈 Recomendaciones Técnicas
- Externalizar credenciales de conexión SQL.
- Parametrizar todas las consultas SQL.
- Introducir capa de API/servicio entre la Activity y SQL Server.
- Eliminar `Btnlogin_ClickLEGACY` y código comentado.
- Hashear contraseñas del lado servidor.
- Servir actualizaciones por HTTPS con verificación de integridad.

## 🧾 Resumen Ejecutivo
Es la pantalla de acceso que usan los responsables de "split" para iniciar su turno: valida hora del equipo, ofrece actualizar la app, verifica usuario/contraseña y evita que la misma persona trabaje en dos equipos a la vez o que un equipo sea compartido por dos personas simultáneamente. El riesgo principal es de seguridad: credenciales de la base de datos de la empresa escritas directamente en el código de la app y contraseñas de usuario manejadas sin cifrado.

---

# 2. SolicitarPed.cs

## 🧭 Propósito
Activity central del flujo operativo tras el login: permite al responsable ingresar/consultar pedidos, ver su estatus (split pendiente, capturado, etc.), navegar hacia captura, impresión, cancelación y reasignación, y sincronizar periódicamente el estatus de pedidos vía un temporizador.

## ⚙️ Responsabilidades
- Presentar y filtrar la lista de pedidos disponibles para el responsable autenticado, usando un `RecyclerView`/adaptador (`FlimStarInfo`, `myGVItemAdapter`).
- Alternar entre "Modo Captura" y "Modo Consulta".
- Validar y asignar pedidos al usuario en sesión (`validapedidoalta`, `asignapedidoalta`, `pedidoasignadoalta`).
- Calcular cuántos splits están pendientes por pedido (`Splitpendiente`, `GetSplitPendientesLocal`).
- Consultar el estatus de un pedido (`EstatusPed`, `ConsPedSur`).
- Ejecutar un temporizador (`Timer_Elapsed`) para refrescar datos periódicamente.
- Manejar el resultado de Activities hijas vía `OnActivityResult` (retorno desde captura, impresión, cancelación, etc.).
- Validar reglas de asignación de órdenes (`validar_ordenes`), incluyendo impedir que un usuario se asigne pedidos a sí mismo en el flujo de reasignación.

## 🔄 Flujo de Funcionamiento
1. `OnCreate` recibe por `Intent` los datos del responsable autenticado (clave, nombre, versión, IMEI) provenientes de `MainActivity`.
2. Configura el `RecyclerView` (`ConfigurarRecyclerView`) y carga los pedidos correspondientes.
3. El usuario puede alternar Modo Captura / Modo Consulta, cada uno mostrando un mensaje de confirmación (`Toast`).
4. Al seleccionar un pedido (`OnGridView_ItemClicked` / `OnRecyclerViewItemClicked`), se consulta su estatus y disponibilidad de split.
5. Según el estatus, se navega a la Activity correspondiente: captura (`capturar_split`), impresión (`ImprimirSplit`), cancelación (`CancelarSplit`/`CancelarParcial`), detalle (`DetalleCaptura`), reasignación (`reasignarterminar`), o solicitud de producto/reimpresión.
6. `OnResume` reactiva el temporizador que refresca periódicamente el estatus de pedidos.
7. `OnActivityResult` procesa el valor de retorno de las Activities hijas para actualizar la lista mostrada.

## 📐 Reglas de Negocio

**🔒 Restricciones**
- Un usuario no puede asignarse órdenes a sí mismo (mensaje: *"No puede Asignarse Ordenes a usted mismo"*).

**✅ Validaciones**
- Usuario y contraseña deben coincidir en la reautenticación de operaciones sensibles (mensaje: *"USUARIO Y PASSWORD INCORRECTO!!!"*).
- Debe existir al menos un producto/pedido para mostrar (mensaje: *"No hay productos para mostrar"*).

**🔁 Agrupaciones**
- Los pedidos se agrupan/filtran por tabla `tb_mstr_pedidos_nal` (valor por defecto de `tb_tabla`) y tipo de embarque `NAL` (valor por defecto de `tipoembarque`), sugiriendo que la Activity puede reutilizarse para otros tipos de embarque (no confirmado con la información disponible).

**⚙️ Reglas Operativas**
- El conteo de "split pendiente" se calcula tanto localmente (`GetSplitPendientesLocal`) como contra base de datos (`Splitpendiente`), sugiriendo una capa de caché/local que después se reconcilia contra el servidor.
- Un temporizador (`Timer_Elapsed`) refresca el estatus mientras la Activity está en primer plano.

## 🔗 Dependencias
SQL Server (`TB_DET_PEDIDOS`, `TB_MSTR_PEDIDOS_NAL`, `TB_DET_SPLIT`, `TB_MSTR_EMBARQUE`, `TB_DET_EMBARQUE`, `TB_CAT_PRODUCTO`, `TB_RESPON_SPLIT`, `TB_DET_ACCESO_CELULARES`, `TB_AUTORIZA_ODEP`, `TB_DET_SOL_PRODUCTO`, `XPROD`, `CONPEDIDOS`/`Pedidos` — modelos SQLite locales); Activities relacionadas: `capturar_split`, `ImprimirSplit`, `CancelarSplit`, `CancelarParcial`, `DetalleCaptura`, `reasignarterminar`, `productosolicitar`, `solicitarreimpresion`; adaptador `myGVItemAdapter` y modelo `FlimStarInfo`.

## ⚠️ Riesgos Técnicos
- Es la Activity más grande y con más responsabilidades del proyecto (~1,730 líneas), concentrando lógica de negocio, acceso a datos, UI y navegación — alto acoplamiento y baja cohesión.
- `DetalleCaptura` hereda directamente de esta clase (`DetalleCaptura : SolicitarPed`), lo que acopla fuertemente ambas Activities y complica el mantenimiento independiente de cada una.
- Uso combinado de datos locales (SQLite vía modelos `Pedidos`/`ConPedidos`) y remotos (SQL Server), sin que el mecanismo de sincronización/consistencia entre ambos sea determinable con la información disponible.
- Cadenas SQL concatenadas dinámicamente (mismo patrón de riesgo de inyección que en el resto del proyecto).
- Dependencia de un temporizador activo en `OnResume` sin evidencia clara de su cancelación fuera de `OnResume`/ciclo de vida — riesgo de fugas si no se detiene correctamente en `OnPause`/`OnStop` (no determinable con la información disponible sin revisar el ciclo de vida completo).

## 🧪 Casos Edge
- Reasignación de una orden al mismo usuario que la solicita: bloqueada explícitamente.
- Falla al cargar productos: capturada y notificada vía Toast (*"Error cargando productos"*), sin detalle de reintento.
- No determinable con la información disponible: comportamiento exacto cuando el split pendiente local y el remoto difieren.

## 🧱 Suposiciones Detectadas
- Se asume que el responsable autenticado en `MainActivity` sigue siendo válido durante toda la sesión de `SolicitarPed` (no se revalida el login más allá de las operaciones puntuales que piden usuario/contraseña de nuevo).
- Se asume una única tabla activa de pedidos por defecto (`tb_mstr_pedidos_nal`) salvo que se sobreescriba `tb_tabla`.

## 📈 Recomendaciones Técnicas
- Dividir esta Activity en componentes más pequeños (ViewModel/Presenter + repositorio de datos) para reducir su tamaño y responsabilidades.
- Evitar herencia de Activity a Activity (`DetalleCaptura : SolicitarPed`); preferir composición o una clase base ligera compartida.
- Documentar y probar explícitamente la lógica de reconciliación entre datos locales (SQLite) y remotos (SQL Server).
- Parametrizar consultas SQL.

## 🧾 Resumen Ejecutivo
Esta es la pantalla principal de trabajo diario del responsable de split: aquí ve sus pedidos, decide si va a capturar o solo consultar, y desde aquí se dirige a cada operación (capturar, imprimir, cancelar, reasignar, solicitar producto o reimpresión). Al ser el punto central de navegación, concentra mucha lógica de negocio, lo que la hace crítica para el negocio pero también la parte más compleja de mantener y probar.

---

# 3. capturar_split.cs

## 🧭 Propósito
Activity para la captura física de las etiquetas (lotes/tarimas) que conforman un split de pedido, incluyendo validación de estructura de etiqueta, existencias, fechas de caducidad y generación de folios, siendo el módulo más extenso del proyecto (~5,845 líneas).

## ⚙️ Responsabilidades
- Capturar y validar cada etiqueta escaneada/ingresada (`BtnGuardar_Click`, `validarGuardar`, `validaestructuraetiqueta`, `valida`).
- Validar existencia de folio/producto/tarima contra catálogos y pedidos.
- Verificar fecha de caducidad de la etiqueta contra reglas configurables (`ValiFechacad`, `FechaCaducada`).
- Controlar duplicidad de etiquetas ya capturadas (mensaje *"Duplicidad Evitada"*).
- Agrupar productos capturados por pedido (`AgregaProdXPedido`) y generar registros temporales de split (`AgregaTempSplit`).
- Registrar folios sin existencia (`AgregaFolioSinExistencia`) para trazabilidad de faltantes.
- Enviar notificaciones por correo del proceso (`correo enviado exitosamente` / `correo no enviado`).
- Verificar disponibilidad de un servicio de "respaldo" remoto (`estado_respaldo.txt`) al iniciar.
- Ofrecer autoactualización de versión igual que `MainActivity`.

## 🔄 Flujo de Funcionamiento
1. `OnCreate`: configura `StrictMode`, valida versión/respaldo remoto (`getData`), enlaza controles y eventos táctiles (`OnTouch`, `ITextWatcher`).
2. El usuario captura/escanea una etiqueta; `valida()` y `validaestructuraetiqueta()` verifican el formato de la cadena leída.
3. `BtnGuardar_Click` orquesta: valida estructura, valida existencia de folio/producto, valida fecha de caducidad, valida duplicidad, y si todo es correcto, guarda el registro (mensaje *"Split Almacenado Correctamente."*).
4. `ConsPedSurdos` consulta el estado de surtido del pedido asociado.
5. `AgregaProdXPedido` y `AgregaTempSplit` consolidan la captura en las tablas temporales/definitivas de split.
6. `ImprimirDialogs` gestiona la impresión de etiquetas asociadas al proceso de captura.
7. Un modo "Concentrado" (mensaje *"Modo Concentrado Activado"*) altera el comportamiento de captura (probablemente para capturar múltiples cajas bajo una sola tarima; alcance exacto no determinable con la información disponible).

## 📐 Reglas de Negocio

**🔒 Restricciones**
- No se permite capturar una etiqueta duplicada (*"Duplicidad Evitada"*).
- La contraseña de autorización debe ser correcta para ciertas operaciones (*"PASSWORD INCORRECTO!!!"*).

**✅ Validaciones**
- La etiqueta debe cumplir una estructura válida (`validaestructuraetiqueta`, controlado por la bandera `EstructuraEtiqueta`).
- Debe existir el folio/producto/orden referenciado (`OrdenExiste`).
- Debe haber existencias suficientes (`HayExistencias`).
- La fecha de caducidad debe ser válida según reglas del sistema (`ValiFechacad`, `FechaCaducada`, `ValiMinFechaPTC`).
- El surtido no puede superar lo permitido (`Surtidomayor`).

**🔁 Agrupaciones**
- Los productos capturados se agrupan por pedido (`AgregaProdXPedido`) antes de consolidarse en el split temporal.

**⚙️ Reglas Operativas**
- Al confirmar el guardado exitoso, se dispara un correo de notificación del proceso.
- Los folios capturados sin existencia detectada se registran aparte (`AgregaFolioSinExistencia`) en vez de rechazarse silenciosamente, permitiendo trazabilidad posterior.
- El sistema valida disponibilidad de un servicio de "respaldo" (probablemente un mecanismo de contingencia/offline) mediante un archivo de estado remoto (`estado_respaldo.txt`).

## 🔗 Dependencias
SQL Server (extenso: `TB_DET_ETIQUETA`, `TB_DET_ETIQUETA_PRESPLIT`, `TB_DET_ETI_FINAL`, `TB_DET_FOLIO_ADELANTADO`, `TB_DET_PEDIDOS`, `TB_DET_SOL_MOD_INVENTARIO`, `TB_DET_SOL_REETIQUETADO`, `TB_DET_SPLIT`, `TB_DET_SPLIT_FOLIOSINEXIS`, `TB_DET_SPLIT_PRODXPED`, `TB_DET_TRAZABILIDAD`, `TB_ETIQUETA_CAPTURADA_VALIDAR`, `TB_ETIQUETA_MENSAJES_VALIDAR`, `TB_ETIQUETA_SPLIT_VALIDAR`, `TB_FOLIO_CAMPO`, `TB_MSTR_ORDENES_PROD`, `TB_MSTR_RECEPCION_PT`, `TB_MSTR_TRAILER`, `TB_PED_EMBARQUE`, `TB_REGISTRO_MOVIMIENTOS`, `TB_TMP_PED`, `TB_AUTORIZA_ODEP`, `TB_CAT_PRODUCTO`, `TB_CAT_PROD_ESPECIAL`); modelos SQLite locales (`XLote`, `XLoteSug`, `xprod`, `ConPedidos`); endpoint HTTP de estado de respaldo y de actualización de versión; envío de correo (mecanismo exacto no determinable con la información disponible, no se localizó configuración SMTP en este archivo).

## ⚠️ Riesgos Técnicos
- **Archivo extremadamente grande (5,845 líneas)** concentrando validación, acceso a datos, UI e impresión — el de mayor riesgo de mantenimiento de todo el proyecto.
- Múltiples banderas estáticas de tipo `string "S"/"N"` (`EtiquetaExiste`, `EtiquetaCapturada`, `FechaCaducada`, `OrdenExiste`, `HayExistencias`, `Surtidomayor`, `ValiFechacad`, `ValiMinFechaPTC`, `EstructuraEtiqueta`) usadas como pseudo-booleanos compartidos entre validaciones, lo que dificulta razonar sobre el estado real del formulario y es propenso a errores de sincronización entre banderas.
- Existe un método `ValidaCaja` duplicado, uno comentado y otro activo (línea ~2694), similar al patrón LEGACY visto en otros módulos.
- Fuerte dependencia de tablas de "validación" espejo (`TB_ETIQUETA_CAPTURADA_VALIDAR`, `TB_ETIQUETA_MENSAJES_VALIDAR`, `TB_ETIQUETA_SPLIT_VALIDAR`), cuyo propósito exacto y relación con las tablas operativas no es determinable con la información disponible sin más contexto o acceso al esquema de base de datos.
- Mismo patrón de concatenación SQL y credenciales heredadas de `MainActivity.cadenaConexion`.

## 🧪 Casos Edge
- Etiqueta con estructura inválida pero folio existente: el orden de validaciones (estructura → existencia → caducidad → duplicidad) determina qué mensaje se muestra primero; comportamiento exacto ante combinaciones no es completamente determinable sin trazar cada rama.
- Servicio de "respaldo" no disponible al iniciar: manejo específico no determinable con la información disponible.

## 🧱 Suposiciones Detectadas
- Se asume que el escaneo de etiquetas produce una cadena de texto con una estructura fija y predecible, validada por `validaestructuraetiqueta`.
- Se asume disponibilidad de un servicio de correo saliente accesible desde el dispositivo/red interna.

## 📈 Recomendaciones Técnicas
- Dividir el archivo en clases de responsabilidad única: validador de etiquetas, repositorio de datos, y Activity/UI.
- Reemplazar las banderas `string "S"/"N"` por un `enum` o `bool` explícito por regla de validación.
- Eliminar el método duplicado/comentado `ValidaCaja`.
- Documentar el propósito de las tablas `*_VALIDAR` para asegurar que futuros desarrolladores entiendan su rol en el flujo.

## 🧾 Resumen Ejecutivo
Este módulo es el corazón operativo de la captura de split: aquí el personal de campo registra, etiqueta por etiqueta, qué producto se está separando de un trailer, validando en el momento que la etiqueta sea válida, que no esté caducada, que no se repita y que exista inventario. Es, con diferencia, el módulo más complejo y de mayor tamaño del sistema, lo que lo convierte en el de mayor impacto de negocio si falla, pero también en el más costoso de mantener y el más urgente de refactorizar.

---

# 4. CancelarParcial.cs

## 🧭 Propósito
Activity que permite cancelar/liberar parcialmente etiquetas ya capturadas dentro de un split (comparte gran parte de la lógica de validación de `capturar_split.cs`, incluyendo estructura de etiqueta, existencias y fechas de caducidad).

## ⚙️ Responsabilidades
- Validar y procesar la cancelación parcial de etiquetas capturadas (`BtnGuardar_Click`, `valida`, `validaestructuraetiqueta`).
- Liberar etiquetas previamente capturadas (mensajes *"Etiquetas Liberadas Correctamente."*).
- Registrar folios sin existencia (`AgregaFolioSinExistencia`) igual que en captura.
- Consolidar productos por pedido y registros temporales de split (`AgregaProdXPedido`, `AgregaTempSplit`, `AgregaRegistroPedidoAuto`).
- Enviar notificación por correo del resultado del proceso.
- Ofrecer impresión asociada a la cancelación (`ImprimirDialogs`, con una versión `ImprimirDialogsLEGACY` duplicada).

## 🔄 Flujo de Funcionamiento
1. `OnCreate` valida versión/respaldo (`getData`) y configura la vista.
2. El usuario captura/escanea la etiqueta a liberar; se valida estructura y existencia igual que en `capturar_split`.
3. `BtnGuardar_Click` ejecuta la cancelación parcial: verifica duplicidad, actualiza el estatus de la etiqueta y libera el registro correspondiente.
4. `ConsPedSurdos` reconsulta el estatus de surtido tras la liberación.
5. Se dispara notificación por correo y, si aplica, impresión del comprobante de liberación.

## 📐 Reglas de Negocio

**🔒 Restricciones**
- No se puede procesar una etiqueta duplicada dentro de la misma operación de cancelación parcial.
- Se requiere contraseña de autorización correcta para completar ciertas acciones.

**✅ Validaciones**
- Estructura de etiqueta válida antes de procesar la liberación.
- Existencia del folio/producto asociado.
- Validación de fecha de caducidad (comparte banderas `ValiFechacad`, `EtiquetaExiste`, etc. con el patrón de `capturar_split`).

**🔁 Agrupaciones**
- Igual que en captura, los productos liberados se agrupan por pedido antes de consolidarse.

**⚙️ Reglas Operativas**
- Toda liberación exitosa dispara notificación por correo y, opcionalmente, reimpresión del comprobante.

## 🔗 Dependencias
SQL Server (`TB_DET_ETIQUETA`, `TB_DET_ETIQUETA_PRESPLIT`, `TB_DET_ETI_FINAL`, `TB_DET_FOLIO_ADELANTADO`, `TB_DET_PEDIDOS`, `TB_DET_SPLIT`, `TB_DET_SPLIT_FOLIOSINEXIS`, `TB_DET_SPLIT_PRODXPED`, `TB_DET_TRAZABILIDAD`, `TB_ETIQUETA_CAPTURADA_VALIDAR`, `TB_ETIQUETA_MENSAJES_VALIDAR`, `TB_ETIQUETA_SPLIT_VALIDAR`, `TB_FOLIO_CAMPO`, `TB_MSTR_ORDENES_PROD`, `TB_MSTR_RECEPCION_PT`, `TB_PED_EMBARQUE`, `TB_REGISTRO_MOVIMIENTOS`, `TB_TMP_PED`, `TB_AUTORIZA_ODEP`, `TB_CAT_PRODUCTO`); modelos SQLite locales (`XLote`, `XLoteSug`, `xprod`, `ConPedidos`).

## ⚠️ Riesgos Técnicos
- Archivo de gran tamaño (3,120 líneas) con lógica de validación **duplicada** respecto a `capturar_split.cs` en vez de reutilizarse desde un componente común — alto riesgo de que una corrección de regla de negocio se aplique en un archivo y se olvide en el otro.
- Método `ImprimirDialogsLEGACY` duplicado junto al método activo, mismo patrón de deuda técnica visto en `MainActivity`.
- Mismo patrón de concatenación SQL sin parametrizar.
- Reutiliza banderas estáticas de validación (`EtiquetaExiste`, `HayExistencias`, `Surtidomayor`, `ValiFechacad`) que, al ser `static`, son compartidas potencialmente con otras Activities del mismo proceso, lo que puede producir efectos colaterales entre pantallas si no se resetean correctamente.

## 🧪 Casos Edge
- Cancelación parcial de una etiqueta que ya fue cancelada previamente: comportamiento exacto no determinable sin trazar el flujo completo de `valida()`.

## 🧱 Suposiciones Detectadas
- Se asume que la lógica de validación de estructura/existencia/caducidad debe ser idéntica a la de captura, ya que se duplicó en vez de reutilizarse.

## 📈 Recomendaciones Técnicas
- Extraer la lógica de validación compartida con `capturar_split.cs` a una clase/servicio común para evitar divergencia de reglas de negocio.
- Eliminar `ImprimirDialogsLEGACY`.
- Auditar el uso de banderas `static` compartidas entre Activities para evitar efectos colaterales.

## 🧾 Resumen Ejecutivo
Permite corregir capturas de split cuando una etiqueta se registró por error o debe liberarse antes de cerrar el proceso, notificando por correo y permitiendo reimprimir el comprobante. Su principal riesgo de negocio es que comparte casi toda su lógica de validación con el módulo de captura pero de forma duplicada, por lo que un cambio de regla (por ejemplo, una nueva validación de caducidad) podría aplicarse solo a uno de los dos módulos si no se actualizan en conjunto.

---

# 5. CancelarSplit.cs

## 🧭 Propósito
Activity para cancelar un split completo (a diferencia de `CancelarParcial`, que libera etiquetas individuales), operando sobre una orden de venta u orden de embarque específica.

## ⚙️ Responsabilidades
- Listar/seleccionar el split a cancelar mediante un `GridView`/lista (`OnGridView_ItemClicked`).
- Ejecutar la cancelación completa del split (`SaveAction`) previa confirmación.
- Registrar el movimiento de cancelación.
- Obtener el identificador de dispositivo (`GetDeviceID`) para trazabilidad.

## 🔄 Flujo de Funcionamiento
1. `OnCreate` carga la conexión (`LoadConnection`) y la información del split/orden a cancelar recibida por `Intent`.
2. El usuario selecciona el registro a cancelar en la lista.
3. Se solicita confirmación (diálogo con `SaveAction`/`CancelaAction`).
4. Al confirmar, se actualiza el estatus del split/pedido a cancelado y se registra el movimiento correspondiente.

## 📐 Reglas de Negocio

**🔒 Restricciones**
- No determinable con la información disponible: no se detectaron mensajes explícitos de validación (`Toast`) en este archivo, lo que sugiere que las validaciones podrían delegarse a diálogos de confirmación (`DialogClickEventArgs`) sin mensajes de texto libre adicionales.

**✅ Validaciones**
- La cancelación requiere confirmación explícita del usuario antes de ejecutarse (patrón `SaveAction`/`CancelaAction`).

**🔁 Agrupaciones**
- No determinable con la información disponible.

**⚙️ Reglas Operativas**
- Toda cancelación completa se registra en `TB_REGISTRO_MOVIMIENTOS`.

## 🔗 Dependencias
SQL Server (`TB_DET_EMBARQUE`, `TB_DET_ETIQUETA`, `TB_DET_ETI_FINAL`, `TB_DET_PEDIDOS`, `TB_DET_SPLIT`, `TB_DET_TRAZABILIDAD`, `TB_MSTR_EMBARQUE`, `TB_REGISTRO_MOVIMIENTOS`).

## ⚠️ Riesgos Técnicos
- Ausencia de mensajes de validación explícitos podría indicar falta de retroalimentación al usuario en casos de error (no confirmable sin revisar el archivo completo, dado que solo se inspeccionaron firmas de métodos y consultas SQL).
- Mismo patrón de concatenación SQL sin parametrizar.
- Uso de variables estáticas compartidas (`split`, `pedidocancelar`, `responsplit`, `cveresponsplit`, `imei`) entre Activities.

## 🧪 Casos Edge
- No determinable con la información disponible (se requeriría lectura línea por línea del cuerpo de `SaveAction` para identificar validaciones previas a la cancelación).

## 🧱 Suposiciones Detectadas
- Se asume que la cancelación de un split completo es una operación distinta e independiente de la cancelación parcial (`CancelarParcial.cs`), operando a nivel de orden/split en vez de etiqueta individual.

## 📈 Recomendaciones Técnicas
- Añadir mensajes de retroalimentación explícitos al usuario ante fallas de cancelación, si no existen ya.
- Parametrizar las consultas SQL.

## 🧾 Resumen Ejecutivo
Permite anular un split completo (no solo una etiqueta) cuando el proceso completo debe deshacerse, dejando registro del movimiento para auditoría. Es un módulo más pequeño y acotado que sus contrapartes de captura y cancelación parcial, aunque su nivel de validación explícita no pudo confirmarse completamente con la información disponible.

---

# 6. DetalleCaptura.cs

## 🧭 Propósito
Activity que muestra el detalle de una orden/pedido específico y sus productos capturados, extendiendo directamente la clase `SolicitarPed`.

## ⚙️ Responsabilidades
- Mostrar el detalle de captura de una orden (`tb_mstr_pedidos_nal`, tipo de embarque `NAL` por defecto, igual que `SolicitarPed`).
- Consultar y refrescar el estatus del pedido periódicamente (hereda el patrón de temporizador de `SolicitarPed`, variable `countminute`).
- Manejar la navegación de retorno (`OnBackPressed`) y selección de ítems en la lista (`OnGridView_ItemClicked`).
- Cargar la conexión a base de datos (`LoadConnection`).

## 🔄 Flujo de Funcionamiento
1. `OnCreate` recibe datos de la orden a detallar (probablemente vía `Intent`, heredando variables estáticas como `cvresponsable`, `responsablesplit`, `imei`).
2. Se configura el `LaunchMode = LaunchMode.SingleTask`, evitando múltiples instancias apiladas de esta Activity.
3. Se consulta el detalle del pedido y sus productos asociados.
4. `OnBackPressed` gestiona el retorno del usuario, posiblemente hacia `SolicitarPed`, dado el vínculo de herencia.

## 📐 Reglas de Negocio

**🔒 Restricciones**
- `LaunchMode.SingleTask` implica que solo puede existir una instancia activa de esta pantalla a la vez.

**✅ Validaciones**
- No determinable con la información disponible (no se localizaron mensajes `Toast` en este archivo).

**🔁 Agrupaciones**
- Igual que `SolicitarPed`, opera por defecto sobre `tb_mstr_pedidos_nal` y tipo `NAL`.

**⚙️ Reglas Operativas**
- Refresca su estado cada cierto intervalo, similar al mecanismo de `SolicitarPed` (variable `countminute`).

## 🔗 Dependencias
Hereda de `SolicitarPed` (acoplamiento directo de clase); SQL Server (`TB_DET_EMBARQUE`, `TB_DET_PEDIDOS`, `TB_DET_SPLIT`, `TB_MSTR_PEDIDOS_NAL`, `TB_REGISTRO_MOVIMIENTOS`).

## ⚠️ Riesgos Técnicos
- **Herencia de Activity a Activity** (`DetalleCaptura : SolicitarPed`): antipatrón en Android/Xamarin que acopla fuertemente el ciclo de vida y el estado de ambas pantallas, dificultando pruebas y mantenimiento independientes.
- Duplicación de campos estáticos (`cvvehiculo`, `cvresponsable`, `imei`, `currentVersionName`) ya declarados en la clase base `SolicitarPed`, lo que puede generar ambigüedad sobre cuál valor se está usando en tiempo de ejecución (ocultamiento de miembros).

## 🧪 Casos Edge
- No determinable con la información disponible.

## 🧱 Suposiciones Detectadas
- Se asume que reutilizar la lógica de `SolicitarPed` mediante herencia es válido para esta pantalla de detalle, aunque conceptualmente "ver detalle de una orden" no es una especialización de "ingresar pedido".

## 📈 Recomendaciones Técnicas
- Refactorizar para que `DetalleCaptura` no herede de `SolicitarPed`; extraer la lógica compartida a una clase base ligera o a servicios/helpers reutilizables por composición.
- Eliminar la redeclaración de campos ya heredados de la clase base.

## 🧾 Resumen Ejecutivo
Muestra el detalle de una orden y sus productos ya capturados. Desde el punto de vista técnico, este módulo depende directamente de la pantalla principal de pedidos (`SolicitarPed`) mediante herencia de clase, lo cual no es un problema visible para el usuario final pero sí incrementa el riesgo de que cambios futuros en la pantalla principal rompan accidentalmente esta pantalla de detalle.

---

# 7. ImprimirSplit.cs

## 🧭 Propósito
Activity encargada de imprimir las etiquetas de split generadas, con soporte de impresión Bluetooth (uso de `Android.Bluetooth` y el paquete `woosimprinter_bt`).

## ⚙️ Responsabilidades
- Buscar y conectar con una impresora Bluetooth (`FindPrinter`, `OpenPrinter`).
- Enviar los datos de impresión a la impresora (`sendData`, `beginListenForData`).
- Listar y seleccionar el split/trailer a imprimir (`OnGridView_ItemClicked`).
- Registrar el movimiento de impresión.
- Confirmar/cancelar la operación de impresión (`SaveAction`/`CancelaAction`).

## 🔄 Flujo de Funcionamiento
1. `OnCreate` inicializa la vista y localiza la impresora Bluetooth emparejada.
2. El usuario selecciona el split/trailer a imprimir de una lista.
3. Al confirmar (`SaveAction`), se abre conexión con la impresora (`OpenPrinter`) y se transmiten los datos (`sendData`).
4. Ante error de impresión, se notifica al usuario (mensaje: *"Error al Imprimir - "* + detalle de la excepción).
5. Se registra el movimiento de impresión en base de datos.

## 📐 Reglas de Negocio

**🔒 Restricciones**
- No determinable con la información disponible más allá del manejo de errores de impresión.

**✅ Validaciones**
- Manejo de excepciones durante el envío a impresora, notificando el error específico al usuario.

**🔁 Agrupaciones**
- No determinable con la información disponible.

**⚙️ Reglas Operativas**
- Cada impresión exitosa se asocia a un registro de movimiento (`TB_REGISTRO_MOVIMIENTOS`) y a la información del trailer/embarque correspondiente (`TB_MSTR_TRAILER`, `TB_MSTR_EMBARQUE`).

## 🔗 Dependencias
Hardware de impresión Bluetooth (`woosimprinter_bt.dll`); SQL Server (`TB_DET_SPLIT`, `TB_MSTR_EMBARQUE`, `TB_MSTR_TRAILER`, `TB_REGISTRO_MOVIMIENTOS`).

## ⚠️ Riesgos Técnicos
- Dependencia de hardware físico específico (impresora Bluetooth Woosim) sin abstracción visible de un driver genérico, lo que acopla la app a un modelo/fabricante concreto.
- Manejo de errores de impresión reportado directamente como texto de excepción al usuario final (`ex.ToString()`), lo cual puede exponer detalles técnicos internos en pantalla.
- Mismo patrón de concatenación SQL sin parametrizar.

## 🧪 Casos Edge
- Impresora no emparejada o fuera de rango al momento de imprimir: el comportamiento exacto (reintento, mensaje específico) no es completamente determinable sin trazar `FindPrinter`/`OpenPrinter` a detalle.

## 🧱 Suposiciones Detectadas
- Se asume que el dispositivo Android tiene Bluetooth habilitado y una impresora Woosim previamente emparejada por el sistema operativo.

## 📈 Recomendaciones Técnicas
- Encapsular la lógica de impresión Bluetooth detrás de una interfaz/abstracción que permita sustituir el fabricante de impresora sin modificar la Activity.
- Mostrar mensajes de error amigables al usuario y registrar el detalle técnico solo en logs internos.

## 🧾 Resumen Ejecutivo
Este módulo imprime físicamente las etiquetas del split ya capturado, usando una impresora portátil Bluetooth. Es un módulo relativamente acotado, cuyo principal riesgo es la dependencia directa de un modelo específico de impresora y la exposición de errores técnicos al usuario final en caso de falla de conexión.

---

# 8. detalle_split_cancelar.cs

## 🧭 Propósito
Activity de apoyo que muestra el detalle de un split específico previo a su cancelación, sirviendo como pantalla intermedia entre la selección y la confirmación de cancelación (posiblemente invocada desde `CancelarSplit.cs`).

## ⚙️ Responsabilidades
- Mostrar el detalle del split a cancelar (`crcancelar`, `split`, `pedidocancelar`, `detallesplit`).
- Confirmar o cancelar la acción sobre ese detalle (`SaveAction`/`CancelaAction`).
- Obtener el identificador de dispositivo para trazabilidad.

## 🔄 Flujo de Funcionamiento
1. `OnCreate` recibe el detalle del split a mostrar.
2. El usuario revisa la información y selecciona un ítem si aplica (`OnGridView_ItemClicked`).
3. Al confirmar (`SaveAction`), se ejecuta la acción de cancelación asociada al detalle mostrado.

## 📐 Reglas de Negocio

**🔒 Restricciones**
- No determinable con la información disponible (archivo pequeño, sin mensajes `Toast` detectados).

**✅ Validaciones**
- Confirmación explícita requerida antes de proceder (patrón estándar `SaveAction`/`CancelaAction` visto en el resto del proyecto).

**🔁 Agrupaciones**
- No determinable con la información disponible.

**⚙️ Reglas Operativas**
- Toda acción confirmada se refleja en `TB_REGISTRO_MOVIMIENTOS`.

## 🔗 Dependencias
SQL Server (`TB_DET_ETIQUETA`, `TB_DET_ETI_FINAL`, `TB_DET_SPLIT`, `TB_DET_TRAZABILIDAD`, `TB_REGISTRO_MOVIMIENTOS`).

## ⚠️ Riesgos Técnicos
- Módulo pequeño (183 líneas) pero con el mismo patrón de acceso directo a SQL Server y variables estáticas compartidas entre Activities que el resto del proyecto.

## 🧪 Casos Edge
- No determinable con la información disponible.

## 🧱 Suposiciones Detectadas
- Se asume que actúa como paso intermedio del flujo de `CancelarSplit.cs`, dado el nombre y las variables compartidas (`crcancelar`, `pedidocancelar`), aunque la relación exacta entre ambos archivos no está declarada explícitamente en el código inspeccionado.

## 📈 Recomendaciones Técnicas
- Documentar explícitamente (en comentarios o README) la relación de navegación entre `CancelarSplit.cs` y `detalle_split_cancelar.cs` para facilitar el onboarding de nuevos desarrolladores.

## 🧾 Resumen Ejecutivo
Es una pantalla de apoyo que muestra el detalle de un split antes de confirmarlo como cancelado, dando al responsable una última oportunidad de revisión antes de ejecutar la baja definitiva.

---

# 9. productosolicitar.cs

## 🧭 Propósito
Activity para solicitar producto faltante durante el proceso de split, permitiendo registrar la cantidad a "armar" cuando existe un faltante real respecto al pedido y notificarlo por correo.

## ⚙️ Responsabilidades
- Calcular y mostrar productos por surtir (`productosporsurtir`, `Genera`, `CreaTable`).
- Validar la cantidad ingresada para armar el faltante (`BtnGuardar_Click`).
- Calcular lotes y fechas asociadas (`Lote`, `ConviertetoFecha`).
- Enviar notificación por correo de la solicitud (`SendMail`).
- Cargar la conexión a base de datos (`LoadConnection`).

## 🔄 Flujo de Funcionamiento
1. `OnCreate` carga los productos pendientes por surtir para el pedido en curso.
2. `productosporsurtir` construye la tabla de productos faltantes comparando lo pedido contra lo surtido.
3. El usuario ingresa la cantidad a armar por producto faltante.
4. `BtnGuardar_Click` valida que el valor ingresado sea válido y que no supere el faltante real; si pasa la validación, registra la solicitud.
5. `SendMail` notifica por correo electrónico la solicitud generada.

## 📐 Reglas de Negocio

**🔒 Restricciones**
- La cantidad a armar no puede superar el faltante real del pedido (mensaje: *"La cantidad por Armar No Puede Superar Al Faltante Real"*).

**✅ Validaciones**
- El valor ingresado para el faltante debe ser un valor numérico válido (mensaje: *"Debe Ingresar Un Valor Valido Para el Faltante"*).

**🔁 Agrupaciones**
- Los productos se agrupan por semana (`TB_CAT_SEMANAS`) y por usuario (`TB_CAT_USUARIOS`), sugiriendo que la solicitud de producto puede estar contextualizada a un periodo de trabajo y a un usuario solicitante específico.

**⚙️ Reglas Operativas**
- Toda solicitud registrada dispara una notificación por correo electrónico al área correspondiente (destinatario, cuerpo y asunto parametrizados en `SendMail`).

## 🔗 Dependencias
SQL Server (`TB_CAT_SEMANAS`, `TB_CAT_USUARIOS`, `TB_DET_EMBARQUE`, `TB_DET_ETIQUETA_PRESPLIT`, `TB_DET_ETI_FINAL`, `TB_DET_PEDIDOS`, `TB_DET_SOL_PRODUCTO`, `TB_DET_SPLIT`, `TB_DET_TRAZABILIDAD`, `TB_MSTR_INVENTARIO_FISICO`, `CONPEDIDOS`/`PEDIDOS` — modelos locales); servicio de envío de correo (`SendMail`, mecanismo SMTP subyacente no visible en este archivo).

## ⚠️ Riesgos Técnicos
- La clase `productosolicitar` no está declarada como `partial` a diferencia de la mayoría de las demás Activities del proyecto, lo que podría indicar una implementación menos alineada al patrón general del proyecto o simplemente que no requiere un archivo de diseño asociado (no determinable con la información disponible).
- Mismo patrón de concatenación SQL sin parametrizar.
- El mecanismo de envío de correo (`SendMail`) no evidencia manejo de credenciales seguras dentro de este archivo; su configuración exacta no es determinable con la información disponible sin revisar dependencias externas.

## 🧪 Casos Edge
- Ingreso de un valor no numérico o vacío para el faltante: capturado por la validación de "Valor Valido".
- Solicitud de una cantidad igual al faltante exacto: comportamiento en el límite no determinable con la información disponible (el mensaje solo prohíbe superar el faltante, sugiriendo que igualarlo sí es válido).

## 🧱 Suposiciones Detectadas
- Se asume que existe un "faltante real" calculado previamente y confiable contra el cual se valida la cantidad ingresada.
- Se asume conectividad para el envío de correo al momento de guardar la solicitud.

## 📈 Recomendaciones Técnicas
- Confirmar y documentar la configuración segura del servicio de correo (credenciales fuera del código fuente).
- Parametrizar las consultas SQL.
- Añadir manejo explícito para el caso límite de "cantidad igual al faltante".

## 🧾 Resumen Ejecutivo
Permite a un responsable pedir formalmente el producto que hace falta para completar un pedido, evitando que se solicite más de lo que realmente falta, y avisando por correo a quien corresponda para que surta la diferencia. Es un módulo enfocado en un caso de negocio puntual (gestión de faltantes) con reglas de validación claras y acotadas.

---

# 10. reasignarterminar.cs

## 🧭 Propósito
Activity que permite reasignar una orden de venta a otro responsable para continuar su armado, o marcarla como terminada, dentro del flujo de trabajo de split.

## ⚙️ Responsabilidades
- Reasignar una orden de venta a otro responsable (`Btnreasignar_Click`, `ReasignarCarga`).
- Marcar una orden como terminada (`Btnterminar_Click`, `TerminarCarga`).
- Validar la selección de una orden válida antes de operar sobre ella.
- Registrar el movimiento correspondiente a la reasignación o terminación.

## 🔄 Flujo de Funcionamiento
1. `OnCreate` carga las órdenes disponibles para reasignar/terminar, asociadas al responsable en sesión.
2. El usuario selecciona una orden de la lista (`OnGridView_ItemClicked`) y un nuevo responsable en el spinner (`spinner_ItemSelected2`).
3. Al presionar "Reasignar" (`Btnreasignar_Click`), se valida que exista una orden seleccionada (mensaje: *"Seleccione Una Orden Valida"*) y que el usuario y contraseña del nuevo responsable sean correctos (mensaje: *"USUARIO Y PASSWORD INCORRECTO!!!"*); de ser así, `ReasignarCarga` ejecuta el cambio de responsable (mensaje de éxito: *"Orden de Venta Reasignada Correctamente para Continuar Armado"*).
4. Al presionar "Terminar" (`Btnterminar_Click`), `TerminarCarga` marca la orden como finalizada previa confirmación.

## 📐 Reglas de Negocio

**🔒 Restricciones**
- No se puede reasignar sin seleccionar una orden válida.
- El usuario y contraseña del responsable receptor deben ser correctos para autorizar la reasignación.

**✅ Validaciones**
- Selección de orden obligatoria antes de reasignar o terminar.
- Autenticación del nuevo responsable antes de completar la reasignación.

**🔁 Agrupaciones**
- Las órdenes se listan filtradas por responsable/estatus desde `TB_DET_PEDIDOS`/`TB_DET_SPLIT` (detalle exacto de filtro no determinable con la información disponible).

**⚙️ Reglas Operativas**
- Toda reasignación o terminación exitosa se registra en `TB_REGISTRO_MOVIMIENTOS`.

## 🔗 Dependencias
SQL Server (`TB_DET_ACCESO_CELULARES`, `TB_DET_EMBARQUE`, `TB_DET_PEDIDOS`, `TB_DET_SPLIT`, `TB_REGISTRO_MOVIMIENTOS`, `TB_RESPON_SPLIT`).

## ⚠️ Riesgos Técnicos
- Mismo patrón de concatenación SQL sin parametrizar y credenciales heredadas de `MainActivity.cadenaConexion`.
- Reutiliza el mismo mensaje literal de error de autenticación (*"USUARIO Y PASSWORD INCORRECTO!!!"*) que `SolicitarPed.cs`, evidenciando duplicación de lógica de validación de credenciales en al menos dos módulos distintos en vez de un único punto de autenticación reutilizable.

## 🧪 Casos Edge
- Intento de reasignar una orden a un responsable que no tiene permisos suficientes: no determinable con la información disponible si existe una validación de rol adicional más allá de usuario/contraseña.

## 🧱 Suposiciones Detectadas
- Se asume que cualquier responsable con usuario/contraseña válidos puede recibir una orden reasignada, sin una validación de rol o permiso diferenciado visible en este archivo.

## 📈 Recomendaciones Técnicas
- Centralizar la lógica de validación de usuario/contraseña de responsable en un único componente reutilizable en vez de duplicarla en `SolicitarPed.cs` y `reasignarterminar.cs`.
- Parametrizar las consultas SQL.

## 🧾 Resumen Ejecutivo
Permite mover el trabajo pendiente de un pedido de un responsable a otro (por ejemplo, si alguien no puede terminar su turno) o cerrar formalmente una orden como terminada, siempre validando que el nuevo responsable se autentique correctamente antes de recibir la carga de trabajo.

---

# 11. solicitarreimpresion.cs

## 🧭 Propósito
Activity para solicitar la reimpresión o reetiquetado de cajas/tarimas ya procesadas, cubriendo distintos escenarios de recibo (PTP y PTC) y validando duplicidad de folios/cajas ya leídos.

## ⚙️ Responsabilidades
- Capturar folio, tarima y número de caja a reimprimir/reetiquetar.
- Validar que folio, tarima y número de caja no estén vacíos ni en cero.
- Procesar el recibo bajo dos modalidades: `ReciboPTP` y `ReciboPTC`.
- Validar duplicidad de cajas ya leídas bajo distintas variantes de etiqueta (`ValidaCajaEtiVerde`, `ValidaCajaPreesplitVerde`, `ValidaCajasolreetiqueta`, `repetido`).
- Calcular el total de cajas para un código de producto (`traetotal`).
- Cargar la conexión a base de datos (`LoadConnection`).

## 🔄 Flujo de Funcionamiento
1. `OnCreate` inicializa spinners de producto, tarima y caja.
2. El usuario ingresa/escanea folio, tarima y número de caja.
3. `Btnguardar_Click` valida que ningún campo esté vacío o en cero (mensajes específicos por campo: folio, tarima, caja).
4. Según el escenario (`ReciboPTP` o `ReciboPTC`), se valida duplicidad contra el DataTable de folios ya leídos (`ValidaCajaEtiVerde`, `ValidaCajaPreesplitVerde`, `ValidaCajasolreetiqueta`) y se determina si la caja ya fue registrada (`repetido`).
5. Si todas las validaciones pasan, se registra la solicitud de reetiquetado/reimpresión.

## 📐 Reglas de Negocio

**🔒 Restricciones**
- El folio no puede estar en 0 ni vacío.
- El número de tarima no puede estar en 0.
- El número de caja no puede estar en 0 ni vacío.

**✅ Validaciones**
- *"Favor de escribir el folio"* / *"Favor de escribir un folio válido"* / *"Ingrese un folio válido"*.
- *"Favor de escribir el número de tarima"*.
- *"Favor de escribir el número de caja"*.
- Verificación de duplicidad de caja ya leída bajo tres variantes distintas de etiqueta (verde, presplit verde, solicitud de reetiquetado), lo que indica que el sistema maneja al menos tres tipos/estados de etiqueta con reglas de validación de duplicidad independientes entre sí.

**🔁 Agrupaciones**
- Las cajas leídas se agrupan en un DataTable en memoria (`foliosleidos`) contra el cual se valida cada nueva lectura antes de aceptarla.

**⚙️ Reglas Operativas**
- Existen dos flujos de recibo distintos y mutuamente excluyentes: PTP (`ReciboPTP`) y PTC (`ReciboPTC`), cuyo significado exacto de las siglas no es determinable con la información disponible, aunque por el contexto del ecosistema (Producto Terminado) es razonable inferir que corresponden a distintos puntos/tipos de recepción — esto último es una inferencia contextual, no una regla confirmada en el código, y debe validarse con el equipo de negocio.

## 🔗 Dependencias
SQL Server (`TB_DET_ETIQUETA`, `TB_DET_ETI_FINAL`, `TB_DET_FINAL_ODP`, `TB_DET_RECEPCION_PT`, `TB_DET_SOL_REETIQUETADO`, `TB_DET_TRAZABILIDAD`, `TB_FOLIO_CAMPO`, `TB_MSTR_ORDENES_PROD`, `TB_MSTR_RECEPCION_PT`); modelo local `xprod`; modelo `CONPEDIDOS`.

## ⚠️ Riesgos Técnicos
- Tres métodos de validación de duplicidad muy similares (`ValidaCajaEtiVerde`, `ValidaCajaPreesplitVerde`, `ValidaCajasolreetiqueta`) sugieren lógica repetida que podría consolidarse, reduciendo el riesgo de que una corrección se aplique solo a una de las tres variantes.
- Mismo patrón de concatenación SQL sin parametrizar.
- El significado de los flujos "PTP" y "PTC" no está documentado en el código ni en comentarios, dificultando el onboarding de nuevos desarrolladores sin conocimiento previo del negocio.

## 🧪 Casos Edge
- Caja ya leída bajo un tipo de etiqueta pero solicitada nuevamente bajo otro tipo (por ejemplo, ya validada como "verde" pero ahora se solicita como "presplit verde"): el comportamiento cruzado entre los tres validadores no es determinable con la información disponible.

## 🧱 Suposiciones Detectadas
- Se asume que un folio, tarima y caja identifican de forma única una unidad física a reetiquetar/reimprimir.

## 📈 Recomendaciones Técnicas
- Documentar el significado de negocio de "PTP" y "PTC" directamente en el código (comentarios XML) para facilitar el mantenimiento futuro.
- Consolidar los tres métodos de validación de duplicidad en una única función parametrizada por tipo de etiqueta.
- Parametrizar las consultas SQL.

## 🧾 Resumen Ejecutivo
Este módulo permite solicitar que se vuelva a imprimir o reetiquetar una caja o tarima que ya fue procesada, evitando que la misma caja se registre dos veces bajo el mismo tipo de etiqueta. Maneja dos escenarios de recepción distintos y tres variantes de validación de duplicidad, lo que lo hace un módulo con reglas de negocio detalladas pero cuyo propósito exacto de cada variante requiere confirmación con el equipo de negocio para documentarse completamente.

---

# 12. Helpers y Modelos de Datos

## 🧭 Propósito
Conjunto de utilidades compartidas (`Helpers/DialogHelper.cs`, `Helpers/ThemeHelper.cs`) y modelos de datos locales SQLite (`Models/*.cs`, `Modal/FlimStarInfo.cs`) usados de forma transversal por las Activities del proyecto.

## ⚙️ Responsabilidades
- **DialogHelper**: centraliza la construcción de diálogos Material (`ShowErrorDialog`, `ShowSuccessDialog`, `ShowWarningDialog`, `ShowConfirmDialog`, `ShowInfoDialog`, `ShowSingleChoiceDialog`) con estilo consistente (colores por tipo, íconos, botones sin mayúsculas forzadas).
- **ThemeHelper**: resuelve colores del tema de la app en tiempo de ejecución (`GetColorFromTheme`, `GetColorIntFromTheme`).
- **Models** (`ConPedidos`, `Mensajes`, `Pedidos`, `XLoteSug`, `xLote`, `xLoteFinal`, `xprod`): entidades SQLite (atributo `[PrimaryKey, AutoIncrement]` de la librería `SQLite-net`) que representan caché/almacenamiento local de pedidos, lotes capturados y mensajes.
- **Modal/FlimStarInfo**: modelo de presentación usado por el adaptador `myGVItemAdapter` para poblar listas/grids en la UI (nombre, edad, imagen, información adicional, conteo de ítems).

## 🔄 Flujo de Funcionamiento
- `DialogHelper` es invocado desde las distintas Activities (confirmado su uso directo en `MainActivity.cs`) para mostrar mensajes de éxito, error, advertencia o confirmación con estilo visual unificado, delegando la resolución de colores del tema a `ThemeHelper`.
- Los modelos de `Models/` son usados por la librería `SQLite-net` para persistencia local en el dispositivo (uso confirmado por el atributo `[PrimaryKey, AutoIncrement]` y el namespace `SQLite`), permitiendo operar sin conexión constante a SQL Server durante la captura.
- `FlimStarInfo` se usa como modelo de vista para poblar el adaptador `myGVItemAdapter`, que a su vez alimenta listas visuales (por ejemplo, en `SolicitarPed.cs`, método `OnRecyclerViewItemClicked`).

## 📐 Reglas de Negocio

**🔒 Restricciones**
- No determinable con la información disponible (son componentes de infraestructura/presentación, no contienen reglas de negocio propias).

**✅ Validaciones**
- `xprod` define la columna `Lecturabd` como `[Unique]`, lo que impone a nivel de base de datos local que no puede existir más de una lectura idéntica registrada — esta es una regla de negocio real de no-duplicidad aplicada directamente en el modelo de datos local.

**🔁 Agrupaciones**
- No determinable con la información disponible.

**⚙️ Reglas Operativas**
- El uso de modelos SQLite locales (`XLote`, `xLoteFinal`, `xprod`, `ConPedidos`, `Pedidos`, `Mensajes`) sugiere una estrategia de trabajo offline/local-first para la captura de split, sincronizándose posteriormente contra SQL Server (mecanismo exacto de sincronización no determinable con la información disponible en estos archivos).

## 🔗 Dependencias
Librería `SQLite-net` (`SQLite-net.dll`) para los modelos; `Google.Android.Material.Dialog` (`MaterialAlertDialogBuilder`) para `DialogHelper`; usado por prácticamente todas las Activities del proyecto (acoplamiento transversal esperado para un helper compartido).

## ⚠️ Riesgos Técnicos
- Los modelos `Models/*.cs` no están marcados como `public`, sino con visibilidad implícita `internal` (`class ConPedidos`, `class Pedidos`, etc., sin modificador de acceso explícito), lo que podría limitar su reutilización fuera del ensamblado si en el futuro se separa la lógica de datos en un proyecto independiente.
- No se observa una capa de repositorio/DAO explícita para estos modelos SQLite dentro de este archivo; el acceso a ellos desde las Activities no es visible en este análisis y su ubicación exacta no es determinable con la información disponible sin revisar cada Activity a detalle.
- `DialogHelper` y `ThemeHelper`, al ser estáticos, no permiten inyección de dependencias ni sustitución en pruebas unitarias.

## 🧪 Casos Edge
- Intento de insertar un registro duplicado en `Lecturabd` (modelo `xprod`): al ser `[Unique]`, debería fallar a nivel de SQLite; el manejo de esa excepción específica no es determinable con la información disponible en este archivo.

## 🧱 Suposiciones Detectadas
- Se asume que todas las Activities que muestran diálogos deben usar `DialogHelper` para mantener consistencia visual (evidenciado por su adopción parcial ya visible en `MainActivity.cs`, en convivencia con bloques de diálogo "Material" comentados que sugieren una migración en curso desde diálogos ad-hoc hacia este helper centralizado).

## 📈 Recomendaciones Técnicas
- Completar la migración de todos los diálogos ad-hoc restantes (visibles como bloques comentados `#region MATERIAL DIALOG` en varias Activities) hacia `DialogHelper`, y eliminar el código comentado una vez migrado.
- Documentar y, si no existe, introducir una capa de repositorio explícita para los modelos SQLite, separando el acceso a datos local de la lógica de UI de cada Activity.

## 🧾 Resumen Ejecutivo
Estos son los componentes compartidos de la aplicación: por un lado, una utilidad para mostrar mensajes al usuario de forma visualmente consistente (éxito, error, advertencia, confirmación); por otro, un conjunto de modelos que permiten a la app guardar información localmente en el dispositivo mientras trabaja, probablemente para poder operar aunque la conexión al servidor central falle momentáneamente. Es la base común sobre la que se construyen todas las pantallas operativas de la app.

---

# Hallazgos Transversales del Proyecto

Estos hallazgos aplican a múltiples módulos y se documentan una sola vez para evitar redundancia:

1. **Credenciales de SQL Server hardcodeadas** en `MainActivity.cadenaConexion` (usuario `sa` y contraseña en texto plano), reutilizadas por absolutamente todos los módulos del proyecto — es el riesgo de seguridad más crítico y de mayor alcance de todo el sistema.
2. **Inyección SQL sistemática**: todas las Activities construyen sentencias SQL por concatenación de cadenas en vez de usar parámetros, sin excepción detectada en los módulos revisados.
3. **Patrón "LEGACY" recurrente**: múltiples Activities (`MainActivity`, `CancelarParcial`) conservan métodos duplicados con sufijo `LEGACY` junto a su versión activa, además de extensos bloques de código comentado (diálogos Material alternos), incrementando la deuda técnica y el riesgo de mantenimiento.
4. **Validación de usuario/contraseña duplicada** en al menos `SolicitarPed.cs` y `reasignarterminar.cs`, en vez de un único componente de autenticación reutilizable.
5. **Estrategia local-first parcial**: coexisten modelos SQLite locales (`Models/*.cs`) con acceso directo y constante a SQL Server, sin que el mecanismo de sincronización esté documentado en el código revisado.
6. **Control de sesión por dispositivo** (`tb_det_acceso_celulares`) como mecanismo central de negocio para evitar doble sesión, replicado conceptualmente en varias Activities.

# Recomendaciones Técnicas Generales

- Priorizar la remoción de credenciales hardcodeadas y la parametrización de consultas SQL como acción de seguridad inmediata, dado que afecta a todos los módulos.
- Introducir una capa de acceso a datos (repositorio/API) compartida para eliminar la duplicación de lógica SQL entre Activities.
- Establecer un proceso de limpieza de código muerto (métodos `LEGACY`, bloques comentados) previo a futuras iteraciones.
- Consolidar la lógica de autenticación de responsable en un único servicio reutilizable.
- Documentar formalmente el propósito de negocio de siglas y flujos no evidentes desde el código (p. ej. "PTP"/"PTC", tablas `*_VALIDAR`) junto con el equipo funcional.

# Resumen Ejecutivo General

SplitTrailersPRO es la aplicación móvil que el personal de campo usa para dividir ("split") pedidos entre distintos trailers: desde que un responsable inicia sesión, pasa por capturar cada etiqueta física, solicitar producto faltante, imprimir comprobantes, reasignar trabajo entre compañeros y, si es necesario, cancelar total o parcialmente lo ya registrado. El sistema está construido para operar en campo, con capacidad parcial de trabajo local y controles para evitar que dos personas trabajen la misma sesión o el mismo dispositivo a la vez. El riesgo de negocio más importante y transversal es de seguridad: las credenciales de acceso a la base de datos central de la empresa están escritas directamente en el código de la aplicación —y por lo tanto en el instalador (APK) que llega a los dispositivos de campo—, lo que representa una exposición significativa que debería atenderse como prioridad antes que cualquier otra mejora funcional.
