using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Stun : IBuff<MovementStatsNetwork, MovementStatsLocal>
{
    public float _duration { get; private set; }

    public Stun(float duration)
    {
        this._duration = duration;
    }
    public void ApplyBuff(MovementStatsNetwork currentStats, MovementStatsLocal baseStats)
    {
        currentStats.isStuned.Value = !baseStats.isStuned;
    }

    public void CancelBuff(MovementStatsNetwork currentStats, MovementStatsLocal baseStats)
    {
        currentStats.isStuned.Value = baseStats.isStuned;
    }

    public bool IsSameType(IBuff<MovementStatsNetwork, MovementStatsLocal> other)
    {
        return other is Stun;
    }

    public void ResetDuration(float newDuration)
    {
        _duration = newDuration;
    }
}
