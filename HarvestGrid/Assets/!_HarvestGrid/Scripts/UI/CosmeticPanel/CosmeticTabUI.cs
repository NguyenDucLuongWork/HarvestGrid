using UnityEngine;
using UnityEngine.UI;
using System;

namespace HarvestGrid.UI.Cosmetic
{
    public class CosmeticTabUI : MonoBehaviour
    {
        public CosmeticsType tabType;
        [SerializeField] private Button button;
        [SerializeField] private GameObject selectedIndicator;
        
        private Action<CosmeticTabUI> onClickCallback;

        public void Init(Action<CosmeticTabUI> onClick)
        {
            onClickCallback = onClick;
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => onClickCallback?.Invoke(this));
        }

        public void SetSelectedState(bool isSelected)
        {
            if (selectedIndicator != null)
            {
                selectedIndicator.SetActive(isSelected);
            }
            else if (button != null)
            {
                button.image.color = isSelected ? Color.white : new Color(0.8f, 0.8f, 0.8f);
            }
        }
    }
}
