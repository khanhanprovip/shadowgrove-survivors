using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using RGame.RoguelikeKit;
using UnityEngine;

namespace DATN.Trial
{
    public sealed partial class DatnRegression
    {
        private static readonly BindingFlags AudioBudgetFlags = BindingFlags.Instance | BindingFlags.Static |
            BindingFlags.NonPublic | BindingFlags.Public;

        IEnumerator CheckAudioBudget()
        {
            var audio = FindFirstObjectByType<ShadowgroveAudio>();
            if (audio == null) throw new InvalidOperationException("Persistent audio player is missing");

            var voiceField = typeof(ShadowgroveAudio).GetField("voices", AudioBudgetFlags);
            var categoryField = typeof(ShadowgroveAudio).GetField("voiceCategories", AudioBudgetFlags);
            var cooldownField = typeof(ShadowgroveAudio).GetField("nextPlayback", AudioBudgetFlags);
            var settingsMethod = typeof(ShadowgroveAudio).GetMethod("GetCueSettings", AudioBudgetFlags);
            var voices = (AudioSource[])voiceField?.GetValue(audio);
            var categories = (ShadowgroveVoiceCategory[])categoryField?.GetValue(audio);
            var cooldowns = (float[])cooldownField?.GetValue(audio);
            if (voices == null || categories == null || cooldowns == null || settingsMethod == null)
                throw new InvalidOperationException("Audio budget runtime state is unavailable");

            var cues = (SoundCue[])Enum.GetValues(typeof(SoundCue));
            int sourceCount = audio.GetComponents<AudioSource>().Length;
            bool assertionsPassed = true;
            void Check(bool condition, string detail)
            {
                if (condition) Record("PASS", detail);
                else { Record("FAIL", detail); assertionsPassed = false; }
            }

            object CueSetting(SoundCue cue, int item)
            {
                object settings = settingsMethod.Invoke(null, new object[] { cue });
                return settings.GetType().GetField("Item" + item).GetValue(settings);
            }

            ShadowgroveVoiceCategory CueCategory(SoundCue cue) =>
                (ShadowgroveVoiceCategory)CueSetting(cue, 1);
            int CueLimit(SoundCue cue) => (int)CueSetting(cue, 5);
            int CuePriority(SoundCue cue) => (int)CueSetting(cue, 4);

            int ActiveCount()
            {
                int count = 0;
                foreach (var voice in voices) if (voice != null && voice.isPlaying) count++;
                return count;
            }

            int ActiveIn(ShadowgroveVoiceCategory category)
            {
                int count = 0;
                for (int i = 0; i < voices.Length; i++)
                    if (voices[i] != null && voices[i].isPlaying && categories[i] == category) count++;
                return count;
            }

            bool HasPriority(ShadowgroveVoiceCategory category, int priority)
            {
                for (int i = 0; i < voices.Length; i++)
                    if (voices[i] != null && voices[i].isPlaying && categories[i] == category &&
                        voices[i].priority == priority) return true;
                return false;
            }

            void PlayFresh(SoundCue cue)
            {
                // Bypass only the per-cue debounce so every call exercises the real public Play path.
                cooldowns[(int)cue] = Time.unscaledTime - 1f;
                ShadowgroveAudio.Play(cue);
            }

            foreach (var voice in voices)
            {
                if (voice == null) continue;
                voice.Stop();
                voice.clip = null;
            }

            Check(voices.Length == 12 && sourceCount == 13,
                $"Audio host owns 12 reusable voices plus one music source (voices={voices.Length}, sources={sourceCount})");
            Check(cues.Length > 0 && cues.Length == cooldowns.Length,
                $"Every public sound cue has an independent cooldown slot ({cues.Length})");

            var combat = CueCategory(SoundCue.EnemyHit);
            Check(combat == CueCategory(SoundCue.PlayerHit) && CueLimit(SoundCue.EnemyHit) == 4 &&
                  CueLimit(SoundCue.PlayerHit) == 3,
                "Combat repro uses EnemyHit cap 4 and incoming PlayerHit cap 3 in the same category");

            for (int i = 0; i < 4; i++) PlayFresh(SoundCue.EnemyHit);
            int combatBefore = ActiveIn(combat);
            Check(combatBefore == 4, $"Four EnemyHit voices fill the Combat cap (actual={combatBefore})");

            PlayFresh(SoundCue.PlayerHit);
            int combatAfterPlayerHit = ActiveIn(combat);
            Record("INFO", $"PlayerHit cap repro: Combat before={combatBefore}, after={combatAfterPlayerHit}, " +
                            $"incoming limit={CueLimit(SoundCue.PlayerHit)}, active voices={ActiveCount()}/12, " +
                            $"priority={CuePriority(SoundCue.PlayerHit)}");
            Check(combatAfterPlayerHit <= CueLimit(SoundCue.PlayerHit),
                $"Incoming PlayerHit enforces its Combat cap ({combatAfterPlayerHit}/{CueLimit(SoundCue.PlayerHit)})");
            Check(HasPriority(combat, CuePriority(SoundCue.PlayerHit)),
                "Higher-priority PlayerHit is playing after it replaces lower-priority EnemyHit");

            // Return Combat to its larger cap, then prove equal-priority spam cannot evict PlayerHit.
            PlayFresh(SoundCue.EnemyHit);
            int combatAtFour = ActiveIn(combat);
            int totalAtFour = ActiveCount();
            PlayFresh(SoundCue.EnemyHit);
            Check(combatAtFour == CueLimit(SoundCue.EnemyHit),
                $"EnemyHit can use its configured four-voice Combat cap ({combatAtFour}/4)");
            Check(ActiveIn(combat) == combatAtFour && ActiveCount() == totalAtFour &&
                  HasPriority(combat, CuePriority(SoundCue.PlayerHit)),
                "Equal-priority EnemyHit at cap is rejected without stealing the stronger PlayerHit");

            var categoryCaps = new Dictionary<ShadowgroveVoiceCategory, int>
            {
                { ShadowgroveVoiceCategory.Ui, 2 },
                { ShadowgroveVoiceCategory.Hero, 2 },
                { ShadowgroveVoiceCategory.Boss, 1 },
                { ShadowgroveVoiceCategory.Combat, 4 },
                { ShadowgroveVoiceCategory.Reward, 3 }
            };

            // Force mixed category load before stressing every cue on one player frame.
            for (int i = 0; i < 3; i++) PlayFresh(SoundCue.Experience);
            for (int i = 0; i < 2; i++) PlayFresh(SoundCue.Cast);
            for (int i = 0; i < 2; i++) PlayFresh(SoundCue.UiSelect);
            PlayFresh(SoundCue.BossEclipseWraith);
            Check(ActiveCount() <= voices.Length,
                $"Mixed audio load stays within the fixed pool ({ActiveCount()}/{voices.Length})");
            foreach (var cap in categoryCaps)
                Check(ActiveIn(cap.Key) <= cap.Value,
                    $"{cap.Key} category stays within its voice cap ({ActiveIn(cap.Key)}/{cap.Value})");

            int peakVoices = ActiveCount();
            var peakByCategory = new Dictionary<ShadowgroveVoiceCategory, int>();
            foreach (var cap in categoryCaps) peakByCategory[cap.Key] = ActiveIn(cap.Key);
            float burstFrame = Time.unscaledTime;
            for (int i = 0; i < 100; i++)
            {
                PlayFresh(cues[i % cues.Length]);
                peakVoices = Mathf.Max(peakVoices, ActiveCount());
                foreach (var cap in categoryCaps)
                    peakByCategory[cap.Key] = Mathf.Max(peakByCategory[cap.Key], ActiveIn(cap.Key));
            }

            Check(Mathf.Approximately(Time.unscaledTime, burstFrame),
                "100 public cue requests are stressed within one unscaled player frame");
            Check(peakVoices <= voices.Length,
                $"100-cue burst never exceeds the 12-source pool (peak={peakVoices})");
            foreach (var cap in categoryCaps)
                Check(peakByCategory[cap.Key] <= cap.Value,
                    $"100-cue burst respects {cap.Key} cap (peak={peakByCategory[cap.Key]}/{cap.Value})");
            Check(audio.GetComponents<AudioSource>().Length == sourceCount,
                $"Audio burst allocates no extra AudioSources (before={sourceCount}, after={audio.GetComponents<AudioSource>().Length})");

            Record("INFO", $"Audio budget summary: cues={cues.Length}, burst=100, peak={peakVoices}/12, " +
                            $"sources={audio.GetComponents<AudioSource>().Length}, Combat PlayerHit={combatAfterPlayerHit}/{CueLimit(SoundCue.PlayerHit)}");
            foreach (var voice in voices)
            {
                if (voice == null) continue;
                voice.Stop();
                voice.clip = null;
            }

            if (!assertionsPassed)
                throw new InvalidOperationException("Audio voice budget or priority assertion failed; see measured counts above.");
            yield return null;
        }
    }
}
