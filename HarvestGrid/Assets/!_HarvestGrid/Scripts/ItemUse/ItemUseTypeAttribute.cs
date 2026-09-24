using System;
using System.Collections.Generic;

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public class ItemUseTypeAttribute : Attribute
{
    public ItemUseType Type { get; }

    public ItemUseTypeAttribute(ItemUseType type)
    {
        Type = type;
    }

    private static readonly Dictionary<Type, ItemUseType> cache = new();

    /// <summary>
    /// Returns the ItemUseType declared on the given ItemUse subclass via [ItemUseType(...)].
    /// Throws if the attribute is missing so misconfigured subclasses fail loudly.
    /// </summary>
    public static ItemUseType Resolve(Type itemUseType)
    {
        if (cache.TryGetValue(itemUseType, out var cached))
            return cached;

        var attr = (ItemUseTypeAttribute)GetCustomAttribute(itemUseType, typeof(ItemUseTypeAttribute));
        if (attr == null)
        {
            throw new InvalidOperationException(
                $"{itemUseType.Name} is missing an [ItemUseType(...)] attribute.");
        }

        cache[itemUseType] = attr.Type;
        return attr.Type;
    }

    public static ItemUseType Resolve(ItemUse use) => Resolve(use.GetType());
}