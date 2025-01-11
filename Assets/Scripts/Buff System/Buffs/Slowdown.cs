using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Slowdown : IBuff<MovementStatsNetwork, MovementStatsLocal>
{
    public float duration { get; private set; }

    private float slowFactor;

    public Slowdown(float slowFactor, float duration)
    {
        this.slowFactor = slowFactor;
        this.duration = duration;
    }
    public void applyBuff(MovementStatsNetwork currentStats, MovementStatsLocal baseStats)
    {
        currentStats.moveSpeed.Value = baseStats.moveSpeed * (1 - slowFactor);
    }

    public void cancelBuff(MovementStatsNetwork currentStats, MovementStatsLocal baseStats)
    {
        currentStats.moveSpeed.Value = baseStats.moveSpeed;
    }

    public bool isSameType(IBuff<MovementStatsNetwork, MovementStatsLocal> other)
    {
        return other is Slowdown;
    }

    public void resetDuration(float newDuration)
    {
        duration = newDuration;
    }
}
