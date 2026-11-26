using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using RGame.Framework;
using UnityEngine;

namespace RGame.RoguelikeKit
{
    public class BuffManager : MonoBehaviour
    {
        [SerializeField] private AddBuffEvent _addBuffEvent;
        [SerializeField] private RemoveAllBuffEvent _removeAllBuffEvent;
        [SerializeField] private VoidEventChannelSO _clearAllBuffsEvent;
       private readonly Dictionary<BaseBuff, float> mBuffActivateDic = new Dictionary<BaseBuff, float>();
        private readonly List<BaseBuff> mBuffActivateKeys = new List<BaseBuff>();
        private readonly Dictionary<(BaseEnemy, string), BaseBuff> mOwnerBuff = new Dictionary<(BaseEnemy, string), BaseBuff>();
        private readonly List<BaseBuff> mWaitRemoveKey = new List<BaseBuff>();
        internal List<RunBuff> CaptureRunBuffs(RunStateFields fields)
        {
            var result=new List<RunBuff>();
            foreach(var buff in mBuffActivateKeys)
            {
                if(buff.Owner==null||!buff.Owner.gameObject.activeInHierarchy||fields.Id(buff.Owner)==null)continue;
                string name=buff.name.Replace("(Clone)","").Trim();
                result.Add(new RunBuff{owner=fields.Id(buff.Owner),asset=buff.GetType().FullName+":"+name,age=mBuffActivateDic[buff],fields=fields.Capture(buff)});
            }
            return result;
        }
        internal void RestoreRunBuffs(List<RunBuff> saved,RunStateFields fields)
        {
            foreach(var state in saved)
            {
                var owner=fields.Resolve(state.owner) as BaseEnemy;
                var asset=Resources.FindObjectsOfTypeAll<BaseBuff>().FirstOrDefault(b=>!b.name.EndsWith("(Clone)")&&b.GetType().FullName+":"+b.name==state.asset);
                if(owner==null||asset==null||state.age<0||state.age>asset.Duration+1)throw new ArgumentException("Invalid saved status effect");
                AddBuff(owner,asset);
                var instance=mBuffActivateKeys.Last(b=>b.Owner==owner&&b.GetType()==asset.GetType());
                fields.Restore(instance,state.fields);mBuffActivateDic[instance]=state.age;
            }
        }

        private void OnEnable()
        {
            _addBuffEvent.RegisterListener(AddBuff);
            _removeAllBuffEvent.RegisterListener(RemoveBuff);
            _clearAllBuffsEvent.RegisterListener(ClearAllBuffs);
        }

        private void OnDisable()
        {
            _addBuffEvent.UnregisterListener(AddBuff);
            _removeAllBuffEvent.UnregisterListener(RemoveBuff);
            _clearAllBuffsEvent.UnregisterListener(ClearAllBuffs);
        }

        private void FixedUpdate()
        {
            BuffTimer();
        }

        public void AddBuff(BaseEnemy _owner, BaseBuff _buff)
        {
            if(_owner is EndlessBossEnemy boss && boss.Entering)return;
            if (mOwnerBuff.ContainsKey((_owner, _buff.BuffName)))
            {
                var existingBuff = mOwnerBuff[(_owner, _buff.BuffName)];
                
                mBuffActivateDic[existingBuff] = 0f;
            }
            else
            {
                var buff = Instantiate(_buff);
                if (buff == null) return;

                mOwnerBuff[(_owner, buff.BuffName)] = buff;
                mBuffActivateDic.Add(buff, 0f);
                mBuffActivateKeys.Add(buff);
                buff.Owner = _owner;
                _owner.Returned-=OnOwnerReturned;_owner.Returned+=OnOwnerReturned;
                buff.Activate();
            }
        }

        public void RemoveBuff(BaseBuff _buff)
        {
            if(!mBuffActivateDic.ContainsKey(_buff))return;
            var owner=_buff.Owner;
            _buff.DeActivate();
            
            mBuffActivateDic.Remove(_buff);
            mBuffActivateKeys.Remove(_buff);
            
            var ownerBuffPair = mOwnerBuff.FirstOrDefault(x => x.Value == _buff);
            if (ownerBuffPair.Value != null) mOwnerBuff.Remove(ownerBuffPair.Key);
            if(owner!=null&&!mBuffActivateKeys.Any(b=>b.Owner==owner))owner.Returned-=OnOwnerReturned;
        }
        private void OnOwnerReturned(BaseEnemy owner)
        {
            foreach(var buff in mBuffActivateKeys.Where(b=>b.Owner==owner).ToArray())RemoveBuff(buff);
        }
        
        public void ClearAllBuffs()
        {
            var allBuffs = mBuffActivateKeys.ToList();
            foreach (var buff in allBuffs)
            {
                RemoveBuff(buff);
            }
        }
        
        private void BuffTimer()
        {
            foreach (var key in mBuffActivateKeys)
            {
                mBuffActivateDic[key] += Time.fixedDeltaTime;

                if (mBuffActivateDic[key] > key.Duration) mWaitRemoveKey.Add(key);

                if (mBuffActivateDic[key] % key.TickInterval < Time.fixedDeltaTime) key.TickEffect();
            }

            foreach (var key in mWaitRemoveKey) RemoveBuff(key);

            mWaitRemoveKey.Clear();
        }
    }
}
