using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
public class PlayerStatsHUD : MonoBehaviour
{
    [SerializeField] private PlayerStats playerStats;

    private UIDocument uiDocument;
    private VisualElement shieldFill;
    private VisualElement healthFill;
    private VisualElement staminaFill;
    private VisualElement shieldMarker;
    private VisualElement healthMarker;
    private VisualElement staminaMarker;

    private bool isBound;
    private float cachedShield = -1f;
    private float cachedHealth = -1f;
    private float cachedStamina = -1f;
    private float cachedMaxShield = -1f;
    private float cachedMaxHealth = -1f;
    private float cachedMaxStamina = -1f;

    private void Awake()
    {
        uiDocument = GetComponent<UIDocument>();

        if (playerStats == null)
        {
            playerStats = FindFirstObjectByType<PlayerStats>();
        }

        if (playerStats == null)
        {
            throw new MissingReferenceException("PlayerStatsHUD requires a PlayerStats reference. Assign it in the Inspector.");
        }
    }

    private void OnEnable()
    {
        TryBind();
        uiDocument.rootVisualElement?.RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
    }

    private void OnDisable()
    {
        uiDocument.rootVisualElement?.UnregisterCallback<GeometryChangedEvent>(OnGeometryChanged);
        isBound = false;
    }

    private void OnGeometryChanged(GeometryChangedEvent _)
    {
        if (!isBound)
        {
            TryBind();
        }
    }

    private void Start()
    {
        TryBind();
        ForceRefresh();
    }

    private void Update()
    {
        if (!isBound)
        {
            TryBind();
        }

        if (!isBound)
        {
            return;
        }

        RefreshIfChanged();
    }

    private void TryBind()
    {
        VisualElement root = uiDocument.rootVisualElement;
        if (root == null)
        {
            return;
        }

        shieldFill = root.Q<VisualElement>("shield-fill");
        healthFill = root.Q<VisualElement>("health-fill");
        staminaFill = root.Q<VisualElement>("stamina-fill");
        shieldMarker = root.Q<VisualElement>("shield-marker");
        healthMarker = root.Q<VisualElement>("health-marker");
        staminaMarker = root.Q<VisualElement>("stamina-marker");

        if (shieldFill == null || healthFill == null || staminaFill == null)
        {
            Debug.LogError("PlayerStatsHUD could not find required UI elements. Check PlayerHUD.uxml element names.", this);
            isBound = false;
            return;
        }

        isBound = true;
        ForceRefresh();
    }

    private void ForceRefresh()
    {
        cachedShield = -1f;
        cachedHealth = -1f;
        cachedStamina = -1f;
        cachedMaxShield = -1f;
        cachedMaxHealth = -1f;
        cachedMaxStamina = -1f;
        RefreshIfChanged();
    }

    private void RefreshIfChanged()
    {
        float currentShield = playerStats.CurrentShield;
        float currentHealth = playerStats.CurrentHealth;
        float currentStamina = playerStats.CurrentStamina;
        float maxShield = playerStats.MaxShield;
        float maxHealth = playerStats.MaxHealth;
        float maxStamina = playerStats.MaxStamina;

        if (!HasStatsChanged(currentShield, currentHealth, currentStamina, maxShield, maxHealth, maxStamina))
        {
            return;
        }

        float shieldPercent = GetPercent(currentShield, maxShield);
        float healthPercent = GetPercent(currentHealth, maxHealth);
        float staminaPercent = GetPercent(currentStamina, maxStamina);

        SetBarWidth(shieldFill, shieldPercent);
        SetBarWidth(healthFill, healthPercent);
        SetMarkerPosition(shieldMarker, shieldPercent);
        SetMarkerPosition(healthMarker, healthPercent);

        SetBarWidth(staminaFill, staminaPercent);
        SetMarkerPosition(staminaMarker, staminaPercent);

        cachedShield = currentShield;
        cachedHealth = currentHealth;
        cachedStamina = currentStamina;
        cachedMaxShield = maxShield;
        cachedMaxHealth = maxHealth;
        cachedMaxStamina = maxStamina;
    }

    private bool HasStatsChanged(
        float currentShield,
        float currentHealth,
        float currentStamina,
        float maxShield,
        float maxHealth,
        float maxStamina)
    {
        return !Mathf.Approximately(currentShield, cachedShield)
            || !Mathf.Approximately(currentHealth, cachedHealth)
            || !Mathf.Approximately(currentStamina, cachedStamina)
            || !Mathf.Approximately(maxShield, cachedMaxShield)
            || !Mathf.Approximately(maxHealth, cachedMaxHealth)
            || !Mathf.Approximately(maxStamina, cachedMaxStamina);
    }

    private static void SetBarWidth(VisualElement fill, float percent)
    {
        float clamped = Mathf.Clamp(percent, 0f, 100f);
        fill.style.width = new Length(clamped, LengthUnit.Percent);
        fill.style.flexGrow = 0f;
        fill.style.flexShrink = 0f;
    }

    private static float GetPercent(float current, float max)
    {
        if (max <= 0f)
        {
            return 0f;
        }

        return Mathf.Clamp01(current / max) * 100f;
    }

    private static void SetMarkerPosition(VisualElement marker, float percent)
    {
        if (marker == null)
        {
            return;
        }

        marker.style.left = new Length(Mathf.Clamp(percent, 0f, 100f), LengthUnit.Percent);
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (playerStats == null)
        {
            Debug.LogWarning("PlayerStatsHUD: PlayerStats reference is not assigned.", this);
        }
    }
#endif
}
