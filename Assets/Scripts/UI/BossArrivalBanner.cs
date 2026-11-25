using DATN.Localization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using RGame.RoguelikeKit;

namespace DATN.UI
{
    [DisallowMultipleComponent]
    public sealed class BossArrivalBanner : MonoBehaviour
    {
        private const float DisplayDuration = 3f;
        private const float FadeInDuration = 0.22f;
        private const float FadeOutDuration = 0.38f;

        private CanvasGroup _canvasGroup;
        private RectTransform _bannerRect;
        private TMP_Text _announcementLabel;
        private TMP_Text _bossNameLabel;
        private Image _leftSeal;
        private Image _rightSeal;
        private Vector2 _restPosition;
        private int _displayedKind = -1;
        private float _elapsed;
        private string _language;
        private bool _showing;

        public bool IsVisible => _showing && _canvasGroup != null && _canvasGroup.alpha > 0.01f;
        public int DisplayedKind => _displayedKind;
        public float ShownSeconds => _elapsed;

        private void Awake()
        {
            BuildView();
            RefreshLocale(true);
        }

        private void Update()
        {
            if (_canvasGroup == null || !_showing)
                return;

            RefreshLocale(false);

            // Time.deltaTime is scaled, so pause and choice panels hold the banner's timer.
            if (Time.timeScale <= 0f)
            {
                _canvasGroup.alpha = 0f;
                return;
            }

            _elapsed += Time.deltaTime;
            if (_elapsed >= DisplayDuration)
            {
                Hide();
                return;
            }

            float enter = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(_elapsed / FadeInDuration));
            float fadeOutStart = DisplayDuration - FadeOutDuration;
            float exit = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((_elapsed - fadeOutStart) / FadeOutDuration));
            _canvasGroup.alpha = enter * (1f - exit);
            _bannerRect.anchoredPosition = _restPosition + Vector2.up * ((1f - enter) * 4f + exit * 4f);
        }

        public void Show(int kind)
        {
            if (kind < 0 || kind >= BossCatalog.Colors.Length)
            {
                Hide();
                return;
            }

            if (_canvasGroup == null)
                BuildView();

            _displayedKind = kind;
            _elapsed = 0f;
            _showing = true;
            RefreshLocale(true);
            _canvasGroup.alpha = 0f;
            _bannerRect.anchoredPosition = _restPosition + Vector2.up * 4f;
        }

        public void Hide()
        {
            _showing = false;
            _displayedKind = -1;
            _elapsed = 0f;

            if (_canvasGroup != null)
                _canvasGroup.alpha = 0f;
            if (_bannerRect != null)
                _bannerRect.anchoredPosition = _restPosition;
        }

        private void BuildView()
        {
            if (_canvasGroup != null)
                return;

            var canvasObject = new GameObject(
                "BossArrivalBannerCanvas",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(CanvasGroup));
            canvasObject.transform.SetParent(transform, false);

            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 220;

            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            _canvasGroup = canvasObject.GetComponent<CanvasGroup>();
            _canvasGroup.alpha = 0f;
            _canvasGroup.interactable = false;
            _canvasGroup.blocksRaycasts = false;

            var contentObject = new GameObject("BossArrivalContent", typeof(RectTransform));
            contentObject.transform.SetParent(canvasObject.transform, false);
            var contentRect = contentObject.GetComponent<RectTransform>();
            contentRect.anchorMin = Vector2.zero;
            contentRect.anchorMax = Vector2.one;
            contentRect.offsetMin = Vector2.zero;
            contentRect.offsetMax = Vector2.zero;
            contentRect.pivot = new Vector2(0.5f, 0.5f);
            contentRect.localScale = Vector3.one;
            _bannerRect = contentRect;
            _restPosition = contentRect.anchoredPosition;

            Image plate = ForestUI.Picture(contentRect, "BossArrivalPlate", "Panel", 0.25f, 0.785f, 0.75f, 0.855f);
            plate.color = Color.white;

            _leftSeal = CreateSeal(contentRect, "BossArrivalSealLeft", 0.268f, 0.801f, 0.305f, 0.84f);
            _rightSeal = CreateSeal(contentRect, "BossArrivalSealRight", 0.695f, 0.801f, 0.732f, 0.84f);

            _announcementLabel = ForestUI.Label(
                contentRect,
                "BossArrivalAnnouncement",
                "BOSS XUẤT HIỆN",
                "BOSS APPROACHES",
                18f,
                0.31f, 0.833f, 0.69f, 0.854f,
                TextAlignmentOptions.Center,
                ForestUI.Gold);
            _bossNameLabel = ForestUI.Label(
                contentRect,
                "BossArrivalName",
                string.Empty,
                string.Empty,
                44f,
                0.31f, 0.792f, 0.69f, 0.832f,
                TextAlignmentOptions.Center,
                ForestUI.Ink);

            StyleLabel(_announcementLabel, 16f, 18f);
            StyleLabel(_bossNameLabel, 32f, 44f);
            DisableForestLocalizer(_announcementLabel);
            DisableForestLocalizer(_bossNameLabel);
        }

        private static Image CreateSeal(Transform parent, string name, float xMin, float yMin, float xMax, float yMax)
        {
            var sealObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            sealObject.transform.SetParent(parent, false);
            ForestUI.Place(sealObject.transform, xMin, yMin, xMax, yMax);

            var seal = sealObject.GetComponent<Image>();
            seal.sprite = ForestUI.Art("sigil");
            seal.preserveAspect = true;
            seal.raycastTarget = false;
            return seal;
        }

        private static void StyleLabel(TMP_Text label, float minimumSize, float maximumSize)
        {
            label.fontStyle = FontStyles.Bold;
            label.enableAutoSizing = true;
            label.fontSizeMin = minimumSize;
            label.fontSizeMax = maximumSize;
            label.enableWordWrapping = false;
            label.raycastTarget = false;
            label.outlineWidth = 0.2f;
            label.outlineColor = Color.black;
        }

        private static void DisableForestLocalizer(TMP_Text label)
        {
            var localizer = label.GetComponent<ForestText>();
            if (localizer != null)
                localizer.enabled = false;
        }

        private void RefreshLocale(bool force)
        {
            string code = LanguageManager.GetCurrentCode();
            if (!force && code == _language)
                return;

            _language = code;
            bool vietnamese = code == "vi";

            if (_announcementLabel != null)
                _announcementLabel.text = vietnamese ? "BOSS XUẤT HIỆN" : "BOSS APPROACHES";

            if (_bossNameLabel != null && _displayedKind >= 0)
            {
                _bossNameLabel.text = BossCatalog.Name(_displayedKind, vietnamese);
                Color bossColor = BossCatalog.Colors[_displayedKind];
                _bossNameLabel.color = bossColor;
                if (_leftSeal != null)
                    _leftSeal.color = bossColor;
                if (_rightSeal != null)
                    _rightSeal.color = bossColor;
            }
        }

        private void OnDisable()
        {
            Hide();
        }
    }
}
