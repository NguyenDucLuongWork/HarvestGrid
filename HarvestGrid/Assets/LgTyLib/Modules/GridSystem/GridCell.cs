using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace LgTyLib.Modules.GridSystem
{
    [RequireComponent(typeof(Image))]
    public class GridCell : MonoBehaviour, IPointerClickHandler
    {
        private Image image;
        public bool IsEnable { get; private set; } = true;
        public int X { get; private set; }
        public int Y { get; private set; }

        public event System.Action<GridCell> OnClicked;

        private void Awake()
        {
            image = GetComponent<Image>();
        }

        internal void SetCoordinates(int x, int y)
        {
            X = x;
            Y = y;
        }

        internal void SetEnable(bool enable)
        {
            IsEnable = enable;
        }

        public void UpdateCellSprite(Sprite sprite)
        {
            image.sprite = sprite;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            OnClicked?.Invoke(this);
        }
    }
}