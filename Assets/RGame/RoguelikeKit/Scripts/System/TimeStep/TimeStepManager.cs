using System;
using System.Collections.Generic;
using RGame.Framework;
using UnityEngine;
using UnityEngine.Events;

namespace RGame.RoguelikeKit
{
    public class TimerData
    {
        public float Timer;
        public FloatWrapper EndTime;
        public UnityAction Callback;
        
        public TimerData(FloatWrapper endTime, UnityAction callback)
        {
            EndTime = endTime;
            Callback = callback;
        }
    }
    
    public class TimeStepManager : MonoBehaviour
    {
        [SerializeField] private VoidEventChannelSO mFixedUpdateChannel;
        [SerializeField] private AddTimerChannel _addTimerChannel;
        
        private List<TimerData> _activeTimers = new List<TimerData>();
        internal void RemoveTimer(FloatWrapper end)=>_activeTimers.RemoveAll(t=>t.EndTime==end);
        internal List<RunTimer> CaptureRunTimers(RunStateFields fields)
        {
            var result=new List<RunTimer>();
            foreach(var timer in _activeTimers)
            {
                string key=fields.Id(timer.EndTime);
                if(key==null)throw new InvalidOperationException("Unbound gameplay timer");
                result.Add(new RunTimer{key=key,elapsed=timer.Timer,end=timer.EndTime.Value});
            }
            return result;
        }
        internal void RestoreRunTimers(List<RunTimer> saved,RunStateFields fields)
        {
            if(saved.Count!=_activeTimers.Count)throw new InvalidOperationException("Checkpoint timer count mismatch");
            foreach(var state in saved)
            {
                var timer=_activeTimers.Find(t=>fields.Id(t.EndTime)==state.key);
                if(timer==null)throw new InvalidOperationException("Checkpoint timer is missing");
                timer.Timer=state.elapsed;timer.EndTime.Value=state.end;
            }
        }

        private void OnEnable()
        {
            _addTimerChannel.RegisterListener(AddTimer);
        }

        private void OnDisable()
        {
            _addTimerChannel.UnregisterListener(AddTimer);
        }
        
        private void FixedUpdate()
        {
            mFixedUpdateChannel.RaiseEvent();

            for (int i = 0; i < _activeTimers.Count; i++)
            {
                _activeTimers[i].Timer += Time.fixedDeltaTime;

                if (_activeTimers[i].Timer > _activeTimers[i].EndTime.Value)
                {
                    _activeTimers[i].Timer -= _activeTimers[i].EndTime.Value;
                    _activeTimers[i].Callback?.Invoke();
                }
            }
        }
        
        private void AddTimer(FloatWrapper endTime, UnityAction callback)
        {
            _activeTimers.Add(new TimerData(endTime, callback));
        }
    }
}
