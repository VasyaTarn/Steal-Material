using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public struct MovementStatsLocal : IStats
{
    public float moveSpeed;
    public float jumpHeight;
    public float mouseSensitivity;
    public bool isStuned;
}
