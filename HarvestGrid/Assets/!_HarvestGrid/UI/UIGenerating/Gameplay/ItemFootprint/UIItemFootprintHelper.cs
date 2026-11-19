using UnityEngine;
using UnityEngine.UIElements;

public static class UIItemFootprintHelper
{
    public static void BindFootprint(VisualElement root, ItemFootprintView data, float cellSize = 28f, float gap = 3f)
    {
        if (root == null || data == null) return;

        // Assign dataSource for UI Toolkit DataBinding
        root.dataSource = data;

        var fpRoot = root.Q<VisualElement>("item-footprint-root") ?? root;
        fpRoot.dataSource = data;
        fpRoot.style.paddingLeft = 0;
        fpRoot.style.paddingRight = 0;
        fpRoot.style.paddingTop = 0;
        fpRoot.style.paddingBottom = 0;
        fpRoot.style.marginLeft = 0;
        fpRoot.style.marginRight = 0;
        fpRoot.style.marginTop = 0;
        fpRoot.style.marginBottom = 0;

        int w = Mathf.Max(1, data.width);
        int h = Mathf.Max(1, data.height);
        float totalW = w * cellSize + (w - 1) * gap;
        float totalH = h * cellSize + (h - 1) * gap;

        root.style.width = totalW;
        root.style.height = totalH;
        fpRoot.style.width = totalW;
        fpRoot.style.height = totalH;

        var lblIcon = root.Q<Label>("lbl-fp-icon");
        var imgIcon = root.Q<Image>("img-fp-icon");
        var imgVe = root.Q<VisualElement>("img-fp-icon");
        var grid = root.Q<VisualElement>("fp-grid");

        // Icon sprite vs emoji label setup
        if (data.iconSprite != null)
        {
            if (imgIcon != null)
            {
                imgIcon.sprite = data.iconSprite;
                imgIcon.style.display = DisplayStyle.Flex;
            }
            else if (imgVe != null)
            {
                imgVe.style.backgroundImage = new StyleBackground(data.iconSprite);
                imgVe.style.display = DisplayStyle.Flex;
            }
            if (lblIcon != null) lblIcon.style.display = DisplayStyle.None;
        }
        else
        {
            if (imgIcon != null) imgIcon.style.display = DisplayStyle.None;
            else if (imgVe != null) imgVe.style.display = DisplayStyle.None;

            if (lblIcon != null)
            {
                lblIcon.style.display = DisplayStyle.Flex;
                lblIcon.text = string.IsNullOrEmpty(data.icon) ? "🌾" : data.icon;
            }
        }

        // Footprint cells grid
        if (grid != null)
        {
            grid.Clear();
            grid.style.paddingLeft = 0;
            grid.style.paddingRight = 0;
            grid.style.paddingTop = 0;
            grid.style.paddingBottom = 0;
            grid.style.marginLeft = 0;
            grid.style.marginRight = 0;
            grid.style.marginTop = 0;
            grid.style.marginBottom = 0;
            grid.style.width = totalW;
            grid.style.height = totalH;

            for (int r = 0; r < h; r++)
            {
                var row = new VisualElement();
                row.AddToClassList("fp-row");
                row.style.flexDirection = FlexDirection.Row;
                row.style.height = cellSize;
                row.style.marginBottom = (r < h - 1) ? gap : 0;
                row.style.paddingLeft = 0;
                row.style.paddingRight = 0;
                row.style.paddingTop = 0;
                row.style.paddingBottom = 0;
                row.style.marginLeft = 0;
                row.style.marginRight = 0;
                row.style.marginTop = 0;

                for (int c = 0; c < w; c++)
                {
                    var cell = new VisualElement();
                    cell.AddToClassList("fp-cell");
                    cell.style.width = cellSize;
                    cell.style.height = cellSize;
                    cell.style.marginRight = (c < w - 1) ? gap : 0;
                    cell.style.paddingLeft = 0;
                    cell.style.paddingRight = 0;
                    cell.style.paddingTop = 0;
                    cell.style.paddingBottom = 0;
                    cell.style.marginLeft = 0;
                    cell.style.marginTop = 0;
                    cell.style.marginBottom = 0;

                    bool occupied = data.IsCellOccupied(c, r);
                    if (occupied)
                    {
                        cell.AddToClassList("fp-cell-on");
                    }
                    else
                    {
                        cell.AddToClassList("fp-cell-off");
                    }
                    row.Add(cell);
                }
                grid.Add(row);
            }
        }

        // Rotation of the icon to match orientation
        var iconWrap = root.Q<VisualElement>("fp-icon-wrap");
        if (iconWrap != null)
        {
            iconWrap.style.rotate = new Rotate(Angle.Degrees(data.rotation * 90f));
        }
        root.style.rotate = new Rotate(Angle.Degrees(0f));

        // Cooldown and progress update
        UpdateProgress(root, data.progress);
    }

    public static void UpdateProgress(VisualElement root, float progress)
    {
        if (root == null) return;
        float clamped = Mathf.Clamp01(progress);

        // Footprint cooldown veil (shades cells until complete)
        var cdFill = root.Q<VisualElement>("fp-cd-fill");
        if (cdFill != null)
        {
            float remainingPct = (1f - clamped) * 100f;
            cdFill.style.height = Length.Percent(remainingPct);
        }
    }
}
