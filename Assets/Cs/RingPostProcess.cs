using UnityEngine;
using UnityEngine.Rendering;
public class RingPostProcess : MonoBehaviour
{
    public static RingPostProcess Instance;
    private Volume volume;
    private float targetWeight = 0f;

    void Awake() => Instance = this;

    void Start() => volume = GetComponent<Volume>();

    void Update()
    {
        // Suavizamos el peso del efecto para que no se active/desactive de golpe
        volume.weight = Mathf.Lerp(volume.weight, targetWeight, Time.deltaTime * 10f);

        // Si ya casi llega a 0, empezamos a bajar el target para que desaparezca
        if (targetWeight > 0) targetWeight -= Time.deltaTime * 2f;
    }

    public void TriggerFeedback()
    {
        targetWeight = 1f; // Activa el efecto al 100%
    }
}