using System.Collections.Generic;
using UnityEngine;

public class TrackManager : MonoBehaviour
{
    public static TrackManager Instance { get; private set; }

    [Header("Prefabs de Tramos de Pista")]
    [Tooltip("Lista con tus variantes: Recta, Curva, Chicana, etc.")]
    [SerializeField] private List<GameObject> trackPrefabs;

    [Header("Configuración del Mundo")]
    public float roadScrollSpeed = 30f;

    [Tooltip("Cuántos bloques quieres visibles adelante del coche")]
    [SerializeField] private int forwardChunks = 5;

    [SerializeField] private Transform initialSpawnPoint;

    private List<GameObject> activeChunks = new List<GameObject>();
    private Vector3 nextSpawnPosition;

    // Esta variable llevará el control de qué bloque está cruzando el coche
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
            if (i == 0 || i == 1) SpawnTrackChunk(0); // Rectas seguras al inicio
            else SpawnRandomTrackChunk();
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
        // 1. Instanciamos la pista en la posición calculada
        GameObject newChunk = Instantiate(trackPrefabs[index], nextSpawnPosition, Quaternion.identity);

        // =========================================================================
        // APLICAR CORRECCIÓN DE OFFSET EN X (Ajuste de 0.25f)
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
    }

    /// <summary>
    /// Devuelve el bloque de pista que se encuentra más alejado del jugador (el último en el horizonte).
    /// Utilizado por anillas, rampas y obstáculos para anclarse proceduralmente en curvas correctas.
    /// </summary>
    public Transform ObtenerBloqueMasLejano()
    {
        if (activeChunks != null && activeChunks.Count > 0)
        {
            // Retornamos el transform del último chunk generado en la lista
            GameObject latestChunk = activeChunks[activeChunks.Count - 1];
            if (latestChunk != null)
            {
                return latestChunk.transform;
            }
        }
        return null;
    }
}