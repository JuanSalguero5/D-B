using UnityEngine;

/// <summary>
/// Activa partículas (por ejemplo de derrape) cuando el coche gira.
/// Si mantienes el giro durante mas de tiempoMinimoGiro (0.2s), las partículas
/// se activan; al soltar el giro se detienen.
/// USO: poner sobre el GameObject que tiene los ParticleSystem (hijo del Player)
/// o sobre el Player. Si "sistemas" queda vacío, se buscan en este objeto y sus hijos.
/// </summary>
public class TurnParticles : MonoBehaviour
{
    [Header("Partículas de Giro")]
    [Tooltip("Sistemas de partículas para el giro. Vacío = buscarlos en este objeto y sus hijos.")]
    public ParticleSystem[] sistemas;

    [Tooltip("Segundos manteniendo el giro antes de activar las partículas.")]
    public float tiempoMinimoGiro = 0.2f;

    [Tooltip("Mínimo input lateral (|x|) para considerar que se está girando.")]
    public float umbralGiro = 0.05f;

    private SimpleFluidDrive drive;
    private float tiempoGirando = 0f;
    private bool activas = false;

    void OnEnable()
    {
        if (sistemas == null || sistemas.Length == 0)
            sistemas = GetComponentsInChildren<ParticleSystem>(true);

        drive = GetComponentInParent<SimpleFluidDrive>();
    }

    void OnDisable()
    {
        if (activas)
            Desactivar();
    }

    void Update()
    {
        if (drive == null)
            return;

        if (GameManager.Instance == null || GameManager.Instance.currentState != GameManager.GameState.Playing)
        {
            tiempoGirando = 0f;
            if (activas) Desactivar();
            return;
        }

        bool estaGirando = Mathf.Abs(drive.HorizontalInput) > umbralGiro;

        if (estaGirando)
        {
            tiempoGirando += Time.deltaTime;

            if (tiempoGirando >= tiempoMinimoGiro && !activas)
                Activar();
        }
        else
        {
            tiempoGirando = 0f;
            if (activas) Desactivar();
        }
    }

    private void Activar()
    {
        activas = true;
        foreach (ParticleSystem ps in sistemas)
        {
            if (ps != null)
                ps.Play();
        }
    }

    private void Desactivar()
    {
        activas = false;
        foreach (ParticleSystem ps in sistemas)
        {
            if (ps != null)
                ps.Stop();
        }
    }
}
