# Hexenknall: campaña completa mejorada

Esta entrega corrige la demo retirada. **Jugar** inicia el prólogo y la campaña original: Level01–Level09, refugio de Old Friend después de Level04, jefe y desenlace. **Continuar** recupera la campaña y las elecciones pendientes. El menú conserva Jugar, Continuar, Ajustes, Salir y Créditos con su presentación original.

Build Windows x64: **D:\MEJORAS_CFFF\Hexenknall\Builds\MejoraCampana\20261115-204626\Hexenknall.exe**. Carpeta completa: **D:\MEJORAS_CFFF\Hexenknall\Builds\MejoraCampana\20261115-204626**. Las pruebas están en VALIDACION_FERIA.md. Copia siempre la carpeta completa del ejecutable, incluidos datos y DLL. Las builds anteriores permanecen separadas.

## Controles

- WASD/flechas: movimiento por casillas; pulsar hacia una pared también orienta al personaje.
- **Z**: círculo explosivo. **X**: sello hacia donde mira; permite moverse durante su carga.
- **C**: apuntar; flechas/WASD desplazan la mira, Enter/Espacio confirma y Esc cancela.
- **Esc**: pausa cuando no estás apuntando. Z/Enter/Espacio avanza los diálogos.

Las bombas turquesas con cubo son propias y las rojas con esfera son rivales. Su contador sigue el tiempo del combate; pausa y ralentización también afectan la mecha. Una cadena puede adelantar la detonación. En Fácil, la previsión de alcance sigue GridBoard.Blast, incluidos sus obstáculos actuales.

## Tres dificultades

| Regla base, antes de cartas | FÁCIL | DIFÍCIL | MUY DIFÍCIL |
|---|---|---|---|
| Vida | 5 | Original: 3 | Original: 3 |
| Velocidad del jugador | Original | Original | Original |
| Enemigos | 80% de velocidad; preparación mínima 0,6 s; reacción 0,8–1,15 s | Balance original | Balance original |
| Bombas propias | No dañan al jugador | Sí dañan | Sí dañan |
| Cámara y controles | Estable, controles relativos | Giros originales, controles relativos a la cámara | Giros originales, direcciones fijas del tablero |
| Z / carga X / recarga X | 1,5 / 0,3 / 2,5 s | 2 / 1,2 / 5 s | 2 / 1,2 / 5 s |
| C: mecha / recarga | 1 / 5 s | 3,5 / 8 s | 3,5 / 8 s |
| Poderes | X desde Level01, C desde Level03 | Cartas originales | Cartas originales |
| Recompensas | Bendición gratis y pacto adicional opcional, una vez por nivel | Bendición y maldición | Bendición y maldición |
| Orbes | Recuperan 1 vida, máximo 3 por sala | Desactivados por defecto | Desactivados por defecto |

En Fácil hay 2 segundos de margen inicial, inmunidad tras daño de al menos 1,2 segundos y apuntado C a ×0,3. Bombas enemigas, golpes y ataques del jefe siguen dañando. El jefe mantiene su vida y sus ataques; los avisos y recuperaciones dan más margen y sus patrones se separan. Las expansiones conservan su animación, con máximo una ampliación de +4 por dimensión.

Las cartas modifican los valores base. Los poderes garantizados de Fácil no escriben desbloqueos permanentes. Un desbloqueo elegido mediante una carta conserva el comportamiento original. Cambiar dificultad se aplica al entrar o reiniciar una sala; la política de una recompensa ya pendiente se conserva.

Los impactos, bajas y cadenas tienen respuesta visual y sonora breve. ¡DOBLE!/¡TRIPLE! cuentan bajas reales atribuidas a una misma explosión o cadena del jugador. Algunas cajas rotas por el jugador dejan un orbe sin colisión: espera a que acabe la llama y recógelo con vida incompleta. Hay una ayuda garantizada por sala al romper una caja con 2 vidas o menos; los otros drops tienen probabilidad configurable del 18%.

## Guardados y recuperación

Se conserva Labyrinth.Run.v2 y su respaldo. El avance nuevo cuenta niveles completados, independientemente de las maldiciones. Puedes guardar y volver al menú durante las elecciones; una bendición aplicada no se duplica al continuar. El refugio abre su salida aunque no tengas maldiciones.

Los valores originales de dificultad **0=Difícil, 1=Muy difícil** mantienen su significado. La nueva clave HEXENKNALL.Difficulty.v2 añade **2=Fácil**. Se copia una preferencia original válida; sin ella, la instalación empieza en Fácil. Las preferencias antiguas Balance/Rotate de la entrega retirada quedan inactivas, sin borrarlas. HEXENKNALL.DifficultyMigration registra la migración.

Referencia recuperable de la entrega retirada: commit 7a6cc35, rama respaldo/entrega-demo-rechazada. Base original: bf5cb2f. Trabajo actual: mejora/campana-tres-dificultades. No se ha publicado ni hecho push.

Para el editor usa Unity **6000.5.5f1** y Assets/00_Scenes/MainMenu.unity. No se cambiaron paquetes ni versión del motor. Revisa el informe antes de presentar el juego: la comprobación técnica y las capturas no sustituyen una partida humana ni demuestran diversión.
