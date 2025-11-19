using UnityEngine;

/// <summary>
/// ПРОСТАЯ трансформация - создаём визуальную модель как child
/// Синхронизируем позицию/поворот с родителем в каждом кадре
/// Родитель (паладин) - невидимый, но управляет движением
/// </summary>
public class SimpleTransformation : MonoBehaviour
{
    private GameObject transformedModel; // Визуальная модель (медведь)
    private SkinnedMeshRenderer playerRenderer; // Renderer паладина (для настоящих моделей)
    private MeshRenderer playerMeshRenderer; // Fallback для TestPlayer (капсула)
    private bool isTransformed = false;

    private Animator playerAnimator; // Аниматор паладина (источник)
    private Animator bearAnimator; // Аниматор медведя (получатель)

    /// <summary>
    /// Трансформироваться (показать модель трансформации, скрыть паладина)
    /// ОБНОВЛЕНО: Принимает аниматора паладина явно, чтобы избежать путаницы
    /// </summary>
    public bool TransformTo(GameObject transformationPrefab, Animator paladinAnimator = null)
    {
        if (transformationPrefab == null)
        {
            Debug.LogError("[SimpleTransformation] ❌ Префаб трансформации == null!");
            return false;
        }

        if (isTransformed)
        {
            Debug.LogWarning("[SimpleTransformation] ⚠️ Уже трансформирован!");
            return false;
        }

        // Находим renderer паладина (пробуем SkinnedMeshRenderer, потом MeshRenderer)
        playerRenderer = GetComponentInChildren<SkinnedMeshRenderer>();
        if (playerRenderer == null)
        {
            Debug.LogWarning("[SimpleTransformation] ⚠️ SkinnedMeshRenderer не найден, пробуем MeshRenderer (TestPlayer mode)");

            // Fallback для TestPlayer - ищем обычный MeshRenderer
            playerMeshRenderer = GetComponentInChildren<MeshRenderer>();
            if (playerMeshRenderer == null)
            {
                Debug.LogError("[SimpleTransformation] ❌ Ни SkinnedMeshRenderer, ни MeshRenderer не найдены!");
                return false;
            }

            Debug.Log($"[SimpleTransformation] ✅ Использую MeshRenderer: {playerMeshRenderer.gameObject.name}");
        }
        else
        {
            Debug.Log($"[SimpleTransformation] ✅ Использую SkinnedMeshRenderer: {playerRenderer.gameObject.name}");
        }

        // НАХОДИМ АНИМАТОР ПАЛАДИНА ДО создания медведя!
        // ВАЖНО: Если передан аниматор явно (от NetworkPlayer) - используем его!
        // Иначе ищем через GetComponentInChildren (для локального игрока)
        if (paladinAnimator != null)
        {
            playerAnimator = paladinAnimator;
            Debug.Log($"[SimpleTransformation] ✅ Используем переданный аниматор паладина: {playerAnimator.gameObject.name}");
        }
        else
        {
            playerAnimator = GetComponentInChildren<Animator>();
            if (playerAnimator == null)
            {
                Debug.LogWarning($"[SimpleTransformation] ⚠️ Аниматор паладина не найден на {gameObject.name}!");
            }
            else
            {
                Debug.Log($"[SimpleTransformation] ✅ Аниматор паладина найден через GetComponentInChildren: {playerAnimator.gameObject.name}");
            }
        }

        // СКРЫВАЕМ ВСЕ рендереры паладина И ОРУЖИЯ (делаем полностью невидимым)
        SkinnedMeshRenderer[] allSkinnedRenderers = GetComponentsInChildren<SkinnedMeshRenderer>();
        foreach (SkinnedMeshRenderer renderer in allSkinnedRenderers)
        {
            renderer.enabled = false;
        }
        Debug.Log($"[SimpleTransformation] 👻 Паладин скрыт (отключено {allSkinnedRenderers.Length} SkinnedMeshRenderer)");

        // КРИТИЧЕСКИ ВАЖНО: Скрываем ВСЕ MeshRenderer (включая оружие!)
        MeshRenderer[] allMeshRenderers = GetComponentsInChildren<MeshRenderer>();
        int hiddenWeaponRenderers = 0;
        foreach (MeshRenderer renderer in allMeshRenderers)
        {
            // Логируем что скрываем
            if (renderer.gameObject.name.Contains("Sword") || renderer.gameObject.name.Contains("Weapon") || renderer.gameObject.name.Contains("Paladin"))
            {
                Debug.Log($"[SimpleTransformation] ⚔️ Скрываю оружие: {renderer.gameObject.name}");
                hiddenWeaponRenderers++;
            }

            renderer.enabled = false;
        }
        Debug.Log($"[SimpleTransformation] 👻 Отключено {allMeshRenderers.Length} MeshRenderer (из них оружие: {hiddenWeaponRenderers})");

        // СОЗДАЁМ визуальную модель трансформации как child
        transformedModel = Instantiate(transformationPrefab, transform);
        transformedModel.name = "TransformedModel_Visual";

        // Позиционируем на месте родителя
        transformedModel.transform.localPosition = Vector3.zero;
        transformedModel.transform.localRotation = Quaternion.identity;
        transformedModel.transform.localScale = Vector3.one;

        // ОТКЛЮЧАЕМ все коллайдеры модели трансформации
        // Физика управляется CharacterController на родителе!
        Collider[] colliders = transformedModel.GetComponentsInChildren<Collider>();
        foreach (Collider col in colliders)
        {
            col.enabled = false;
        }
        Debug.Log($"[SimpleTransformation] 🔧 Отключено коллайдеров: {colliders.Length}");

        // ОТКЛЮЧАЕМ Rigidbody если есть
        Rigidbody[] rigidbodies = transformedModel.GetComponentsInChildren<Rigidbody>();
        foreach (Rigidbody rb in rigidbodies)
        {
            rb.isKinematic = true;
        }

        // НАХОДИМ АНИМАТОР МЕДВЕДЯ
        // КРИТИЧЕСКИ ВАЖНО: Используем GetComponentInChildren потому что Animator находится на дочернем объекте!
        bearAnimator = transformedModel.GetComponentInChildren<Animator>();

        if (bearAnimator == null)
        {
            Debug.LogWarning($"[SimpleTransformation] ⚠️ Аниматор медведя не найден на {transformedModel.name}!");
        }
        else
        {
            Debug.Log($"[SimpleTransformation] ✅ Аниматор медведя найден: {bearAnimator.gameObject.name}");

            // КРИТИЧЕСКИ ВАЖНО: Медведь должен использовать ТОТ ЖЕ Animator Controller что и паладин!
            // Иначе параметры (IsMoving, MoveY) не будут синхронизироваться!
            if (playerAnimator != null && playerAnimator.runtimeAnimatorController != null)
            {
                // Проверяем что контроллеры разные
                if (bearAnimator.runtimeAnimatorController != playerAnimator.runtimeAnimatorController)
                {
                    Debug.LogWarning($"[SimpleTransformation] ⚠️ У медведя другой AnimatorController!");
                    Debug.LogWarning($"[SimpleTransformation] 🔧 Паладин: {playerAnimator.runtimeAnimatorController.name}");
                    Debug.LogWarning($"[SimpleTransformation] 🔧 Медведь (старый): {bearAnimator.runtimeAnimatorController.name}");

                    // ИСПРАВЛЯЕМ: Заменяем контроллер медведя на контроллер паладина
                    bearAnimator.runtimeAnimatorController = playerAnimator.runtimeAnimatorController;

                    Debug.Log($"[SimpleTransformation] ✅ Контроллер медведя заменён на контроллер паладина: {bearAnimator.runtimeAnimatorController.name}");
                }
                else
                {
                    Debug.Log($"[SimpleTransformation] ✅ Контроллеры совпадают: {bearAnimator.runtimeAnimatorController.name}");
                }
            }
        }

        // ПРИКРЕПЛЯЕМ ОРУЖИЕ к медведю
        AttachWeaponToBear();

        isTransformed = true;

        Debug.Log($"[SimpleTransformation] ✅ Трансформация завершена! Модель: {transformationPrefab.name}");
        return true;
    }

    /// <summary>
    /// Прикрепить оружие паладина к медведю
    /// </summary>
    private void AttachWeaponToBear()
    {
        Debug.Log("[SimpleTransformation] 🔍 НАЧИНАЕМ ПОИСК ОРУЖИЯ...");

        // Находим правую руку медведя
        Transform bearRightHand = FindBoneRecursive(transformedModel.transform, "mixamorig:RightHand");

        if (bearRightHand == null)
        {
            Debug.LogWarning("[SimpleTransformation] ⚠️ Правая рука медведя НЕ НАЙДЕНА!");

            // Логируем структуру медведя
            Debug.Log("[SimpleTransformation] 🔍 Структура медведя:");
            LogChildrenRecursive(transformedModel.transform, 0);
            return;
        }

        Debug.Log($"[SimpleTransformation] ✅ Правая рука медведя найдена: {bearRightHand.name}");

        // Находим правую руку паладина
        Transform paladinRightHand = FindBoneRecursive(transform, "mixamorig:RightHand");
        if (paladinRightHand == null)
        {
            Debug.LogWarning("[SimpleTransformation] ⚠️ Правая рука паладина НЕ НАЙДЕНА!");

            // Логируем структуру паладина
            Debug.Log("[SimpleTransformation] 🔍 Структура паладина:");
            LogChildrenRecursive(transform, 0);
            return;
        }

        Debug.Log($"[SimpleTransformation] ✅ Правая рука паладина найдена: {paladinRightHand.name}");
        Debug.Log($"[SimpleTransformation] 🔍 Количество детей в руке паладина: {paladinRightHand.childCount}");

        // Логируем всех детей в руке паладина
        for (int i = 0; i < paladinRightHand.childCount; i++)
        {
            Transform child = paladinRightHand.GetChild(i);
            Debug.Log($"[SimpleTransformation] 🔍 Child {i}: {child.name} (активен: {child.gameObject.activeSelf})");
        }

        // Ищем АКТИВНОЕ оружие в руке паладина (игнорируем неактивные дубликаты)
        GameObject weapon = null;
        foreach (Transform child in paladinRightHand)
        {
            Debug.Log($"[SimpleTransformation] 🔍 Проверяем child: {child.name} (активен: {child.gameObject.activeSelf})");

            // Оружие обычно имеет название типа "SwordPaladin" или "WeaponName"
            // ВАЖНО: Берем только АКТИВНОЕ оружие (игнорируем неактивные дубликаты)
            if ((child.name.Contains("Sword") || child.name.Contains("Weapon") || child.name.Contains("Paladin"))
                && child.gameObject.activeSelf)
            {
                weapon = child.gameObject;
                Debug.Log($"[SimpleTransformation] ✅ ОРУЖИЕ НАЙДЕНО ПО ИМЕНИ: {child.name}");
                break;
            }
        }

        if (weapon == null && paladinRightHand.childCount > 0)
        {
            // Берём первый child если не нашли по имени
            weapon = paladinRightHand.GetChild(0).gameObject;
            Debug.Log($"[SimpleTransformation] ⚠️ Взяли первый child как оружие: {weapon.name}");
        }

        if (weapon == null)
        {
            Debug.LogWarning("[SimpleTransformation] ❌ ОРУЖИЕ НЕ НАЙДЕНО в руке паладина!");
            return;
        }

        Debug.Log($"[SimpleTransformation] ⚔️ Переносим оружие {weapon.name} на руку медведя...");

        // СОХРАНЯЕМ оригинальные локальные трансформы оружия (из WeaponAttachment)
        Vector3 originalLocalPos = weapon.transform.localPosition;
        Quaternion originalLocalRot = weapon.transform.localRotation;
        Vector3 originalLocalScale = weapon.transform.localScale;

        Debug.Log($"[SimpleTransformation] 📍 Оригинальная локальная позиция: {originalLocalPos}");
        Debug.Log($"[SimpleTransformation] 📍 Оригинальный локальный поворот: {originalLocalRot.eulerAngles}");

        // Перемещаем оружие на руку медведя
        weapon.transform.SetParent(bearRightHand, false); // false = используем локальные трансформы

        // ВОССТАНАВЛИВАЕМ оригинальные локальные трансформы (они были настроены в WeaponAttachment)
        weapon.transform.localPosition = originalLocalPos;
        weapon.transform.localRotation = originalLocalRot;
        weapon.transform.localScale = originalLocalScale;

        // КРИТИЧЕСКИ ВАЖНО: СКРЫВАЕМ оружие на медведе (медведь не должен держать меч!)
        MeshRenderer[] weaponRenderers = weapon.GetComponentsInChildren<MeshRenderer>();
        foreach (MeshRenderer renderer in weaponRenderers)
        {
            renderer.enabled = false;
            Debug.Log($"[SimpleTransformation] 🙈 Скрыт renderer оружия на медведе: {renderer.gameObject.name}");
        }

        SkinnedMeshRenderer[] weaponSkinnedRenderers = weapon.GetComponentsInChildren<SkinnedMeshRenderer>();
        foreach (SkinnedMeshRenderer renderer in weaponSkinnedRenderers)
        {
            renderer.enabled = false;
            Debug.Log($"[SimpleTransformation] 🙈 Скрыт SkinnedMeshRenderer оружия на медведе: {renderer.gameObject.name}");
        }

        Debug.Log($"[SimpleTransformation] ✅ Оружие {weapon.name} прикреплено к медведю (СКРЫТО, медведь не держит меч)");
        Debug.Log($"[SimpleTransformation] 📍 Итоговая локальная позиция: {weapon.transform.localPosition}");
        Debug.Log($"[SimpleTransformation] 📍 Итоговый локальный поворот: {weapon.transform.localEulerAngles}");
    }

    /// <summary>
    /// Логирование структуры объекта (для дебага)
    /// </summary>
    private void LogChildrenRecursive(Transform parent, int depth)
    {
        string indent = new string(' ', depth * 2);
        Debug.Log($"{indent}- {parent.name}");

        if (depth < 3) // Ограничиваем глубину
        {
            foreach (Transform child in parent)
            {
                LogChildrenRecursive(child, depth + 1);
            }
        }
    }

    /// <summary>
    /// Рекурсивный поиск кости по имени
    /// </summary>
    private Transform FindBoneRecursive(Transform parent, string boneName)
    {
        if (parent.name == boneName)
        {
            return parent;
        }

        foreach (Transform child in parent)
        {
            Transform found = FindBoneRecursive(child, boneName);
            if (found != null)
            {
                return found;
            }
        }

        return null;
    }

    /// <summary>
    /// Вернуться к оригинальной модели
    /// </summary>
    public void RevertToOriginal()
    {
        if (!isTransformed)
        {
            Debug.LogWarning("[SimpleTransformation] ⚠️ Не трансформирован, нечего возвращать");
            return;
        }

        // УДАЛЯЕМ модель трансформации
        if (transformedModel != null)
        {
            Destroy(transformedModel);
            Debug.Log("[SimpleTransformation] 🗑️ Модель трансформации удалена");
        }

        // ПОКАЗЫВАЕМ ВСЕ рендереры паладина И ОРУЖИЯ (восстанавливаем видимость)
        SkinnedMeshRenderer[] allSkinnedRenderers = GetComponentsInChildren<SkinnedMeshRenderer>(true); // true = включая неактивные
        foreach (SkinnedMeshRenderer renderer in allSkinnedRenderers)
        {
            renderer.enabled = true;
        }
        Debug.Log($"[SimpleTransformation] ✅ Паладин восстановлен (включено {allSkinnedRenderers.Length} SkinnedMeshRenderer)");

        // КРИТИЧЕСКИ ВАЖНО: Восстанавливаем видимость ОРУЖИЯ
        MeshRenderer[] allMeshRenderers = GetComponentsInChildren<MeshRenderer>(true);
        int restoredWeaponRenderers = 0;
        foreach (MeshRenderer renderer in allMeshRenderers)
        {
            // Логируем что восстанавливаем
            if (renderer.gameObject.name.Contains("Sword") || renderer.gameObject.name.Contains("Weapon") || renderer.gameObject.name.Contains("Paladin"))
            {
                Debug.Log($"[SimpleTransformation] ⚔️ Восстанавливаю видимость оружия: {renderer.gameObject.name}");
                restoredWeaponRenderers++;
            }

            renderer.enabled = true;
        }
        Debug.Log($"[SimpleTransformation] ✅ Включено {allMeshRenderers.Length} MeshRenderer (из них оружие: {restoredWeaponRenderers})");

        transformedModel = null;
        isTransformed = false;

        Debug.Log("[SimpleTransformation] ✅ Возврат к оригинальной модели завершён");
    }

    /// <summary>
    /// Проверка состояния трансформации
    /// </summary>
    public bool IsTransformed()
    {
        return isTransformed;
    }

    /// <summary>
    /// Получить аниматор паладина (для NetworkPlayer)
    /// </summary>
    public Animator GetPlayerAnimator()
    {
        return playerAnimator;
    }

    /// <summary>
    /// Получить аниматор медведя (для дебага)
    /// </summary>
    public Animator GetBearAnimator()
    {
        return bearAnimator;
    }

    /// <summary>
    /// Установить триггер на аниматоре (для атаки)
    /// Если трансформирован - устанавливает на медведя
    /// Если нет - на паладина
    /// </summary>
    public void SetAnimatorTrigger(string triggerName)
    {
        if (isTransformed && bearAnimator != null)
        {
            bearAnimator.SetTrigger(triggerName);
            Debug.Log($"[SimpleTransformation] ⚡ Триггер '{triggerName}' установлен на медведя");
        }
        else if (playerAnimator != null)
        {
            playerAnimator.SetTrigger(triggerName);
            Debug.Log($"[SimpleTransformation] ⚡ Триггер '{triggerName}' установлен на паладина");
        }
    }

    /// <summary>
    /// Установить bool параметр на аниматоре
    /// </summary>
    public void SetAnimatorBool(string paramName, bool value)
    {
        if (isTransformed && bearAnimator != null)
        {
            bearAnimator.SetBool(paramName, value);
        }
        else if (playerAnimator != null)
        {
            playerAnimator.SetBool(paramName, value);
        }
    }

    /// <summary>
    /// Установить float параметр на аниматоре
    /// </summary>
    public void SetAnimatorFloat(string paramName, float value)
    {
        if (isTransformed && bearAnimator != null)
        {
            bearAnimator.SetFloat(paramName, value);
        }
        else if (playerAnimator != null)
        {
            playerAnimator.SetFloat(paramName, value);
        }
    }

    void LateUpdate()
    {
        // ДИАГНОСТИКА: Логируем каждые 60 кадров для проверки
        if (Time.frameCount % 60 == 0)
        {
            Debug.Log($"[SimpleTransformation] 🔍 LateUpdate: isTransformed={isTransformed}, transformedModel={(transformedModel != null ? "существует" : "NULL")}, gameObject={gameObject.name}");
        }

        // СИНХРОНИЗАЦИЯ: Модель трансформации всегда следует за родителем
        // Это гарантирует что визуальная модель не "отстаёт" от движения
        if (isTransformed && transformedModel != null)
        {
            transformedModel.transform.localPosition = Vector3.zero;
            transformedModel.transform.localRotation = Quaternion.identity;

            // СИНХРОНИЗАЦИЯ АНИМАТОРА: копируем параметры с паладина на медведя
            SyncAnimatorParameters();
        }
        else if (isTransformed && transformedModel == null)
        {
            // ОШИБКА: трансформирован, но медведь удалён!
            Debug.LogError($"[SimpleTransformation] ❌ isTransformed=true но transformedModel==NULL для {gameObject.name}!");
        }
        else if (!isTransformed)
        {
            // ДИАГНОСТИКА: Не трансформирован
            if (Time.frameCount % 60 == 0)
            {
                Debug.Log($"[SimpleTransformation] ⏸️ LateUpdate пропущен: isTransformed=false для {gameObject.name}");
            }
        }
    }

    /// <summary>
    /// Синхронизировать параметры аниматора паладина с медведем
    /// </summary>
    private void SyncAnimatorParameters()
    {
        if (playerAnimator == null)
        {
            Debug.LogError($"[SimpleTransformation] ❌ playerAnimator == NULL для {gameObject.name}!");
            return;
        }

        if (bearAnimator == null)
        {
            Debug.LogError($"[SimpleTransformation] ❌ bearAnimator == NULL для {gameObject.name}!");
            return;
        }

        // ДИАГНОСТИКА: Логируем каждые 60 кадров
        bool shouldLog = Time.frameCount % 60 == 0;

        if (shouldLog)
        {
            Debug.Log($"[SimpleTransformation] 🔄 Синхронизация аниматоров: {gameObject.name}");
            Debug.Log($"[SimpleTransformation] 📍 playerAnimator: {playerAnimator.gameObject.name} (enabled: {playerAnimator.enabled})");
            Debug.Log($"[SimpleTransformation] 📍 bearAnimator: {bearAnimator.gameObject.name} (enabled: {bearAnimator.enabled})");
        }

        // Копируем все параметры аниматора
        foreach (AnimatorControllerParameter param in playerAnimator.parameters)
        {
            try
            {
                switch (param.type)
                {
                    case AnimatorControllerParameterType.Float:
                        float floatValue = playerAnimator.GetFloat(param.name);
                        bearAnimator.SetFloat(param.name, floatValue);

                        if (shouldLog && (param.name == "MoveY" || param.name == "MoveX" || param.name == "Speed"))
                        {
                            Debug.Log($"[SimpleTransformation] 📊 {param.name}: {floatValue:F2}");
                        }
                        break;

                    case AnimatorControllerParameterType.Int:
                        int intValue = playerAnimator.GetInteger(param.name);
                        bearAnimator.SetInteger(param.name, intValue);
                        break;

                    case AnimatorControllerParameterType.Bool:
                        bool boolValue = playerAnimator.GetBool(param.name);
                        bearAnimator.SetBool(param.name, boolValue);

                        if (shouldLog && (param.name == "IsMoving" || param.name == "InBattle"))
                        {
                            Debug.Log($"[SimpleTransformation] 📊 {param.name}: {boolValue}");
                        }
                        break;

                    case AnimatorControllerParameterType.Trigger:
                        // Проверяем активен ли триггер у паладина
                        // Unity не даёт напрямую проверить триггер, но мы можем проверить через AnimatorStateInfo
                        // Для атаки просто копируем все триггеры которые были установлены
                        bool triggerActive = false;

                        // Проверяем текущее состояние аниматора паладина
                        AnimatorStateInfo currentState = playerAnimator.GetCurrentAnimatorStateInfo(0);

                        // Если это атака и параметр называется Attack
                        if (param.name == "Attack" && currentState.IsName("Attack"))
                        {
                            triggerActive = true;
                        }

                        if (triggerActive)
                        {
                            bearAnimator.SetTrigger(param.name);

                            if (shouldLog)
                            {
                                Debug.Log($"[SimpleTransformation] ⚡ Trigger скопирован: {param.name}");
                            }
                        }
                        break;
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[SimpleTransformation] ❌ Ошибка копирования параметра {param.name}: {e.Message}");
            }
        }
    }

    void OnDestroy()
    {
        // Очистка при удалении компонента
        if (transformedModel != null)
        {
            Destroy(transformedModel);
        }
    }
}
