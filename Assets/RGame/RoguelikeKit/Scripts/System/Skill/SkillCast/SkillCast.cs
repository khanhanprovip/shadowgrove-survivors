using RGame.CommonStat;
using RGame.Framework;
using UnityEngine;

namespace RGame.RoguelikeKit
{
    public class SkillCast : MonoBehaviour
    {
        public string Key;
        [SerializeField] protected PoolRuntimeSO _pool;
        [SerializeField] protected CommonStatRuntimeSO _stat;
        [SerializeField] protected GlobalConfigSO _globalConfig;
        [SerializeField] protected EnemySystem _enemySystem;
        [SerializeField] private Vector3 _originLocalScale;
        
        protected SkillDataSO _mySkillData;
        public void BindData(SkillDataSO data) { _mySkillData=data; }

        // Directional/area weapons can cast freely; targeted weapons override this.
        public virtual bool CanCast() => _globalConfig.GlobalPlayer != null;
        
        public virtual void Cast(SkillDataSO skillData)
        {
            _originLocalScale = skillData.SkillPrefab.transform.localScale;
            _mySkillData = skillData;
        }

        public virtual void MixSkill()
        {
            _mySkillData.CurrentState = SkillCurrentState.Mixed;
            _mySkillData.Damage = Mathf.CeilToInt(_mySkillData.Damage*1.2f);
        }
        
        protected void HandleSkillDeath(SkillBase skill)
        {
            if (skill != null)
            {
                skill.transform.localScale = _originLocalScale;
                skill.OnDeath -= HandleSkillDeath;
                _pool.Return(skill.gameObject);
            }
        }
    }
}
