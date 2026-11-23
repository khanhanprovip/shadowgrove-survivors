#region

using System;
using System.Collections.Generic;
using DG.Tweening;
using RGame.Framework;
using Unity.VisualScripting;
using UnityEngine;
using Random = UnityEngine.Random;

#endregion

namespace RGame.RoguelikeKit
{
    [Serializable]
    public class DropOutGO
    {
        public string Key;
        public int Percent;
    }
    
    public class DropBox : MonoBehaviour
    {
        [SerializeField] private PoolRuntimeSO _poolRuntimeSO;
        [SerializeField] private DissolveAnimationSO _dissolveAnimationSO;
        [SerializeField] private SpriteRenderer _spriteRenderer;
        [SerializeField] private SpriteRenderer _shadowRenderer;
        [SerializeField] private List<DropOutGO> _dropOutGoes;
        private bool hasSentDissolveCommand;
        [SerializeField] private GlobalConfigSO _touchGoldConfig;
        [SerializeField, Min(1)] private int _touchGold = 50;

        private void FixedUpdate()
        {
            if (hasSentDissolveCommand) return;

            // Jade gold chests are collected on touch, rather than broken by
            // stray projectiles. Other legacy breakable props keep their drops.
            if (_touchGoldConfig != null)
            {
                foreach (var hit in Physics2D.OverlapCircleAll(transform.position, 0.55f))
                {
                    if (!hit.CompareTag("PlayerHit")) continue;
                    hasSentDissolveCommand = true;
                    _touchGoldConfig.CurrentGetGold += _touchGold;
                    ShadowgroveAudio.Play(SoundCue.Gold);
                    gameObject.SetActive(false);
                    Destroy(gameObject);
                    break;
                }
                return;
            }

            var colliders = Physics2D.OverlapCircleAll(transform.position, 0.2f);

            foreach (var collider in colliders)
                if (collider.CompareTag("Skill"))
                {
                    if (!hasSentDissolveCommand) hasSentDissolveCommand = true;
                    
                    _dissolveAnimationSO.Play(_spriteRenderer);
                    _shadowRenderer.DOFade(0, 0.35f).OnComplete(() =>
                    {
                        Destroy(transform.gameObject);
                    });
                    CreateDropOutGoes();
                    return;
                }
        }

        private void CreateDropOutGoes()
        {
            if (_dropOutGoes == null || _dropOutGoes.Count == 0) return;

            int totalPercent = 0;
            foreach (var drop in _dropOutGoes)
            {
                totalPercent += drop.Percent;
            }

            int randomValue = Random.Range(0, totalPercent);
            int accumulated = 0;

            foreach (var drop in _dropOutGoes)
            {
                accumulated += drop.Percent;
                if (randomValue < accumulated)
                {
                    var go = _poolRuntimeSO.Request(drop.Key);
                    go.transform.position = transform.position;
                    return;
                }
            }
        }
    }
}
