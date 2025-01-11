using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Stun : IBuff<MovementStatsNetwork, MovementStatsLocal>
{
    public float duration { get; private set; }

    public Stun(float duration)
    {
        this.duration = duration;
    }
    public void applyBuff(MovementStatsNetwork currentStats, MovementStatsLocal baseStats)
    {
        currentStats.isStuned.Value = true;
    }

    public void cancelBuff(MovementStatsNetwork currentStats, MovementStatsLocal baseStats)
    {
        currentStats.isStuned.Value = false;
    }

    public bool isSameType(IBuff<MovementStatsNetwork, MovementStatsLocal> other)
    {
        return other is Stun;
    }

    public void resetDuration(float newDuration)
    {
        duration = newDuration;
    }
}
