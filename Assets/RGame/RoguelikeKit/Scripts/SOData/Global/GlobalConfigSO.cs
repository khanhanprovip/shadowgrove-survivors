#region

using System;
using RGame.Framework;
using UnityEngine;

#endregion

namespace RGame.RoguelikeKit
{
    public class GlobalConfigSO : DescriptionBaseSO
    {
        public float MoveSpeedBalanceFactor = 0.05f;
        public Player GlobalPlayer { get; set; }
        public int CurrentGetGold;
        public int CurrentGetKill;
        [SerializeField] private SaveSystemSO _saveSystem;
        private int bankedGold;
        public int UnbankedGold => Math.Max(0, CurrentGetGold-bankedGold);
        public void BankGold()
        {TryBankGold();}
        public bool TryBankGold()
        {
            int amount=UnbankedGold;
            if(amount<=0)return true;
            if(_saveSystem==null||!_saveSystem.TryBankEarnings(amount))return false;
            bankedGold=CurrentGetGold;return true;
        }
        public event Action EnemyDefeated;

        public void RegisterEnemyDefeat()
        {
            CurrentGetKill++;
            EnemyDefeated?.Invoke();
        }

        public void OnGameStart()
        {
            CurrentGetGold = 0;
            bankedGold = 0;
            CurrentGetKill = 0;
        }
    }
}
