using LgTyLib.Core;
using LgTyLib.Modules.DataPersistence;
using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class CosmeticManager : BaseSingleton<CosmeticManager>, IDataPersistence
{
    [Header("All Cosmetics Data")]
    [Tooltip("Assign all CosmeticData ScriptableObjects here, or load via Resources/Addressables in a real project")]
    public List<CosmeticData> allCosmetics;

    // Runtime state
    private List<string> unlockedCosmetics = new List<string>();
    private Dictionary<CosmeticsType, string> equippedCosmetics = new Dictionary<CosmeticsType, string>();

    // Events
    public event Action<CosmeticData> OnCosmeticEquipped;
    public event Action<CosmeticsType> OnCosmeticUnequipped;

    public void LoadGame(GameData gameData)
    {
        unlockedCosmetics = gameData.unlockedCosmetics != null ? new List<string>(gameData.unlockedCosmetics) : new List<string>();
        equippedCosmetics.Clear();
        
        if (gameData.equippedCosmetics != null)
        {
            foreach (var item in gameData.equippedCosmetics)
            {
                equippedCosmetics[item.type] = item.cosmeticId;
            }
        }
    }

    public void SaveGame(ref GameData gameData)
    {
        gameData.unlockedCosmetics = new List<string>(unlockedCosmetics);
        
        List<EquippedCosmeticData> eqList = new List<EquippedCosmeticData>();
        foreach (var kvp in equippedCosmetics)
        {
            eqList.Add(new EquippedCosmeticData { type = kvp.Key, cosmeticId = kvp.Value });
        }
        gameData.equippedCosmetics = eqList;
    }

    public void UnlockCosmetic(string cosmeticId)
    {
        if (!unlockedCosmetics.Contains(cosmeticId))
        {
            unlockedCosmetics.Add(cosmeticId);
        }
    }

    public bool IsUnlocked(string cosmeticId)
    {
        // For prototyping, we can assume all are unlocked, 
        // or check the list if you want to enforce unlocking.
        // Uncomment below to enforce unlock checks:
        // return unlockedCosmetics.Contains(cosmeticId);
        
        return true; 
    }

    public void EquipCosmetic(CosmeticData cosmeticData)
    {
        if (cosmeticData == null) return;
        
        // Optional: Check if unlocked
        // if (!IsUnlocked(cosmeticData.id)) return;
        
        equippedCosmetics[cosmeticData.cosmeticsType] = cosmeticData.id;
        
        OnCosmeticEquipped?.Invoke(cosmeticData);
    }

    public void UnequipCosmetic(CosmeticsType type)
    {
        if (equippedCosmetics.ContainsKey(type))
        {
            equippedCosmetics.Remove(type);
            OnCosmeticUnequipped?.Invoke(type);
        }
    }

    public bool IsEquipped(CosmeticData cosmeticData)
    {
        if (cosmeticData == null) return false;
        
        if (equippedCosmetics.TryGetValue(cosmeticData.cosmeticsType, out string equippedId))
        {
            return equippedId == cosmeticData.id;
        }
        return false;
    }

    public CosmeticData GetEquippedCosmetic(CosmeticsType type)
    {
        if (equippedCosmetics.TryGetValue(type, out string equippedId))
        {
            return GetCosmeticById(equippedId);
        }
        return null;
    }

    public CosmeticData GetCosmeticById(string id)
    {
        return allCosmetics.FirstOrDefault(c => c.id == id);
    }
    
    public List<CosmeticData> GetCosmeticsByType(CosmeticsType type)
    {
        return allCosmetics.Where(c => c.cosmeticsType == type).ToList();
    }
}
