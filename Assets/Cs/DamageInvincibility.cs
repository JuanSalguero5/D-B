using UnityEngine;
using System.Collections.Generic; // Necesario para la lista

public class DamageInvincibility : MonoBehaviour
{
    [Header("Configuraci�n Dither")]
    public float invincibilityDuration = 2.0f;
    public float ditherSpeed = 20f;

    // Usaremos una lista para guardar todos los materiales que necesitan el efecto
    [SerializeField]private List<Material> materialesEfecto = new List<Material>();
    private bool isInvulnerable = false;
    private float invincibilityTimer = 0f;

    void Start()
    {
        // IMPORTANTE: limpiamos la lista serializada. Si quedaron referencias a los
        // ASSETS .mat (tipo 2), SetFloat() escribiria el dither sobre el material
        // compartido en disco y la siguiente partida arrancaria con las ruedas "quemadas".
        // Aqui solo usamos instancias de runtime: cada renderer obtiene la suya con
        // r.material y no toca el asset.
        materialesEfecto.Clear();

        // Buscamos TODOS los MeshRenderers en este objeto y sus hijos
        Renderer[] renderers = GetComponentsInChildren<Renderer>();

        foreach (Renderer r in renderers)
        {
            // Solo guardamos el material si tiene nuestro par�metro del Shader
            if (r.material.HasProperty("_AlphaThreshold"))
            {
                materialesEfecto.Add(r.material);
            }
        }

        // Reset expl�cito al iniciar cada partida: si un .mat quedo "quemado" con el
        // valor del dither de una sesi�n anterior, lo limpiamos de una vez.
        foreach (Material mat in materialesEfecto)
        {
            mat.SetFloat("_AlphaThreshold", 0f);
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