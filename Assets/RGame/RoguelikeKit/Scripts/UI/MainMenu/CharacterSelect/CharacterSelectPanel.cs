#region

using System;
using System.Collections.Generic;
using RGame.Framework;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

#endregion

namespace RGame.RoguelikeKit
{
    public class CharacterSelectPanel : MonoBehaviour
    {
        private static readonly Color NormalColor = Color.white;
        private static readonly Color FocusColor = new Color(1.1f, 1.1f, 0.87f, 1f);
        [SerializeField] private GameObject mSelectList;
        [SerializeField] private CharacterItem mCharacterPrefab;
        [SerializeField] private List<CharacterSelectConfigSO> mCharacterSos;
        [SerializeField] private CharacterSaveSO mCharacterSave;
        [SerializeField] private AttributeSO mAttributeSo;
        [SerializeField] private VoidEventChannelSO _saveGameChannel;
        private List<CharacterItem> _characterItems = new List<CharacterItem>();
        public UnityAction Closed;
        public UnityAction Confirm;

        private void Start()
        {
            CreateCharacterItem();
            DATN.UI.ForestUI.Refresh();
        }

        private void CreateCharacterItem()
        {
            // Shadowgrove: 3 nhan vat goc (Rowan/Lyra/Kael) la noi dung chinh cua
            // do an nen mo khoa toan bo — bo co che khoa slot theo save cua kit.
            for (int i = 0; i < mCharacterSos.Count; i++)
            {
                bool isUnlocked = true;
                var characterItem = Instantiate(mCharacterPrefab, mSelectList.transform);
                characterItem.Initialization(mCharacterSos[i], isUnlocked);
                characterItem.BackgroundImage.color = NormalColor;
                characterItem.OnPointerEnterAction += PointerEnterAction;
                characterItem.OnPointerExitAction += PointerExitAction;
                characterItem.OnPointerDownAction += PointerDownAction;
                _characterItems.Add(characterItem);
                characterItem.CharacterID = i;
            }
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

        private void PointerEnterAction(CharacterItem item, bool isUnlocked)
        {
            item.BackgroundImage.color = FocusColor;
        }

        private void PointerExitAction(CharacterItem item, bool isUnlocked)
        {
            if (item.IsSelected) return;
            item.BackgroundImage.color = NormalColor;
        }

        private void PointerDownAction(CharacterItem item, bool isUnlocked)
        {
            if (isUnlocked)
            {
                for (int i = 0; i < _characterItems.Count; i++)
                {
                    _characterItems[i].BackgroundImage.color = NormalColor;
                    _characterItems[i].IsSelected = false;
                }
                item.BackgroundImage.color = FocusColor;
                item.IsSelected = true;
                ShadowgroveAudio.Play(SoundCue.UiSelect);
            }
            else
            {
                if (item.SellPrice <= mAttributeSo.GoldCount)
                {
                    mAttributeSo.SetAttribute("GoldCount",mAttributeSo. GoldCount - item.SellPrice);
                    item.IsUnlocked = true;
                    item.SellGameObject.SetActive(false);

                    if (item.CharacterID == 1)
                    {
                        mCharacterSave.SaveCharacter(true, mCharacterSave.CharacterThree);
                    }
                    
                    if (item.CharacterID == 2)
                    {
                        mCharacterSave.SaveCharacter(mCharacterSave.CharacterTwo, true);
                    }

                    _saveGameChannel.RaiseEvent();
                }
            }
        }

        private void OnDestroy()
        {
            for (int i = 0; i < _characterItems.Count; i++)
            {
                _characterItems[i].OnPointerEnterAction -= PointerEnterAction;
                _characterItems[i].OnPointerExitAction -= PointerExitAction;
                _characterItems[i].OnPointerDownAction -= PointerDownAction;
            }
        }
    }
}

