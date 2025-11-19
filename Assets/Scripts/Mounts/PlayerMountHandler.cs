using System.Collections.Generic;
using StarterAssets;
using UnityEngine;

/// <summary>
/// Позволяет игроку садиться на дракона по нажатию клавиши и передаёт управление маунту.
/// </summary>
[RequireComponent(typeof(StarterAssetsInputs))]
public class PlayerMountHandler : MonoBehaviour
{
    [Header("Mount")]
    [SerializeField] private DragonMountController dragonMount;
    [SerializeField] private Transform dragonTransformOverride;
    [SerializeField] private bool autoFindMount = true;
    [SerializeField] private string autoFindMountName = "";
    [SerializeField] private float maxMountDistance = 8f;

    [Header("Spawn")]
    [SerializeField] private bool spawnMountIfMissing = true;
    [SerializeField] private GameObject dragonPrefab;
    [SerializeField] private string dragonPrefabResourcePath = "Mount/Dragon";
    [SerializeField] private Vector3 spawnOffset = new Vector3(0f, 0f, 3f);

    [Header("Input")]
    [SerializeField] private KeyCode toggleMountKey = KeyCode.G;

    [Header("Dismount")]
    [SerializeField] private float dismountHeightOffset = 1.2f;

    private StarterAssetsInputs starterInputs;
    private CharacterController characterController;
    private readonly List<MonoBehaviour> movementControllers = new();
    private readonly List<MonoBehaviour> controllersToRestore = new();
    private bool isMounted;
    private Vector3 originalScale;
    private GameObject spawnedDragonInstance;
    private bool currentMountSpawnedByHandler;
    [Header("Mount Attachments")]
    [SerializeField] private Transform riderAttachTransform;

    private void Awake()
    {
        starterInputs = GetComponent<StarterAssetsInputs>();
        characterController = GetComponent<CharacterController>();
        if (characterController == null)
        {
            characterController = GetComponentInParent<CharacterController>();
        }
        if (characterController == null)
        {
            characterController = GetComponentInChildren<CharacterController>();
        }

        if (riderAttachTransform == null && characterController != null)
        {
            riderAttachTransform = characterController.transform;
        }
        originalScale = transform.localScale;

        CacheMovementController<PlayerController>();
        CacheMovementController<AetherionPlayerController>();
        CacheMovementController<EasyStartPlayerController>();
        CacheMovementController<SimplePlayerController>();
        CacheMovementController<MixamoPlayerController>();
    }

    private void Start()
    {
        EnsureDragonMountReference();
        if (MobileInputManager.Instance != null)
        {
            MobileInputManager.Instance.SetMountHandler(this);
        }
    }

    private void OnDestroy()
    {
        if (MobileInputManager.Instance != null)
        {
            MobileInputManager.Instance.ClearMountHandler(this);
        }
    }

    private void Update()
    {
        if (dragonMount == null && autoFindMount)
        {
            EnsureDragonMountReference();
        }

        if (Input.GetKeyDown(toggleMountKey))
        {
            if (isMounted)
            {
                Dismount();
            }
            else
            {
                TryMount();
            }
        }
    }

    /// <summary>
    /// Вызвать переключение маунта извне (например, с мобильной кнопки Mount).
    /// </summary>
    public void ToggleMount()
    {
        if (isMounted)
        {
            Dismount();
        }
        else
        {
            TryMount();
        }
    }

    private void TryMount()
    {
        DragonMountController currentMount = GetOrSpawnMount();
        if (currentMount == null || currentMount.IsOccupied)
            return;

        Vector3 playerPosition = GetPlayerWorldPosition();
        float distance = Vector3.Distance(playerPosition, currentMount.SeatPoint.position);
        if (distance > maxMountDistance)
        {
            // Слишком далеко от найденного маунта — пробуем призвать собственного
            DragonMountController spawnedMount = spawnMountIfMissing ? SpawnDragonMount() : null;
            if (spawnedMount == null)
            {
                // Не удалось заспавнить - не садимся на удалённого дракона
                return;
            }

            currentMount = spawnedMount;
            playerPosition = GetPlayerWorldPosition();
            distance = Vector3.Distance(playerPosition, currentMount.SeatPoint.position);
            if (distance > maxMountDistance)
            {
                // Даже после спавна слишком далеко (неожиданно) - отменяем
                Destroy(spawnedMount.gameObject);
                spawnedDragonInstance = null;
                dragonMount = null;
                return;
            }
        }

        DisablePlayerControllers();

        bool mounted = currentMount.TryMount(this, starterInputs);
        if (!mounted)
        {
            if (spawnedDragonInstance != null && currentMount.gameObject == spawnedDragonInstance)
            {
                Destroy(spawnedDragonInstance);
                spawnedDragonInstance = null;
            }

            RestorePlayerControllers();
            return;
        }

        currentMountSpawnedByHandler = spawnedDragonInstance != null && currentMount.gameObject == spawnedDragonInstance;
        dragonMount = currentMount;
        isMounted = true;
    }

    private void Dismount()
    {
        if (!isMounted || dragonMount == null)
            return;

        Vector3 exitPoint = dragonMount.GetSafeDismountPosition() + Vector3.up * dismountHeightOffset;
        dragonMount.ForceDismount(this);

        transform.SetParent(null, true);
        transform.localScale = originalScale;

        bool controllerTemporarilyDisabled = false;
        if (characterController != null && characterController.enabled)
        {
            characterController.enabled = false;
            controllerTemporarilyDisabled = true;
        }

        transform.position = exitPoint;

        if (controllerTemporarilyDisabled && characterController != null)
        {
            characterController.enabled = true;
        }

        RestorePlayerControllers();
        CleanupMountAfterDismount();
        isMounted = false;
    }

    private void DisablePlayerControllers()
    {
        controllersToRestore.Clear();
        foreach (MonoBehaviour controller in movementControllers)
        {
            if (controller != null && controller.enabled)
            {
                controller.enabled = false;
                controllersToRestore.Add(controller);
            }
        }

    }

    private void RestorePlayerControllers()
    {
        foreach (MonoBehaviour controller in controllersToRestore)
        {
            if (controller != null)
            {
                controller.enabled = true;
            }
        }
        controllersToRestore.Clear();
    }

    private void CacheMovementController<T>() where T : MonoBehaviour
    {
        T controller = GetComponent<T>();
        if (controller == null)
        {
            controller = GetComponentInChildren<T>(true);
        }
        if (controller == null)
        {
            controller = GetComponentInParent<T>();
        }

        if (controller != null && !movementControllers.Contains(controller))
        {
            movementControllers.Add(controller);
        }
    }

    private bool EnsureDragonMountReference()
    {
        if (dragonMount != null)
            return true;

        if (spawnedDragonInstance != null)
        {
            dragonMount = GetOrCreateMountComponent(spawnedDragonInstance);
            if (dragonMount != null)
                return true;
        }

        if (dragonTransformOverride != null)
        {
            dragonMount = GetOrCreateMountComponent(dragonTransformOverride.gameObject);
            if (dragonMount != null)
                return true;
        }

        if (!string.IsNullOrEmpty(autoFindMountName))
        {
            GameObject named = GameObject.Find(autoFindMountName);
            if (named != null)
            {
                dragonMount = GetOrCreateMountComponent(named);
                if (dragonMount != null)
                    return true;
            }
        }

        if (autoFindMount)
        {
            dragonMount = FindObjectOfType<DragonMountController>();
            if (dragonMount != null)
                return true;
        }

        return false;
    }

    private DragonMountController GetOrCreateMountComponent(GameObject target)
    {
        if (target == null)
            return null;

        DragonMountController controller = target.GetComponent<DragonMountController>();
        if (controller == null)
        {
            controller = target.AddComponent<DragonMountController>();
        }

        return controller;
    }

    private DragonMountController GetOrSpawnMount()
    {
        if (EnsureDragonMountReference())
            return dragonMount;

        if (!spawnMountIfMissing)
            return null;

        return SpawnDragonMount();
    }

    private DragonMountController SpawnDragonMount()
    {
        GameObject prefab = dragonPrefab;
        if (prefab == null && !string.IsNullOrEmpty(dragonPrefabResourcePath))
        {
            prefab = Resources.Load<GameObject>(dragonPrefabResourcePath);
        }

        if (prefab == null)
        {
            Debug.LogError("[PlayerMountHandler] Невозможно заспавнить дракона: не задан prefab или Resources путь.");
            return null;
        }

        Transform referenceTransform = GetPlayerReferenceTransform();
        Vector3 spawnPosition = referenceTransform.TransformPoint(spawnOffset);
        Vector3 forward = GetPlayerForward();
        Quaternion spawnRotation = Quaternion.LookRotation(forward.sqrMagnitude > 0.01f ? forward : Vector3.forward, Vector3.up);

        GameObject instance = Instantiate(prefab, spawnPosition, spawnRotation);
        spawnedDragonInstance = instance;
        dragonTransformOverride = instance.transform;

        DragonMountController controller = GetOrCreateMountComponent(instance);
        if (controller == null)
        {
            Debug.LogError("[PlayerMountHandler] Заспавненный дракон не получил DragonMountController.");
        }

        return controller;
    }

    private void CleanupMountAfterDismount()
    {
        if (currentMountSpawnedByHandler && dragonMount != null)
        {
            Destroy(dragonMount.gameObject);
        }

        spawnedDragonInstance = null;
        dragonMount = null;
        dragonTransformOverride = null;
        currentMountSpawnedByHandler = false;
    }

    private Transform GetPlayerReferenceTransform()
    {
        if (characterController != null)
        {
            return characterController.transform;
        }

        return transform;
    }

    private Vector3 GetPlayerWorldPosition()
    {
        return GetPlayerReferenceTransform().position;
    }

    private Vector3 GetPlayerForward()
    {
        Transform reference = GetPlayerReferenceTransform();
        Vector3 forward = reference.forward;
        if (forward.sqrMagnitude < 0.0001f)
        {
            forward = reference.up != Vector3.zero ? reference.up : Vector3.forward;
        }

        return forward.normalized;
    }

    public Transform GetMountAttachTransform()
    {
        return riderAttachTransform != null ? riderAttachTransform : transform;
    }
}
