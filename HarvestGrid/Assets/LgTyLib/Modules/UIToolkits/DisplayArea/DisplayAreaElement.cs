using UnityEngine;
using UnityEngine.UIElements;

[UxmlElement]
public partial class DisplayAreaElement : VisualElement
{
    public DisplayAreaElement()
    {
        pickingMode = PickingMode.Ignore;
        style.position = Position.Absolute;

        var holder = DisplayAreaConfigHolderSO.Instance;

        if (holder == null || holder.displayAreaConfig == null)
        {
            Debug.LogError("DisplayAreaConfigHolderSO or config is null.");
            return;
        }

        var config = holder.displayAreaConfig;

        style.top = config.top;
        style.right = config.right;
        style.bottom = config.bottom;
        style.left = config.left;
    }
}