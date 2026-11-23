using System.Collections.Generic;
using RGame.RoguelikeKit;
using UnityEngine;

namespace DATN.World
{
    // Presentation only. The original map dimensions, props and collisions remain authoritative.
    public sealed class LevelBackground : MonoBehaviour
    {
        private static readonly Dictionary<string, string> Themes = new()
        {
            { "TrialLevel", "TrialForest" },
            { "LV2_MoonlitRuins", "MoonlitRuins" },
            { "LV3_SunkenMarsh", "SunkenMarsh" },
            { "LV4_AshenQuarry", "AshenQuarry" },
            { "LV5_FrostHollow", "FrostHollow" },
            { "LV6_CursedVillage", "CursedVillage" },
            { "LV7_ClockworkCitadel", "ClockworkCitadel" },
            { "LV8_CrimsonCathedral", "CrimsonCathedral" },
            { "LV9_EndlessAbyss", "EndlessAbyss" },
            { "LV10_EclipseThrone", "EclipseThrone" }
        };
        private static readonly Dictionary<string, Sprite> Sprites = new();
        public string ThemeKey { get; private set; }
        public SpriteRenderer GroundRenderer { get; private set; }

        public static string ThemeFor(string levelName) => Themes.TryGetValue(levelName, out var theme) ? theme : null;

        public static void Apply(LevelConfigSO level, Transform map)
        {
            string key = ThemeFor(level.name);
            if (key == null || map.GetComponentInChildren<LevelBackground>() != null) return;
            if (!Sprites.TryGetValue(key, out var sprite) || sprite == null)
            {
                var texture = Resources.Load<Texture2D>("SGGround/" + key);
                if (texture == null)
                {
                    Debug.LogError("Missing level terrain: SGGround/" + key);
                    return;
                }
                sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height),
                    Vector2.one * 0.5f, texture.width / 8f, 0, SpriteMeshType.FullRect);
                sprite.name = "Ground_" + key;
                Sprites[key] = sprite;
            }

            // Hide forest decorations embedded in the original ground tiles, but keep
            // those objects intact for systems that inspect the generated map grid.
            foreach (var tile in map.GetComponentsInChildren<SpriteRenderer>()) tile.enabled = false;
            var ground = new GameObject("Ground_" + key, typeof(SpriteRenderer), typeof(LevelBackground));
            ground.transform.SetParent(map, false);
            // Original grid tiles are centred at integer coordinates from -size/2.
            ground.transform.localPosition = new Vector3(-0.5f, -0.5f, 0);
            var renderer = ground.GetComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.drawMode = SpriteDrawMode.Tiled;
            renderer.tileMode = SpriteTileMode.Continuous;
            renderer.size = new Vector2(level.MapConfig.Width, level.MapConfig.Height);
            renderer.sortingLayerName = "Map";
            renderer.sortingOrder = short.MinValue;
            var background = ground.GetComponent<LevelBackground>();
            background.ThemeKey = key;
            background.GroundRenderer = renderer;
        }
    }
}
