using UnityEngine;

public abstract class Interactable : MonoBehaviour
{
    public bool useEvent= false;
    public string promptMessage;

    public void BaseInteract()
    {
        if (useEvent)
        {
            InteractionEvent interactionEvent = GetComponent<InteractionEvent>();

            if (interactionEvent == null)
            {
                throw new MissingComponentException("Interactable is set to use events, but InteractionEvent is missing.");
            }

            interactionEvent.onInteract.Invoke();
        }
        Interact();
    }
    protected virtual void Interact() {}

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (useEvent && GetComponent<InteractionEvent>() == null)
        {
            Debug.LogWarning("Interactable: useEvent is enabled, but InteractionEvent is missing.", this);
        }
    }
#endif
}
