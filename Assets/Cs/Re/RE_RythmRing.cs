using UnityEngine;
using TMPro;

[RequireComponent(typeof(Collider))]
public class RE_RhythmRing : MonoBehaviour
{
    [Header("Feedback Visual Dinámico")]
    public TextMeshPro floatingComboText;

    [Header("Referencia Opcional")]
    [Tooltip("Arrastra aquí la malla 3D/anillo visual para apagarlo de forma exacta sin romper el texto")]
    public MeshRenderer mallaVisualAnilla;

    private void OnTriggerEnter(Collider other)
    {
        if (GameManager.Instance == null || GameManager.Instance.currentState != GameManager.GameState.Playing)
            return;

        if (other.CompareTag("Player"))
        {
            GameManager.Instance.AgregarAnillaCombo();
            MostrarFeedbackFlotante();

            if (RingPostProcess.Instance != null)
            {
                RingPostProcess.Instance.TriggerFeedback();
            }

            // 1. Apagamos el colisionador de inmediato para evitar dobles recolecciones
            GetComponent<Collider>().enabled = false;

            // 2. CORRECCIÓN FLUIDA: Intentamos apagar el MeshRenderer específico si está asignado
            if (mallaVisualAnilla != null)
            {
                mallaVisualAnilla.enabled = false;
            }
            else
            {
                // Alternativa automática si no quieres arrastrar la referencia en el inspector:
                // Busca un MeshRenderer en el objeto actual o en sus hijos y lo apaga
                MeshRenderer mr = GetComponentInChildren<MeshRenderer>();
                if (mr != null) mr.enabled = false;
            }

            // 3. El objeto completo (incluido el texto) se destruye en 1 segundo
            Destroy(gameObject, 1f);
        }
    }

    private void MostrarFeedbackFlotante()
    {
        if (floatingComboText != null)
        {
            // Aseguramos que el objeto del texto esté activo por si acaso
            floatingComboText.gameObject.SetActive(true);

            int multiplicadorActual = GameManager.Instance.scoreMultiplier;
            int comboActual = GameManager.Instance.currentComboCount;

            floatingComboText.text = $"+{comboActual} Combo!\nx{multiplicadorActual}";

            if (multiplicadorActual >= 2 && multiplicadorActual <= 4) floatingComboText.color = Color.green;
            else if (multiplicadorActual >= 5 && multiplicadorActual <= 7) floatingComboText.color = new Color(0f, 0.5f, 1f);
            else if (multiplicadorActual >= 8 && multiplicadorActual <= 10) floatingComboText.color = new Color(0.6f, 0f, 1f);
            else floatingComboText.color = Color.white;
        }
    }
}