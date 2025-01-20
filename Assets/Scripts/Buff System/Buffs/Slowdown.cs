using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Slowdown : IBuff<MovementStatsNetwork, MovementStatsLocal>
{
    private float _slowFactor;
    public float _duration { get; private set; }


    public Slowdown(float slowFactor, float duration)
    {
        this._slowFactor = slowFactor;
        this._duration = duration;
    }
    public void ApplyBuff(MovementStatsNetwork currentStats, MovementStatsLocal baseStats)
    {
        currentStats.moveSpeed.Value = baseStats.moveSpeed * (1 - _slowFactor);
    }

    public void CancelBuff(MovementStatsNetwork currentStats, MovementStatsLocal baseStats)
    {
        currentStats.moveSpeed.Value = baseStats.moveSpeed;
    }

    public bool IsSameType(IBuff<MovementStatsNetwork, MovementStatsLocal> other)
    {
        return other is Slowdown;
    }

    public void ResetDuration(float newDuration)
    {
        _duration = newDuration;
    }
}
