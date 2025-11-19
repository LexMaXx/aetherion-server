using UnityEngine;

/// <summary>
/// Компонент для синхронизации анимаций каста скиллов в мультиплеере
/// Отправляет анимацию на сервер и воспроизводит анимации других игроков
/// </summary>
public class SkillCastAnimationSync : MonoBehaviour
{
    private Animator animator;
    private SocketIOManager socketIO;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        socketIO = SocketIOManager.Instance;
    }

    /// <summary>
    /// Воспроизвести анимацию каста и отправить на сервер
    /// </summary>
    public void PlayCastAnimation(string animationTrigger, float animationSpeed = 1f, float castTime = 0f)
    {
        if (animator == null) return;

        // Воспроизвести локально
        animator.SetFloat("AttackSpeed", animationSpeed);
        animator.SetTrigger(animationTrigger);

        Debug.Log($"[CastAnim] Воспроизведение анимации: {animationTrigger} @ {animationSpeed}x");

        // Отправить на сервер для других игроков
        if (socketIO != null && socketIO.IsConnected)
        {
            var data = new
            {
                animationTrigger = animationTrigger,
                animationSpeed = animationSpeed,
                castTime = castTime
            };

            string json = Newtonsoft.Json.JsonConvert.SerializeObject(data);
            socketIO.Emit("skill_cast_animation", json);

            Debug.Log($"[CastAnim] 🌐 Анимация отправлена на сервер");
        }
    }

    /// <summary>
    /// Воспроизвести анимацию от другого игрока (вызывается NetworkSyncManager)
    /// </summary>
    public void PlayRemoteCastAnimation(string animationTrigger, float animationSpeed)
    {
        if (animator == null) return;

        animator.SetFloat("AttackSpeed", animationSpeed);
        animator.SetTrigger(animationTrigger);

        Debug.Log($"[CastAnim] Воспроизведение удалённой анимации: {animationTrigger} @ {animationSpeed}x");
    }
}
