#region

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

#endregion

namespace RGame.RoguelikeKit
{
    public class LevelSelectPanel : MonoBehaviour
    {
        private static readonly Color NormalColor = Color.white;
        private static readonly Color FocusColor = new Color(1.1f, 1.1f, 0.87f, 1f);
        [SerializeField] private GameObject mSelectList;
        [SerializeField] private LevelItem mLevelPrefab;
        [SerializeField] private List<LevelConfigSO> mLevelSos;
        private List<LevelItem> _levelItems = new List<LevelItem>();
        public UnityAction Closed;
        public UnityAction Confirm;

        private void Start()
        {
            CreateLevelItem();
        }

        private void CreateLevelItem()
        {
            foreach (var item in mLevelSos)
            {
                var levelItem = Instantiate(mLevelPrefab, mSelectList.transform);

                levelItem.Initialization(item);
                
                levelItem.OnPointerEnterAction += PointerEnterAction;
                levelItem.OnPointerExitAction += PointerExitAction;
                levelItem.OnPointerDownAction += PointerDownAction;
                _levelItems.Add(levelItem);
            }
            DATN.UI.ForestUI.Refresh();
        }

        public void ClosedButton()
        {
            ShadowgroveAudio.Play(SoundCue.UiSelect);
            Closed.Invoke();
        }

        public void ConfirmButton()
        {
            ShadowgroveAudio.Play(SoundCue.UiConfirm);
            Confirm.Invoke();
        }
        
        private void PointerEnterAction(LevelItem item)
        {
            item.BackgroundImage.color = FocusColor;
        }

        private void PointerExitAction(LevelItem item)
        {
            if (item.IsSelected) return;
            item.BackgroundImage.color = NormalColor;
        }

        private void PointerDownAction(LevelItem item)
        {
            for (int i = 0; i < _levelItems.Count; i++)
            {
                _levelItems[i].BackgroundImage.color = NormalColor;
                _levelItems[i].IsSelected = false;
            }
            item.BackgroundImage.color = FocusColor;
            item.IsSelected = true;
            ShadowgroveAudio.Play(SoundCue.UiSelect);
        }

        private void OnDestroy()
        {
            for (int i = 0; i < _levelItems.Count; i++)
            {
                _levelItems[i].OnPointerEnterAction -= PointerEnterAction;
                _levelItems[i].OnPointerExitAction -= PointerExitAction;
                _levelItems[i].OnPointerDownAction -= PointerDownAction;
            }
        }
    }
}

