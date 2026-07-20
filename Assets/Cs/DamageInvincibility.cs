using UnityEngine;
using System.Collections.Generic; // Necesario para la lista

public class DamageInvincibility : MonoBehaviour
{
    [Header("Configuración Dither")]
    public float invincibilityDuration = 2.0f;
    public float ditherSpeed = 20f;

    // Usaremos una lista para guardar todos los materiales que necesitan el efecto
    [SerializeField]private List<Material> materialesEfecto = new List<Material>();
    private bool isInvulnerable = false;
    private float invincibilityTimer = 0f;

    void Start()
    {
        // Buscamos TODOS los MeshRenderers en este objeto y sus hijos
        Renderer[] renderers = GetComponentsInChildren<Renderer>();

        foreach (Renderer r in renderers)
        {
            // Solo guardamos el material si tiene nuestro parámetro del Shader
            if (r.material.HasProperty("_AlphaThreshold"))
            {
                materialesEfecto.Add(r.material);
            }
        }
    }

    void Update()
    {
        if (isInvulnerable)
        {
            invincibilityTimer -= Time.deltaTime;
            float ditherValue = Mathf.Abs(Mathf.Sin(Time.time * ditherSpeed)) * 0.9f;

            // Aplicamos el efecto a TODA la lista de materiales encontrados
            foreach (Material mat in materialesEfecto)
            {
                mat.SetFloat("_AlphaThreshold", ditherValue);
            }

            if (invincibilityTimer <= 0)
                TerminarInvulnerabilidad();
        }
    }

    public void ActivarInvulnerabilidad()
    {
        isInvulnerable = true;
        invincibilityTimer = invincibilityDuration;
    }

    private void TerminarInvulnerabilidad()
    {
        isInvulnerable = false;
        foreach (Material mat in materialesEfecto)
        {
            mat.SetFloat("_AlphaThreshold", 0f);
        }
    }

    public bool IsInvulnerable() => isInvulnerable;
}