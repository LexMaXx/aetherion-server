using UnityEngine;

/// <summary>
/// Автоматически уничтожает GameObject после завершения Particle System
/// </summary>
public class AutoDestroyParticleSystem : MonoBehaviour
{
    private ParticleSystem ps;
    private float lifetime;

    void Start()
    {
        ps = GetComponent<ParticleSystem>();
        if (ps != null)
        {
            lifetime = ps.main.duration + ps.main.startLifetime.constantMax;
            Destroy(gameObject, lifetime);
        }
        else
        {
            // Если нет ParticleSystem, удаляем через 2 секунды
            Destroy(gameObject, 2f);
        }
    }
}
