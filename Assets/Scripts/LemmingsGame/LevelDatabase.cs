using System.Collections.Generic;
using UnityEngine;

namespace Hakaton.Lemmings
{
    public enum TerrainMaterial
    {
        Empty,
        Platform,
        Wall,
        DecorativePlatform
    }

    public enum SpikeOrientation
    {
        Up,
        Down,
        Left,
        Right
    }

    public readonly struct SpikeDefinition
    {
        public SpikeDefinition(Rect rect, SpikeOrientation orientation)
        {
            Rect = rect;
            Orientation = orientation;
        }

        public Rect Rect { get; }
        public SpikeOrientation Orientation { get; }
    }

    public sealed class LevelDefinition
    {
        public LevelDefinition(int number, params string[] rows)
        {
            Number = number;
            Rows = rows;
        }

        public int Number { get; }
        public IReadOnlyList<string> Rows { get; }
    }

    public sealed class ParsedLevel
    {
        public ParsedLevel(
            int number,
            int widthCells,
            int heightCells,
            TerrainMaterial[,] terrainCells,
            bool[,] destructibleCells,
            IReadOnlyList<SpikeDefinition> spikes,
            Vector2 spawnPoint,
            Vector2 exitCell)
        {
            Number = number;
            WidthCells = widthCells;
            HeightCells = heightCells;
            TerrainCells = terrainCells;
            DestructibleCells = destructibleCells;
            Spikes = spikes;
            SpawnPoint = spawnPoint;
            ExitCell = exitCell;
        }

        public int Number { get; }
        public int WidthCells { get; }
        public int HeightCells { get; }
        public TerrainMaterial[,] TerrainCells { get; }
        public bool[,] DestructibleCells { get; }
        public IReadOnlyList<SpikeDefinition> Spikes { get; }
        public Vector2 SpawnPoint { get; }
        public Vector2 ExitCell { get; }
        public Vector2 WorldSize => new Vector2(WidthCells, HeightCells);
    }

    public static class LevelDatabase
    {
        private static readonly LevelDefinition[] Definitions =
        {
            new LevelDefinition(
                1,
                "__________________",
                "|^^^^^^^^^^^^^^^^|",
                "|                |",
                "|                |",
                "|----            |",
                "| A              |",
                "|                |",
                "|                |",
                "|                |",
                "|                |",
                "|________________|",
                "|                |",
                "|                |",
                "|                |",
                "|                |",
                "|                |",
                "|                |",
                "|              B |",
                "|________________|",
                "|                |",
                "|^^^^^^^^^^^^^^^^|",
                "|________________|"),
            new LevelDefinition(
                2,
                "__________________",
                "|^^^^^^^^^^^^^^^^|",
                "|                |",
                "|                |",
                "|---             |",
                "| A              |",
                "|                |",
                "|                |",
                "|                |",
                "|                |",
                "|________________|",
                "|                |",
                "|                |",
                "|                |",
                "|                |",
                "|                |",
                "|                |",
                "| B              |",
                "|_________       |",
                "|                |",
                "|^^^^^^^^^^^^^^^^|",
                "|________________|"),
            new LevelDefinition(
                3,
                "__________________",
                "|^^^^^^^^^^^^^^^^|",
                "|                |",
                "|---             |",
                "| A              |",
                "|                |",
                "|                |",
                "|                |",
                "|________________|",
                "|                |",
                "|^^^^            |",
                "|                |",
                "|                |",
                "|________________|",
                "|                |",
                "|            ^^^^|",
                "|                |",
                "| B              |",
                "|______          |",
                "|                |",
                "|^^^^^^^^^^^^^^^^|",
                "|________________|")
        };

        public static int Count => Definitions.Length;

        public static ParsedLevel GetLevel(int index)
        {
            LevelDefinition definition = Definitions[Mathf.Clamp(index, 0, Definitions.Length - 1)];
            int height = definition.Rows.Count;
            int width = 0;
            for (int row = 0; row < height; row++)
            {
                width = Mathf.Max(width, definition.Rows[row].Length);
            }

            TerrainMaterial[,] terrain = new TerrainMaterial[width, height];
            bool[,] destructible = new bool[width, height];
            List<SpikeDefinition> spikes = new List<SpikeDefinition>();
            Vector2 spawn = Vector2.zero;
            Vector2 exit = Vector2.zero;

            for (int row = 0; row < height; row++)
            {
                string line = definition.Rows[row];
                for (int column = 0; column < width; column++)
                {
                    char symbol = column < line.Length ? line[column] : ' ';
                    int worldRow = height - 1 - row;

                    switch (symbol)
                    {
                        case '_':
                            terrain[column, worldRow] = TerrainMaterial.Platform;
                            destructible[column, worldRow] = true;
                            break;
                        case '-':
                            terrain[column, worldRow] = TerrainMaterial.DecorativePlatform;
                            break;
                        case '|':
                            terrain[column, worldRow] = TerrainMaterial.Wall;
                            break;
                        case 'A':
                            // "A" already marks the hole position in open air below a platform.
                            // Use that cell directly and let the lemming begin in falling state.
                            spawn = new Vector2(column + 0.5f, worldRow + 0.5f);
                            break;
                        case 'B':
                            exit = new Vector2(column + 0.5f, worldRow + 0.18f);
                            break;
                    }
                }
            }

            for (int row = 0; row < height; row++)
            {
                string line = definition.Rows[row];
                for (int column = 0; column < width; column++)
                {
                    char symbol = column < line.Length ? line[column] : ' ';
                    if (symbol != '^')
                    {
                        continue;
                    }

                    SpikeOrientation orientation = DetectOrientation(definition.Rows, width, height, row, column);
                    int worldRow = height - 1 - row;
                    spikes.Add(new SpikeDefinition(BuildSpikeRect(column, worldRow, orientation), orientation));
                }
            }

            return new ParsedLevel(definition.Number, width, height, terrain, destructible, spikes, spawn, exit);
        }

        private static SpikeOrientation DetectOrientation(IReadOnlyList<string> rows, int width, int height, int row, int column)
        {
            if (IsSolidSymbol(ReadSymbol(rows, width, height, row - 1, column)))
            {
                return SpikeOrientation.Down;
            }

            if (IsSolidSymbol(ReadSymbol(rows, width, height, row + 1, column)))
            {
                return SpikeOrientation.Up;
            }

            if (IsSolidSymbol(ReadSymbol(rows, width, height, row, column - 1)))
            {
                return SpikeOrientation.Right;
            }

            return SpikeOrientation.Left;
        }

        private static Rect BuildSpikeRect(int column, int row, SpikeOrientation orientation)
        {
            const float depth = 0.36f;
            const float inset = 0.08f;

            switch (orientation)
            {
                case SpikeOrientation.Down:
                    return new Rect(column + inset, row + 0.1f, 1f - inset * 2f, depth);
                case SpikeOrientation.Left:
                    return new Rect(column + 0.54f, row + inset, depth, 1f - inset * 2f);
                case SpikeOrientation.Right:
                    return new Rect(column + 0.1f, row + inset, depth, 1f - inset * 2f);
                default:
                    return new Rect(column + inset, row + 0.54f, 1f - inset * 2f, depth);
            }
        }

        private static char ReadSymbol(IReadOnlyList<string> rows, int width, int height, int row, int column)
        {
            if (row < 0 || row >= height || column < 0 || column >= width)
            {
                return ' ';
            }

            string line = rows[row];
            return column < line.Length ? line[column] : ' ';
        }

        private static bool IsSolidSymbol(char symbol)
        {
            return symbol == '_' || symbol == '|';
        }
    }
}
