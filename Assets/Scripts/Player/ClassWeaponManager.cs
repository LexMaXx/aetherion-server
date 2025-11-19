using UnityEngine;

/// <summary>
/// Управляет оружием для разных классов персонажей
/// Автоматически назначает правильное оружие каждому классу
/// Поддерживает редактирование позиции оружия в реальном времени
/// </summary>
public class ClassWeaponManager : MonoBehaviour
{
    [System.Serializable]
    public class WeaponConfig
    {
        [Tooltip("Префаб или модель оружия")]
        public GameObject weaponModel;

        [Tooltip("Локальная позиция оружия в руке")]
        public Vector3 localPosition = Vector3.zero;

        [Tooltip("Локальный поворот оружия (Euler углы)")]
        public Vector3 localRotation = Vector3.zero;

        [Tooltip("Локальный масштаб оружия")]
        public Vector3 localScale = Vector3.one;
    }

    [Header("Weapon Configurations (Optional - Auto-loads from WeaponDatabase)")]
    [SerializeField] private WeaponConfig warriorRightHand;  // Меч
    [SerializeField] private WeaponConfig warriorLeftHand;   // Щит
    [SerializeField] private WeaponConfig mageWeapon;
    [SerializeField] private WeaponConfig archerLeftHand;    // Лук (левая рука)
    [SerializeField] private WeaponConfig archerBack;        // Колчан со стрелами (спина)
    [SerializeField] private WeaponConfig rogueWeapon;
    [SerializeField] private WeaponConfig paladinWeapon;

    [Header("Auto-Load Settings")]
    [Tooltip("Автоматически загружать оружие из WeaponDatabase если не настроено вручную")]
    [SerializeField] private bool autoLoadFromDatabase = true;

    [Header("Settings")]
    [SerializeField] private string rightHandBoneName = "mixamorig:RightHand";
    [SerializeField] private string leftHandBoneName = "mixamorig:LeftHand";
    [SerializeField] private string spineBoneName = "mixamorig:Spine2";  // Для колчана на спине

    private GameObject attachedRightWeapon;
    private GameObject attachedLeftWeapon;
    private GameObject attachedBackWeapon;  // Для колчана
    private Transform rightHandBone;
    private Transform leftHandBone;
    private Transform spineBone;

    // КЕШИРУЕМ класс персонажа чтобы не определять его каждый кадр
    private CharacterClass cachedCharacterClass;
    private bool classInitialized = false;
    private bool weaponsAttached = false;

    /// <summary>
    /// Определить класс персонажа по имени GameObject
    /// </summary>
    public CharacterClass GetCharacterClass()
    {
        // Если класс уже определён - возвращаем кеш
        if (classInitialized)
        {
            return cachedCharacterClass;
        }

        string name = gameObject.name.ToLower();
        Debug.Log($"[ClassWeaponManager] Определяю класс по имени: '{gameObject.name}' (lowercase: '{name}')");

        if (name.Contains("warrior")) cachedCharacterClass = CharacterClass.Warrior;
        else if (name.Contains("mage")) cachedCharacterClass = CharacterClass.Mage;
        else if (name.Contains("archer")) cachedCharacterClass = CharacterClass.Archer;
        else if (name.Contains("rogue")) cachedCharacterClass = CharacterClass.Rogue;
        else if (name.Contains("paladin")) cachedCharacterClass = CharacterClass.Paladin;
        else
        {
            // Если не определён - возвращаем Warrior по умолчанию (логируем только один раз)
            Debug.LogWarning($"[ClassWeaponManager] ❌ Не удалось определить класс для '{gameObject.name}', использую Warrior");
            cachedCharacterClass = CharacterClass.Warrior;
        }

        classInitialized = true;
        Debug.Log($"[ClassWeaponManager] ✅ Класс определён: {cachedCharacterClass}");
        return cachedCharacterClass;
    }

    /// <summary>
    /// Установить класс персонажа вручную (для сетевых игроков)
    /// </summary>
    public void SetCharacterClass(CharacterClass characterClass)
    {
        // КРИТИЧЕСКОЕ: Если класс изменился - удаляем старое оружие!
        if (classInitialized && cachedCharacterClass != characterClass)
        {
            Debug.Log($"[ClassWeaponManager] ⚠️ Класс изменился: {cachedCharacterClass} → {characterClass}, удаляем старое оружие");
            ClearAllWeapons();
        }

        cachedCharacterClass = characterClass;
        classInitialized = true;
        Debug.Log($"[ClassWeaponManager] Класс установлен вручную: {characterClass}");
    }

    /// <summary>
    /// Удалить все прикрепленное оружие
    /// </summary>
    public void ClearAllWeapons()
    {
        if (attachedRightWeapon != null)
        {
            Destroy(attachedRightWeapon);
            attachedRightWeapon = null;
            Debug.Log("[ClassWeaponManager] 🗑️ Удалено оружие правой руки");
        }

        if (attachedLeftWeapon != null)
        {
            Destroy(attachedLeftWeapon);
            attachedLeftWeapon = null;
            Debug.Log("[ClassWeaponManager] 🗑️ Удалено оружие левой руки");
        }

        if (attachedBackWeapon != null)
        {
            Destroy(attachedBackWeapon);
            attachedBackWeapon = null;
            Debug.Log("[ClassWeaponManager] 🗑️ Удалено оружие со спины");
        }

        weaponsAttached = false;
    }

    void Start()
    {
        AttachWeaponForClass();
    }

    void Update()
    {
        // Обновляем трансформы оружия в реальном времени (только в редакторе)
        #if UNITY_EDITOR
        if (!UnityEngine.Application.isPlaying)
        {
            UpdateWeaponTransforms();
        }
        #endif
    }

    /// <summary>
    /// Обновить трансформы оружия в реальном времени (для редактирования в Inspector)
    /// </summary>
    private void UpdateWeaponTransforms()
    {
        CharacterClass characterClass = cachedCharacterClass;

        switch (characterClass)
        {
            case CharacterClass.Warrior:
                if (attachedRightWeapon != null && warriorRightHand != null)
                {
                    UpdateWeaponTransform(attachedRightWeapon, warriorRightHand);
                }
                if (attachedLeftWeapon != null && warriorLeftHand != null)
                {
                    UpdateWeaponTransform(attachedLeftWeapon, warriorLeftHand);
                }
                break;

            case CharacterClass.Mage:
                if (attachedRightWeapon != null && mageWeapon != null)
                {
                    UpdateWeaponTransform(attachedRightWeapon, mageWeapon);
                }
                break;

            case CharacterClass.Archer:
                if (attachedLeftWeapon != null && archerLeftHand != null)
                {
                    UpdateWeaponTransform(attachedLeftWeapon, archerLeftHand);
                }
                if (attachedBackWeapon != null && archerBack != null)
                {
                    UpdateWeaponTransform(attachedBackWeapon, archerBack);
                }
                break;

            case CharacterClass.Rogue:
                if (attachedRightWeapon != null && rogueWeapon != null)
                {
                    UpdateWeaponTransform(attachedRightWeapon, rogueWeapon);
                }
                break;

            case CharacterClass.Paladin:
                if (attachedRightWeapon != null && paladinWeapon != null)
                {
                    UpdateWeaponTransform(attachedRightWeapon, paladinWeapon);
                }
                break;
        }
    }

    /// <summary>
    /// Обновить трансформ одного оружия
    /// </summary>
    private void UpdateWeaponTransform(GameObject weapon, WeaponConfig config)
    {
        if (weapon == null || config == null)
            return;

        weapon.transform.localPosition = config.localPosition;
        weapon.transform.localRotation = Quaternion.Euler(config.localRotation);
        weapon.transform.localScale = config.localScale;
    }

    /// <summary>
    /// Загрузить оружие из базы данных
    /// </summary>
    private void LoadWeaponsFromDatabase(CharacterClass characterClass)
    {
        Debug.Log($"\n[ClassWeaponManager] LoadWeaponsFromDatabase для класса: {characterClass}");

        WeaponDatabase db = WeaponDatabase.Instance;
        if (db == null)
        {
            Debug.LogError("[ClassWeaponManager] ❌ WeaponDatabase не найдена! Создайте через Tools → Create Weapon Database");
            return;
        }

        Debug.Log("[ClassWeaponManager] ✓ WeaponDatabase загружена");

        // Загружаем для правой руки
        var rightWeapon = db.GetRightHandWeapon(characterClass);
        Debug.Log($"[ClassWeaponManager] Правая рука: {(rightWeapon != null ? rightWeapon.weaponName : "null")}");
        if (rightWeapon != null && rightWeapon.weaponPrefab != null)
        {
            switch (characterClass)
            {
                case CharacterClass.Warrior:
                    if (warriorRightHand == null || warriorRightHand.weaponModel == null)
                    {
                        warriorRightHand = CreateWeaponConfig(rightWeapon);
                    }
                    break;
                case CharacterClass.Mage:
                    if (mageWeapon == null || mageWeapon.weaponModel == null)
                    {
                        mageWeapon = CreateWeaponConfig(rightWeapon);
                    }
                    break;
                case CharacterClass.Rogue:
                    if (rogueWeapon == null || rogueWeapon.weaponModel == null)
                    {
                        rogueWeapon = CreateWeaponConfig(rightWeapon);
                    }
                    break;
                case CharacterClass.Paladin:
                    if (paladinWeapon == null || paladinWeapon.weaponModel == null)
                    {
                        paladinWeapon = CreateWeaponConfig(rightWeapon);
                    }
                    break;
            }
        }

        // Загружаем для левой руки
        var leftWeapon = db.GetLeftHandWeapon(characterClass);
        Debug.Log($"[ClassWeaponManager] Левая рука: {(leftWeapon != null ? leftWeapon.weaponName : "null")}");
        if (leftWeapon != null && leftWeapon.weaponPrefab != null)
        {
            if (characterClass == CharacterClass.Warrior)
            {
                if (warriorLeftHand == null || warriorLeftHand.weaponModel == null)
                {
                    warriorLeftHand = CreateWeaponConfig(leftWeapon);
                }
            }
            else if (characterClass == CharacterClass.Archer)
            {
                if (archerLeftHand == null || archerLeftHand.weaponModel == null)
                {
                    archerLeftHand = CreateWeaponConfig(leftWeapon);
                }
            }
        }

        // Загружаем для спины
        var backWeapon = db.GetBackWeapon(characterClass);
        Debug.Log($"[ClassWeaponManager] Спина: {(backWeapon != null ? backWeapon.weaponName : "null")}");
        if (backWeapon != null && backWeapon.weaponPrefab != null)
        {
            if (characterClass == CharacterClass.Archer)
            {
                if (archerBack == null || archerBack.weaponModel == null)
                {
                    archerBack = CreateWeaponConfig(backWeapon);
                }
            }
        }
    }

    /// <summary>
    /// Создать WeaponConfig из WeaponEntry
    /// </summary>
    private WeaponConfig CreateWeaponConfig(WeaponDatabase.WeaponEntry entry)
    {
        Debug.Log($"[ClassWeaponManager] Создаю WeaponConfig для {entry.weaponName}:");
        Debug.Log($"  - Position: {entry.defaultPosition}");
        Debug.Log($"  - Rotation: {entry.defaultRotation}");
        Debug.Log($"  - Scale: {entry.defaultScale}");

        return new WeaponConfig
        {
            weaponModel = entry.weaponPrefab,
            localPosition = entry.defaultPosition,
            localRotation = entry.defaultRotation,
            localScale = entry.defaultScale
        };
    }

    /// <summary>
    /// Прикрепить оружие в зависимости от класса
    /// </summary>
    public void AttachWeaponForClass()
    {
        if (weaponsAttached)
        {
            Debug.Log($"[ClassWeaponManager] Оружие уже прикреплено к {gameObject.name}, пропускаю.");
            return;
        }

        CharacterClass characterClass = GetCharacterClass();

        // Автоматическая загрузка из базы данных если не настроено вручную
        if (autoLoadFromDatabase)
        {
            LoadWeaponsFromDatabase(characterClass);
        }

        // Находим кости рук
        FindHandBones();

        if (rightHandBone == null)
        {
            Debug.LogError($"[ClassWeaponManager] Не найдена кость правой руки у {gameObject.name}");
            return;
        }

        // Прикрепляем оружие в зависимости от класса
        switch (characterClass)
        {
            case CharacterClass.Warrior:
                // Воин: меч в правой руке, щит в левой
                if (warriorRightHand != null && warriorRightHand.weaponModel != null)
                {
                    AttachWeapon(warriorRightHand, rightHandBone, ref attachedRightWeapon);
                    Debug.Log($"[ClassWeaponManager] Меч прикреплен к правой руке воина");
                }
                if (warriorLeftHand != null && warriorLeftHand.weaponModel != null && leftHandBone != null)
                {
                    AttachWeapon(warriorLeftHand, leftHandBone, ref attachedLeftWeapon);
                    Debug.Log($"[ClassWeaponManager] Щит прикреплен к левой руке воина");
                }
                break;

            case CharacterClass.Mage:
                if (mageWeapon != null && mageWeapon.weaponModel != null)
                {
                    AttachWeapon(mageWeapon, rightHandBone, ref attachedRightWeapon);
                    Debug.Log($"[ClassWeaponManager] Посох прикреплен к магу");
                }
                break;

            case CharacterClass.Archer:
                // Лучник: лук в левой руке, колчан на спине
                if (archerLeftHand != null && archerLeftHand.weaponModel != null && leftHandBone != null)
                {
                    AttachWeapon(archerLeftHand, leftHandBone, ref attachedLeftWeapon);
                    Debug.Log($"[ClassWeaponManager] Лук прикреплен к левой руке лучника");
                }
                if (archerBack != null && archerBack.weaponModel != null && spineBone != null)
                {
                    AttachWeapon(archerBack, spineBone, ref attachedBackWeapon);
                    Debug.Log($"[ClassWeaponManager] Колчан прикреплен к спине лучника");
                }
                break;

            case CharacterClass.Rogue:
                if (rogueWeapon != null && rogueWeapon.weaponModel != null)
                {
                    AttachWeapon(rogueWeapon, rightHandBone, ref attachedRightWeapon);
                    Debug.Log($"[ClassWeaponManager] Кинжал прикреплен к разбойнику");
                }
                break;

            case CharacterClass.Paladin:
                if (paladinWeapon != null && paladinWeapon.weaponModel != null)
                {
                    AttachWeapon(paladinWeapon, rightHandBone, ref attachedRightWeapon);
                    Debug.Log($"[ClassWeaponManager] Меч прикреплен к паладину");
                }
                break;
        }

        weaponsAttached = true;
    }

    /// <summary>
    /// Прикрепить оружие к кости
    /// </summary>
    private void AttachWeapon(WeaponConfig config, Transform bone, ref GameObject attachedWeaponRef)
    {
        // Удаляем старое оружие
        if (attachedWeaponRef != null)
        {
            Destroy(attachedWeaponRef);
        }

        // Создаем новое оружие
        attachedWeaponRef = Instantiate(config.weaponModel, bone);
        attachedWeaponRef.name = config.weaponModel.name;

        // Применяем трансформы
        attachedWeaponRef.transform.localPosition = config.localPosition;
        attachedWeaponRef.transform.localRotation = Quaternion.Euler(config.localRotation);
        attachedWeaponRef.transform.localScale = config.localScale;

        // Устанавливаем тот же Layer что у персонажа (важно для отображения)
        int characterLayer = gameObject.layer;
        SetLayerRecursively(attachedWeaponRef, characterLayer);

        // Удаляем коллайдеры (если есть)
        Collider[] colliders = attachedWeaponRef.GetComponentsInChildren<Collider>();
        foreach (Collider col in colliders)
        {
            Destroy(col);
        }

        // Проверяем и включаем Renderer
        Renderer[] renderers = attachedWeaponRef.GetComponentsInChildren<Renderer>();
        foreach (Renderer rend in renderers)
        {
            rend.enabled = true;
        }

        // Добавляем компонент эффекта свечения
        WeaponGlowEffect glowEffect = attachedWeaponRef.GetComponent<WeaponGlowEffect>();
        if (glowEffect == null)
        {
            glowEffect = attachedWeaponRef.AddComponent<WeaponGlowEffect>();
            Debug.Log("[ClassWeaponManager] ✨ Добавлен WeaponGlowEffect");
        }

        Debug.Log($"[ClassWeaponManager] Оружие настроено: {attachedWeaponRef.name}, Layer: {LayerMask.LayerToName(characterLayer)}, Renderers: {renderers.Length}");
    }

    /// <summary>
    /// Установить Layer рекурсивно для всех детей
    /// </summary>
    private void SetLayerRecursively(GameObject obj, int layer)
    {
        obj.layer = layer;
        foreach (Transform child in obj.transform)
        {
            SetLayerRecursively(child.gameObject, layer);
        }
    }

    /// <summary>
    /// Найти кости рук и спины
    /// </summary>
    private void FindHandBones()
    {
        rightHandBone = FindBoneRecursive(transform, rightHandBoneName);
        leftHandBone = FindBoneRecursive(transform, leftHandBoneName);
        spineBone = FindBoneRecursive(transform, spineBoneName);

        if (rightHandBone != null)
        {
            Debug.Log($"[ClassWeaponManager] Найдена правая рука: {rightHandBone.name}");
        }

        if (leftHandBone != null)
        {
            Debug.Log($"[ClassWeaponManager] Найдена левая рука: {leftHandBone.name}");
        }

        if (spineBone != null)
        {
            Debug.Log($"[ClassWeaponManager] Найдена кость спины: {spineBone.name}");
        }
    }

    /// <summary>
    /// Рекурсивный поиск кости
    /// </summary>
    private Transform FindBoneRecursive(Transform parent, string boneName)
    {
        if (parent.name == boneName)
            return parent;

        foreach (Transform child in parent)
        {
            Transform found = FindBoneRecursive(child, boneName);
            if (found != null)
                return found;
        }

        return null;
    }

    /// <summary>
    /// Активировать эффект свечения оружия (Lineage 2 style - синяя аура с электричеством)
    /// </summary>
    public void ActivateWeaponGlow()
    {
        ApplyWeaponGlow(true);
        Debug.Log("[ClassWeaponManager] ⚡ Оружие светится синей аурой");
    }

    /// <summary>
    /// Деактивировать эффект свечения оружия
    /// </summary>
    public void DeactivateWeaponGlow()
    {
        ApplyWeaponGlow(false);
        Debug.Log("[ClassWeaponManager] 💤 Свечение оружия отключено");
    }

    /// <summary>
    /// Применить или убрать эффект свечения
    /// </summary>
    private void ApplyWeaponGlow(bool activate)
    {
        if (attachedRightWeapon != null)
        {
            ApplyGlowToWeapon(attachedRightWeapon, activate);
        }

        if (attachedLeftWeapon != null)
        {
            ApplyGlowToWeapon(attachedLeftWeapon, activate);
        }

        // Колчан не светится
    }

    /// <summary>
    /// Применить эффект свечения к конкретному оружию
    /// </summary>
    private void ApplyGlowToWeapon(GameObject weapon, bool activate)
    {
        if (weapon == null) return;

        // Используем WeaponGlowEffect компонент
        WeaponGlowEffect glowEffect = weapon.GetComponent<WeaponGlowEffect>();
        if (glowEffect != null)
        {
            if (activate)
            {
                glowEffect.ActivateGlow();
            }
            else
            {
                glowEffect.DeactivateGlow();
            }
        }
        else
        {
            Debug.LogWarning($"[ClassWeaponManager] WeaponGlowEffect не найден на {weapon.name}!");
        }
    }

    /// <summary>
    /// Отсоединить оружие
    /// </summary>
    public void DetachWeapon()
    {
        if (attachedRightWeapon != null)
        {
            Destroy(attachedRightWeapon);
            attachedRightWeapon = null;
        }

        if (attachedLeftWeapon != null)
        {
            Destroy(attachedLeftWeapon);
            attachedLeftWeapon = null;
        }

        if (attachedBackWeapon != null)
        {
            Destroy(attachedBackWeapon);
            attachedBackWeapon = null;
        }
    }

    void OnDestroy()
    {
        DetachWeapon();
    }
}
