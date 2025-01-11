using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

[Serializable]
public struct HealthStats : IStats
{
    public float maxHp;
    public bool inResistance;
    public float resistancePercentage;
    public bool isImmortal;
}
