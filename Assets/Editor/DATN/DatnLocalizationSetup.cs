using System;
using UnityEditor;
using UnityEngine;
using TMPro;

namespace DATN
{
    /// <summary>
    ///     Chay mot lan qua -executeMethod DATN.DatnLocalizationSetup.SetupVietnameseFont.
    ///     Tao TMP_FontAsset tu font Arial cua Windows o che do atlas dong (nap dau
    ///     tieng Viet khi can) va gan lam fallback trong TMP Settings — het loi
    ///     ky tu vuong khi hien tieng Viet co dau.
    /// </summary>
    public static class DatnLocalizationSetup
    {
        private const string FontPath = "Assets/TextMesh Pro/Resources/Fonts & Materials/ArialVN-Dynamic SDF.asset";

        public static void SetupVietnameseFont()
        {
            try
            {
                var existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
                if (existing == null)
                {
                    // Nhan ban font LiberationSans SDF co san va bat atlas dong —
                    // glyph tieng Viet se duoc nap tu dong khi chay (font goc ho tro VN).
                    var src = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
                        "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
                    if (src == null) throw new Exception("Khong tim thay LiberationSans SDF goc");

                    var clone = UnityEngine.Object.Instantiate(src);
                    clone.name = "ArialVN-Dynamic SDF";
                    var soFont = new SerializedObject(clone);
                    soFont.FindProperty("m_AtlasPopulationMode").enumValueIndex = (int)AtlasPopulationMode.Dynamic;
                    soFont.ApplyModifiedPropertiesWithoutUndo();
                    AssetDatabase.CreateAsset(clone, FontPath);
                    existing = clone;
                    Debug.Log("[LocSetup] Da nhan ban font + bat atlas dong cho tieng Viet");
                }
                else
                {
                    Debug.Log("[LocSetup] Font da ton tai");
                }

                // Gan vao danh sach fallback cua TMP Settings (thuoc tinh static)
                var list = TMP_Settings.fallbackFontAssets;
                if (list == null || !list.Contains(existing))
                {
                    if (list == null) list = new System.Collections.Generic.List<TMP_FontAsset>();
                    list.Add(existing);
                    TMP_Settings.fallbackFontAssets = list;
                    AssetDatabase.SaveAssets();
                    Debug.Log("[LocSetup] Da gan font VN lam fallback trong TMP Settings");
                }
                else
                {
                    Debug.Log("[LocSetup] Fallback da co san font VN");
                }

                EditorApplication.Exit(0);
            }
            catch (Exception ex)
            {
                Debug.LogError("[LocSetup] LOI: " + ex);
                EditorApplication.Exit(1);
            }
        }
    }
}
