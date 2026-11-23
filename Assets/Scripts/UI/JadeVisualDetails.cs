using DATN.Localization;
using RGame.RoguelikeKit;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
namespace DATN.UI
{
    // Presentation-only refinements. Never writes character stats or save data.
    public static class JadeVisualDetails
    {
        private static bool Mark(Component component)
        {
            if (component.GetComponent<JadeLayout>() != null) return false;
            component.gameObject.AddComponent<JadeLayout>();
            return true;
        }
        public static void Refresh()
        {
            var loading=GameObject.Find("LoadingScreen");
            if(loading!=null && Mark(loading.transform))
            {
                foreach(var image in loading.GetComponentsInChildren<Image>(true))image.enabled=false;
                foreach(var text in loading.GetComponentsInChildren<TMP_Text>(true))text.enabled=false;
                ForestUI.Background(loading.transform);
                ForestUI.Picture(loading.transform,"Jade_LoadingSeal","sigil",.42f,.44f,.58f,.71f,true);
                ForestUI.Label(loading.transform,"Jade_LoadingTitle","Cánh cổng đang mở","The gate is opening",42,.2f,.31f,.8f,.42f,TextAlignmentOptions.Center,ForestUI.Gold);
                ForestUI.Label(loading.transform,"Jade_LoadingNote","Hành trình qua khu rừng cổ sắp bắt đầu.","Your journey through the ancient grove is about to begin.",25,.16f,.24f,.84f,.31f,TextAlignmentOptions.Center,ForestUI.Muted);
            }
            var timer = GameObject.Find("TrialTimeHud");
            if (timer != null && Mark(timer.transform))
            {
                ForestUI.Place(timer.transform, .385f, .885f, .615f, .98f);
                var label = timer.GetComponent<TMP_Text>();
                if (label != null) { label.color=ForestUI.Ink; label.fontSize=39; label.enableAutoSizing=true; label.fontSizeMin=28; label.fontSizeMax=39; }
            }
            foreach (var panel in Object.FindObjectsByType<PowerUpPanel>(FindObjectsSortMode.None))
            {
                if (!Mark(panel)) continue;
                foreach (var upgrade in panel.GetComponentsInChildren<PowerUpAttributeUpgrade>(true))
                {
                    var icon=upgrade.transform.Find("Item/Icon")?.GetComponent<Image>();
                    if(icon == null) continue;
                    var sprite=Resources.Load<Sprite>("SGJade/Icons/"+upgrade.name.Trim());
                    if(sprite!=null)icon.sprite=sprite;
                    icon.preserveAspect=true;
                }
            }
            foreach(var panel in Object.FindObjectsByType<TreasurePanel>(FindObjectsSortMode.None))
            {
                if(!Mark(panel))continue;
                ForestUI.Label(panel.transform,"Jade_TreasureTitle","Di vật thức tỉnh","A relic awakens",44,.14f,.82f,.86f,.91f,TextAlignmentOptions.Center,ForestUI.Gold);
                ForestUI.Label(panel.transform,"Jade_TreasureNote","Sức mạnh còn lại từ khu rừng cổ.","Power left behind by the ancient grove.",25,.15f,.75f,.85f,.81f,TextAlignmentOptions.Center,ForestUI.Muted);
                foreach(var button in panel.GetComponentsInChildren<Button>(true))ForestUI.ButtonStyle(button,true);
            }
        }
    }
    public sealed class JadeLayout:MonoBehaviour {}
}
