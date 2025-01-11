using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;
using System;
using System.Linq;

public class StatusEffectsController<TCurrent, TBase> : IBuffable<TCurrent, TBase> where TCurrent : IStats where TBase : IStats
{
    public List<IBuff<TCurrent, TBase>> buffs = new();
    private Dictionary<IBuff<TCurrent, TBase>, float> buffTimers = new();

    private TCurrent currentStats;
    private TBase baseStats;

    public StatusEffectsController(TCurrent currentStats, TBase baseStats)
    {
        this.currentStats = currentStats;
        this.baseStats = baseStats;
    }

    public async void addBuff(IBuff<TCurrent, TBase> buff)
    {
        var existingBuff = buffs.FirstOrDefault(b => b.isSameType(buff));

        if (existingBuff != null)
        {
            buffTimers[existingBuff] = buff.duration;
        }
        else
        {
            buffs.Add(buff);
            buff.applyBuff(currentStats, baseStats);
            buffTimers[buff] = buff.duration;

            await startBuffDuration(buff);
        }
    }

    public void removeBuff(IBuff<TCurrent, TBase> buff)
    {
        buffs.Remove(buff);

        buff.cancelBuff(currentStats, baseStats);
    }

    private async UniTask startBuffDuration(IBuff<TCurrent, TBase> buff)
    {
        while (buffTimers.TryGetValue(buff, out float remainingTime) && remainingTime > 0)
        {
            await UniTask.Yield(PlayerLoopTiming.Update);
            buffTimers[buff] -= Time.deltaTime;
        }

        if (buffTimers.ContainsKey(buff))
        {
            removeBuff(buff);
        }
    }
}
