using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace HarvestGrid.Services
{
    public class SortSuggestionItem
    {
        public ItemWithFootprintMono UnplacedItem { get; set; }
        public Vector2Int SuggestedPivot { get; set; }
        public int SuggestedRotation { get; set; }
        
        // The footprint after suggested rotation
        public Footprint FinalFootprint { get; set; }
        
        // If this item was matched against a specific level requirement
        public Footprint MatchedRequirement { get; set; }
        
        public int Score { get; set; }
    }

    public class SortSuggestion
    {
        public List<SortSuggestionItem> Items { get; set; } = new List<SortSuggestionItem>();
        public int TotalScore { get; set; }
        public int UnplacedItemsCount { get; set; }
    }

    /// <summary>
    /// Service for calculating the best inventory sort suggestion without modifying the actual inventory.
    /// Only suggests placement for unplaced items into empty spots on the grid.
    /// </summary>
    public static class InventorySortService
    {
        public static bool EnableDebugLogs = false;

        public static SortSuggestion CalculateBestSort(
            List<ItemWithFootprintMono> unplacedItems,
            StoringSpace storingSpace,
            List<Footprint> requiredFootprints = null)
        {
            if (EnableDebugLogs)
            {
                Debug.Log($"[InventorySortService] Starting SortBest calculation for {unplacedItems?.Count} unplaced items.");
            }

            var suggestion = new SortSuggestion();
            if (unplacedItems == null || unplacedItems.Count == 0 || storingSpace == null)
                return suggestion;

            requiredFootprints ??= new List<Footprint>();
            var unmatchedRequirements = new List<Footprint>(requiredFootprints);

            int gridWidth = storingSpace.Cells.GetLength(0);
            int gridHeight = storingSpace.Cells.GetLength(1);

            // Create a simulated grid for collision detection.
            // true means occupied/unavailable, false means available
            bool[,] grid = new bool[gridWidth, gridHeight];
            
            for (int x = 0; x < gridWidth; x++)
            {
                for (int y = 0; y < gridHeight; y++)
                {
                    if (storingSpace.Cells[x, y] == StoringCellType.Unavailable || 
                        storingSpace.Cells[x, y] == StoringCellType.Occupied)
                    {
                        grid[x, y] = true;
                    }
                }
            }

            // Prepare items to be sorted
            var itemsList = new List<ItemWithFootprintMono>(unplacedItems);

            // Sort items by priority: Larger area first
            itemsList.Sort((a, b) => 
            {
                int areaA = GetFootprintArea(a.Item.Footprint);
                int areaB = GetFootprintArea(b.Item.Footprint);
                return areaB.CompareTo(areaA); // Descending
            });

            foreach (var unplaced in itemsList)
            {
                if (unplaced?.Item?.Footprint == null) continue;

                var bestPlacement = FindBestPlacement(unplaced, grid, gridWidth, gridHeight, unmatchedRequirements);

                if (bestPlacement != null)
                {
                    // Place on simulated grid to block other unplaced items from taking this spot
                    PlaceOnGrid(grid, bestPlacement.FinalFootprint, bestPlacement.SuggestedPivot);

                    // If it matched a requirement, remove that requirement from unmatched list
                    if (bestPlacement.MatchedRequirement != null)
                    {
                        unmatchedRequirements.Remove(bestPlacement.MatchedRequirement);
                    }

                    suggestion.Items.Add(bestPlacement);
                    suggestion.TotalScore += bestPlacement.Score;
                }
                else
                {
                    suggestion.UnplacedItemsCount++;
                }
            }

            if (EnableDebugLogs)
            {
                Debug.Log($"[InventorySortService] Finished SortBest. Total Score: {suggestion.TotalScore}. Placed: {suggestion.Items.Count}/{itemsList.Count}. Unplaced: {suggestion.UnplacedItemsCount}.");
            }

            return suggestion;
        }

        private static SortSuggestionItem FindBestPlacement(
            ItemWithFootprintMono unplacedItem, 
            bool[,] grid, 
            int gridWidth, 
            int gridHeight, 
            List<Footprint> unmatchedRequirements)
        {
            SortSuggestionItem bestSuggestion = null;
            int bestScore = int.MinValue;

            // Try all 4 rotations
            for (int rot = 0; rot < 4; rot++)
            {
                Footprint currentRotatedFootprint = unplacedItem.Item.Footprint.Clone();
                for (int r = 0; r < rot; r++)
                {
                    currentRotatedFootprint.Rotate();
                }

                // Check if this footprint matches any requirement
                Footprint matchedReq = unmatchedRequirements.FirstOrDefault(req => AreFootprintsEqual(currentRotatedFootprint, req));

                // Try all possible pivots (x, y)
                for (int x = 0; x < gridWidth; x++)
                {
                    for (int y = 0; y < gridHeight; y++)
                    {
                        var pivot = new Vector2Int(x, y);

                        if (CanPlace(grid, gridWidth, gridHeight, currentRotatedFootprint, pivot))
                        {
                            int score = CalculatePlacementScore(grid, gridWidth, gridHeight, currentRotatedFootprint, pivot, matchedReq);
                            
                            if (score > bestScore)
                            {
                                bestScore = score;
                                bestSuggestion = new SortSuggestionItem
                                {
                                    UnplacedItem = unplacedItem,
                                    SuggestedPivot = pivot,
                                    SuggestedRotation = rot,
                                    FinalFootprint = currentRotatedFootprint,
                                    MatchedRequirement = matchedReq,
                                    Score = score
                                };
                            }
                        }
                    }
                }
            }

            return bestSuggestion;
        }

        private static bool CanPlace(bool[,] grid, int gridWidth, int gridHeight, Footprint footprint, Vector2Int pivot)
        {
            var positions = footprint.ToSpace(pivot);
            foreach (var pos in positions)
            {
                if (pos.x < 0 || pos.x >= gridWidth || pos.y < 0 || pos.y >= gridHeight)
                    return false; // Out of bounds

                if (grid[pos.x, pos.y])
                    return false; // Collision
            }
            return true;
        }

        private static void PlaceOnGrid(bool[,] grid, Footprint footprint, Vector2Int pivot)
        {
            var positions = footprint.ToSpace(pivot);
            foreach (var pos in positions)
            {
                grid[pos.x, pos.y] = true;
            }
        }

        private static int CalculatePlacementScore(
            bool[,] grid, 
            int gridWidth, 
            int gridHeight, 
            Footprint footprint, 
            Vector2Int pivot, 
            Footprint matchedReq)
        {
            int score = 0;

            // 1. Huge bonus for fulfilling a requirement
            if (matchedReq != null)
            {
                score += 10000;
            }

            // 2. Compactness / Adjacency score
            // We want to reward placements that touch edges of the grid or other items.
            var positions = footprint.ToSpace(pivot);
            int adjacencyScore = 0;

            foreach (var pos in positions)
            {
                // Check 4 neighbors
                Vector2Int[] neighbors = {
                    new Vector2Int(pos.x - 1, pos.y),
                    new Vector2Int(pos.x + 1, pos.y),
                    new Vector2Int(pos.x, pos.y - 1),
                    new Vector2Int(pos.x, pos.y + 1)
                };

                foreach (var n in neighbors)
                {
                    if (n.x < 0 || n.x >= gridWidth || n.y < 0 || n.y >= gridHeight)
                    {
                        // Touches boundary
                        adjacencyScore += 2;
                    }
                    else if (grid[n.x, n.y])
                    {
                        // Touches another item
                        adjacencyScore += 3;
                    }
                }
            }
            score += adjacencyScore;

            // 3. Prefer packing towards the bottom-left (y=0, x=0)
            // Smaller x and y should give a higher score, acting as a tie-breaker.
            int distanceScore = (gridWidth - pivot.x) + (gridHeight - pivot.y);
            score += distanceScore;

            return score;
        }

        private static int GetFootprintArea(Footprint footprint)
        {
            if (footprint == null || footprint.Requiring == null) return 0;
            int count = 0;
            var req = footprint.Requiring;
            int w = req.GetLength(0);
            int h = req.GetLength(1);
            for (int x = 0; x < w; x++)
            {
                for (int y = 0; y < h; y++)
                {
                    if (req[x, y]) count++;
                }
            }
            return count;
        }

        private static bool AreFootprintsEqual(Footprint a, Footprint b)
        {
            if (a == null || b == null) return false;
            var reqA = a.Requiring;
            var reqB = b.Requiring;
            
            if (reqA.GetLength(0) != reqB.GetLength(0) || reqA.GetLength(1) != reqB.GetLength(1))
                return false;

            int w = reqA.GetLength(0);
            int h = reqA.GetLength(1);

            for (int x = 0; x < w; x++)
            {
                for (int y = 0; y < h; y++)
                {
                    if (reqA[x, y] != reqB[x, y])
                        return false;
                }
            }

            return true;
        }
    }
}
