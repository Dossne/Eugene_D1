using System.Collections.Generic;
using UnityEngine;

namespace Hakaton.Lemmings
{
    public static class PixelArtFactory
    {
        private static readonly Dictionary<string, Sprite> SpriteCache = new Dictionary<string, Sprite>();

        private static readonly Color32 Transparent = new Color32(0, 0, 0, 0);
        private static readonly Color32 Skin = new Color32(244, 210, 170, 255);
        private static readonly Color32 Hair = new Color32(58, 180, 88, 255);
        private static readonly Color32 Tunic = new Color32(65, 93, 209, 255);
        private static readonly Color32 Boot = new Color32(83, 56, 42, 255);
        private static readonly Color32 Metal = new Color32(187, 196, 214, 255);
        private static readonly Color32 Wood = new Color32(153, 108, 63, 255);
        private static readonly Color32 Dark = new Color32(20, 24, 40, 255);
        private static readonly Color32 Glow = new Color32(255, 237, 124, 255);

        public static Sprite CreateLemmingAtlasSprite(int frame)
        {
            Texture2D texture = LoadSpriteTexture("lemming_atlas");
            if (texture != null)
            {
                int clampedFrame = Mathf.Clamp(frame, 0, 5);
                const int frameWidth = 128;
                const int frameHeight = 128;
                int frameX = frameWidth * clampedFrame;
                return GetOrCreateTextureSprite(
                    $"lemming_atlas_frame_{clampedFrame}",
                    texture,
                    new Rect(frameX, 0f, frameWidth, frameHeight),
                    new Vector2(0.5f, 0f));
            }

            return CreateLegacyLemmingWalkSprite(Mathf.Clamp(frame, 0, 1));
        }

        private static Sprite CreateLegacyLemmingWalkSprite(int frame)
        {
            return GetOrCreateSprite(
                $"lemming_walk_{frame}",
                frame == 0
                    ? new[]
                    {
                        "....hh....",
                        "...hhhh...",
                        "...hssh...",
                        "...tttt...",
                        "..tttttt..",
                        "..tttttt..",
                        "...t..t...",
                        "..b....b..",
                        ".bb....bb.",
                        ".........."
                    }
                    : new[]
                    {
                        "....hh....",
                        "...hhhh...",
                        "...hssh...",
                        "...tttt...",
                        "..tttttt..",
                        "..tttttt..",
                        "...t..t...",
                        ".b......b.",
                        "..bb..bb..",
                        ".........."
                    });
        }

        private static Sprite CreateLegacyLemmingDigSprite(int frame)
        {
            return GetOrCreateSprite(
                $"lemming_dig_{frame}",
                frame == 0
                    ? new[]
                    {
                        "....hh....",
                        "...hhhh...",
                        "...hssh...",
                        "...ttttm..",
                        "..tttttm..",
                        "..tttttw..",
                        "...t..m...",
                        "..b...m...",
                        ".bb.......",
                        ".........."
                    }
                    : new[]
                    {
                        "....hh....",
                        "...hhhh...",
                        "...hssh...",
                        "...tttt...",
                        "..tttttwm.",
                        "..tttttm..",
                        "...t..m...",
                        "..b...m...",
                        ".bb.......",
                        ".........."
                    });
        }

        public static Sprite CreateHighlightSprite()
        {
            return GetOrCreateSprite(
                "highlight",
                new[]
                {
                    "...yyyy...",
                    "..y....y..",
                    ".y......y.",
                    "y........y",
                    "y........y",
                    "y........y",
                    "y........y",
                    ".y......y.",
                    "..y....y..",
                    "...yyyy..."
                });
        }

        public static Sprite CreateSpawnSprite()
        {
            Texture2D texture = LoadSpriteTexture("spawn_point");
            if (texture != null)
            {
                int frameWidth = texture.width / 2;
                return GetOrCreateTextureSprite(
                    "spawn_point_frame_0",
                    texture,
                    new Rect(0f, 0f, frameWidth, texture.height),
                    new Vector2(0.5f, 0.5f));
            }

            return GetOrCreateSprite(
                "spawn",
                new[]
                {
                    "..........",
                    "..dddddd..",
                    ".dddddddd.",
                    ".dd....dd.",
                    ".d......d.",
                    ".d......d.",
                    ".dd....dd.",
                    ".dddddddd.",
                    "..dddddd..",
                    ".........."
                },
                new Dictionary<char, Color32>
                {
                    { '.', Transparent },
                    { 'd', new Color32(32, 20, 26, 255) }
                });
        }

        public static Sprite CreateSpawnSprite(int frame)
        {
            Texture2D texture = LoadSpriteTexture("spawn_point");
            if (texture != null)
            {
                int clampedFrame = Mathf.Clamp(frame, 0, 1);
                int frameWidth = texture.width / 2;
                int frameX = frameWidth * clampedFrame;
                return GetOrCreateTextureSprite(
                    $"spawn_point_frame_{clampedFrame}",
                    texture,
                    new Rect(frameX, 0f, frameWidth, texture.height),
                    new Vector2(0.5f, 0.5f));
            }

            return CreateSpawnSprite();
        }

        public static Sprite CreateExitSprite()
        {
            const string cacheKey = "exit_point_asset";
            if (SpriteCache.TryGetValue(cacheKey, out Sprite cachedExitSprite))
            {
                return cachedExitSprite;
            }

            Texture2D texture = LoadSpriteTexture("exit_point");
            if (texture != null)
            {
                Sprite loadedSprite = Sprite.Create(
                    texture,
                    new Rect(0f, 0f, texture.width, texture.height),
                    new Vector2(0.5f, 0f),
                    texture.height);
                SpriteCache[cacheKey] = loadedSprite;
                return loadedSprite;
            }

            return GetOrCreateSprite(
                "exit",
                new[]
                {
                    "..dddddd..",
                    ".dggggggd.",
                    ".dgddddgd.",
                    ".dgddddgd.",
                    ".dgddddgd.",
                    ".dgddddgd.",
                    ".dgddddgd.",
                    ".dggggggd.",
                    ".dddddddd.",
                    ".........."
                },
                new Dictionary<char, Color32>
                {
                    { '.', Transparent },
                    { 'd', new Color32(120, 66, 44, 255) },
                    { 'g', new Color32(91, 193, 140, 255) }
                });
        }

        public static Sprite CreatePickaxeSprite()
        {
            const string cacheKey = "axe_asset";
            if (SpriteCache.TryGetValue(cacheKey, out Sprite cachedAxeSprite))
            {
                return cachedAxeSprite;
            }

            Texture2D texture = LoadSpriteTexture("axe");
            if (texture != null)
            {
                Sprite loadedSprite = Sprite.Create(
                    texture,
                    new Rect(0f, 0f, texture.width, texture.height),
                    new Vector2(0.5f, 0.5f),
                    texture.height);
                SpriteCache[cacheKey] = loadedSprite;
                return loadedSprite;
            }

            return GetOrCreateSprite(
                "pickaxe",
                new[]
                {
                    "....mm....",
                    "...mmmm...",
                    "....ww....",
                    "....ww....",
                    "...w......",
                    "..w.......",
                    ".w........",
                    "w.........",
                    "..........",
                    ".........."
                });
        }

        public static Sprite CreateSpikeSprite(SpikeOrientation orientation)
        {
            const string cacheKey = "thorns_asset";
            if (SpriteCache.TryGetValue(cacheKey, out Sprite cachedSpikeSprite))
            {
                return cachedSpikeSprite;
            }

            Texture2D texture = LoadSpriteTexture("thorns");
            if (texture != null)
            {
                Sprite loadedSprite = Sprite.Create(
                    texture,
                    new Rect(0f, 0f, texture.width, texture.height),
                    new Vector2(0.5f, 0f),
                    texture.height);
                SpriteCache[cacheKey] = loadedSprite;
                return loadedSprite;
            }

            return GetOrCreateSprite(
                "spike_fallback",
                new[]
                {
                    "....tt....",
                    "...tttt...",
                    "..tttttt..",
                    ".tttttttt.",
                    "tt.tt.tt.t",
                    "t..t..t..t",
                    "..........",
                    "..........",
                    "..........",
                    ".........."
                },
                new Dictionary<char, Color32>
                {
                    { '.', Transparent },
                    { 't', Metal }
                });
        }

        public static Sprite CreateStarEffectSprite()
        {
            const string cacheKey = "star_effect_asset";
            if (SpriteCache.TryGetValue(cacheKey, out Sprite cachedStarSprite))
            {
                return cachedStarSprite;
            }

            Texture2D texture = LoadSpriteTexture("star");
            if (texture != null)
            {
                Sprite loadedSprite = Sprite.Create(
                    texture,
                    new Rect(0f, 0f, texture.width, texture.height),
                    new Vector2(0.5f, 0.5f),
                    texture.height);
                SpriteCache[cacheKey] = loadedSprite;
                return loadedSprite;
            }

            return GetOrCreateSprite(
                "star_effect_fallback",
                new[]
                {
                    ".....y.....",
                    ".....y.....",
                    "..y..y..y..",
                    "...yyyyy...",
                    "yyyyyyyyyyy",
                    "...yyyyy...",
                    "..y..y..y..",
                    ".....y.....",
                    ".....y.....",
                    "..........."
                });
        }

        public static Sprite CreateDeathSkullSprite()
        {
            const string cacheKey = "death_skull_asset";
            if (SpriteCache.TryGetValue(cacheKey, out Sprite cachedSkullSprite))
            {
                return cachedSkullSprite;
            }

            Texture2D texture = LoadSpriteTexture("skull");
            if (texture != null)
            {
                Sprite loadedSprite = Sprite.Create(
                    texture,
                    new Rect(0f, 0f, texture.width, texture.height),
                    new Vector2(0.5f, 0.5f),
                    texture.height);
                SpriteCache[cacheKey] = loadedSprite;
                return loadedSprite;
            }

            return GetOrCreateSprite(
                "death_skull_fallback",
                new[]
                {
                    "...rrrr...",
                    "..rrrrrr..",
                    ".rrr..rrr.",
                    ".rr....rr.",
                    ".rrr..rrr.",
                    "..rrrrrr..",
                    "..rr..rr..",
                    ".rr....rr.",
                    "..........",
                    ".........."
                },
                new Dictionary<char, Color32>
                {
                    { '.', Transparent },
                    { 'r', new Color32(208, 48, 48, 255) }
                });
        }

        public static Sprite CreateBackgroundSprite()
        {
            const string cacheKey = "background_asset";
            if (SpriteCache.TryGetValue(cacheKey, out Sprite cachedBackgroundSprite))
            {
                return cachedBackgroundSprite;
            }

            Texture2D texture = LoadSpriteTexture("back");
            if (texture != null)
            {
                Sprite loadedSprite = Sprite.Create(
                    texture,
                    new Rect(0f, 0f, texture.width, texture.height),
                    new Vector2(0.5f, 0.5f),
                    texture.height);
                SpriteCache[cacheKey] = loadedSprite;
                return loadedSprite;
            }

            return GetOrCreateSprite(
                "background_fallback",
                new[]
                {
                    "abababab",
                    "babababa",
                    "abababab",
                    "babababa",
                    "abababab",
                    "babababa",
                    "abababab",
                    "babababa"
                },
                new Dictionary<char, Color32>
                {
                    { 'a', new Color32(30, 43, 67, 255) },
                    { 'b', new Color32(39, 58, 91, 255) }
                });
        }

        public static Font GetUIFont()
        {
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            return font != null ? font : Resources.GetBuiltinResource<Font>("Arial.ttf");
        }

        private static Sprite GetOrCreateSprite(string cacheKey, string[] rows, Dictionary<char, Color32> paletteOverride = null)
        {
            if (SpriteCache.TryGetValue(cacheKey, out Sprite cachedSprite))
            {
                return cachedSprite;
            }

            Dictionary<char, Color32> palette = paletteOverride ?? new Dictionary<char, Color32>
            {
                { '.', Transparent },
                { 'h', Hair },
                { 's', Skin },
                { 't', Tunic },
                { 'b', Boot },
                { 'm', Metal },
                { 'w', Wood },
                { 'd', Dark },
                { 'y', Glow }
            };

            int width = rows[0].Length;
            int height = rows.Length;
            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Point;
            texture.wrapMode = TextureWrapMode.Clamp;

            Color32[] pixels = new Color32[width * height];
            for (int y = 0; y < height; y++)
            {
                string row = rows[height - 1 - y];
                for (int x = 0; x < width; x++)
                {
                    char symbol = row[x];
                    pixels[y * width + x] = palette.TryGetValue(symbol, out Color32 color) ? color : Transparent;
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply();

            Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, width, height), new Vector2(0.5f, 0f), height);
            SpriteCache[cacheKey] = sprite;
            return sprite;
        }

        private static Sprite GetOrCreateTextureSprite(string cacheKey, Texture2D texture, Rect rect, Vector2 pivot)
        {
            if (SpriteCache.TryGetValue(cacheKey, out Sprite cachedSprite))
            {
                return cachedSprite;
            }

            Sprite sprite = Sprite.Create(texture, rect, pivot, rect.height);
            SpriteCache[cacheKey] = sprite;
            return sprite;
        }

        private static Texture2D LoadSpriteTexture(string assetName)
        {
            Texture2D texture = Resources.Load<Texture2D>($"sprites/{assetName}");
            if (texture != null)
            {
                texture.filterMode = FilterMode.Point;
                texture.wrapMode = TextureWrapMode.Clamp;
            }

            return texture;
        }
    }
}
