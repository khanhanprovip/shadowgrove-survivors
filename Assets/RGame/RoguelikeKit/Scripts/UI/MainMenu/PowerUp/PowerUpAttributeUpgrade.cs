using System;
using System.Collections.Generic;
using System.Reflection;
using RGame.CommonStat;
using RGame.Framework;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace RGame.RoguelikeKit
{
    public class PowerUpAttributeUpgrade : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler
    {
        [SerializeField] private CommonStatRuntimeSO mStatSO;
        [SerializeField] private string mAttributeName;
        [SerializeField] private string mDescription;
        
        [Space(10)] [Header("SaveInfo")]
        [SerializeField] private VoidEventChannelSO _saveGameChannel;
        [SerializeField] private VoidEventChannelSO mUpdateGoldUI;
        [SerializeField] private AttributeSO mAttributeSo;
        
        [Space(10)] [Header("LevelInfo")]
        [SerializeField] private int mMaxLevel;
        [SerializeField] private List<int> mRequireGold;
        [SerializeField] private List<int> mAttributeChangeValue;
        
        [Space(10)] [Header("LevelUIInfo")]
        [SerializeField] private Transform mLevelParent;
        [SerializeField] private ChildObjectController mLevelInfo;

        [Space(10)] [Header("ShowTip")] [SerializeField]
        private Image mBG;

        private Color mOriginColor;
        private List<ChildObjectController> mLevelInfos = new List<ChildObjectController>();
        private int mCurLevel;

        public UnityAction<string,string,int,UnityAction> SelectAction;

        public void SetHitSurface(Image surface)
        {
            mBG = surface;
            mBG.raycastTarget = true;
        }
        
        private void Awake()
        {
            InitLevel();
            mOriginColor = mBG.color;
        }

        private void Start()
        {
            PermanentBalance.Apply(mAttributeSo, mStatSO);
            var tempLevel = mAttributeSo.GetAttribute(mAttributeName);
            for (int i = 0; i < Mathf.Min(tempLevel, mMaxLevel); i++)
            {
                UpgradeInit();
            }
        }

        private void InitLevel()
        {
            foreach (Transform child in mLevelParent.transform)
            {
                Destroy(child.gameObject);
            }

            for (int i = 0; i < mMaxLevel; i++)
            {
                var levelInfo = Instantiate(mLevelInfo, mLevelParent);

                levelInfo.ShowAction += () => { levelInfo.BG.color = Color.black; };
                
                levelInfo.HideChildren();
                
                mLevelInfos.Add(levelInfo);
            }
        }
        
        private void UpgradeInit()
        {
            mLevelInfos[mCurLevel].ShowChildren();
            
            mCurLevel++;
        }
        
        private void Upgrade()
        {
            if (mCurLevel >= mMaxLevel) return;

            int cost = PermanentBalance.Cost(mAttributeName, mCurLevel);
            if (mAttributeSo.GoldCount < cost) return;
            
            mLevelInfos[mCurLevel].ShowChildren();
            
            mCurLevel++;
            mAttributeSo.SetAttribute("GoldCount",mAttributeSo.GoldCount - cost);
            mAttributeSo.SetAttribute(mAttributeName,mCurLevel);
            PermanentBalance.Apply(mAttributeSo, mStatSO);
            _saveGameChannel.RaiseEvent();
            mUpdateGoldUI.RaiseEvent();
            UpdateInfo();
        }
        
        public void OnPointerEnter(PointerEventData eventData)
        {
            mBG.color = new Color(1.1f, 1.1f, .87f);
        }
        
        public void OnPointerExit(PointerEventData eventData)
        {
            mBG.color = Color.white;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            UpdateInfo();
        }

        public void UpdateInfo()
        {
            if (mCurLevel >= mMaxLevel)
            {
                SelectAction?.Invoke(mAttributeName, mDescription, -1, null);
                return;
            }
            
            SelectAction?.Invoke(mAttributeName,mDescription,PermanentBalance.Cost(mAttributeName,mCurLevel),Upgrade);
        }
    }
}
