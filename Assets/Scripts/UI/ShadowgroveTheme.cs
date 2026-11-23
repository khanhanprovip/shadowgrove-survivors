using UnityEngine;
namespace DATN.UI
{
    public sealed class ShadowgroveTheme : MonoBehaviour
    {
        private float _next;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (FindFirstObjectByType<ShadowgroveTheme>() != null) return;
            var go = new GameObject("DATN_ShadowgroveTheme"); DontDestroyOnLoad(go); go.AddComponent<ShadowgroveTheme>();
        }
        private void LateUpdate()
        {
            if (Time.unscaledTime < _next) return; _next = Time.unscaledTime + .12f; ForestUI.Refresh(); JadeVisualDetails.Refresh();
        }
    }
}
