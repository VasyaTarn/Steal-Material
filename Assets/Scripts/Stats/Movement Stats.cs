using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

[Serializable]
public struct MovementStats : IStat
{
    public float moveSpeed;
    public float jumpHeight;
    public float mouseSensitivity;
}
