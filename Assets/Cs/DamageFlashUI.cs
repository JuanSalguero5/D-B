using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Flash de daño en la UI (imagen de rayos / vignette).
/// Cada golpe recibido sube el alpha de la imagen por niveles:
///   - 1er golpe  -> alphaNivel1
///   - 2do golpe  -> alphaNivel2
///   - 3er golpe o más -> alphaNivel3
/// Si no se recibe daño durante tiempoSinDanioParaOcultar, el alpha se
/// desvanece hasta 0 y el contador vuelve a cero (el próximo golpe empieza
/// de nuevo desde el nivel 1).
/// USO: arrastrar sobre el GameObject que tiene la Image (el rayo).
/// </summary>
[RequireComponent(typeof(Image))]
public class DamageFlashUI : MonoBehaviour
{
    [Header("Niveles de flash (alpha 0..1)")]
    [Tooltip("Alpha tras el 1er golpe. 0.01 = 1%")]
    public float alphaNivel1 = 0.01f;
    [Tooltip("Alpha tras el 2do golpe. 0.5 = 50%")]
    public float alphaNivel2 = 0.5f;
    [Tooltip("Alpha desde el 3er golpe en adelante. 0.9 = 90%")]
    public float alphaNivel3 = 0.9f;

    [Header("Tiempo")]
    [Tooltip("Segundos sin recibir daño para que el flash empiece a desvanecerse.")]
    public float tiempoSinDanioParaOcultar = 2f;
    [Tooltip("Velocidad de desvanecido (unidades de alpha por segundo).")]
    public float velocidadDesvanecer = 1.5f;

    private Image imagen;
    private float alphaActual;
    private float tiempoDesdeUltimoGolpe;
    private int golpesConsecutivos;

    void Awake()
    {
        imagen = GetComponent<Image>();
        alphaActual = 0f;
        AplicarAlpha();
    }

    void OnEnable()
    {
        PlayerHealth salud = FindObjectOfType<PlayerHealth>();
        if (salud != null)
            salud.OnDamageReceived += RecibirGolpe;
    }

    void OnDisable()
    {
        PlayerHealth salud = FindObjectOfType<PlayerHealth>();
        if (salud != null)
            salud.OnDamageReceived -= RecibirGolpe;
    }

    void Update()
    {
        if (alphaActual <= 0f)
            return;

        tiempoDesdeUltimoGolpe += Time.deltaTime;
        if (tiempoDesdeUltimoGolpe >= tiempoSinDanioParaOcultar)
        {
            alphaActual = Mathf.MoveTowards(alphaActual, 0f, velocidadDesvanecer * Time.deltaTime);
            if (alphaActual <= 0f)
                golpesConsecutivos = 0; // recuperado del todo, el próximo golpe empieza de nuevo

            AplicarAlpha();
        }
    }

    private void RecibirGolpe(int cantidad)
    {
        golpesConsecutivos = Mathf.Min(golpesConsecutivos + 1, 3);

        float objetivo;
        switch (golpesConsecutivos)
        {
            case 1:  objetivo = alphaNivel1; break;
            case 2:  objetivo = alphaNivel2; break;
            default: objetivo = alphaNivel3; break;
        }

        // Un golpe nuevo nunca baja el flash, solo lo sube.
        alphaActual = Mathf.Max(alphaActual, objetivo);
        tiempoDesdeUltimoGolpe = 0f;
        AplicarAlpha();
    }

    private void AplicarAlpha()
    {
        if (imagen == null) return;
        Color c = imagen.color;
        c.a = alphaActual;
        imagen.color = c;
    }
}
