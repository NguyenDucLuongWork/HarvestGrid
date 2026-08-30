using UnityEngine;
using UnityEngine.UI;

namespace LgTyLib.Modules.SpriteLocalCoords
{
    [ExecuteInEditMode]
    public class SpriteLocalCoords : MonoBehaviour
    {
        [SerializeField] private bool usePropertyBlock = true;
        // For URP 2023+ likely want set to false, as sprites support SRP batcher now
        // For Built-in RP / prior URP versions, may be better to keep true
        // NOTE: ignored for UI Image — property blocks aren't supported by CanvasRenderer,
        // so Image always uses a material instance.

        private SpriteRenderer spriteRenderer;
        private Image image;
        private MaterialPropertyBlock mpb;
        private Material instancedMaterial;

        void OnEnable()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            image = GetComponent<Image>();

            RecalculateSpriteLocalUV();
        }

        void OnDisable()
        {
            if (image != null)
            {
                if (Application.isPlaying && instancedMaterial != null)
                {
                    image.material = null; // revert to default material
                    Destroy(instancedMaterial);
                    instancedMaterial = null;
                }
            }
            else if (spriteRenderer != null)
            {
                if (!usePropertyBlock && Application.isPlaying)
                {
                    Destroy(spriteRenderer.material);
                }
            }
        }

        /// <summary>
        /// Recalculates and reapplies the _UVRemap vector from the current sprite.
        /// Call this whenever the sprite on the SpriteRenderer/Image is changed at runtime,
        /// since the remap is otherwise only computed once in OnEnable.
        /// </summary>
        [ContextMenu("RecalculateSpriteLocalUV")]
        public void RecalculateSpriteLocalUV()
        {
            if (spriteRenderer == null && image == null)
            {
                // Support calling this before OnEnable has run (e.g. from Awake elsewhere)
                spriteRenderer = GetComponent<SpriteRenderer>();
                image = GetComponent<Image>();
            }

            Sprite sprite = GetSprite();
            if (sprite == null) return;

            Rect rect = sprite.textureRect;
            Vector2 texelSize = sprite.texture.texelSize;
            Vector4 uvRemap = new(
                rect.x * texelSize.x,
                rect.y * texelSize.y,
                rect.width * texelSize.x,
                rect.height * texelSize.y
            );

            ApplyUVRemap(uvRemap);
        }

        private void ApplyUVRemap(Vector4 uvRemap)
        {
            if (image != null)
            {
                // UI Image: CanvasRenderer doesn't support MaterialPropertyBlock,
                // so we must use a material instance.
                if (Application.isPlaying)
                {
                    if (instancedMaterial == null)
                        instancedMaterial = image.material = new Material(image.material);

                    instancedMaterial.SetVector("_UVRemap", uvRemap);
                }
                else
                {
                    // In edit mode, set directly on the shared material to preview.
                    if (image.material != null)
                        image.material.SetVector("_UVRemap", uvRemap);
                }
            }
            else if (spriteRenderer != null)
            {
                if (!usePropertyBlock && Application.isPlaying)
                {
                    spriteRenderer.material.SetVector("_UVRemap", uvRemap);
                }
                else
                {
                    mpb ??= new MaterialPropertyBlock();
                    spriteRenderer.GetPropertyBlock(mpb);
                    mpb.SetVector("_UVRemap", uvRemap);
                    spriteRenderer.SetPropertyBlock(mpb);
                }
            }
        }

        private Sprite GetSprite()
        {
            if (image != null) return image.sprite;
            if (spriteRenderer != null) return spriteRenderer.sprite;
            return null;
        }
    }
}