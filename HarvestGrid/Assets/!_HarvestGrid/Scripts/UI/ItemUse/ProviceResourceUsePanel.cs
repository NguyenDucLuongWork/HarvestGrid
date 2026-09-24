using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ProviceResourceUsePanel : ItemUsePanel<ProviceResourceUse>
{
    [SerializeField] private Image resourceIcon;
    [SerializeField] private TextMeshProUGUI amountLabel;

    protected override void OnBind()
    {
        var resource = Use.Resource;

        resourceIcon.sprite = EnumIconManager.Instance.resourceIconDict[resource];
        if (amountLabel != null)
            amountLabel.text = $"{Use.Amount}";
    }
}