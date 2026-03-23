using UnityEngine;

namespace Hakaton.Lemmings
{
    public sealed class TerrainMap
    {
        public const int PixelsPerCell = 12;

        private readonly int widthPixels;
        private readonly int heightPixels;
        private readonly TerrainMaterial[] materialPixels;
        private readonly Color32[] colorPixels;
        private readonly Texture2D texture;

        private bool dirty;

        public TerrainMap(ParsedLevel level, Transform parent)
        {
            WidthCells = level.WidthCells;
            HeightCells = level.HeightCells;
            widthPixels = level.WidthCells * PixelsPerCell;
            heightPixels = level.HeightCells * PixelsPerCell;
            materialPixels = new TerrainMaterial[widthPixels * heightPixels];
            colorPixels = new Color32[materialPixels.Length];

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

            texture = new Texture2D(widthPixels, heightPixels, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Point;
            texture.wrapMode = TextureWrapMode.Clamp;
            RefreshAllColors();
            texture.SetPixels32(colorPixels);
            texture.Apply();

            GameObject terrainObject = new GameObject("Terrain");
            terrainObject.transform.SetParent(parent, false);
            terrainObject.transform.localPosition = Vector3.zero;
            SpriteRenderer spriteRenderer = terrainObject.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, widthPixels, heightPixels),
                new Vector2(0f, 0f),
                PixelsPerCell);
            spriteRenderer.sortingOrder = 0;
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
                    colorPixels[index] = new Color32(0, 0, 0, 0);
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

            texture.SetPixels32(colorPixels);
            texture.Apply(false);
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
                colorPixels[index] = ColorFor(materialPixels[index]);
            }
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

        private static Color32 ColorFor(TerrainMaterial material)
        {
            switch (material)
            {
                case TerrainMaterial.Platform:
                    return new Color32(164, 112, 78, 255);
                case TerrainMaterial.DecorativePlatform:
                    return new Color32(196, 158, 108, 255);
                case TerrainMaterial.Wall:
                    return new Color32(96, 117, 149, 255);
                default:
                    return new Color32(0, 0, 0, 0);
            }
        }

        private int ToIndex(int x, int y)
        {
            return y * widthPixels + x;
        }
    }
}
