using System;
using RGame.Framework;
using UnityEngine;

namespace RGame.RoguelikeKit
{
    public class TreasureBox : MonoBehaviour, IPoolR
    {
        public string Key { get; set; }
        
        [SerializeField] private PoolRuntimeSO _poolRuntime;
        [SerializeField] private VoidEventChannelSO _openTreasureChannel;
        [SerializeField] private GlobalConfigSO _globalConfig;
        [SerializeField, Min(1)] private int _goldCount = 50;
        private bool _collected;

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!_collected && other.CompareTag("PlayerHit") && _globalConfig != null)
            {
                // Claim before raising any events/returning to the pool: multiple
                // player colliders must never collect the same chest twice.
                _collected = true;
                _globalConfig.CurrentGetGold += _goldCount;
                ShadowgroveAudio.Play(SoundCue.Gold);
                _poolRuntime.Return(this.gameObject);
            }
        }
        
        public void Request()
        {
            _collected = false;
        }

        public void Return()
        {
            _collected = true;
        }
    }
}
