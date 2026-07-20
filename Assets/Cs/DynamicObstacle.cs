using UnityEngine;

public class DynamicObstacle : MonoBehaviour
{
    [Header("Movimiento Lateral (Ping-Pong)")]
    [Tooltip("Qué tan lejos se mueve a la izquierda y derecha desde su centro")]
    public float amplitudMovimiento = 3f;

    [Tooltip("Velocidad del vaivén. Sintonízala con los BPM de tu Drum and Bass")]
    public float velocidadOscilacion = 5f;

    [Tooltip("Desfase de tiempo (útil si pones varios obstáculos seguidos para que no se muevan idénticos)")]
    public float desfaseInicio = 0f;

    private float posicionInicialLocalX;

    void Start()
    {
        // Guardamos la X local en la que lo acomodaste en el prefab
        posicionInicialLocalX = transform.localPosition.x;

        // Si quieres que el desfase sea aleatorio por defecto, descomenta la siguiente línea:
        // desfaseInicio = Random.Range(0f, 2f * Mathf.PI);
    }

    void Update()
    {
        // El obstáculo solo oscila si el juego está activo
        if (GameManager.Instance == null || GameManager.Instance.currentState != GameManager.GameState.Playing)
            return;

        // 1. Calculamos la onda de vaivén usando el tiempo musical del juego
        float ondaSeno = Mathf.Sin((Time.time * velocidadOscilacion) + desfaseInicio);

        // 2. Modificamos únicamente la posición LOCAL en X
        float nuevaXLocal = posicionInicialLocalX + (ondaSeno * amplitudMovimiento);

        // 3. Aplicamos manteniendo intactos Y y Z locales del tramo de pista
        transform.localPosition = new Vector3(nuevaXLocal, transform.localPosition.y, transform.localPosition.z);
    }
}