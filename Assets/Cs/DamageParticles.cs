using UnityEngine;

/// <summary>
/// Reproduce las partículas de daño cada vez que el coche recibe daño real
/// (después de pasar la invulnerabilidad).
/// USO: poner sobre el GameObject que tiene el ParticleSystem (hijo del Player),
/// o sobre el Player y arrastrar los sistemas en el campo "sistemas".
/// Si el campo queda vacío, se buscan los ParticleSystem en este objeto y sus hijos.
/// </summary>
public class DamageParticles : MonoBehaviour
{
    [Tooltip("Sistemas de partículas a reproducir al recibir daño. Vacío = buscarlos en este objeto y sus hijos.")]
    public ParticleSystem[] sistemas;

    private PlayerHealth salud;

    void OnEnable()
    {
        if (sistemas == null || sistemas.Length == 0)
            sistemas = GetComponentsInChildren<ParticleSystem>(true);

        salud = GetComponentInParent<PlayerHealth>();
        if (salud == null)
            salud = FindObjectOfType<PlayerHealth>();

        if (salud != null)
            salud.OnDamageReceived += ReproducirDanio;
    }

    void OnDisable()
    {
        if (salud != null)
            salud.OnDamageReceived -= ReproducirDanio;
    }

    private void ReproducirDanio(int cantidad)
    {
        if (sistemas == null) return;

        foreach (ParticleSystem ps in sistemas)
        {
            if (ps == null) continue;

            // Reinicia el sistema desde cero para que el golpe se note de nuevo.
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            ps.Play();
        }
    }
}
