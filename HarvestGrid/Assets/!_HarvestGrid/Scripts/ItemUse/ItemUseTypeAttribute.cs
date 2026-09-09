using System;


[AttributeUsage(AttributeTargets.Class)]
public class ItemUseTypeAttribute : Attribute
{
    public ItemUseType Type { get; }
    public ItemUseTypeAttribute(ItemUseType type) => Type = type;
}