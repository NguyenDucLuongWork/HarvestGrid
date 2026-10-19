using System.Collections.Generic;
using UnityEngine;

namespace HarvestGrid.Services
{
    public class RepackPlacement
    {
        public int Index;                 // index in the footprint list that was passed in
        public Vector2Int Pivot;
        public int Rotation;
        public Footprint FinalFootprint;
    }

    public class RepackResult
    {
        public bool Success;              // everything fit
        public int PlacedCount;
        public int TotalCount;
        public int FocusIndex = -1;       // optional: the footprint the caller cares about (e.g. the item to buy)
        public List<RepackPlacement> Placements = new();

        public RepackPlacement Find(int index)
        {
            foreach (var p in Placements)
                if (p.Index == index) return p;
            return null;
        }
    }

    // NOTE: InventorySortService must be declared `public static partial class` for this file to compile.
    public static partial class InventorySortService
    {
        /// <summary>
        /// Pure-data version of CalculateBestSort: packs the given footprints from an empty grid
        /// (only Unavailable cells block). Nothing in the scene or inventory is touched, so the AI
        /// can ask "what if I re-pack everything plus this new item?".
        /// Uses the same ordering (largest area first), CanPlace and CalculatePlacementScore as
        /// the real sort, so the answer matches what SortAllItems() will do.
        /// </summary>
        public static RepackResult SimulateRepack(IReadOnlyList<Footprint> footprints, StoringCellType[,] cells)
        {
            var result = new RepackResult { TotalCount = footprints?.Count ?? 0 };
            if (footprints == null || cells == null) return result;

            int width = cells.GetLength(0);
            int height = cells.GetLength(1);

            // Occupied cells are treated as free: every stored item is part of `footprints`.
            bool[,] grid = new bool[width, height];
            for (int x = 0; x < width; x++)
                for (int y = 0; y < height; y++)
                    grid[x, y] = cells[x, y] == StoringCellType.Unavailable;

            var order = new List<int>();
            for (int i = 0; i < footprints.Count; i++)
                if (footprints[i] != null) order.Add(i);

            order.Sort((a, b) =>
            {
                int c = GetFootprintArea(footprints[b]).CompareTo(GetFootprintArea(footprints[a]));
                return c != 0 ? c : a.CompareTo(b);
            });

            foreach (int index in order)
            {
                Footprint original = footprints[index];

                if (GetFootprintArea(original) == 0)
                {
                    result.PlacedCount++;
                    continue;
                }

                RepackPlacement best = null;
                int bestScore = int.MinValue;

                for (int rot = 0; rot < 4; rot++)
                {
                    Footprint rotated = original.Clone();
                    for (int r = 0; r < rot; r++) rotated.Rotate();

                    for (int x = 0; x < width; x++)
                    {
                        for (int y = 0; y < height; y++)
                        {
                            var pivot = new Vector2Int(x, y);
                            if (!CanPlace(grid, width, height, rotated, pivot)) continue;

                            int score = CalculatePlacementScore(grid, width, height, rotated, pivot, null);
                            if (score > bestScore)
                            {
                                bestScore = score;
                                best = new RepackPlacement
                                {
                                    Index = index,
                                    Pivot = pivot,
                                    Rotation = rot,
                                    FinalFootprint = rotated
                                };
                            }
                        }
                    }
                }

                if (best != null)
                {
                    PlaceOnGrid(grid, best.FinalFootprint, best.Pivot);
                    result.Placements.Add(best);
                    result.PlacedCount++;
                }
            }

            result.Success = result.PlacedCount == result.TotalCount;
            return result;
        }
    }
}