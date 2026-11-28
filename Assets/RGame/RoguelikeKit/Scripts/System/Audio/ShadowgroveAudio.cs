using UnityEngine;
using UnityEngine.Audio;

namespace RGame.RoguelikeKit
{
    internal enum SoundCue
    {
        UiSelect, UiConfirm, EnemyHit, PlayerHit, Experience, LevelUp, Treasure,
        Victory, Defeat, Cast, Gold, Heal,
        HeroRowanThornSlash, HeroLyraDarkOrb, HeroKaelArrow,
        BossEclipseWraith, BossThornRegent, BossFrostMourner,
        BossCrimsonHarvester, BossAbyssOracle
    }

    internal enum ShadowgroveVoiceCategory { Ui, Hero, Boss, Combat, Reward }

    // Owned by the persistent settings system and shared across additive scenes.
    // One source per voice lets important cues take priority over pickup/hit clutter.
    internal sealed class ShadowgroveAudio : MonoBehaviour
    {
        // Cosmetic pitch must not consume the combat/spawn RNG captured in run saves.
        readonly System.Random pitchRandom = new System.Random();
        private const string ResourcePath = "ShadowgroveAudio/";
        private const int VoicePoolSize = 12;
        private const float MusicGain = 0.40f;
        private const float PausedMusicGain = 0.38f;
        private const float BossCueMusicDuck = 0.78f;

        private static ShadowgroveAudio instance;
        private AudioSource music;
        private ShadowgroveAudioMixerRoutes mixerRoutes;
        private AudioSource[] voices;
        private ShadowgroveVoiceCategory[] voiceCategories;
        private float[] voiceGains;
        private AudioClip menuMusic;
        private AudioClip battleMusic;
        private AudioClip[] cues;
        private float[] nextPlayback;
        private float musicVolume;
        private float sfxVolume;
        private float musicDuck = 1f;
        private bool inBattle;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetInstance() => instance = null;

        internal static void ApplySettings(SettingsSO settings)
        {
            if (settings == null) return;

            if (instance == null)
            {
                var host = new GameObject("Shadowgrove Audio");
                DontDestroyOnLoad(host);
                instance = host.AddComponent<ShadowgroveAudio>();
            }

            float master = Mathf.Clamp01(settings.MasterVolume);
            instance.musicVolume = master * Mathf.Clamp01(settings.MusicVolume) * MusicGain;
            instance.sfxVolume = master * Mathf.Clamp01(settings.SfxVolume);
            instance.RefreshMusicVolume();
            instance.RefreshVoiceVolumes();
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
            if (instance == null || instance.sfxVolume <= 0f) return;

            int slot = (int)cue;
            if (slot < 0 || slot >= instance.cues.Length || instance.cues[slot] == null) return;

            var settings = GetCueSettings(cue);
            float now = Time.unscaledTime;
            if (now < instance.nextPlayback[slot]) return;

            var voice = instance.AcquireVoice(settings.category, settings.categoryLimit, settings.priority);
            if (voice == null) return;

            int voiceIndex = System.Array.IndexOf(instance.voices, voice);
            instance.nextPlayback[slot] = now + settings.cooldown;
            voice.clip = instance.cues[slot];
            voice.priority = settings.priority;
            voice.pitch = 1f + (float)(instance.pitchRandom.NextDouble()*2-1)*settings.pitchVariation;
            instance.voiceCategories[voiceIndex] = settings.category;
            instance.voiceGains[voiceIndex] = settings.gain;
            voice.volume = instance.sfxVolume * settings.gain;
            if (instance.mixerRoutes != null)
                voice.outputAudioMixerGroup = settings.category == ShadowgroveVoiceCategory.Ui
                    ? instance.mixerRoutes.Ui
                    : instance.mixerRoutes.Sfx;
            voice.Play();
        }

        // Call once at the starter weapon's cast event. Legacy profiles retain the kit cue.
        internal static void PlayHero(StarterWeaponKind kind)
        {
            switch (kind)
            {
                case StarterWeaponKind.ThornSlash: Play(SoundCue.HeroRowanThornSlash); break;
                case StarterWeaponKind.DarkOrb: Play(SoundCue.HeroLyraDarkOrb); break;
                case StarterWeaponKind.Arrow: Play(SoundCue.HeroKaelArrow); break;
                default: Play(SoundCue.Cast); break;
            }
        }

        // BossCatalog order: EclipseWraith, ThornRegent, FrostMourner,
        // CrimsonHarvester, AbyssOracle. Call at each boss cast telegraph.
        internal static void PlayBoss(int bossKind)
        {
            switch (bossKind)
            {
                case 0: Play(SoundCue.BossEclipseWraith); break;
                case 1: Play(SoundCue.BossThornRegent); break;
                case 2: Play(SoundCue.BossFrostMourner); break;
                case 3: Play(SoundCue.BossCrimsonHarvester); break;
                case 4: Play(SoundCue.BossAbyssOracle); break;
            }
        }

        private void Awake()
        {
            music = gameObject.AddComponent<AudioSource>();
            music.playOnAwake = false;
            music.loop = true;
            music.priority = 64;

            voices = new AudioSource[VoicePoolSize];
            voiceCategories = new ShadowgroveVoiceCategory[VoicePoolSize];
            voiceGains = new float[VoicePoolSize];
            for (int i = 0; i < voices.Length; i++)
            {
                var source = gameObject.AddComponent<AudioSource>();
                source.playOnAwake = false;
                source.loop = false;
                source.priority = 160;
                source.pitch = 1f;
                voices[i] = source;
            }

            menuMusic = LoadClip("MenuLoop");
            battleMusic = LoadClip("BattleLoop");
            var values = (SoundCue[])System.Enum.GetValues(typeof(SoundCue));
            cues = new AudioClip[values.Length];
            nextPlayback = new float[values.Length];
            foreach (var cue in values)
                cues[(int)cue] = LoadClip(ResourceName(cue));

            mixerRoutes = Resources.Load<ShadowgroveAudioMixerRoutes>(ResourcePath + "ShadowgroveAudioMixerRoutes");
            if (mixerRoutes != null)
            {
                music.outputAudioMixerGroup = mixerRoutes.Music;
            }
            else
            {
                Debug.LogWarning("Shadowgrove mixer routes are not configured. Run Tools/Shadowgrove/Configure Audio Prototype.");
            }
        }

        private AudioSource AcquireVoice(ShadowgroveVoiceCategory category, int categoryLimit, int priority)
        {
            int activeInCategory = 0;
            int activeTotal = 0;
            for (int i = 0; i < voices.Length; i++)
            {
                if (!voices[i].isPlaying) continue;
                activeTotal++;
                if (voiceCategories[i] == category) activeInCategory++;
            }

            if (activeInCategory >= categoryLimit)
            {
                int required=activeInCategory-categoryLimit+1,stealable=0;
                for(int i=0;i<voices.Length;i++)
                    if(voices[i].isPlaying&&voiceCategories[i]==category&&voices[i].priority>priority)stealable++;
                // A cue may impose a lower category cap than the preceding cue.
                // Reserve every required replacement before stopping any voice.
                if(stealable<required)return null;
                for(int i=0;i<required;i++)
                {
                    int categoryVictim=FindStealableVoice(category,priority);
                    if(categoryVictim<0)return null;
                    StopVoice(categoryVictim);activeTotal--;
                }
            }

            if (activeTotal >= voices.Length)
            {
                int victim = FindStealableVoice(null, priority);
                if (victim < 0) return null;
                StopVoice(victim);
            }

            for (int i = 0; i < voices.Length; i++)
                if (!voices[i].isPlaying) return voices[i];

            return null;
        }

        // Lower AudioSource.priority values are more important. Only interrupt a weaker voice.
        private int FindStealableVoice(ShadowgroveVoiceCategory? category, int incomingPriority)
        {
            int candidate = -1;
            int weakestPriority = incomingPriority;
            for (int i = 0; i < voices.Length; i++)
            {
                if (!voices[i].isPlaying || (category.HasValue && voiceCategories[i] != category.Value)) continue;
                if (voices[i].priority <= weakestPriority) continue;
                weakestPriority = voices[i].priority;
                candidate = i;
            }
            return candidate;
        }

        private void StopVoice(int index)
        {
            voices[index].Stop();
            voices[index].clip = null;
        }

        private void Update() => RefreshMusicVolume();

        private void RefreshMusicVolume()
        {
            bool bossCuePlaying = false;
            for (int i = 0; i < voices.Length; i++)
            {
                if (voices[i].isPlaying && voiceCategories[i] == ShadowgroveVoiceCategory.Boss)
                {
                    bossCuePlaying = true;
                    break;
                }
            }

            float targetDuck = bossCuePlaying ? BossCueMusicDuck : 1f;
            float rate = targetDuck < musicDuck ? 2.8f : 0.85f;
            musicDuck = Mathf.MoveTowards(musicDuck, targetDuck, Time.unscaledDeltaTime * rate);
            float pauseGain = inBattle && Time.timeScale <= 0f ? PausedMusicGain : 1f;
            music.volume = musicVolume * musicDuck * pauseGain;
        }

        private void RefreshVoiceVolumes()
        {
            for (int i = 0; i < voices.Length; i++)
                voices[i].volume = sfxVolume * voiceGains[i];
        }

        private static string ResourceName(SoundCue cue) => cue switch
        {
            SoundCue.HeroRowanThornSlash => "Hero_Rowan_ThornSlash_SyntheticSketch",
            SoundCue.HeroLyraDarkOrb => "Hero_Lyra_DarkOrb_SyntheticSketch",
            SoundCue.HeroKaelArrow => "Hero_Kael_Arrow_SyntheticSketch",
            SoundCue.BossEclipseWraith => "Boss_EclipseWraith_SyntheticSketch",
            SoundCue.BossThornRegent => "Boss_ThornRegent_SyntheticSketch",
            SoundCue.BossFrostMourner => "Boss_FrostMourner_SyntheticSketch",
            SoundCue.BossCrimsonHarvester => "Boss_CrimsonHarvester_SyntheticSketch",
            SoundCue.BossAbyssOracle => "Boss_AbyssOracle_SyntheticSketch",
            _ => cue.ToString()
        };

        private static (ShadowgroveVoiceCategory category, float gain, float cooldown,
            int priority, int categoryLimit, float pitchVariation) GetCueSettings(SoundCue cue) => cue switch
        {
            SoundCue.UiSelect => (ShadowgroveVoiceCategory.Ui, 0.55f, 0.06f, 64, 2, 0.012f),
            SoundCue.UiConfirm => (ShadowgroveVoiceCategory.Ui, 0.62f, 0.07f, 56, 2, 0.012f),
            SoundCue.PlayerHit => (ShadowgroveVoiceCategory.Combat, 0.65f, 0.19f, 24, 3, 0.02f),
            SoundCue.EnemyHit => (ShadowgroveVoiceCategory.Combat, 0.26f, 0.105f, 160, 4, 0.035f),
            SoundCue.Experience => (ShadowgroveVoiceCategory.Reward, 0.42f, 0.125f, 180, 3, 0.025f),
            SoundCue.Gold => (ShadowgroveVoiceCategory.Reward, 0.47f, 0.12f, 176, 3, 0.025f),
            SoundCue.LevelUp => (ShadowgroveVoiceCategory.Reward, 0.72f, 0.4f, 72, 2, 0.01f),
            SoundCue.Treasure => (ShadowgroveVoiceCategory.Reward, 0.72f, 0.2f, 88, 2, 0.012f),
            SoundCue.Victory => (ShadowgroveVoiceCategory.Ui, 0.72f, 0.2f, 40, 2, 0.01f),
            SoundCue.Defeat => (ShadowgroveVoiceCategory.Ui, 0.72f, 0.2f, 40, 2, 0.01f),
            SoundCue.Cast => (ShadowgroveVoiceCategory.Hero, 0.22f, 0.16f, 120, 2, 0.03f),
            SoundCue.Heal => (ShadowgroveVoiceCategory.Combat, 0.72f, 0.2f, 72, 3, 0.015f),
            SoundCue.HeroRowanThornSlash => (ShadowgroveVoiceCategory.Hero, 0.7f, 0.08f, 88, 2, 0.025f),
            SoundCue.HeroLyraDarkOrb => (ShadowgroveVoiceCategory.Hero, 0.66f, 0.08f, 84, 2, 0.02f),
            SoundCue.HeroKaelArrow => (ShadowgroveVoiceCategory.Hero, 0.66f, 0.08f, 88, 2, 0.025f),
            SoundCue.BossEclipseWraith => (ShadowgroveVoiceCategory.Boss, 0.62f, 0.45f, 52, 1, 0.012f),
            SoundCue.BossThornRegent => (ShadowgroveVoiceCategory.Boss, 0.68f, 0.45f, 52, 1, 0.012f),
            SoundCue.BossFrostMourner => (ShadowgroveVoiceCategory.Boss, 0.62f, 0.45f, 52, 1, 0.012f),
            SoundCue.BossCrimsonHarvester => (ShadowgroveVoiceCategory.Boss, 0.66f, 0.45f, 48, 1, 0.012f),
            SoundCue.BossAbyssOracle => (ShadowgroveVoiceCategory.Boss, 0.62f, 0.45f, 52, 1, 0.012f),
            _ => (ShadowgroveVoiceCategory.Combat, 0.77f, 0.2f, 96, 3, 0.02f)
        };

        private static AudioClip LoadClip(string name)
        {
            var clip = Resources.Load<AudioClip>(ResourcePath + name);
            if (clip == null) Debug.LogError($"Missing Shadowgrove audio clip: {name}");
            return clip;
        }
    }
}
