using RGame.CommonStat;
using RGame.Framework;
using UnityEngine;
using DATN.Trial;
namespace RGame.RoguelikeKit
{
    public class LevelManager : MonoBehaviour
    {
        [SerializeField] private GlobalConfigSO _globalConfig;
        [SerializeField] private CommonStatRuntimeSO _stat;
        [SerializeField] private VoidEventChannelSO _updateLevelUIChannel;
        [SerializeField] private VoidEventChannelSO _levelUpgradeChannel;
        [SerializeField] private PoolRuntimeSO _poolRuntime;
        [SerializeField] private ExpConfig expConfig;
        [SerializeField] private AttributeManager _attributes;
        [SerializeField] private PlayerSpawnChannelSO _playerSpawn;
        private int initializedPlayer;
        private int pendingChoices;
        private bool processingExp, choiceOpen;
        private TrialController round;
        public int PendingChoices => pendingChoices;
        private void Start()
        {
            _globalConfig.OnGameStart();
            _stat.ResetAllValues();
            _stat.AddAction("Exp", ExpChange);
            UpgradeSkillPanel.SelectionCompleted += OnChoiceCompleted;
            _playerSpawn.RegisterListener(OnPlayerSpawned);
        }
        private void OnDestroy()
        {
            _stat.RemoveAction("Exp", ExpChange);
            UpgradeSkillPanel.SelectionCompleted -= OnChoiceCompleted;
            _playerSpawn.UnregisterListener(OnPlayerSpawned);
        }
        private void OnPlayerSpawned(Player player)
        {
            if(initializedPlayer==player.GetInstanceID())return;
            initializedPlayer=player.GetInstanceID();
            _attributes.InitAttribute(_stat);
        }
        private void Update()
        {
            if (round == null) round = FindFirstObjectByType<TrialController>();
            if (round != null && round.IsEnded) { pendingChoices = 0; choiceOpen = false; return; }
            TryPresent();
        }
        private void ExpChange(int value, int maxValue)
        {
            if (processingExp) return;
            processingExp = true;
            try
            {
                while (_stat.GetValue("Exp") >= Mathf.Max(1, expConfig.GetExperienceForLevel(_stat.GetValue("Level"))))
                {
                    int cost = Mathf.Max(1, expConfig.GetExperienceForLevel(_stat.GetValue("Level")));
                    _stat.ModifyValue("Level", 1);
                    _stat.ModifyValue("Exp", -cost);
                    pendingChoices++;
                }
            }
            finally { processingExp = false; }
            _updateLevelUIChannel.RaiseEvent();
            TryPresent();
        }
        // Direct boss reward: existing experience is deliberately untouched.
        public void GrantLevels(int count)
        {
            if (count <= 0) return;
            _stat.ModifyValue("Level", count);
            pendingChoices += count;
            _updateLevelUIChannel.RaiseEvent();
            TryPresent();
        }
        private void TryPresent()
        {
            if (pendingChoices <= 0 || choiceOpen || _stat.GetValue("HP") <= 0 || Time.timeScale<=0) return;
            if (round != null && (round.IsPaused || round.IsEnded)) return;
            choiceOpen = true;
            pendingChoices--;
            ShadowgroveAudio.Play(SoundCue.LevelUp);
            _levelUpgradeChannel.RaiseEvent();
        }
        private void OnChoiceCompleted() { choiceOpen = false; TryPresent(); }
    }
}
