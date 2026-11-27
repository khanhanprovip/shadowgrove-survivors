#region

using System.Collections;
using UnityEngine;

#endregion

namespace RGame.RoguelikeKit
{
    public class LevelGenerator : MonoBehaviour
    {
        [SerializeField] private LevelAndCharacterSO mLevelAndCharacterSO;
        [SerializeField] private GeneratorMapChannelSO mGeneratorMapChannelSO;
        [SerializeField] private StartStageEventChannelSO mStartStageEventChannelSo;
        [SerializeField] private GlobalConfigSO mGlobalConfigSO;
        [SerializeField] private InputReader mInputReader;

        private IEnumerator Start()
        {
            yield return new WaitForSeconds(0.5f);

            mInputReader.EnableGameplayInput();

            var map = new GameObject("Map");
            mGeneratorMapChannelSO.RaiseEvent(mLevelAndCharacterSO.SelectLevelSO.MapConfig, map.transform);
            DATN.World.LevelBackground.Apply(mLevelAndCharacterSO.SelectLevelSO, map.transform);
            DATN.World.StageLayout.Apply(mLevelAndCharacterSO.SelectLevelSO,map.transform);

            //Generate Player
            var player = Instantiate(mLevelAndCharacterSO.SelectCharacterSo.CharacterPrefab, transform.position, transform.rotation);

            mGlobalConfigSO.GlobalPlayer = player.GetComponent<Player>();
            
            var enemies = new GameObject("Enemies");
            var waveConfig = ScriptableObject.CreateInstance<RuntimeStageConfigSO>();
            waveConfig.SelectStageConfig = mLevelAndCharacterSO.SelectLevelSO.StageConfig;
            waveConfig.EnemyParent = enemies.transform;
            var mode = mLevelAndCharacterSO.SelectLevelSO.GameMode;
            waveConfig.IsEndless = mode != null && RunCheckpointSession.PendingModeId(mode.ModeId) == DATN.Modes.GameModeId.Endless;

            mStartStageEventChannelSo.RaiseEvent(waveConfig);
            Destroy(waveConfig);
            if(RunCheckpointSession.HasPending)
            {
                yield return null; // Player.Start registers the starter and round context.
                yield return RunCheckpointSession.RestoreAfterSpawn();
            }
        }
    }
}
