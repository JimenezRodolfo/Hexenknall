# Corrección de Hexenknall: campaña completa
Fecha: 15 de noviembre de 2026. Unity 6000.5.5f1. Windows x64.

## Entrega

- Ejecutable: **D:\MEJORAS_CFFF\Hexenknall\Builds\MejoraCampana\20261115-204626\Hexenknall.exe**
- Carpeta completa para trasladar: **D:\MEJORAS_CFFF\Hexenknall\Builds\MejoraCampana\20261115-204626**
- Build: **Succeeded, 0 errores**. SHA256 del ejecutable: c7e7c0fb799bc979d2a086d8836d9570443f925c753442c2979fac9e38d69c13
- SHA256 de la lógica nueva, Assembly-CSharp.dll: 75536ed7bf9137bea29e2d61699ad3419dbedc63b627739cdff32f077a13a172. Difiere de la demo; el ejecutable base de Unity puede ser idéntico entre builds. La DLL contiene DifficultyRules/RecoveryOrb y excluye FeriaSession y el fixture CampaignPlayChecks.
- Referencia recuperable anterior: commit **7a6cc35**, rama **respaldo/entrega-demo-rechazada**. Base original **bf5cb2f**; corrección en **mejora/campana-tres-dificultades**. Sin push. Builds/Feria permanece separado.

## Qué cambió

Se retiraron FeriaSession, FeriaPlayProbe y FeriaValidation, el botón de demo, sus cartas y sus transiciones. No hay dependencia de ese recorrido en el juego entregado. El menú y el orden de la campaña original se conservan: prólogo, nueve niveles, refugio después del cuarto, jefe y desenlace. Se mantienen los textos narrativos; los diálogos de entrada se presentan una vez por partida y la espera de victoria baja a 0,45 s.

Hay exactamente Fácil, Difícil y Muy difícil. Los valores guardados originales 0/1 conservan sus reglas. Fácil añade el perfil descrito en el LEEME: 5 vidas, enemigos al 80%, bombas propias inocuas por origen real, X/C garantizadas sin desbloqueos permanentes, tiempos ágiles y cámara estable. Los otros perfiles conservan sus valores serializados originales y sus diferencias de control.

La campaña incorpora impactos y bajas con respuesta breve, cadenas reconocibles, bajas múltiples atribuidas al jugador, señal del protagonista, mensajes de recarga y origen de peligro, semillas persistentes, orbes limitados y recompensas útiles. Fácil permite una bendición gratuita y un único pacto opcional con beneficio y coste visibles. Las elecciones se guardan juntas y el avance ya no exige una maldición por nivel. El refugio admite llegar sin maldiciones. El jefe conserva 4 vidas y sus ataques, con ventanas separadas en Fácil. Las expansiones de Fácil mantienen el espectáculo con +4 por dimensión y máximo una ampliación.

## Evidencia ejecutada

- **8/8 grupos de controles de editor PASS**, incluidos los controles existentes: 14.615 aserciones de gameplay, 9.993 de balance/distribuciones y 33 de regresión de guardados/bloques; además refugio/embestida, arte, animación, migración/recompensas y escenas. Se revisaron las 14 escenas de build, referencias de prefabs y componentes ausentes.
- **153 comprobaciones Play Mode PASS** en la campaña real. Incluyen Jugar/Continuar, prólogo narrativo, tres perfiles, giro contra pared, X/C, pausa/apuntado, daño propio/rival, detonación de una cadena mixta real con origen individual, caja real, orbe durante una llama, reintento/semilla, ambas fases de elección al continuar, pactos alternados, refugio con/sin maldiciones, oleadas/expansiones y llegada al desenlace.
- El fixture de progresión usa bajas controladas e invulnerabilidad durante la comprobación del grafo completo. Es una prueba de integración, **no una partida humana ni una medida de dificultad o diversión**. No usa el bot de demo retirado. El fixture se excluye del Player.
- Hashes del guardado principal y respaldo de **Editor y Player iguales antes y después de la ronda final, incluido el cierre del editor**. Los datos del fixture permanecen en memoria hasta salir.
- **Player de la build entregada**: arranque durante 20 segundos, motor inicializado, proceso vivo y cero excepciones de juego detectadas; hashes de campaña iguales antes/después. Este smoke no reproduce entradas ni valida la campaña dentro del Player.
- Capturas generadas por Unity y revisadas: menú, ajustes con las tres dificultades, prólogo, combate, cadena activa con señal TÚ, bendición, pacto, refugio, tablero expandido, jefe y desenlace. Resolución de captura del Game View: 763 × 355.
- La prueba de animación conserva fuentes serializadas, estados y comparación de curvas tras reimportar. Se corrigió una expectativa de identidad nativa de alias de clips en Unity 6000.5; no se omitió la comprobación del contenido.
- Escenas, prefabs, GUID existentes, recursos serializados, ProjectSettings y Packages permanecen sin diferencias respecto de la base. Los cambios automáticos de URP al construir se archivaron como patch en Logs/MejoraCampana y se restauraron; no forman parte de la corrección. El ejecutable previo de la demo conserva su SHA256.
- Los intentos incompletos de fixture no se cuentan como PASS. Se corrigieron una expectativa de recarga que ignoraba cartas, el collider durante el primer frame y la sincronización física/espera del portal del fixture.

Evidencias completas: **Logs/MejoraCampana** en el proyecto; una copia transportable de resultados y capturas está en **Verificacion** junto al ejecutable. **SHA256SUMS.txt** verifica los archivos de esa carpeta completa.

## Límites pendientes

El control gráfico nativo no estuvo disponible (native pipe no disponible). Se revisaron capturas del editor, no una partida interactiva del ejecutable. No se escuchó el audio ni se midieron rendimiento del equipo de feria, accesibilidad con usuarios, duración humana o diversión. El jefe aparece y se comprueba su configuración y progresión; sus ventanas de ataque y el balance completo necesitan una partida humana. Hay advertencias de APIs obsoletas/serialización y un fallo del indexador de búsqueda del editor; no son errores del Player ni impidieron los controles o la build. Conviene jugar la campaña completa en el equipo de presentación antes de exhibirla.
