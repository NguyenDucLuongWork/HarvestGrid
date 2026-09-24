using UnityEngine;

/// <summary>
/// Non-generic base so ItemInfoPanel can hold a single collection of mixed panel types
/// (Dictionary<ItemUseType, ItemUsePanel>) and call Bind polymorphically.
/// </summary>
public abstract class ItemUsePanel : MonoBehaviour
{
    public abstract void Bind(ItemUse use);
}

/// <summary>
/// Strongly-typed base for a specific ItemUse subclass. Concrete panels (HarvestUsePanel,
/// AddPlantUsePanel, ...) inherit from this and only need to implement OnBind to wire up
/// their UI, with Use already cast to the correct type.
/// </summary>
public abstract class ItemUsePanel<T> : ItemUsePanel where T : ItemUse
{
    protected T Use { get; private set; }

    public sealed override void Bind(ItemUse use)
    {
        if (use is not T typed)
        {
            Debug.LogError($"{GetType().Name} received {use?.GetType().Name ?? "null"}, expected {typeof(T).Name}.");
            return;
        }

        Use = typed;
        OnBind();
    }

    protected abstract void OnBind();
}