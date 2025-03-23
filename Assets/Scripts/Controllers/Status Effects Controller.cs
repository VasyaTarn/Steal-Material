using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;
using System;
using System.Linq;

public class StatusEffectsController<TCurrent, TBase> : IBuffable<TCurrent, TBase> where TCurrent : IStats where TBase : IStats
{
    public List<IBuff<TCurrent, TBase>> buffs = new();
    private Dictionary<IBuff<TCurrent, TBase>, float> _buffTimers = new();

    private TCurrent _currentStats;
    private TBase _baseStats;

    public StatusEffectsController(TCurrent currentStats, TBase baseStats)
    {
        this._currentStats = currentStats;
        this._baseStats = baseStats;
    }

    public async void AddBuff(IBuff<TCurrent, TBase> buff)
    {
        var existingBuff = buffs.FirstOrDefault(b => b.IsSameType(buff));

        if (existingBuff != null)
        {
            _buffTimers[existingBuff] = buff._duration;
        }
        else
        {
            buffs.Add(buff);
            buff.ApplyBuff(_currentStats, _baseStats);
            _buffTimers[buff] = buff._duration;

            await StartBuffDuration(buff);
        }
    }

    public async void AddBuff(IBuff<TCurrent, TBase> buff, Action action)
    {
        var existingBuff = buffs.FirstOrDefault(b => b.IsSameType(buff));

        if (existingBuff != null)
        {
            _buffTimers[existingBuff] = buff._duration;
        }
        else
        {
            buffs.Add(buff);
            buff.ApplyBuff(_currentStats, _baseStats);
            _buffTimers[buff] = buff._duration;

            await StartBuffDuration(buff, action);
        }
    }

    public void RemoveBuff(IBuff<TCurrent, TBase> buff)
    {
        buffs.Remove(buff);

        buff.CancelBuff(_currentStats, _baseStats);
    }

    private async UniTask StartBuffDuration(IBuff<TCurrent, TBase> buff)
    {
        while (_buffTimers.TryGetValue(buff, out float remainingTime) && remainingTime > 0)
        {
            await UniTask.Yield(PlayerLoopTiming.Update);
            _buffTimers[buff] -= Time.deltaTime;
        }

        if (_buffTimers.ContainsKey(buff))
        {
            RemoveBuff(buff);
        }
    }

    private async UniTask StartBuffDuration(IBuff<TCurrent, TBase> buff, Action action)
    {
        while (_buffTimers.TryGetValue(buff, out float remainingTime) && remainingTime > 0)
        {
            await UniTask.Yield(PlayerLoopTiming.Update);
            _buffTimers[buff] -= Time.deltaTime;
        }

        if (_buffTimers.ContainsKey(buff))
        {
            RemoveBuff(buff);
        }

        action?.Invoke();
    }
}
