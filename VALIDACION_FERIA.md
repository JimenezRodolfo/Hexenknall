# Entrega de feria

Proyecto: `D:\MEJORAS_CFFF\Hexenknall`. Unity **6000.5.5f1**. Rama local `feria/accesibilidad-demo`, base `bf5cb2f`. Sin push ni publicación. El árbol estaba limpio al iniciar.

## Comportamiento

Entrada **Jugar demo de feria** en el menú, separada de Nueva campaña y Continuar campaña. Reutiliza Level01 → Level02 → FinalBoss: práctica segura Z/X, combate, carta gratuita, muros cambiantes y una oleada, práctica C, jefe y victoria. Las expansiones están desactivadas solo en la demo para conservar tamaño y lectura.

Los datos de demo viven en memoria. No pasan por carga, migración ni escritura de las claves de campaña. Empezar otra demo limpia sus cartas y aprendizaje. Leer la campaña en el menú normaliza solo en memoria; sus autoguardados por pausa/cierre se limitan a escenas de campaña. Reintentar una sala conserva su semilla. Los guardados antiguos siguen usando la versión 2; reciben una semilla al entrar por primera vez después de esta mejora.

Original conserva los valores de balance de escenas/prefabs. Accesible aplica una vez por instancia: 5 de vida base, velocidad enemiga ×0,8, preparación cuerpo a cuerpo ≥0,55 s, reacción a bombas ajenas de 0,65–1 s y margen inicial de 2 s. C ralentiza a ×0,2. Estos valores están expuestos en GameDirector. El jefe tiene aviso ≥1,15 s, recuperación ≥1,5 s, embestida de hasta cuatro casillas y evita iniciar esa embestida sobre bombas/llamas activas. El patrón celeste adicional queda desactivado en Accesible.

Cartas de demo: +2 vida; recargas X/C un 16 % más rápidas; o escudo para el primer golpe de cada sala más 0,30 s de protección tras recibir daño. Todas reutilizan CardRules y el panel de cartas. No hay maldición obligatoria en demo. Las recompensas de campaña conservan su flujo.

## Archivos y propósito

Rutas de scripts relativas a `Assets/01_Scripts/`:

| Archivo | Propósito |
| --- | --- |
| `Core/FeriaSession.cs` (+ `.meta`) | Sesión temporal, aprendizaje, cartas y opciones de feria. |
| `Core/GameDirector.cs` | Recorrido, victoria, margen inicial, parámetros de Accesible y tiempo de apuntado. |
| `Core/GridActor.cs` | Orientación hacia casillas bloqueadas, escudo y fuente conocida del daño. |
| `Player/PlayerController.cs` | Aprendizaje Z/X/C, escudo, perfil y restauración del tiempo al apuntar. |
| `Enemies/EnemyController.cs` | Aplicación única del perfil y ataques más legibles. |
| `Enemies/BossCombat.cs` | Avisos/recuperaciones accesibles y separación de embestida y explosiones. |
| `Camera/OrbitCamera.cs` | Cámara estable y frontal en demo. |
| `Levels/StageEventManager.cs` | Giros opcionales, expansiones pendientes y transformación obligatoria de demo. |
| `Grid/ProceduralLayout.cs` | Semilla por partida y nivel, conservada al reintentar. |
| `Grid/GridBoard.cs` | Aclara en Inspector el nuevo contrato de la semilla. |
| `Progression/RunProgress.cs` | Campos compatibles de semilla/escudo y aislamiento del guardado. |
| `Abilities/Bomb.cs` | Propiedad por color/forma, temporizador real, señal final, alcance con Blast y fuente de daño. |
| `Abilities/Hazard.cs` | Transmisión de la fuente conocida de daño. |
| `UI/GameUI.cs` | Entrada de feria, cartas/victoria, ajustes, objetivos, contadores y mensajes. |
| `Editor/AnimationBindingChecks.cs` | Verifica recursos de origen y curvas de los alias clonados por Unity 6000.5. |
| `Editor/FeriaValidation.cs` (+ `.meta`) | Comprobaciones, revisión serializada y build repetibles desde menú o lote. |
| `Core/FeriaPlayProbe.cs` (+ `.meta`) | Fixture y bot optativos del editor; excluidos del Player con `UNITY_EDITOR`. |
| `LEEME_FERIA.md` | Instrucciones del puesto y controles. |
| `VALIDACION_FERIA.md` | Alcance, evidencia y límites de la entrega. |

No se cambian escenas, prefabs, arte, paquetes ni sus GUID existentes. Los sonidos de impactos, bajas y victoria se reutilizan; elegir una carta usa el sonido existente correspondiente.

## Evidencia

**Build Windows x64:** `Builds/Feria/20261115-192022/Hexenknall.exe`. Resultado Unity: **Succeeded, 0 errores**. Conservados los candidatos anteriores en sus carpetas separadas. Copia la carpeta completa para llevarla a la feria.

| Verificación ejecutada | Resultado y alcance |
| --- | --- |
| Compilación y comprobaciones del editor | **8/8 grupos PASS**: jugabilidad, balance/mapas, guardado/bloques, refugio/embestida, arte integrado, animaciones, aislamiento/cartas de demo y referencias serializadas. Los tres conjuntos principales registran 14.615, 9.993 y 33 aserciones. |
| Escenas y prefabs | Cargadas las **14 escenas habilitadas**. Referencias a servicios/prefabs presentes y sin scripts faltantes en sus jerarquías. |
| Fixture en Play Mode | **29 comprobaciones PASS**. Entradas Z/X/C reales mediante Input System; bajas controladas para verificar transiciones. Incluye orientación contra muro, semilla al reintentar, derrota, pausa, apuntado/cancelación/confirmación, cadenas, muros, oleada, victoria, siguiente visitante y retorno al menú. No equivale a superar el combate con habilidad humana. |
| Bot en Play Mode | **Victoria completa, sin reintentos en la última ejecución**. Teclas reales generadas, salud normal, carta de vida elegida en el flujo normal y sin bajas controladas, teletransporte ni invulnerabilidad añadida. Es una comprobación automatizada, no una medición de principiantes. |
| Guardado durante fixture y bot | Cadenas principal y respaldo comparadas antes/después e idénticas. |
| Arranque del Player final | **PASS**: ejecutable activo 20 s, motor 6000.5.5f1 y Direct3D11 inicializados, sin excepciones registradas del juego. **Dos claves existentes de campaña en Windows idénticas antes/después** mediante hashes. Proceso de prueba cerrado al terminar. No se operaron sus menús ni se inspeccionó su imagen. |
| Preservación del proyecto | Sin diferencias finales en escenas, prefabs, GUID existentes, paquetes, recursos de renderizado ni ProjectSettings. Restauradas las serializaciones automáticas de Unity fuera del alcance. `git diff --check` sin errores. |

Después del último ajuste de lectura/autoguardado del menú se repitieron compilación, los ocho grupos del editor y el arranque con comparación de claves del Player. El código de combate del fixture y bot final no cambió con ese ajuste.

Los resultados detallados se guardan en `Logs/Feria/checks.txt`, `play.txt`, `bot.txt`, `build.txt` y `player-smoke.json`. `delivery-final-editor.log` corresponde a la compilación entregada; `play-final-editor.log` al fixture y bot finales. Los demás logs/candidatos son históricos de esta tarea.

Para repetir con el editor cerrado:

```powershell
& 'E:\Unity\Editor\Unity.exe' -batchmode -projectPath 'D:\MEJORAS_CFFF\Hexenknall' -executeMethod Labyrinth.Editor.FeriaValidation.Run -quit -logFile 'Logs/Feria/recheck.log'
```

También están los menús **HEXENKNALL → Feria → Run checks / Build Windows x64**. Para el fixture, usa `Labyrinth.Editor.FeriaValidation.RunPlayBatch` sin `-quit`; para el bot, `RunBotBatch`; para ambos, `RunAllPlayBatch`. Cierran el editor al completar y escriben su resultado. No deben ejecutarse con trabajo sin guardar en el editor.

## Límites

El servicio de control de ventanas no está disponible en esta sesión (`Computer Use native pipe is unavailable`). No se certifican revisión visual humana, sonido escuchado, comprensión de principiantes ni una partida manual en la build. La duración objetivo de **5–8 minutos queda pendiente de medición con principiantes**. El bot y las pruebas técnicas no acreditan esa duración ni la diversión.

La campaña se comprueba mediante regresiones de progreso, recompensas pendientes, refugio, jefe, serialización y referencias. No se ha vuelto a jugar manualmente toda la campaña. Conserva una copia de la build completa, junto con su carpeta de datos y DLL; no distribuyas únicamente el ejecutable.

Unity mantiene advertencias existentes sobre APIs antiguas y serialización de HashSet. El vídeo existente también avisa de timestamps H.264 corregidos al reproducirse. No impidieron las comprobaciones ni el arranque; su acabado audiovisual queda pendiente de revisión en el puesto. No quedan fallos bloqueantes detectados en las comprobaciones ejecutadas.
