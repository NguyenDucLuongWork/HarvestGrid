using System;
using System.Collections.Generic;
using UnityEngine;

namespace LgTyLib.Modules.GridSystem
{
    public abstract class CellStyleHandler : ScriptableObject
    {
        /// <summary>
        /// Returns the sprite associated with an enum value.
        /// </summary>
        public abstract Sprite GetSprite(Enum value);

        /// <summary>
        /// Returns the sprite used for disabled cells, regardless of their value.
        /// </summary>
        public abstract Sprite GetDisabledSprite();

        /// <summary>
        /// Checks whether this handler supports the given enum type.
        /// </summary>
        public abstract bool Supports(Type enumType);
    }

    public abstract class CellStyleHandler<TEnum> : CellStyleHandler
        where TEnum : Enum
    {
        [Serializable]
        public class StylePair
        {
            public TEnum value;
            public Sprite sprite;
        }

        [SerializeField]
        private List<StylePair> styles = new();

        [SerializeField]
        private Sprite disabledSprite;

        private Dictionary<TEnum, Sprite> spriteLookup;

        protected virtual void OnEnable()
        {
            BuildLookup();
        }

        private void BuildLookup()
        {
            spriteLookup = new Dictionary<TEnum, Sprite>();

            foreach (var style in styles)
            {
                spriteLookup[style.value] = style.sprite;
            }
        }

        public override Sprite GetSprite(Enum value)
        {
            if (value is not TEnum enumValue)
            {
                Debug.LogWarning(
                    $"[{GetType().Name}] Expected enum type {typeof(TEnum).Name}, " +
                    $"but received {value.GetType().Name}"
                );

                return null;
            }

            if (spriteLookup == null)
            {
                BuildLookup();
            }

            return spriteLookup.TryGetValue(enumValue, out var sprite)
                ? sprite
                : null;
        }

        public Sprite GetSprite(TEnum value)
        {
            if (spriteLookup == null)
            {
                BuildLookup();
            }

            return spriteLookup.TryGetValue(value, out var sprite)
                ? sprite
                : null;
        }

        public override Sprite GetDisabledSprite() => disabledSprite;

        public override bool Supports(Type enumType)
        {
            return enumType == typeof(TEnum);
        }
    }
}