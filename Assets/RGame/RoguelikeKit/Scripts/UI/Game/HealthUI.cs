using System;
using RGame.CommonStat;
using RoguelikeKit;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace RGame.RoguelikeKit
{
    public class HealthUI : MonoBehaviour
    {
        [SerializeField] private CommonStatRuntimeSO mStat;
        [SerializeField] private Image mHPBar;
        [SerializeField] private Image mHpTransitionBar;
        
        private BarAnimator mHpBar;
        private UnityAction<int, int> mHPChange;
        
        private void Start()
        {
            if (mStat == null)
            {
                Debug.LogError("[HealthUI] CommonStatRuntimeSO chua duoc gan — HP bar se khong hoat dong", this);
                enabled = false;
                return;
            }

            mHpBar = new BarAnimator(mHPBar, mHpTransitionBar, 0.5f);

            mHPChange += mHpBar.SetValue;
            mStat.AddAction("HP", mHPChange);
        }

        private void OnDestroy()
        {
            // Start() co the chua chay neu object bi huy ngay sau khi kich dong
            // (vd. restart man nhanh) — phai kiem null truoc khi giai phong.
            if (mStat != null && mHPChange != null)
            {
                mStat.RemoveAction("HP", mHPChange);
            }

            mHpBar?.Kill();
            mHPChange = null;
        }
    }
}
