using UnityEngine;
using UnityEngine.Audio;

namespace RGame.RoguelikeKit
{
    // Runtime-created audio sources use this Resources asset to route into the
    // project's existing mixer without embedding editor-only asset lookups.
    public sealed class ShadowgroveAudioMixerRoutes : ScriptableObject
    {
        [SerializeField] private AudioMixerGroup music;
        [SerializeField] private AudioMixerGroup sfx;
        [SerializeField] private AudioMixerGroup ui;

        public AudioMixerGroup Music { get => music; set => music = value; }
        public AudioMixerGroup Sfx { get => sfx; set => sfx = value; }
        public AudioMixerGroup Ui { get => ui; set => ui = value; }
    }
}
