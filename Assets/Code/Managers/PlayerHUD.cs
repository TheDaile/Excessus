using UnityEngine;
using TMPro;

public class PlayerHUD : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI promptText;
    void Start()
    {
        UpdateText(string.Empty);
    }

    private void Awake()
    {
        if (promptText == null)
        {
            throw new MissingReferenceException("PlayerHUD requires a Prompt Text reference. Assign it in the Inspector.");
        }
    }

    public void UpdateText(string promptMessage)
    {
        promptText.text = promptMessage;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (promptText == null)
        {
            Debug.LogWarning("PlayerHUD: Prompt Text reference is not assigned.", this);
        }
    }
#endif
}
