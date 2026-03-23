using UnityEngine;
using System.Collections.Generic;

namespace Hakaton.Lemmings
{
    public sealed class TerrainMap
    {
        public const int PixelsPerCell = 12;

        private readonly int widthPixels;
        private readonly int heightPixels;
        private readonly TerrainMaterial[] materialPixels;
        private readonly Color32[] baseColorPixels;
        private readonly Color32[] decorativeColorPixels;
        private readonly Texture2D baseTexture;
        private readonly Texture2D decorativeTexture;

        private bool dirty;

        public TerrainMap(ParsedLevel level, Transform parent)
        {
            WidthCells = level.WidthCells;
            HeightCells = level.HeightCells;
            widthPixels = level.WidthCells * PixelsPerCell;
            heightPixels = level.HeightCells * PixelsPerCell;
            materialPixels = new TerrainMaterial[widthPixels * heightPixels];
            baseColorPixels = new Color32[materialPixels.Length];
            decorativeColorPixels = new Color32[materialPixels.Length];

            for (int cellY = 0; cellY < level.HeightCells; cellY++)
            {
                for (int cellX = 0; cellX < level.WidthCells; cellX++)
                {
                    TerrainMaterial material = level.TerrainCells[cellX, cellY];
                    if (material == TerrainMaterial.Empty)
                    {
                        continue;
                    }

                    FillCell(cellX, cellY, material);
                }
            }

            baseTexture = new Texture2D(widthPixels, heightPixels, TextureFormat.RGBA32, false);
            baseTexture.filterMode = FilterMode.Point;
            baseTexture.wrapMode = TextureWrapMode.Clamp;

            decorativeTexture = new Texture2D(widthPixels, heightPixels, TextureFormat.RGBA32, false);
            decorativeTexture.filterMode = FilterMode.Point;
            decorativeTexture.wrapMode = TextureWrapMode.Clamp;

            RefreshAllColors();
            baseTexture.SetPixels32(baseColorPixels);
            baseTexture.Apply();
            decorativeTexture.SetPixels32(decorativeColorPixels);
            decorativeTexture.Apply();

            GameObject terrainObject = new GameObject("Terrain");
            terrainObject.transform.SetParent(parent, false);
            terrainObject.transform.localPosition = Vector3.zero;
            SpriteRenderer terrainRenderer = terrainObject.AddComponent<SpriteRenderer>();
            terrainRenderer.sprite = Sprite.Create(
                baseTexture,
                new Rect(0f, 0f, widthPixels, heightPixels),
                new Vector2(0f, 0f),
                PixelsPerCell);
            terrainRenderer.sortingOrder = 0;

            GameObject decorativeObject = new GameObject("DecorativeOverlay");
            decorativeObject.transform.SetParent(parent, false);
            decorativeObject.transform.localPosition = Vector3.zero;
            SpriteRenderer decorativeRenderer = decorativeObject.AddComponent<SpriteRenderer>();
            decorativeRenderer.sprite = Sprite.Create(
                decorativeTexture,
                new Rect(0f, 0f, widthPixels, heightPixels),
                new Vector2(0f, 0f),
                PixelsPerCell);
            decorativeRenderer.sortingOrder = 12;
        }

        public int WidthCells { get; }
        public int HeightCells { get; }

        public bool HasAnyDestructibleMaterialInRect(Rect rect)
        {
            return SampleRect(rect, TerrainMaterial.Platform);
        }

        public bool OverlapsSolid(Rect rect)
        {
            return SampleRect(rect, TerrainMaterial.Platform, TerrainMaterial.Wall);
        }

        public bool OverlapsIndestructible(Rect rect)
        {
            return SampleRect(rect, TerrainMaterial.Wall);
        }

        public bool IsGrounded(Vector2 position, float width, float probeDepth = 0.08f)
        {
            Rect rect = new Rect(position.x - width * 0.45f, position.y - probeDepth, width * 0.9f, probeDepth);
            return OverlapsSolid(rect);
        }

        public bool TryGetPlatformBodyAtWorld(Vector2 worldPoint, out int bodyId, out float bottomY)
        {
            bodyId = -1;
            bottomY = 0f;

            if (!TryFindPlatformPixelNearWorld(worldPoint, out int startX, out int startY))
            {
                return false;
            }

            bool[] visited = new bool[materialPixels.Length];
            Queue<Vector2Int> queue = new Queue<Vector2Int>();
            queue.Enqueue(new Vector2Int(startX, startY));
            visited[ToIndex(startX, startY)] = true;

            int minY = startY;
            int minX = startX;

            while (queue.Count > 0)
            {
                Vector2Int pixel = queue.Dequeue();
                minY = Mathf.Min(minY, pixel.y);
                minX = Mathf.Min(minX, pixel.x);

                TryEnqueuePlatformPixel(pixel.x + 1, pixel.y, visited, queue);
                TryEnqueuePlatformPixel(pixel.x - 1, pixel.y, visited, queue);
                TryEnqueuePlatformPixel(pixel.x, pixel.y + 1, visited, queue);
                TryEnqueuePlatformPixel(pixel.x, pixel.y - 1, visited, queue);
            }

            bodyId = minY * widthPixels + minX;
            bottomY = minY / (float)PixelsPerCell;
            return true;
        }

        public void DigCircle(Vector2 worldCenter, float radius)
        {
            int minX = Mathf.Max(0, Mathf.FloorToInt((worldCenter.x - radius) * PixelsPerCell));
            int maxX = Mathf.Min(widthPixels - 1, Mathf.CeilToInt((worldCenter.x + radius) * PixelsPerCell));
            int minY = Mathf.Max(0, Mathf.FloorToInt((worldCenter.y - radius) * PixelsPerCell));
            int maxY = Mathf.Min(heightPixels - 1, Mathf.CeilToInt((worldCenter.y + radius) * PixelsPerCell));
            float radiusSquared = radius * radius;

            for (int y = minY; y <= maxY; y++)
            {
                float worldY = (y + 0.5f) / PixelsPerCell;
                for (int x = minX; x <= maxX; x++)
                {
                    float worldX = (x + 0.5f) / PixelsPerCell;
                    Vector2 delta = new Vector2(worldX - worldCenter.x, worldY - worldCenter.y);
                    if (delta.sqrMagnitude > radiusSquared)
                    {
                        continue;
                    }

                    int index = ToIndex(x, y);
                    if (materialPixels[index] != TerrainMaterial.Platform)
                    {
                        continue;
                    }

                    materialPixels[index] = TerrainMaterial.Empty;
                    baseColorPixels[index] = new Color32(0, 0, 0, 0);
                    decorativeColorPixels[index] = new Color32(0, 0, 0, 0);
                    dirty = true;
                }
            }
        }

        public void ApplyPendingVisuals()
        {
            if (!dirty)
            {
                return;
            }

            baseTexture.SetPixels32(baseColorPixels);
            baseTexture.Apply(false);
            decorativeTexture.SetPixels32(decorativeColorPixels);
            decorativeTexture.Apply(false);
            dirty = false;
        }

        private void FillCell(int cellX, int cellY, TerrainMaterial material)
        {
            int startX = cellX * PixelsPerCell;
            int startY = cellY * PixelsPerCell;
            for (int y = 0; y < PixelsPerCell; y++)
            {
                for (int x = 0; x < PixelsPerCell; x++)
                {
                    int index = ToIndex(startX + x, startY + y);
                    materialPixels[index] = material;
                }
            }
        }

        private void RefreshAllColors()
        {
            for (int index = 0; index < materialPixels.Length; index++)
            {
                baseColorPixels[index] = BaseColorFor(materialPixels[index]);
                decorativeColorPixels[index] = DecorativeColorFor(materialPixels[index]);
            }
        }

        private bool TryFindPlatformPixelNearWorld(Vector2 worldPoint, out int pixelX, out int pixelY)
        {
            pixelX = Mathf.Clamp(Mathf.FloorToInt(worldPoint.x * PixelsPerCell), 0, widthPixels - 1);
            pixelY = Mathf.Clamp(Mathf.FloorToInt(worldPoint.y * PixelsPerCell), 0, heightPixels - 1);

            if (materialPixels[ToIndex(pixelX, pixelY)] == TerrainMaterial.Platform)
            {
                return true;
            }

            int searchDepth = Mathf.Min(PixelsPerCell * 2, heightPixels - 1);
            for (int offset = 1; offset <= searchDepth; offset++)
            {
                int candidateY = pixelY - offset;
                if (candidateY < 0)
                {
                    break;
                }

                if (materialPixels[ToIndex(pixelX, candidateY)] == TerrainMaterial.Platform)
                {
                    pixelY = candidateY;
                    return true;
                }
            }

            return false;
        }

        private void TryEnqueuePlatformPixel(int x, int y, bool[] visited, Queue<Vector2Int> queue)
        {
            if (x < 0 || x >= widthPixels || y < 0 || y >= heightPixels)
            {
                return;
            }

            int index = ToIndex(x, y);
            if (visited[index] || materialPixels[index] != TerrainMaterial.Platform)
            {
                return;
            }

            visited[index] = true;
            queue.Enqueue(new Vector2Int(x, y));
        }

        private bool SampleRect(Rect rect, params TerrainMaterial[] materials)
        {
            int minX = Mathf.Max(0, Mathf.FloorToInt(rect.xMin * PixelsPerCell));
            int maxX = Mathf.Min(widthPixels - 1, Mathf.CeilToInt(rect.xMax * PixelsPerCell));
            int minY = Mathf.Max(0, Mathf.FloorToInt(rect.yMin * PixelsPerCell));
            int maxY = Mathf.Min(heightPixels - 1, Mathf.CeilToInt(rect.yMax * PixelsPerCell));

            if (maxX < minX || maxY < minY)
            {
                return false;
            }

            for (int y = minY; y <= maxY; y++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    TerrainMaterial material = materialPixels[ToIndex(x, y)];
                    for (int i = 0; i < materials.Length; i++)
                    {
                        if (material == materials[i])
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        private static Color32 BaseColorFor(TerrainMaterial material)
        {
            switch (material)
            {
                case TerrainMaterial.Platform:
                    return new Color32(164, 112, 78, 255);
                case TerrainMaterial.Wall:
                    return new Color32(96, 117, 149, 255);
                default:
                    return new Color32(0, 0, 0, 0);
            }
        }

        private static Color32 DecorativeColorFor(TerrainMaterial material)
        {
            return material == TerrainMaterial.DecorativePlatform
                ? new Color32(196, 158, 108, 255)
                : new Color32(0, 0, 0, 0);
        }

        private int ToIndex(int x, int y)
        {
            return y * widthPixels + x;
        }
    }
}
