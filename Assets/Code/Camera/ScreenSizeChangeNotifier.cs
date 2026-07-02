using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

public class ScreenSizeChangeNotifier : UIBehaviour
{
    [SerializeField] private UnityEvent notifyScreenSizeChange;

    protected override void OnRectTransformDimensionsChange()
    {
        notifyScreenSizeChange.Invoke();
    }


}
