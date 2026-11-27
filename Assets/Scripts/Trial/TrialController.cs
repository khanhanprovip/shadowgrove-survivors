using System;
using RGame.CommonStat;
using RGame.Framework;
using RGame.RoguelikeKit;
using TMPro;
using DATN.Localization;
using DATN.Modes;
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
        [Header("Du lieu man choi")]
        [SerializeField] private LevelAndCharacterSO mLevelAndCharacterSO;

        [Header("Su kien & du lieu")]
        [SerializeField] private PlayerSpawnChannelSO mPlayerSpawnChannel;
        [SerializeField] private VoidEventChannelSO mGameWinChannel;
        [SerializeField] private VoidEventChannelSO mGameOverChannel;
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

        private IGameMode mMode;
        private bool mRunning;
        private bool mEnded;
        private bool mPaused;

        private TextMeshProUGUI mTimeText;
        private GameObject mPausePanel;
        private Button mPauseResumeButton;
        private Button mPauseRestartButton;
        private Button mPauseExitButton;
        private Button mPauseSaveButton;

        public bool IsPaused => mPaused;
        public bool IsEnded => mEnded;
        public float MatchSeconds => (float)_matchSeconds;

        /// <summary>Tai nguyen can thiet cho kiem thu tu dong tren ban build.</summary>
        public CommonStatRuntimeSO PlayerStat => mPlayerStat;
        public GlobalConfigSO GlobalConfig => mGlobalConfig;
        internal LevelAndCharacterSO RunSelection=>mLevelAndCharacterSO;
        internal object RunMode=>mMode;
        internal GameModeSO RunModeConfig=>mModeConfig;
        internal void RestorePauseView(){mPaused=false;Time.timeScale=1;Pause();}

        private void OnEnable()
        {
            mPlayerSpawnChannel.RegisterListener(OnPlayerSpawned);
            mGameWinChannel.RegisterListener(OnWon);
            mGameOverChannel.RegisterListener(OnLost);
            mGlobalConfig.EnemyDefeated += OnEnemyDefeated;
            mInputReader.MenuPauseEvent += OnPausePressed;
            mInputReader.MenuUnpauseEvent += Resume;
        }

        private void OnDisable()
        {
            mPlayerSpawnChannel.UnregisterListener(OnPlayerSpawned);
            mGameWinChannel.UnregisterListener(OnWon);
            mGameOverChannel.UnregisterListener(OnLost);
            mGlobalConfig.EnemyDefeated -= OnEnemyDefeated;
            mInputReader.MenuPauseEvent -= OnPausePressed;
            mInputReader.MenuUnpauseEvent -= Resume;
        }

        private GameModeSO mModeConfig;

        private void Update()
        {
            if(mPaused&&_pauseLanguage!=LanguageManager.GetCurrentCode())RefreshPauseTexts();
            LogRuntimeStats();
            if (!mRunning || mPlayerStat.GetValue("HP") <= 0) return;
            _matchSeconds += Time.deltaTime;

            // Mode tra ve chuoi muc tieu da dich cho HUD moi frame
            string objective = mMode.UpdateObjective();
            var director=FindFirstObjectByType<EndlessBossDirector>();
            if(mMode is SurvivalMode survival&&director!=null&&director.HasSurvivalBoss&&survival.Remaining<=0)
                objective=LanguageManager.Get(director.WaitingAtArena?"Objective_EnterArena":"Objective_DefeatBoss");
            if (objective != null && mTimeText != null) mTimeText.text = objective;

            TryWin();
        }

        private void OnEnemyDefeated()
        {
            if (mMode is not KillTargetMode || !mRunning || mPlayerStat.GetValue("HP") <= 0) return;
            if (mTimeText != null) mTimeText.text = mMode.UpdateObjective();
            TryWin();
        }

        private void TryWin()
        {
            var director=FindFirstObjectByType<EndlessBossDirector>();
            bool bossSurvival=mMode is SurvivalMode&&director!=null&&director.HasSurvivalBoss;
            if(bossSurvival&&((SurvivalMode)mMode).Remaining<=0)
            {
                director.RequestSurvivalBoss();
                if(!director.FiniteComplete)return;
            }
            if (!mMode.CheckWin()) return;
            // A slow frame may cross the deadline by several tenths of a
            // second. Record the Survival deadline, not that frame's overshoot.
            if (mMode is SurvivalMode&&!bossSurvival)
                _matchSeconds = mModeConfig != null ? mModeConfig.Duration : 90f;
            mRunning = false;
            mGameWinChannel.RaiseEvent();
        }

        /// <summary>Lay GameModeSO tu LevelConfigSO cua man dang chon (fallback: Survival 90s).</summary>
        private IGameMode ResolveMode()
        {
            mModeConfig = mLevelAndCharacterSO != null && mLevelAndCharacterSO.SelectLevelSO != null
                ? mLevelAndCharacterSO.SelectLevelSO.GameMode
                : null;
            if(mModeConfig!=null&&RunCheckpointSession.HasPending)
            {
                mModeConfig=Instantiate(mModeConfig);
                mModeConfig.hideFlags=HideFlags.DontSave;
                mModeConfig.ModeId=RunCheckpointSession.PendingModeId(mModeConfig.ModeId);
                if(RunCheckpointSession.UsesLegacyWorld)mModeConfig.FinalBossKind=-1;
            }
            if (mModeConfig == null)
                return new SurvivalMode();
            return mModeConfig.CreateInstance();
        }

        // ---- Ghi FPS/thong ke vao log (lam bang chung kiem thu hieu nang) ----

        private float _statsTimer;
        private int _statsFrames;

        private void LogRuntimeStats()
        {
            _statsTimer += Time.unscaledDeltaTime;
            _statsFrames++;

            if (_statsTimer < 10f) return;

            float avgFps = _statsFrames / _statsTimer;
            int enemies = FindObjectsByType<BaseEnemy>(FindObjectsSortMode.None).Length;
            Debug.Log($"[Trial] FPS trung binh 10s: {avgFps:F0} | quai dang song: {enemies} | mode: {mModeConfig?.ModeId ?? GameModeId.Survival} | level: {mPlayerStat.GetValue("Level")}");
            _statsTimer = 0f;
            _statsFrames = 0;
        }

        // ---- Vong doi man choi ----

        private void OnPlayerSpawned(Player _)
        {
            mPlayerStat.ResetAllValues();
            mGlobalConfig.OnGameStart();
            mClearAllBuffsChannel.RaiseEvent();

            mMode = ResolveMode();
            mMode.Initialize(new GameModeContext
            {
                Config = mModeConfig,
                GlobalConfig = mGlobalConfig,
                PlayerStat = mPlayerStat,
            });
            mMode.Begin();
            _matchSeconds = 0f;
            mRunning = true;
            mEnded = false;
            mPaused = false;
            Time.timeScale = 1f;
            HidePausePanel();
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
            if (!mRunning || mEnded || mPaused || Time.timeScale <= 0f || mPlayerStat.GetValue("HP") <= 0) return;
            mPaused = true;
            ShadowgroveAudio.Play(SoundCue.UiSelect);
            Time.timeScale = 0f;
            mInputReader.EnableMenuInput();
            ShowPausePanel();
        }

        public void Resume()
        {
            if (!mPaused) return;
            ShadowgroveAudio.Play(SoundCue.UiSelect);
            mPaused = false;
            Time.timeScale = 1f;
            mInputReader.EnableGameplayInput();
            HidePausePanel();
        }

        // ---- Restart / thoat ----

        public void Restart()
        {
            mGlobalConfig.BankGold();
            ShadowgroveAudio.Play(SoundCue.UiConfirm);
            Time.timeScale = 1f;
            mPaused = false;
            mEnded = true; // chan cac update trong luc tai lai
            mRunning = false;
            mMode?.Cleanup();
            HidePausePanel();

            mPoolRuntime.RecycleAll();
            mClearAllBuffsChannel.RaiseEvent();
            mGlobalConfig.GlobalPlayer = null; // chan truy cap player cu trong luc tai scene

            mLoadLevelChannel.RaiseEvent(mGameScene);
        }

        public void ExitToMainMenu()
        {
            mGlobalConfig.BankGold();
            ShadowgroveAudio.Play(SoundCue.UiSelect);
            Time.timeScale = 1f;
            mPaused = false;
            mEnded = true;
            mRunning = false;
            mMode?.Cleanup();
            HidePausePanel();

            mPoolRuntime.RecycleAll();
            mClearAllBuffsChannel.RaiseEvent();
            mGlobalConfig.GlobalPlayer = null;

            mLoadMenuChannel.RaiseEvent(mMainMenuScene);
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
            rect.sizeDelta = new Vector2(600f, 85f);

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

            _pauseTitle = CreateLabel(mPausePanel.transform, "", 46f, new Vector2(0f, 130f));
            _pauseTitle.fontStyle = FontStyles.Bold;

            mPauseResumeButton = CreateButton(mPausePanel.transform, "TiepTucBtn", "", new Vector2(0f, 30f));
            mPauseRestartButton = CreateButton(mPausePanel.transform, "ChoiLaiBtn", "", new Vector2(0f, -40f));
            mPauseExitButton = CreateButton(mPausePanel.transform, "VeMenuBtn", "", new Vector2(0f, -110f));
            mPauseSaveButton=CreateButton(mPausePanel.transform,"SaveRunBtn","",new Vector2(0,-180));
            RefreshPauseTexts();

            mPauseResumeButton.onClick.AddListener(Resume);
            mPauseRestartButton.onClick.AddListener(Restart);
            mPauseExitButton.onClick.AddListener(ExitToMainMenu);
            mPauseSaveButton.onClick.AddListener(()=>RunCheckpointSession.SaveAndExit(this));

            mPausePanel.SetActive(false);
        }

        private TextMeshProUGUI _pauseTitle;
        private string _pauseLanguage;
        private TextMeshProUGUI _pauseResumeLabel, _pauseRestartLabel, _pauseExitLabel;

        /// <summary>Cap nhat lai chuoi pause panel theo ngon ngu hien tai.</summary>
        private void RefreshPauseTexts()
        {
            _pauseLanguage=LanguageManager.GetCurrentCode();
            if (_pauseTitle == null) return;
            _pauseTitle.text = LanguageManager.Get("Pause");
            _pauseResumeLabel.text = LanguageManager.Get("Resume");
            _pauseRestartLabel.text = LanguageManager.Get("Restart");
            _pauseExitLabel.text = LanguageManager.Get("BackToMenu");
            if(mPauseSaveButton!=null)mPauseSaveButton.GetComponentInChildren<TextMeshProUGUI>().text=LanguageManager.GetCurrentCode()=="vi"?"LƯU VÀ VỀ MENU":"SAVE & MAIN MENU";
        }

        private void ShowPausePanel()
        {
            RefreshPauseTexts();
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
            if (name == "TiepTucBtn") _pauseResumeLabel = label;
            else if (name == "ChoiLaiBtn") _pauseRestartLabel = label;
            else if (name == "VeMenuBtn") _pauseExitLabel = label;
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
        // ---- Màn tổng kết (Mốc 8): hiện khi thắng/thua ----
        private double _matchSeconds;

        private void OnWon() => ShowResult(true);
        private void OnLost() => ShowResult(false);

        private void ShowResult(bool won)
        {
            if (mEnded) return;
            mEnded = true;
            mRunning = false;
            mPaused = false;
            HidePausePanel();
            var upgradePanel = FindFirstObjectByType<UpgradeSkillPanel>();
            if (upgradePanel != null) upgradePanel.gameObject.SetActive(false);
            var treasurePanel = FindFirstObjectByType<TreasurePanel>();
            if (treasurePanel != null) treasurePanel.gameObject.SetActive(false);
            ShadowgroveAudio.Play(won ? SoundCue.Victory : SoundCue.Defeat);
            BuildResultSummary(won);
        }

        private void BuildResultSummary(bool won)
        {
            var canvas = FindObjectsByType<Canvas>(FindObjectsSortMode.None);
            foreach (var cv in canvas)
            {
                if (cv.name != "Canvas-GamePlay") continue;
                DATN.UI.ForestUI.Result(cv, won, Mathf.FloorToInt(MatchSeconds),
                    mGlobalConfig.CurrentGetKill, mPlayerStat.GetValue("Level"),
                    mLevelAndCharacterSO.SelectCharacterSo.CharacterName);
                break;
            }
        }
        private TMPro.TextMeshProUGUI MakeLabel(Transform parent, string text, float size, Vector2 pos)
        {
            return MakeLabel(parent, text, size, pos, new Color(0.91f, 0.89f, 0.82f));
        }

        private TMPro.TextMeshProUGUI MakeLabel(Transform parent, string text, float size, Vector2 pos, Color color)
        {
            var go = new GameObject("ResultLabel", typeof(RectTransform), typeof(TMPro.TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var r = go.GetComponent<RectTransform>();
            r.anchorMin = r.anchorMax = new Vector2(0.5f, 0.5f);
            r.anchoredPosition = pos; r.sizeDelta = new Vector2(580f, size + 24f);
            var lb = go.GetComponent<TMPro.TextMeshProUGUI>();
            lb.alignment = TMPro.TextAlignmentOptions.Center; lb.fontSize = size; lb.color = color; lb.text = text;
            return lb;
        }

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
                tmp.text = LanguageManager.Get("Restart");
            }

            var button = clone.GetComponent<Button>();
            if (button != null)
            {
                button.onClick = new Button.ButtonClickedEvent();
                button.onClick.AddListener(Restart);
            }
        }
    }
}

