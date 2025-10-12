using UnityEngine;

/// <summary>
/// Анимация подпрыгивания стрелки для индикатора цели
/// </summary>
public class TargetArrowAnimation : MonoBehaviour
{
    [Header("Animation Settings")]
    [Tooltip("Скорость подпрыгивания")]
    [SerializeField] private float bounceSpeed = 2f;

    [Tooltip("Амплитуда подпрыгивания")]
    [SerializeField] private float bounceAmount = 0.3f;

    private Vector3 startPosition;
    private float timer;

    void Start()
    {
        startPosition = transform.localPosition;
    }

    void Update()
    {
        timer += Time.deltaTime * bounceSpeed;
        float bounce = Mathf.Sin(timer) * bounceAmount;
        transform.localPosition = startPosition + Vector3.up * bounce;
    }
}
