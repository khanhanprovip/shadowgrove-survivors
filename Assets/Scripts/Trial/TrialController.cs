using System;
using RGame.CommonStat;
using RGame.Framework;
using RGame.RoguelikeKit;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DATN.Trial
{
    /// <summary>
    ///     Bo dieu khien rieng cua man Trial Forest (code tu phat trien cua do an):
    ///     - Dem nguoc thoi luong man cho (90s) va hien thi dong ho tren HUD.
    ///     - Het gio: raise ken OnGameWin → hien man chien thang (GameEndPanel).
    ///     - Pause/Resume qua phim Esc (InputReader.MenuPauseEvent) voi panel tam dung.
    ///     - Restart: don dep trang thai (pool, buff, chi so) roi nap lai scene Game.
    ///     - Gan nut "Choi lai" vao man thang/thua bang cach clone nut Exit co san.
    ///     Dat tren GameObject "TrialController" trong scene GamePlay.
    /// </summary>
    public class TrialController : MonoBehaviour
    {
        [Header("Thoi luong man choi")]
        [SerializeField] private float mLevelDuration = 90f;

        [Header("Su kien & du lieu")]
        [SerializeField] private PlayerSpawnChannelSO mPlayerSpawnChannel;
        [SerializeField] private VoidEventChannelSO mGameWinChannel;
        [SerializeField] private InputReader mInputReader;
        [SerializeField] private CommonStatRuntimeSO mPlayerStat;
        [SerializeField] private GlobalConfigSO mGlobalConfig;
        [SerializeField] private PoolRuntimeSO mPoolRuntime;
        [SerializeField] private VoidEventChannelSO mClearAllBuffsChannel;

        [Header("Tai nap scene")]
        [SerializeField] private LoadEventChannelSO mLoadLevelChannel;
        [SerializeField] private LoadEventChannelSO mLoadMenuChannel;
        [SerializeField] private GameSceneSO mGameScene;
        [SerializeField] private GameSceneSO mMainMenuScene;

        private float mRemaining;
        private bool mCounting;
        private bool mEnded;
        private bool mPaused;

        private TextMeshProUGUI mTimeText;
        private GameObject mPausePanel;
        private Button mPauseResumeButton;
        private Button mPauseRestartButton;
        private Button mPauseExitButton;

        public bool IsPaused => mPaused;

        private void OnEnable()
        {
            mPlayerSpawnChannel.RegisterListener(OnPlayerSpawned);
            mInputReader.MenuPauseEvent += OnPausePressed;
        }

        private void OnDisable()
        {
            mPlayerSpawnChannel.UnregisterListener(OnPlayerSpawned);
            mInputReader.MenuPauseEvent -= OnPausePressed;
        }

        private void Update()
        {
            if (!mCounting) return;

            mRemaining -= Time.deltaTime;
            if (mRemaining <= 0f)
            {
                mRemaining = 0f;
                mCounting = false;
                EndLevelAsWin();
            }

            UpdateTimeHud();
        }

        // ---- Vong doi man choi ----

        private void OnPlayerSpawned(Player _)
        {
            // Tren restart, LevelManager.Start khong chay lai (GamePlay scene khong
            // bi tai nap) nen phai reset chi so + bo dem tai day cho moi luot choi.
            mPlayerStat.ResetAllValues();
            mGlobalConfig.OnGameStart();
            mClearAllBuffsChannel.RaiseEvent();

            mRemaining = mLevelDuration;
            mCounting = true;
            mEnded = false;
            mPaused = false;
            Time.timeScale = 1f;
            HidePausePanel();
            UpdateTimeHud();
        }

        private void EndLevelAsWin()
        {
            if (mEnded) return;
            mEnded = true;
            mGameWinChannel.RaiseEvent();
        }

        // ---- Pause / Resume ----

        private void OnPausePressed()
        {
            if (mEnded) return;
            if (mPaused) Resume();
            else Pause();
        }

        public void Pause()
        {
            if (mEnded || mPaused) return;
            mPaused = true;
            Time.timeScale = 0f;
            mInputReader.EnableMenuInput();
            ShowPausePanel();
        }

        public void Resume()
        {
            if (!mPaused) return;
            mPaused = false;
            Time.timeScale = 1f;
            mInputReader.EnableGameplayInput();
            HidePausePanel();
        }

        // ---- Restart / thoat ----

        public void Restart()
        {
            Time.timeScale = 1f;
            mPaused = false;
            mEnded = true; // chan cac update trong luc tai lai
            mCounting = false;
            HidePausePanel();

            mPoolRuntime.RecycleAll();
            mClearAllBuffsChannel.RaiseEvent();
            mGlobalConfig.GlobalPlayer = null; // chan truy cap player cu trong luc tai scene

            mLoadLevelChannel.RaiseEvent(mGameScene);
        }

        public void ExitToMainMenu()
        {
            Time.timeScale = 1f;
            mPaused = false;
            mEnded = true;
            mCounting = false;
            HidePausePanel();

            mPoolRuntime.RecycleAll();
            mClearAllBuffsChannel.RaiseEvent();
            mGlobalConfig.GlobalPlayer = null;

            mLoadMenuChannel.RaiseEvent(mMainMenuScene);
        }

        // ---- HUD dong ho ----

        private void UpdateTimeHud()
        {
            if (mTimeText == null) return;

            int total = Mathf.CeilToInt(mRemaining);
            int minutes = total / 60;
            int seconds = total % 60;
            mTimeText.text = $"{minutes:00}:{seconds:00}";
        }

        // ---- Xay dung UI luc runtime (khong sua scene YAML phuc tap) ----

        private void Start()
        {
            BuildTimeHud();
            BuildPausePanel();
            AttachRestartButtons();
        }

        private Canvas FindGameCanvas()
        {
            var canvases = FindObjectsByType<Canvas>(FindObjectsSortMode.None);
            foreach (var canvas in canvases)
            {
                if (canvas.name == "Canvas-GamePlay") return canvas;
            }
            return canvases.Length > 0 ? canvases[0] : null;
        }

        private void BuildTimeHud()
        {
            Canvas canvas = FindGameCanvas();
            if (canvas == null)
            {
                Debug.LogError("[TrialController] Khong tim thay Canvas-GamePlay de gan dong ho", this);
                return;
            }

            var go = new GameObject("TrialTimeHud", typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(canvas.transform, false);

            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, -14f);
            rect.sizeDelta = new Vector2(220f, 44f);

            mTimeText = go.GetComponent<TextMeshProUGUI>();
            mTimeText.alignment = TextAlignmentOptions.Center;
            mTimeText.fontSize = 34f;
            mTimeText.fontStyle = FontStyles.Bold;
            mTimeText.color = new Color(1f, 0.92f, 0.6f, 0.95f);
            mTimeText.text = "01:30";
        }

        private void BuildPausePanel()
        {
            Canvas canvas = FindGameCanvas();
            if (canvas == null) return;

            mPausePanel = new GameObject("TrialPausePanel", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
            mPausePanel.transform.SetParent(canvas.transform, false);

            var panelRect = mPausePanel.GetComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.sizeDelta = Vector2.zero;

            var dimmer = mPausePanel.GetComponent<Image>();
            dimmer.color = new Color(0f, 0f, 0f, 0.65f);

            var group = mPausePanel.GetComponent<CanvasGroup>();
            group.blocksRaycasts = true;

            var title = CreateLabel(mPausePanel.transform, "TAM DUNG", 46f, new Vector2(0f, 130f));
            title.fontStyle = FontStyles.Bold;

            mPauseResumeButton = CreateButton(mPausePanel.transform, "TiepTucBtn", "TIEP TUC", new Vector2(0f, 30f));
            mPauseRestartButton = CreateButton(mPausePanel.transform, "ChoiLaiBtn", "CHOI LAI", new Vector2(0f, -40f));
            mPauseExitButton = CreateButton(mPausePanel.transform, "VeMenuBtn", "VE MENU", new Vector2(0f, -110f));

            mPauseResumeButton.onClick.AddListener(Resume);
            mPauseRestartButton.onClick.AddListener(Restart);
            mPauseExitButton.onClick.AddListener(ExitToMainMenu);

            mPausePanel.SetActive(false);
        }

        private void ShowPausePanel()
        {
            if (mPausePanel != null) mPausePanel.SetActive(true);
        }

        private void HidePausePanel()
        {
            if (mPausePanel != null) mPausePanel.SetActive(false);
        }

        private TextMeshProUGUI CreateLabel(Transform parent, string text, float size, Vector2 position)
        {
            var go = new GameObject("Label_" + text, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);

            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(420f, size + 16f);

            var label = go.GetComponent<TextMeshProUGUI>();
            label.alignment = TextAlignmentOptions.Center;
            label.fontSize = size;
            label.color = new Color(1f, 0.95f, 0.8f);
            label.text = text;
            return label;
        }

        private Button CreateButton(Transform parent, string name, string text, Vector2 position)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);

            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(280f, 56f);

            var image = go.GetComponent<Image>();
            image.color = new Color(0.18f, 0.22f, 0.16f, 0.95f);

            var label = CreateLabel(go.transform, text, 26f, Vector2.zero);
            label.GetComponent<RectTransform>().sizeDelta = new Vector2(280f, 56f);
            label.color = Color.white;

            var button = go.GetComponent<Button>();
            var colors = button.colors;
            colors.highlightedColor = new Color(0.35f, 0.45f, 0.3f, 1f);
            button.colors = colors;
            return button;
        }

        /// <summary>
        ///     Clone nut Exit co san tren man thang/thua de tao nut "Choi lai"
        ///     giu nguyen phong cach UI cua kit, roi gan su kien Restart.
        /// </summary>
        private void AttachRestartButtons()
        {
            AttachRestartButton("GameWin", new Vector2(-300f, 0f));
            AttachRestartButton("GameOver", new Vector2(-300f, 0f));
        }

        private void AttachRestartButton(string panelName, Vector2 offset)
        {
            var panel = GameObject.Find(panelName);
            if (panel == null) return;

            var exitBtn = panel.transform.Find("ExitBtn");
            if (exitBtn == null) return;

            var clone = Instantiate(exitBtn.gameObject, panel.transform);
            clone.name = "RestartBtn";

            var cloneRect = clone.GetComponent<RectTransform>();
            var exitRect = exitBtn.GetComponent<RectTransform>();
            if (cloneRect != null && exitRect != null)
            {
                cloneRect.anchorMin = exitRect.anchorMin;
                cloneRect.anchorMax = exitRect.anchorMax;
                cloneRect.pivot = exitRect.pivot;
                cloneRect.anchoredPosition = exitRect.anchoredPosition + offset;
                cloneRect.sizeDelta = exitRect.sizeDelta;
            }

            // Doi nhan cua nut (Exit → Choi lai)
            foreach (var tmp in clone.GetComponentsInChildren<TextMeshProUGUI>(true))
            {
                tmp.text = "CHOI LAI";
            }

            var button = clone.GetComponent<Button>();
            if (button != null)
            {
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(Restart);
            }
        }
    }
}
