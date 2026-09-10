using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

namespace HarvestGrid.UI.Cosmetic
{
    public class CosmeticPanel : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private CosmeticItemUI itemPrefab;
        [SerializeField] private Transform gridContainer;
        
        [Header("Detail Area")]
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI descriptionText;
        [SerializeField] private Image previewImage;
        [SerializeField] private Button actionButton; // Serves as both Equip/Unequip
        [SerializeField] private TextMeshProUGUI actionButtonText;
        [SerializeField] private GameObject detailArea;

        [Header("Tabs")]
        [SerializeField] private List<CosmeticTabUI> tabs;

        private List<CosmeticItemUI> currentItems = new List<CosmeticItemUI>();
        private CosmeticTabUI currentTab;
        private CosmeticItemUI currentSelectedItem;

        private void Start()
        {
            foreach (var tab in tabs)
            {
                tab.Init(OnTabClicked);
            }

            if (actionButton != null)
                actionButton.onClick.AddListener(OnActionButtonClicked);

            // Auto select first tab if exists
            if (tabs.Count > 0)
            {
                OnTabClicked(tabs[0]);
            }
            else
            {
                if (detailArea != null) detailArea.SetActive(false);
            }
        }

        private void OnEnable()
        {
            if (currentTab != null)
            {
                RefreshGrid(currentTab.tabType);
            }
        }

        private void OnTabClicked(CosmeticTabUI clickedTab)
        {
            currentTab = clickedTab;

            foreach (var tab in tabs)
            {
                tab.SetSelectedState(tab == currentTab);
            }

            RefreshGrid(currentTab.tabType);
        }

        private void RefreshGrid(CosmeticsType type)
        {
            foreach (var item in currentItems)
            {
                Destroy(item.gameObject);
            }
            currentItems.Clear();

            List<CosmeticData> cosmetics = CosmeticManager.Instance.GetCosmeticsByType(type);

            foreach (var cosmetic in cosmetics)
            {
                CosmeticItemUI newItem = Instantiate(itemPrefab, gridContainer);
                newItem.Init(cosmetic, OnItemClicked);
                
                bool isEquipped = CosmeticManager.Instance.IsEquipped(cosmetic);
                bool isUnlocked = CosmeticManager.Instance.IsUnlocked(cosmetic.id);
                
                newItem.SetEquippedState(isEquipped);
                newItem.SetSelectedState(false);
                newItem.SetLockedState(!isUnlocked);
                
                currentItems.Add(newItem);
            }

            CosmeticData equipped = CosmeticManager.Instance.GetEquippedCosmetic(type);
            CosmeticItemUI itemToSelect = null;
            
            if (equipped != null)
            {
                itemToSelect = currentItems.Find(i => i.Cosmetic == equipped);
            }
            
            if (itemToSelect == null && currentItems.Count > 0)
            {
                itemToSelect = currentItems[0];
            }

            if (itemToSelect != null)
            {
                OnItemClicked(itemToSelect);
            }
            else
            {
                if (detailArea != null) detailArea.SetActive(false);
            }
        }

        private void OnItemClicked(CosmeticItemUI itemUI)
        {
            currentSelectedItem = itemUI;

            foreach (var item in currentItems)
            {
                item.SetSelectedState(item == currentSelectedItem);
            }

            UpdateDetailArea();
        }

        private void UpdateDetailArea()
        {
            if (currentSelectedItem == null || currentSelectedItem.Cosmetic == null)
            {
                if (detailArea != null) detailArea.SetActive(false);
                return;
            }

            if (detailArea != null) detailArea.SetActive(true);
            CosmeticData cosmetic = currentSelectedItem.Cosmetic;

            if (titleText != null) titleText.text = cosmetic.cosmeticName;
            if (descriptionText != null) descriptionText.text = cosmetic.description;
            
            if (previewImage != null)
            {
                if (cosmetic.previewSprite != null)
                {
                    previewImage.sprite = cosmetic.previewSprite;
                    previewImage.gameObject.SetActive(true);
                }
                else if (cosmetic.icon != null)
                {
                    previewImage.sprite = cosmetic.icon;
                    previewImage.gameObject.SetActive(true);
                }
                else
                {
                    previewImage.gameObject.SetActive(false);
                }
            }

            UpdateActionButtonState();
        }

        private void UpdateActionButtonState()
        {
            if (currentSelectedItem == null || actionButton == null || actionButtonText == null) return;
            
            CosmeticData cosmetic = currentSelectedItem.Cosmetic;
            bool isEquipped = CosmeticManager.Instance.IsEquipped(cosmetic);
            bool isUnlocked = CosmeticManager.Instance.IsUnlocked(cosmetic.id);

            if (!isUnlocked)
            {
                actionButton.interactable = false;
                actionButtonText.text = "Locked";
            }
            else if (isEquipped)
            {
                actionButton.interactable = true;
                actionButtonText.text = "Unequip";
            }
            else
            {
                actionButton.interactable = true;
                actionButtonText.text = "Equip";
            }
        }

        private void OnActionButtonClicked()
        {
            if (currentSelectedItem != null && currentSelectedItem.Cosmetic != null)
            {
                CosmeticData cosmetic = currentSelectedItem.Cosmetic;
                bool isEquipped = CosmeticManager.Instance.IsEquipped(cosmetic);
                
                if (isEquipped)
                {
                    // Unequip
                    CosmeticManager.Instance.UnequipCosmetic(cosmetic.cosmeticsType);
                }
                else
                {
                    // Equip
                    CosmeticManager.Instance.EquipCosmetic(cosmetic);
                }
                
                // Refresh visuals in grid
                foreach (var item in currentItems)
                {
                    item.SetEquippedState(CosmeticManager.Instance.IsEquipped(item.Cosmetic));
                }
                
                UpdateActionButtonState();
            }
        }
        
        public void ClosePanel()
        {
            gameObject.SetActive(false);
        }
    }
}
