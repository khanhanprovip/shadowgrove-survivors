using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class FixCharacterFrames
{
    private const string Art = "Assets/RGame/RoguelikeKit/Art/";
    private const string Output = "F:/Do_An_KT/_assistant_work/Idle_Flicker_Fix_2026-11-21";
    private static readonly string[] Heroes = { "Rowan", "Lyra", "Kael" };

    private static int OpaquePixels(Sprite sprite)
    {
        var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        try
        {
            ImageConversion.LoadImage(texture, File.ReadAllBytes(AssetDatabase.GetAssetPath(sprite.texture)));
            var pixels = texture.GetPixels32();
            Rect rect = sprite.rect;
            int count = 0;
            for (int y = (int)rect.y; y < rect.yMax; y++)
                for (int x = (int)rect.x; x < rect.xMax; x++)
                    if (pixels[y * texture.width + x].a > 8) count++;
            return count;
        }
        finally { UnityEngine.Object.DestroyImmediate(texture); }
    }

    private static int Audit(string label)
    {
        var lines = new List<string>();
        int blank = 0;
        foreach (string hero in Heroes)
            foreach (string state in new[] { "Idle", "Run", "Attack" })
            {
                var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(Art + "Animations/Shadowgrove/" + hero + "_" + state + ".anim");
                foreach (var binding in AnimationUtility.GetObjectReferenceCurveBindings(clip))
                    foreach (var frame in AnimationUtility.GetObjectReferenceCurve(clip, binding))
                    {
                        var sprite = frame.value as Sprite;
                        int count = sprite == null ? 0 : OpaquePixels(sprite);
                        if (count == 0) blank++;
                        lines.Add($"{hero}/{state} t={frame.time:F6}: {sprite?.name} opaquePixels={count}");
                    }
            }
        lines.Add("Blank frames: " + blank);
        Directory.CreateDirectory(Output);
        File.WriteAllLines(Output + "/" + label + ".txt", lines);
        return blank;
    }

    public static string AuditBefore() => "Blank frames before fix: " + Audit("before-frame-audit");

    public static string Fix()
    {
        foreach (string hero in Heroes)
        {
            var sprites = AssetDatabase.LoadAllAssetsAtPath(Art + "Sprites/Shadowgrove/Characters/SG_Char_" + hero + "_Sheet.png")
                .OfType<Sprite>().ToArray();
            var states = new[] { "Idle", "Run", "Attack" };
            for (int row = 0; row < states.Length; row++)
            {
                string state = states[row];
                float y = (2 - row) * 96;
                var frames = sprites.Where(s => Mathf.Abs(s.rect.y - y) < 0.1f && OpaquePixels(s) > 0)
                    .OrderBy(s => s.rect.x).ToArray();
                int expected = row == 0 ? 4 : 6;
                if (frames.Length != expected) throw new Exception(hero + "/" + state + " frame count = " + frames.Length);
                var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(Art + "Animations/Shadowgrove/" + hero + "_" + state + ".anim");
                float fps = row == 0 ? 6 : row == 1 ? 10 : 12;
                var keys = new ObjectReferenceKeyframe[frames.Length + 1];
                for (int i = 0; i < keys.Length; i++)
                    keys[i] = new ObjectReferenceKeyframe { time = i / fps, value = frames[Mathf.Min(i, frames.Length - 1)] };
                AnimationUtility.SetObjectReferenceCurve(clip,
                    EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite"), keys);
                clip.frameRate = fps;
                var settings = AnimationUtility.GetAnimationClipSettings(clip);
                settings.startTime = 0;
                settings.stopTime = frames.Length / fps;
                settings.loopTime = row != 2;
                AnimationUtility.SetAnimationClipSettings(clip, settings);
                EditorUtility.SetDirty(clip);
            }
        }
        AssetDatabase.SaveAssets();
        int remaining = Audit("after-frame-audit");
        if (remaining != 0) throw new Exception("Transparent animation keys remain: " + remaining);
        return "Fixed all 9 clips: Idle=top 4 frames; Run=middle 6; Attack=bottom 6. Blank frames after reload: " + remaining;
    }
}
