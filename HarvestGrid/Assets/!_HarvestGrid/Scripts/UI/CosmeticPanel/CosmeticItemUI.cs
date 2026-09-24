using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

namespace HarvestGrid.UI.Cosmetic
{
    public class CosmeticItemUI : MonoBehaviour
    {
        [SerializeField] private Image iconImage;
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private GameObject equippedIndicator;
        [SerializeField] private GameObject selectedIndicator;
        [SerializeField] private GameObject lockedIndicator;
        [SerializeField] private Button button;

        public CosmeticData Cosmetic { get; private set; }
        private Action<CosmeticItemUI> onClickCallback;

        public void Init(CosmeticData cosmetic, Action<CosmeticItemUI> onClick)
        {
            Cosmetic = cosmetic;
            onClickCallback = onClick;
            
            if (iconImage != null && cosmetic != null && cosmetic.icon != null)
            {
                iconImage.sprite = cosmetic.icon;
            }

            if (nameText != null && cosmetic != null)
            {
                nameText.text = cosmetic.cosmeticName;
            }

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => onClickCallback?.Invoke(this));
        }

        public void SetEquippedState(bool isEquipped)
        {
            if (equippedIndicator != null)
                equippedIndicator.SetActive(isEquipped);
        }

        public void SetSelectedState(bool isSelected)
        {
            if (selectedIndicator != null)
                selectedIndicator.SetActive(isSelected);
        }
        
        public void SetLockedState(bool isLocked)
        {
            if (lockedIndicator != null)
                lockedIndicator.SetActive(isLocked);
                
            // Optional visual feedback for locked items
            if (iconImage != null)
            {
                iconImage.color = isLocked ? new Color(0.5f, 0.5f, 0.5f, 0.5f) : Color.white;
            }
        }
    }
}
