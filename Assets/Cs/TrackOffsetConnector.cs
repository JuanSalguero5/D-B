using UnityEngine;

public class TrackOffsetCorrector : MonoBehaviour
{
    [Header("Ajuste de Alineación")]
    [SerializeField] private float offsetX = 0.25f; // El desfase de 0.25 en X que comentas

    private void Awake()
    {
        // Corregimos la posición local inicial en X apenas el objeto se instancia en el mundo
        Vector3 posicionActual = transform.position;
        posicionActual.x += offsetX;
        transform.position = posicionActual;
    }
}