using System;
using System.Collections.Generic;
using RGame.Framework;
using UnityEngine;

namespace RGame.RoguelikeKit
{
    public class UpgradeSkillPanel : MonoBehaviour
    {
        [SerializeField] private UpgradeSkillUIChannel _upgradeSkillUIChannel;
        [SerializeField] private StringEventChannelSO _upgradeSkillChannel;
        [SerializeField] private UpgradeSkillItem[] _upgradeSkillPrefab;
        [SerializeField] private CanvasGroup _canvasGroup;
        
        private void OnEnable()
        {
            _upgradeSkillUIChannel.RegisterListener(SkillUI);
        }

        /// <summary>Panel dang hien hay khong (dung cho kiem thu tu dong).</summary>
        public bool IsVisible => _canvasGroup != null && _canvasGroup.alpha > 0.5f;

        private void OnDisable()
        {
            _upgradeSkillUIChannel.UnregisterListener(SkillUI);
        }

        private void SkillUI(List<SkillDataSO> skills)
        {
            Show();

            for (int i = 0; i < skills.Count; i++)
            {
                _upgradeSkillPrefab[i].UpgradeSkillAction += UpgradeSkill;
                
                // Set UI for skill or money bag
                _upgradeSkillPrefab[i].SetUI(skills[i]);
            }
        }
        
        public void Show()
        {
            // Keep the dimmer and cards above the HUD created after scene load.
            transform.SetAsLastSibling();
            Time.timeScale = 0;
            
            _canvasGroup.alpha = 1f;
            _canvasGroup.interactable = true; 
            _canvasGroup.blocksRaycasts = true; 
        }

        public void UpgradeSkill(SkillDataSO skill)
        {
            if (!isActiveAndEnabled || !IsVisible) return;
            ShadowgroveAudio.Play(SoundCue.UiConfirm);
            Time.timeScale = 1;
            
            // Handle money bag selection or regular skill upgrade
            if (skill.IsMoneyBag())
            {
                _upgradeSkillChannel.RaiseEvent("MoneyBag");
            }
            else
            {
                _upgradeSkillChannel.RaiseEvent(skill.Key);
            }
            
            _canvasGroup.alpha = 0f;
            _canvasGroup.interactable = false; 
            _canvasGroup.blocksRaycasts = false; 
            
            for (int i = 0; i < _upgradeSkillPrefab.Length; i++)
            {
                _upgradeSkillPrefab[i].UpgradeSkillAction -= UpgradeSkill;
            }
        }
    }
}
