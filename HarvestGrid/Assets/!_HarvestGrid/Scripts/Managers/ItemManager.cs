using LgTyLib.Core;
using LgTyLib.Modules.DataPersistence;
using LgTyLib.Modules.ObjectPooling;
using System.Collections.Generic;
using UnityEngine;

public class ItemManager : BaseSingleton<ItemManager>, IDataPersistence
{
    public ItemMono itemPrefab;
    public ItemEffectMono itemEffectMonoPrefab;
    public RectTransform itemEffectContainer;
    public ItemWithFootprintMono itemWithFootprintPrefab;
    public Transform itemPlaceHolder;

    private readonly List<ItemEffectMono> activeEffects = new();

    #region Item Spawning

    public void SpawnItemWithoutClone(Item item)
    {
        ItemMono newItemMono = Instantiate(itemPrefab);

        newItemMono.transform.localScale = Vector3.one;

        newItemMono.Init(item);
        newItemMono.Run();
    }

    public ItemWithFootprintMono SpawnItemMonoWithFootprint(
        StoredObject storedObject)
    {
        ItemWithFootprintMono newItem =
            Instantiate(itemWithFootprintPrefab, itemPlaceHolder);

        newItem.ForceAddToInventory(storedObject);
        newItem.LateSnap();

        return newItem;
    }

    #endregion

    #region Item Effects

    public void ApplyItemEffect(ItemUseContext useContext)
    {
        if (useContext == null)
            return;

        if (useContext.Item == null)
            return;

        if (useContext.Use == null)
            return;

        if (useContext.TargetSlot == null)
            return;

        ItemEffectMono itemEffectMono =
            ObjectPoolManager.Instance.SpawnObject<ItemEffectMono>(
                itemEffectMonoPrefab,
                Vector3.zero,
                itemEffectContainer,
                PoolGroup.ItemEffects);

        activeEffects.Add(itemEffectMono);

        itemEffectMono.Play(
            useContext,
            OnItemEffectFinished);
    }

    private void OnItemEffectFinished(GameObject itemEffectObject)
    {
        ItemEffectMono effectMono =
            itemEffectObject.GetComponent<ItemEffectMono>();

        if (effectMono != null)
            activeEffects.Remove(effectMono);

        ObjectPoolManager.Instance.ReturnObjectToPool(itemEffectObject);
    }

    #endregion

    #region Save / Load

    public void SaveGame(ref GameData gameData)
    {
        gameData.itemEffects = new List<ItemEffect>();

        for (int i = activeEffects.Count - 1; i >= 0; i--)
        {
            ItemEffectMono effectMono = activeEffects[i];

            if (effectMono == null)
            {
                activeEffects.RemoveAt(i);
                continue;
            }

            ItemUseContext context = effectMono.UseContext;

            if (context == null)
                continue;

            if (context.TargetSlot == null)
                continue;

            if (context.Item == null)
                continue;

            if (context.Use == null)
                continue;

            ItemEffect effect = new ItemEffect
            {
                useContext = context,
                remainingTime = effectMono.RemainingTime,
                farmSlotID = context.TargetSlot.SlotID
            };

            gameData.itemEffects.Add(effect);
        }
    }

    public void LoadGame(GameData gameData)
    {
        ClearActiveEffects();

        if (gameData.itemEffects == null)
            return;

        foreach (ItemEffect savedEffect in gameData.itemEffects)
        {
            LoadItemEffect(savedEffect);
        }
    }

    private void LoadItemEffect(ItemEffect savedEffect)
    {
        if (savedEffect == null)
            return;

        if (savedEffect.useContext == null)
            return;

        if (string.IsNullOrEmpty(savedEffect.farmSlotID))
            return;

        FarmSlot targetSlot =
            FarmMono.Instance.GetFarmSlotByID(savedEffect.farmSlotID);

        if (targetSlot == null)
        {
            Debug.LogWarning(
                $"Could not restore item effect. " +
                $"FarmSlot '{savedEffect.farmSlotID}' was not found.");

            return;
        }

        ItemUseContext context = savedEffect.useContext;

        // Runtime FarmSlot is resolved again from FarmMono.
        context.TargetSlot = targetSlot;

        if (context.Item == null)
        {
            Debug.LogWarning(
                $"Could not restore item effect on slot " +
                $"'{savedEffect.farmSlotID}'. Item is null.");

            return;
        }

        if (context.Use == null)
        {
            Debug.LogWarning(
                $"Could not restore item effect on slot " +
                $"'{savedEffect.farmSlotID}'. ItemUse is null.");

            return;
        }

        if (savedEffect.remainingTime <= 0f)
            return;

        SpawnLoadedEffect(
            context,
            savedEffect.remainingTime);
    }

    private void SpawnLoadedEffect(
        ItemUseContext context,
        float remainingTime)
    {
        ItemEffectMono itemEffectMono =
            ObjectPoolManager.Instance.SpawnObject<ItemEffectMono>(
                itemEffectMonoPrefab,
                Vector3.zero,
                itemEffectContainer,
                PoolGroup.ItemEffects);

        activeEffects.Add(itemEffectMono);

        itemEffectMono.Play(
            context,
            OnItemEffectFinished,
            remainingTime);
    }

    private void ClearActiveEffects()
    {
        for (int i = activeEffects.Count - 1; i >= 0; i--)
        {
            ItemEffectMono effect = activeEffects[i];

            if (effect != null)
            {
                ObjectPoolManager.Instance.ReturnObjectToPool(
                    effect.gameObject);
            }
        }

        activeEffects.Clear();
    }

    #endregion
}