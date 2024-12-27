using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

[Serializable]
public struct HealthStats : IStat
{
    public float maxHp;
    public bool inResistance;
    public float resistancePercentage;
}
