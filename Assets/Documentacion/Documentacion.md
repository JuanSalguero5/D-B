# Documentación del Proyecto — D&B (Unity)

## Índice

1. [GameManager](#1-gamemanager)
2. [SimpleFluidDrive](#2-simplefluiddrive)
3. [RaycastSuspension](#3-raycastsuspension)
4. [PlayerHealth](#4-playerhealth)
5. [DamageInvincibility](#5-damageinvincibility)
6. [PlayerRhythmBehavior](#6-playerrhythmbehavior)
7. [AudioManager](#7-audiomanager)
8. [TrackManager](#8-trackmanager)
9. [TrackOffsetCorrector](#9-trackoffsetcorrector)
10. [DynamicObstacle](#10-dynamicobstacle)
11. [ObstacleDamage](#11-obstacledamage)
12. [RhythmRamp](#12-rhythmramp)
13. [RE_RhythmRing](#13-re_rhythmring)
14. [RingPostProcess](#14-ringpostprocess)
15. [CameraSpeedEffects](#15-cameraspeedeffects)
16. [IA_EnemyRacer](#16-ia_enemyracer)
17. [RhythmGameTests](#17-rhythmgametests)

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
| `Paused` | Pausa, tiempo congelado (`Time.timeScale = 0`) |
| `GameOver` | Jugador murió, tiempo congelado |

### Configuración (Inspector)
- **Paneles UI:** `mainMenuPanel`, `hudPanel`, `pauseMenuPanel`, `gameOverPanel`
- **Textos TMPro:** `scoreText`, `menuLastScoreText`, `gameOverScoreText`, `comboText`, `countdownText`
- **Juego:** `playerVehicle`, `currentScore`
- **Combo:** `currentComboCount`, `scoreMultiplier`, `tiempoMaximoParaCombo` (2s de margen)
- **Conteo regresivo:** `readyDuration` (1s), `goDuration` (0.7s)

### Métodos principales
- `ChangeState(GameState)` — Transiciona estados, activa/desactiva paneles, controla `Time.timeScale`, reproduce música según estado y guarda el HI-SCORE en `PlayerPrefs` al llegar a GameOver
- `ActionIniciarJuego()` — Reinicia score, combo y salud; posiciona al jugador en X=0; arranca el conteo Ready/Go
- `IniciarConteoRegresivo()` — Corrutina que muestra "READY" → "GO!" con sonidos antes de entrar a Playing
- `ActionPausar()` / `ActionReanudar()` — Pausa/reanuda
- `ActionReiniciarJuego()` — Recarga la escena con la bandera `debeIniciarJugando = true` para saltarse el menú
- `ActionRegresarAlMenu()` — Vuelve al menú limpiando score/combo y `Time.timeScale`
- `ActionCerrarAplicacion()` — `Application.Quit()`
- `AgregarAnillaCombo()` — Incrementa combo, cada 3 anillas sube el multiplicador (máx. 10x) y reinicia el cronómetro de combo
- `ResetearComboPorFallo()` — Reinicia combo y multiplicador a 1x (se llama al pasar de tiempo o fallar anilla)

### Score
- Aumenta en tiempo real: `currentScore += Time.deltaTime * 25 * scoreMultiplier`
- Cambia de color según el multiplicador: white (1x), green (2-4x), azul (5-7x), morado (8-10x)
- HI-SCORE persistente en `PlayerPrefs` bajo la clave `"HighScore"`

---

## 2. SimpleFluidDrive

**Archivo:** `Assets/Cs/SimpleFluidDrive.cs`

### Propósito
Controla el movimiento del coche del jugador con **física real** (Rigidbody dinámico + suspensión por raycasts), siguiendo el sistema del video "Custom Car Physics" de Toyful Games.

### Componentes requeridos
- `Rigidbody` (se añade solo con `[RequireComponent]`)

### Inputs
- `moveActionReference` (InputActionReference) — Vector2 para steering horizontal

### Configuración (Inspector)
- **Conducción:** `forwardSpeed` (30), `turnSpeed` (45)
- **Maniobra:** `rotationLean`, `rotationSmoothSpeed` (reservados para game feel)
- **Suspensión:** `suspension` (RaycastSuspension, se auto-asigna si está en el mismo objeto)
- **Salto:** `jumpPitchAngle` (25°), `pitchAirLerpSpeed` (8), `pitchGroundedLerpSpeed` (6), `modeloVisualCoche` (opcional)
- **Ruedas:** `frontWheels`, `allWheels`, `maxSteerAngle` (25), `wheelSpinSpeed` (200), `direccionGiroAvance`

### Funcionamiento
- **Start:** activa `useGravity = true` y congela rotaciones + posición Z (`FreezeRotation | FreezePositionZ`). La rotación Y (yaw) y el pitch del salto son procedurales.
- **Update:** lee el input; en estados de menú/pausa/cuenta regresiva pone el Rigidbody **kinemático** y en `Playing` lo vuelve dinámico.
- **FixedUpdate:**
  1. Calcula el yaw de la trompa según input (límite ±30°, auto-enderezamiento).
  2. `movimientoHorizontalX = Sin(yaw) * forwardSpeed`.
  3. Llama a `suspension.ActualizarSuspension(rb)` — cada rueda aplica su fuerza de muelle con `AddForceAtPosition`.
  4. Sobrescribe `rb.velocity.x` (dirección de la trompa) y `rb.velocity.z = 0`. La **Y la maneja la física** (gravedad + suspensión), por eso el coche colisiona de verdad con el suelo.
  5. Aplica yaw y pitch de salto (lerp a 0 al tocar el suelo).
  6. Anima las ruedas (avance X + dirección Y en las delanteras).

### Métodos públicos
- `EjecutarSaltoRampa(float fuerzaSalto)` — Inyecta velocidad vertical (`rb.velocity.y`) para saltos

---

## 3. RaycastSuspension

**Archivo:** `Assets/Cs/RaycastSuspension.cs`

### Propósito
Sistema de suspensión por raycasts del video de Toyful Games: 4 ruedas lanzan un raycast hacia el suelo y aplican la ley del muelle `F = (compresión * rigidez) − (velocidad * amortiguador)` en el ancla de cada rueda.

### Configuración (Inspector)
- `ruedas` — Array de 4 `RuedaSuspension` (Trasera Izq, Trasera Der, Delantera Izq, Delantera Der)
- `capaSuelo` (LayerMask) — Capas que cuentan como piso
- `dibujarGizmos` — Depuración visual en el editor

### Por rueda (`RuedaSuspension`)
| Campo | Descripción |
|-------|-------------|
| `ancla` | Transform en la parte alta de la rueda (unión del amortiguador al chasis) |
| `ruedaVisual` | (Opcional) malla de la rueda, se baja según la compresión |
| `wheelRadius` | Radio real de la rueda |
| `restDistance` | Longitud de reposo del muelle (ancla→suelo con rueda tocando) |
| `springStrength` | Rigidez del muelle (default 200) |
| `damperStrength` | Amortiguador (default 40) |

### Funcionamiento
- `ActualizarSuspension(Rigidbody rb)` — Se llama desde el `FixedUpdate` del controlador. Lanza raycasts hacia abajo (ignora hits sobre el propio coche), calcula compresión y aplica `rb.AddForceAtPosition` hacia arriba en cada ancla.
- Propiedades de lectura: `IsGrounded`, `RuedasEnSuelo`.
- Acepta colliders trigger como piso (`QueryTriggerInteraction.Collide`).
- Gizmos: verde si la rueda toca el suelo, rojo en posición de reposo.

---

## 4. PlayerHealth

**Archivo:** `Assets/Cs/PlayerHealth.cs`

### Propósito
Administra la salud del jugador. Al llegar a 0, notifica al GameManager para transicionar a GameOver.

### Configuración
- `maxHealth` (100), `currentHealth`

### Métodos
- `TakeDamage(int damage)` — Resta vida (clamp a 0). Ignora el daño si `DamageInvincibility.IsInvulnerable()`. Si la salud llega a 0 llama a `ManejarMuerte()` → `GameManager.ChangeState(GameOver)`; si no, activa la invulnerabilidad.
- `ResetearSalud()` — Restaura `currentHealth = maxHealth`

---

## 5. DamageInvincibility

**Archivo:** `Assets/Cs/DamageInvincibility.cs`

### Propósito
Otorga invulnerabilidad temporal tras recibir daño y muestra un efecto visual "dither" (parpadeo) en los materiales del coche usando el parámetro `_AlphaThreshold` del shader.

### Configuración
- `invincibilityDuration` (2s), `ditherSpeed` (20)
- `materialesEfecto` (List<Material>) — Se llena automáticamente con los materiales de los renderers que tengan `_AlphaThreshold`

### Métodos
- `ActivarInvulnerabilidad()` — Inicia el temporizador y el parpadeo
- `IsInvulnerable()` — Estado actual (bool)

---

## 6. PlayerRhythmBehavior

**Archivo:** `Assets/Cs/PlayerRhythmBehavior.cs`

### Propósito
Activa el "Overdrive" cuando el jugador presiona la acción rítmica (espacio), multiplicando temporalmente la velocidad de la pista.

### Inputs
- `jumpActionReference` — InputActionReference para detectar la pulsación

### Configuración
- `overdriveDuration` (3s), `speedMuliplier` (1.2), `isOverdriveActive`

### Funcionamiento
- `OnJumpPerformed` — Solo activa si el juego está en `Playing` y el overdrive no está activo
- `ActivateOverdrive()` — `roadScrollSpeed *= speedMuliplier`
- `DeactivateOverdrive()` — `roadScrollSpeed /= speedMuliplier` (al agotarse el temporizador)

---

## 7. AudioManager

**Archivo:** `Assets/Cs/AudioManager.cs`

### Propósito
Singleton de audio con `DontDestroyOnLoad`. Gestiona música de menú/juego y los sonidos del conteo regresivo.

### Configuración
- `musicSource`, `sfxSource`, `menuMusic`, `gameMusic`, `readySound`, `goSound`

### Métodos
- `PlayMenuMusic()` / `PlayGameMusic()` — Cambian el clip solo si es distinto al actual
- `ResumeMusic()` — Desmutea y reanuda si estaba detenida
- `SetMusicVolume(bool)` — Mutea/desmutea la música
- `PlayReadySound()` / `PlayGoSound()` — Sonidos del conteo

---

## 8. TrackManager

**Archivo:** `Assets/Cs/TrackManager.cs`

### Propósito
Genera y recicla tramos de pista proceduralmente. Implementa Singleton.

### Configuración
- `trackPrefabs` (List<GameObject>) — Variantes de tramo (recta, curva, chicana, etc.)
- `initialTrackPrefab` — Tramo con el que SIEMPRE arranca la partida (debe ir vacío de obstáculos)
- `roadScrollSpeed` (30), `forwardChunks` (5), `initialSpawnPoint`

### Funcionamiento
- Al inicio genera 2 tramos con `initialTrackPrefab` + `forwardChunks` tramos aleatorios
- En `Update` mueve todos los tramos hacia atrás (`Vector3.back * roadScrollSpeed * Time.deltaTime`)
- Cuando el tramo `[currentCarChunkIndex + 1]` cruza Z=0, recicla el bloque más lejano: destruye `[0]` y genera uno nuevo al final
- Cada tramo se encadena buscando el hijo `PuntoFinal`; si no existe, avanza 40 unidades en Z
- Aplica una corrección de offset en X de **+0.25** a cada tramo instanciado
- `ObtenerBloqueMasLejano()` — Devuelve el último chunk generado (usado por anillas/obstáculos para anclarse proceduralmente)

---

## 9. TrackOffsetCorrector

**Archivo:** `Assets/Cs/TrackOffsetConnector.cs`

### Propósito
Corrige la posición en X de un objeto al instanciarse sumándole `offsetX` (default 0.25), para alinear tramos/props con la pista.

---

## 10. DynamicObstacle

**Archivo:** `Assets/Cs/DynamicObstacle.cs`

### Propósito
Obstáculo con movimiento lateral tipo ping-pong (onda seno) sincronizado con el beat de la música.

### Configuración
- `amplitudMovimiento` (3), `velocidadOscilacion` (5), `desfaseInicio` (0)

### Funcionamiento
- Oscila en la X local alrededor de la posición inicial: `posInicialX + Sin(Time.time * velocidad + desfase) * amplitud`
- Solo oscila cuando el juego está en `Playing`

---

## 11. ObstacleDamage

**Archivo:** `Assets/Cs/ObstacleDamage.cs`

### Propósito
Trigger que detecta colisiones con el jugador (tag "Player") y aplica daño.

### Configuración
- `damageAmount` (15)

### Funcionamiento
- `Awake`: si no tiene Rigidbody, le añade uno **kinemático** (para que el trigger funcione contra el Rigidbody dinámico del coche)
- `OnTriggerEnter` con tag "Player" y estado `Playing`:
  - Llama a `PlayerHealth.TakeDamage(damageAmount)`
  - Desactiva el obstáculo (`SetActive(false)`) para evitar doble daño

---

## 12. RhythmRamp

**Archivo:** `Assets/Cs/RythmRamp.cs`

### Propósito
Rampa que impulsa al jugador verticalmente. Si el overdrive está activo, el salto es 1.5x más fuerte.

### Configuración
- `baseJumpForce` (12), `overdriveMultiplier` (1.5)

### Funcionamiento
- `Awake`: añade Rigidbody kinemático si no lo tiene (mismo motivo que ObstacleDamage)
- `OnTriggerEnter` con tag "Player" y estado `Playing`:
  - Calcula la fuerza final (multiplica por overdrive si aplica)
  - Llama a `SimpleFluidDrive.EjecutarSaltoRampa(fuerza)`
  - Se desactiva para no usarse dos veces

---

## 13. RE_RhythmRing

**Archivo:** `Assets/Cs/Re/RE_RythmRing.cs`

### Propósito
Anilla rítmica que el jugador recolecta para aumentar el combo y el multiplicador de score.

### Configuración
- `floatingComboText` (TextMeshPro) — Texto 3D flotante de feedback
- `mallaVisualAnilla` (MeshRenderer) — Malla del anillo para apagarla al recolectar

### Funcionamiento
- `Awake`: añade Rigidbody kinemático si no lo tiene
- `OnTriggerEnter` con tag "Player" y estado `Playing`:
  1. `GameManager.AgregarAnillaCombo()`
  2. `MostrarFeedbackFlotante()` — muestra `+{combo} Combo!\nx{multiplicador}` con color según multiplicador
  3. `RingPostProcess.Instance.TriggerFeedback()` — pulso de post-procesado
  4. Desactiva el collider y la malla del anillo, y destruye el objeto en 1 segundo

---

## 14. RingPostProcess

**Archivo:** `Assets/Cs/RingPostProcess.cs`

### Propósito
Activa un pulso visual de post-procesado (Volume) al recolectar una anilla.

### Funcionamiento
- `TriggerFeedback()` — Pone `targetWeight = 1`
- En `Update` interpola `volume.weight` hacia `targetWeight` y lo va decrementando para que el efecto se desvanezca

---

## 15. CameraSpeedEffects

**Archivo:** `Assets/Cs/CameraSpeedObject.cs`

### Propósito
Cambia entre la cámara normal y la cámara de velocidad (Cinemachine) según la velocidad de la pista.

### Configuración
- `normalCamera`, `overdriveCamera` (CinemachineCamera), `minSpeed`
- Prioridades: `activePriority` (15), `inactivePriority` (10)

### Funcionamiento
- Si `roadScrollSpeed > minSpeed` (overdrive), sube la prioridad de `overdriveCamera`; si no, la de `normalCamera`. Cinemachine hace la transición automáticamente.

---

## 16. IA_EnemyRacer

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
- `sideSpeed` (8), `laneWidth` (3.5), `maxHorizontalLimit` (6)
- `sensorDistance` (15), `boxExtents` (1.5, 1, 0.5), `obstacleLayer`
- `reactionTime` (0.25)

### Funcionamiento
- `Physics.BoxCast` hacia adelante para detectar obstáculos en `obstacleLayer`
- En `Evading` calcula un desvío al carril opuesto (`laneWidth`) con el `reactionTime` como reacción simulada
- En `Aggressive` (jugador en overdrive) sube `sideSpeed` a 14 y cambia de carril aleatoriamente
- Dibuja el cubo sensor en el editor (Gizmos)

---

## 17. RhythmGameTests

**Archivo:** `Assets/Tests/RhythmGameTests.cs`

### Propósito
Pruebas unitarias para verificar la lógica del juego usando el framework Unity Test Runner.

### Pruebas incluidas
- `PlayerHealth_RecibeDano_ReduceSaludCorrectamente` — Verifica que TakeDamage reduzca la salud
- `PlayerHealth_ResetearSalud_DevuelveVidaAlMaximo` — Verifica que ResetearSalud restaure la vida
- `GameManager_MultiplicadorCombo_NoSuperaTopeMaximo` — Verifica que el multiplicador no pase de 10x
