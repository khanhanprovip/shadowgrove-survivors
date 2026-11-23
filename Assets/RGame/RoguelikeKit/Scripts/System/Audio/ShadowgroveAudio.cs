using UnityEngine;

namespace RGame.RoguelikeKit
{
    internal enum SoundCue
    {
        UiSelect, UiConfirm, EnemyHit, PlayerHit, Experience, LevelUp, Treasure, Victory, Defeat, Cast, Gold, Heal
    }

    // Owned by the persistent settings system and shared across additive scenes.
    internal sealed class ShadowgroveAudio : MonoBehaviour
    {
        private const string ResourcePath = "ShadowgroveAudio/";
        private const float MusicGain = 0.40f;
        private const float PausedMusicGain = 0.38f;

        private static ShadowgroveAudio instance;
        private AudioSource music;
        private AudioSource effects;
        private AudioClip menuMusic;
        private AudioClip battleMusic;
        private AudioClip[] cues;
        private float[] nextPlayback;
        private float musicVolume;
        private bool inBattle;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetInstance() => instance = null;

        internal static void ApplySettings(SettingsSO settings)
        {
            if (instance == null)
            {
                var host = new GameObject("Shadowgrove Audio");
                DontDestroyOnLoad(host);
                instance = host.AddComponent<ShadowgroveAudio>();
            }

            float master = Mathf.Clamp01(settings.MasterVolume);
            instance.musicVolume = master * Mathf.Clamp01(settings.MusicVolume) * MusicGain;
            instance.effects.volume = master * Mathf.Clamp01(settings.SfxVolume);
            instance.RefreshMusicVolume();
        }

        internal static void SetScene(bool gameplay)
        {
            if (instance == null) return;

            instance.inBattle = gameplay;
            var source = instance.music;
            var clip = gameplay ? instance.battleMusic : instance.menuMusic;
            if (source.clip == clip && source.isPlaying) return;

            source.clip = clip;
            instance.RefreshMusicVolume();
            if (clip != null) source.Play();
        }

        internal static void Play(SoundCue cue)
        {
            if (instance == null || instance.effects.volume <= 0f) return;

            int slot = (int)cue;
            float now = Time.unscaledTime;
            if (now < instance.nextPlayback[slot] || instance.cues[slot] == null) return;

            // Swarms and magnet pickups can trigger dozens of events in one frame.
            var (gain, interval) = cue switch
            {
                SoundCue.Cast => (0.22f, 0.16f),
                SoundCue.Gold => (0.47f, 0.12f),
                SoundCue.EnemyHit => (0.26f, 0.105f),
                SoundCue.PlayerHit => (0.65f, 0.19f),
                SoundCue.Experience => (0.42f, 0.125f),
                SoundCue.UiSelect => (0.55f, 0.06f),
                SoundCue.UiConfirm => (0.62f, 0.07f),
                _ => (0.77f, 0.20f)
            };
            instance.nextPlayback[slot] = now + interval;
            instance.effects.PlayOneShot(instance.cues[slot], gain);
        }

        private void Awake()
        {
            music = gameObject.AddComponent<AudioSource>();
            music.playOnAwake = false;
            music.loop = true;
            music.priority = 64;

            effects = gameObject.AddComponent<AudioSource>();
            effects.playOnAwake = false;
            effects.priority = 96;

            menuMusic = LoadClip("MenuLoop");
            battleMusic = LoadClip("BattleLoop");
            var values = (SoundCue[])System.Enum.GetValues(typeof(SoundCue));
            cues = new AudioClip[values.Length];
            nextPlayback = new float[values.Length];
            foreach (var cue in values)
                cues[(int)cue] = LoadClip(cue.ToString());
        }

        private void Update() => RefreshMusicVolume();

        private void RefreshMusicVolume()
        {
            music.volume = inBattle && Time.timeScale <= 0f ? musicVolume * PausedMusicGain : musicVolume;
        }

        private static AudioClip LoadClip(string name)
        {
            var clip = Resources.Load<AudioClip>(ResourcePath + name);
            if (clip == null) Debug.LogError($"Missing Shadowgrove audio clip: {name}");
            return clip;
        }
    }
}
