using System.Collections.Generic;
using UnityEngine;

public class TrackManager : MonoBehaviour
{
    public static TrackManager Instance { get; private set; }

    [Header("Prefabs de Tramos de Pista")]
    [Tooltip("Lista con tus variantes: Recta, Curva, Chicana, etc.")]
    [SerializeField] private List<GameObject> trackPrefabs;

    [Header("Pista Inicial (Vacía)")]
    [Tooltip("Prefab con el que SIEMPRE arranca la partida. Déjalo vacío de obstáculos para no empezar de una.")]
    public GameObject initialTrackPrefab;

    [Header("Configuración del Mundo")]
    public float roadScrollSpeed = 30f;

    [Tooltip("Cu�ntos bloques quieres visibles adelante del coche")]
    [SerializeField] private int forwardChunks = 5;

    [SerializeField] private Transform initialSpawnPoint;

    [Header("Edificios a los Lados de la Pista")]
    [Tooltip("Prefabs de edificios. Cada prefab debe traer edificios a ambos lados y el hueco de la pista en el centro (|Edificios| |pista| |edificios|). Se instancia UNO por cada tramosEntreEdificios.")]
    [SerializeField] private List<GameObject> buildingPrefabs;

    [Tooltip("Activa o desactiva la generacion de edificios")]
    [SerializeField] private bool generarEdificios = true;

    [Tooltip("Cada CUANTOS TRAMOS de pista se genera un conjunto de edificios. Como tu bloque mide ~3 tramos, deja 3 para que enganchen en fila continua.")]
    [SerializeField] private int tramosEntreEdificios = 3;

    [Tooltip("Desplazamiento manual en Z para alinear el borde del prefab con el inicio del tramo (por si el pivot del modelo no esta en el borde).")]
    [SerializeField] private float buildingZOffset = 0f;

    [Tooltip("Desplazamiento manual en X por si un prefab necesita ajustarse. 0 = centrado en la pista (el hueco de la carretera ya viene en el modelo).")]
    [SerializeField] private float buildingOffsetX = 0f;

    [Tooltip("Los edificios se destruyen cuando pasan esta distancia por detras del coche")]
    [SerializeField] private float buildingBehindDestroyZ = -60f;

    private List<GameObject> activeChunks = new List<GameObject>();
    private List<GameObject> activeBuildings = new List<GameObject>();
    private Vector3 nextSpawnPosition;
    private int tramosDesdeUltimoEdificio = 0;

    // Esta variable llevar� el control de qu� bloque est� cruzando el coche
    private int currentCarChunkIndex = 1;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        if (initialSpawnPoint != null)
        {
            nextSpawnPosition = initialSpawnPoint.position;
        }

        // Para lograr tu esquema: [0] [1] [X 2] [3] [4] [5] [6]
        int totalInitialChunks = 2 + forwardChunks;

        for (int i = 0; i < totalInitialChunks; i++)
        {
            // Las dos primeras posiciones usan la Pista Inicial (vacía) si está asignada
            if (i == 0 || i == 1)
            {
                if (initialTrackPrefab != null) SpawnTrackPrefab(initialTrackPrefab);
                else SpawnTrackChunk(0); // Fallback: recta segura
            }
            else
            {
                SpawnRandomTrackChunk();
            }
        }
    }

    void Update()
    {
        if (GameManager.Instance == null || GameManager.Instance.currentState != GameManager.GameState.Playing)
            return;

        Vector3 movement = Vector3.back * roadScrollSpeed * Time.deltaTime;
        nextSpawnPosition += movement;

        // 1. MOVER TODOS LOS TRAMOS
        for (int i = 0; i < activeChunks.Count; i++)
        {
            if (activeChunks[i] != null)
            {
                activeChunks[i].transform.Translate(movement, Space.World);
            }
        }

        // 1b. MOVER LOS EDIFICIOS junto con la pista
        for (int i = 0; i < activeBuildings.Count; i++)
        {
            if (activeBuildings[i] != null)
            {
                activeBuildings[i].transform.Translate(movement, Space.World);
            }
        }

        // 1c. RECICLAR edificios que quedaron detras del coche
        ReciclarEdificiosDetras();

        // 2. DETECTAR CUANDO EL COCHE PASA AL SIGUIENTE BLOQUE
        if (activeChunks.Count > currentCarChunkIndex + 1)
        {
            GameObject nextChunk = activeChunks[currentCarChunkIndex + 1];

            if (nextChunk != null && nextChunk.transform.position.z <= 0f)
            {
                RecycleOldestChunk();
            }
        }
    }

    private void RecycleOldestChunk()
    {
        // 1. Generamos uno nuevo al fondo del horizonte para mantener el camino largo
        SpawnRandomTrackChunk();

        // 2. Eliminamos de forma segura el bloque [0]. 
        GameObject chunkToDestroy = activeChunks[0];
        activeChunks.RemoveAt(0);

        if (chunkToDestroy != null)
        {
            // Opcional: Desparentar hijos sueltos reciclables antes de borrar el bloque padre
            // para evitar excepciones si usas un pool independiente en el futuro.
            chunkToDestroy.transform.DetachChildren();
            Destroy(chunkToDestroy);
        }
    }

    private void SpawnRandomTrackChunk()
    {
        if (trackPrefabs.Count == 0) return;
        int randomIndex = Random.Range(0, trackPrefabs.Count);
        SpawnTrackChunk(randomIndex);
    }

    private void SpawnTrackChunk(int index)
    {
        if (trackPrefabs.Count == 0) return;
        SpawnTrackPrefab(trackPrefabs[index]);
    }

    private void SpawnTrackPrefab(GameObject prefab)
    {
        if (prefab == null) return;

        // 1. Instanciamos la pista en la posici�n calculada
        GameObject newChunk = Instantiate(prefab, nextSpawnPosition, Quaternion.identity);

        // =========================================================================
        // APLICAR CORRECCI�N DE OFFSET EN X (Ajuste de 0.25f)
        // =========================================================================
        Vector3 correctedPosition = newChunk.transform.position;
        correctedPosition.x += 0.25f;
        newChunk.transform.position = correctedPosition;
        // =========================================================================

        activeChunks.Add(newChunk);

        // 2. Calculamos el siguiente punto
        Transform endPoint = newChunk.transform.Find("PuntoFinal");
        if (endPoint != null)
        {
            nextSpawnPosition = endPoint.position;
        }
        else
        {
            nextSpawnPosition += new Vector3(0f, 0f, 40f);
        }

        // 3. GENERAR EDIFICIOS: cada tramosEntreEdificios tramos se instancia un conjunto
        //    alineado con el inicio del tramo que se acaba de generar (misma Z).
        tramosDesdeUltimoEdificio++;
        if (generarEdificios && buildingPrefabs != null && buildingPrefabs.Count > 0 &&
            tramosEntreEdificios > 0 && tramosDesdeUltimoEdificio >= tramosEntreEdificios)
        {
            tramosDesdeUltimoEdificio = 0;
            SpawnBuilding(newChunk.transform.position);
        }
    }

    /// <summary>
    /// Instancia un conjunto de edificios CENTRADO en la pista y alineado con el inicio
    /// de un tramo. Los prefabs ya traen edificios a ambos lados y el hueco de la carretera
    /// en el centro (hechos en Blender con la pista como referencia).
    /// </summary>
    private void SpawnBuilding(Vector3 posicionTramo)
    {
        GameObject prefab = buildingPrefabs[Random.Range(0, buildingPrefabs.Count)];
        if (prefab == null) return;

        // Misma correccion de +0.25 que los tramos de carretera para que el hueco del
        // modelo coincida con la pista real. El resto de offset es un nudge manual.
        float offsetX = buildingOffsetX + 0.25f;
        float y = initialSpawnPoint != null ? initialSpawnPoint.position.y : 0f;
        Vector3 posicion = new Vector3(offsetX, y, posicionTramo.z + buildingZOffset);

        GameObject building = Instantiate(prefab, posicion, Quaternion.identity);
        activeBuildings.Add(building);
    }

    /// <summary>
    /// Destruye los edificios que quedaron detras del coche (ya no se ven).
    /// La lista se recorre por el frente porque se generan en orden de Z creciente.
    /// </summary>
    private void ReciclarEdificiosDetras()
    {
        while (activeBuildings.Count > 0)
        {
            GameObject frente = activeBuildings[0];
            if (frente == null)
            {
                activeBuildings.RemoveAt(0);
                continue;
            }

            if (frente.transform.position.z < buildingBehindDestroyZ)
            {
                activeBuildings.RemoveAt(0);
                Destroy(frente);
            }
            else
            {
                break;
            }
        }
    }

    /// <summary>
    /// Devuelve el bloque de pista que se encuentra m�s alejado del jugador (el �ltimo en el horizonte).
    /// Utilizado por anillas, rampas y obst�culos para anclarse proceduralmente en curvas correctas.
    /// </summary>
    public Transform ObtenerBloqueMasLejano()
    {
        if (activeChunks != null && activeChunks.Count > 0)
        {
            // Retornamos el transform del �ltimo chunk generado en la lista
            GameObject latestChunk = activeChunks[activeChunks.Count - 1];
            if (latestChunk != null)
            {
                return latestChunk.transform;
            }
        }
        return null;
    }
}