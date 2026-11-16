# Hexenknall: puesto de feria

Abre `Assets/00_Scenes/MainMenu.unity` con Unity **6000.5.5f1** y pulsa Play, o ejecuta la build indicada en `VALIDACION_FERIA.md`. Elige **Jugar demo de feria**.

Build entregada: `Builds/Feria/20261115-192022/Hexenknall.exe`. Lleva la carpeta `20261115-192022` completa, con sus datos y DLL.

- WASD o flechas: moverse por casillas. Pulsar hacia un muro también orienta al personaje.
- **Z**: deja una bomba. Sal de la cruz marcada; tus bombas también te dañan.
- **X**: coloca un sello que dispara en la dirección del personaje; puedes moverte durante su carga.
- **C**, antes del jefe: apunta con flechas/WASD, **Enter/Espacio** confirma y **Esc** cancela. El combate se ralentiza mientras apuntas.
- **Esc**: pausa cuando no estás apuntando. En diálogos, **Z/Enter/Espacio** continúa.

El recorrido es aprendizaje seguro → dos guardianes → carta gratuita → laberinto cambiante con una oleada → jefe → victoria. Practica Z y después X para activar el primer combate; practica C para activar al jefe. Las bombas turquesas con cubo son propias; las rojas con esfera son rivales. Todas pueden encadenarse y explotar antes de su cuenta atrás.

En victoria, **Jugar otra demo** prepara una partida limpia para el siguiente visitante. En derrota, **Reiniciar** repite la sala con su distribución inicial; para empezar desde cero, vuelve al menú y elige la demo. En pausa también puedes reiniciar o volver al menú.

La demo usa el perfil Accesible, cámara estable y datos temporales en memoria. No sobrescribe el guardado de campaña. **Nueva campaña** sí inicia una campaña nueva; usa **Continuar campaña** para conservarla. El prólogo original permanece en campaña.

En Ajustes puedes elegir el balance de campaña **Original/Accesible**, su orientación de controles y los giros automáticos de cámara. El balance se aplica al entrar de nuevo a una sala; la demo siempre usa Accesible. Valores configurables en `GameDirector`: vida, velocidad enemiga, preparación del golpe, reacción, margen inicial y ralentización de C.

La duración objetivo de **5–8 minutos está pendiente de medición con principiantes**. Consulta `VALIDACION_FERIA.md` para conocer pruebas y limitaciones reales. Antes de abrir el puesto, comprueba sonido, resolución y una partida completa en el equipo de la feria.
