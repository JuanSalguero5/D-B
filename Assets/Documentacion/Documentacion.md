# Documentación del Proyecto — D&B (Unity)

## Índice

1. [GameManager](#1-gamemanager)
2. [SimpleFluidDrive](#2-simplefluiddrive)
3. [PlayerHealth](#3-playerhealth)
4. [PlayerRhythmBehavior](#4-playerrhythmbehavior)
5. [SmoothFollowCamera](#5-smoothfollowcamera)
6. [CameraSpeedEffects](#6-cameraspeedeffects)
7. [TrackManager](#7-trackmanager)
8. [DynamicObstacle](#8-dynamicobstacle)
9. [ObstacleDamage](#9-obstacledamage)
10. [RhythmRamp](#10-rhythmramp)
11. [RE_RhythmRing](#11-re_rhythmring)
12. [IA_EnemyRacer](#12-ia_enemyracer)
13. [RhythmGameTests](#13-rhythmgametests)

---

## 1. GameManager

**Archivo:** `Assets/Cs/GameManager.cs`

### Propósito
Controla el ciclo de vida del juego: menú principal, HUD, pausa y game over. Implementa el patrón Singleton.

### Estados (`GameState`)
| Estado | Descripción |
|--------|-------------|
| `MainMenu` | Pantalla principal, juego detenido |
| `Playing` | Partida activa |
| `Paused` | Pausa, tiempo congelado |
| `GameOver` | Jugador murió, tiempo congelado |

### Métodos principales
- `ChangeState(GameState)` — Transiciona entre estados, activa/desactiva paneles de UI, controla `Time.timeScale`
- `ActionIniciarJuego()` — Reinicia score, combo y salud; posiciona al jugador en X=0
- `ActionPausar()` / `ActionReanudar()` — Pausa/reanuda
- `ActionReiniciarJuego()` — Recarga la escena con bandera para saltar el menú
- `ActionRegresarAlMenu()` — Vuelve al menú, borra datos guardados si venía de GameOver
- `ActionSalirYGuardar()` — Guarda posición X y score en PlayerPrefs
- `ActionCerrarAplicacion()` — Cierra el juego
- `AgregarAnillaCombo()` — Incrementa combo, cada 3 anillas sube el multiplicador (máx. 10x)
- `ResetearComboPorFallo()` — Reinicia combo y multiplicador a 1x

### Score
- Aumenta en tiempo real: `currentScore += Time.deltaTime * 25 * scoreMultiplier`
- Cambia de color según el multiplicador: white (1x), green (2-4x), blue (5-7x), purple (8-10x)

---

## 2. SimpleFluidDrive

**Archivo:** `Assets/Cs/SimpleFluidDrive.cs`

### Propósito
Controla el movimiento del vehículo del jugador usando `CharacterController` y el New Input System.

### Componentes requeridos
- `CharacterController`

### Inputs
- `moveActionReference` (InputActionReference) — Vector2 para steering horizontal

### Funcionamiento
- **Rotación:** Gira la trompa del auto en el eje Y según input horizontal. Auto-enderezamiento progresivo. Límite: ±30°
- **Movimiento:** Avanza en `transform.forward * forwardSpeed` con gravedad aplicada
- **Gravedad:** 25 m/s², aplicada solo cuando no está en el suelo
- **Animación de ruedas:**
  - Delanteras: rotación X (avance) + rotación Y (dirección)
  - Traseras: solo rotación X (avance)
  - Velocidad de giro leída del `TrackManager.roadScrollSpeed`

### Métodos públicos
- `EjecutarSaltoRampa(float fuerzaSalto)` — Inyecta velocidad vertical para saltos

---

## 3. PlayerHealth

**Archivo:** `Assets/Cs/PlayerHealth.cs`

### Propósito
Administra la salud del jugador. Al llegar a 0, notifica al GameManager para transicionar a GameOver.

### Métodos
- `TakeDamage(int damage)` — Resta vida, clamp a 0, si llega a 0 llama a `ManejarMuerte()`
- `ResetearSalud()` — Restaura `currentHealth = maxHealth`

---

## 4. PlayerRhythmBehavior

**Archivo:** `Assets/Cs/PlayerRhythmBehavior.cs`

### Propósito
Activa el "Overdrive" (velocidad duplicada) cuando el jugador presiona la tecla de acción rítmica (espacio).

### Inputs
- `jumpActionReference` — InputActionReference para detectar la pulsación

### Funcionamiento
- `ActivateOverdrive()` — Duplica `TrackManager.roadScrollSpeed` por 3 segundos
- `DeactivateOverdrive()` — Restaura la velocidad original dividiendo entre 2
- Solo se activa si el juego está en estado `Playing` y el overdrive no está ya activo

---

## 5. SmoothFollowCamera

**Archivo:** `Assets/Cs/SmoothCamera.cs`

### Propósito
Cámara que sigue al jugador con interpolación suave (SmoothDamp).

### Configuración
- `offset` — Distancia ideal respecto al target (default: 0, 5, -10)
- `smoothTime` — Factor de suavizado (0.3s por defecto)

### Funcionamiento
- En `LateUpdate` calcula la posición objetivo como `target.position + offset`
- Usa `Vector3.SmoothDamp` para interpolar
- Siempre mira al target con `LookAt` (ligeramente elevado)

---

## 6. CameraSpeedEffects

**Archivo:** `Assets/Cs/CameraSpeedObject.cs`

### Propósito
Modifica dinámicamente el FOV de la cámara según la velocidad del coche (efecto de velocidad).

### Configuración
- `minFOV` / `maxFOV` — Rango del campo de visión (60° a 85°)
- `maxSpeed` — Velocidad máxima de referencia (100)

### Funcionamiento
Calcula `speedRatio = velocidad / maxSpeed` y aplica `Lerp(minFOV, maxFOV, speedRatio)` al FOV de la CinemachineCamera.

---

## 7. TrackManager

**Archivo:** `Assets/Cs/TrackManager.cs`

### Propósito
Genera y recicla tramos de pista proceduralmente. Implementa Singleton.

### Configuración
- `trackPrefabs` — Lista de prefabs de segmentos de pista
- `roadScrollSpeed` — Velocidad de desplazamiento de la pista
- `forwardChunks` — Bloques visibles adelante del coche

### Funcionamiento
- Al inicio genera 2 bloques fijos (rectas) + `forwardChunks` bloques aleatorios
- En `Update` mueve todos los bloques hacia atrás
- Cuando el coche pasa al siguiente bloque, recicla el más lejano (destruye el [0] y agrega uno nuevo al final)
- Los bloques se instancian en `nextSpawnPosition` que se actualiza buscando un hijo `PuntoFinal` en cada prefab

---

## 8. DynamicObstacle

**Archivo:** `Assets/Cs/DynamicObstacle.cs`

### Propósito
Obstáculo que se desplaza hacia el jugador y se recicla al frente cuando pasa el límite trasero.

### Configuración
- `minSpawnX` / `maxSpawnX` — Rango horizontal de reaparición (-6 a 6)
- `resetZPosition` — Coordenada Z de reaparición (40)
- `despawnZPosition` — Límite Z para reciclaje (-10)

### Funcionamiento
- Se mueve con `Translate(Vector3.back * roadScrollSpeed * Time.deltaTime)`
- Al superar `despawnZPosition` se reposiciona aleatoriamente en X y en `resetZPosition`

---

## 9. ObstacleDamage

**Archivo:** `Assets/Cs/ObstacleDamage.cs`

### Propósito
Detecta colisiones con el jugador (tag "Player") y aplica daño.

### Configuración
- `damageAmount` — Daño infligido (default: 15)

### Funcionamiento
- En `OnTriggerEnter`, si el otro tiene tag "Player" y el juego está en `Playing`:
  - Obtiene `PlayerHealth` y llama a `TakeDamage(damageAmount)`
  - Recicla el obstáculo detrás del `despawnZPosition`

---

## 10. RhythmRamp

**Archivo:** `Assets/Cs/RythmRamp.cs`

### Propósito
Rampa que impulsa al jugador verticalmente. Si el overdrive está activo, el salto es 1.5x más fuerte.

### Configuración
- `baseJumpForce` — Fuerza base del salto (12)
- `overdriveMultiplier` — Multiplicador cuando overdrive está activo (1.5x)

### Funcionamiento
- Se mueve igual que los obstáculos (sincronizado con TrackManager)
- En `OnTriggerEnter` detecta al jugador y llama a `SimpleFluidDrive.EjecutarSaltoRampa()`
- Se recicla inmediatamente tras ser pisada

---

## 11. RE_RhythmRing

**Archivo:** `Assets/Cs/Re/RE_RythmRing.cs`

### Propósito
Anilla rítmica que el jugador recolecta para aumentar el combo y el multiplicador de score.

### Configuración
- Mismos parámetros de movimiento/reciclaje que obstáculos y rampas
- `floatingComboText` (TextMeshPro) — Texto 3D flotante que muestra feedback visual

### Funcionamiento
- Se mueve sincronizada con TrackManager
- Al ser recolectada (`OnTriggerEnter` con tag "Player"):
  1. Llama a `GameManager.AgregarAnillaCombo()`
  2. Muestra feedback visual con el combo actual y multiplicador
  3. Se recicla al frente
- Si pasa de largo sin ser recolectada (`despawnZPosition`), penaliza llamando a `GameManager.ResetearComboPorFallo()`

### Feedback visual
- El texto flotante muestra: `+{combo} Combo!\nx{multiplicador}`
- Color del texto según multiplicador: verde (2-4x), azul (5-7x), morado (8-10x)

---

## 12. IA_EnemyRacer

**Archivo:** `Assets/Cs/Enemy/IA_EnemyRacer.cs`

### Propósito
IA para coches enemigos que esquiva obstáculos usando un sensor volumétrico (BoxCast).

### Estados
| Estado | Descripción |
|--------|-------------|
| `Patrolling` | Conducción normal, sin evasión |
| `Evading` | Detectó obstáculo, busca desviarse |
| `Aggressive` | Overdrive activo, mayor velocidad de reacción |

### Configuración
- `boxExtents` — Tamaño del cubo sensor (1.5, 1, 0.5)
- `sensorDistance` — Distancia de escaneo (15)
- `obstacleLayer` — LayerMask para filtrar obstáculos
- `reactionTime` — Tiempo de reacción simulado (0.25s)
- `sideSpeed` / `laneWidth` / `maxHorizontalLimit` — Parámetros de movimiento

### Funcionamiento
- Usa `Physics.BoxCast` para detectar obstáculos al frente
- En estado `Evading`, calcula desvío hacia el carril opuesto
- En estado `Aggressive` (cuando el jugador activa overdrive), aumenta `sideSpeed` a 14 y cambia de carril aleatoriamente
- Dibuja Gizmos en el editor para visualizar el sensor

---

## 13. RhythmGameTests

**Archivo:** `Assets/Tests/RhythmGameTests.cs`

### Propósito
Pruebas unitarias para verificar la lógica del juego usando el framework Unity Test Runner.

### Pruebas incluidas
- `GameManager_StartInMainMenu` — Verifica que el estado inicial sea MainMenu
- `GameManager_ChangeState_UpdatesTimeScale` — Verifica que Paused/GameOver pongan timeScale en 0
- `GameManager_ActionIniciarJuego_ResetsScoreAndCombo` — Verifica reinicio de score y combo
- `GameManager_AgregarAnillaCombo_IncrementsCombo` — Verifica que agregar anillas incremente combo
- `GameManager_ResetearComboPorFallo_ResetsMultiplier` — Verifica que fallar resetea el multiplicador
- `PlayerHealth_TakeDamage_ReducesHealth` — Verifica que TakeDamage reduzca salud
- `PlayerHealth_TakeDamage_TriggersGameOver` — Verifica que salud 0 active GameOver
- `PlayerHealth_ResetearSalud_RestoresHealth` — Verifica que ResetearSalud funcione
- `PlayerRhythmBehavior_Overdrive_ActivatesAndDeactivates` — Verifica activación/desactivación del overdrive
- `SimpleFluidDrive_EjecutarSaltoRampa_AppliesVelocity` — Verifica que el salto inyecte velocidad vertical
