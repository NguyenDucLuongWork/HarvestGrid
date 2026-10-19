using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

public static class ItemUseFactory
{
    private static readonly Dictionary<ItemUseType, Type> map = new();

    static ItemUseFactory()
    {
        var baseType = typeof(ItemUse);
        foreach (var t in UnityEngine.Assemblies.CurrentAssemblies.GetLoadedAssemblies()                     
            .SelectMany(a => a.GetTypes())
            .Where(t => baseType.IsAssignableFrom(t) && !t.IsAbstract))
        {
            var attr = t.GetCustomAttribute<ItemUseTypeAttribute>();
            if (attr == null)
            {
                Debug.LogWarning($"{t.Name} kế thừa ItemUse nhưng thiếu [ItemUseType].");
                continue;
            }

            if (!map.TryAdd(attr.Type, t))
                Debug.LogError($"Trùng ItemUseType {attr.Type} giữa {map[attr.Type].Name} và {t.Name}");
        }
    }

    public static ItemUse Create(ItemUseType type)
    {
        if (!map.TryGetValue(type, out var t))
        {
            Debug.LogError($"Không tìm thấy ItemUse cho type {type}");
            return null;
        }
        return (ItemUse)Activator.CreateInstance(t);
    }
}