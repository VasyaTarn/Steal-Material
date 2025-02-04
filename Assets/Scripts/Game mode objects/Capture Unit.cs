using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class CaptureUnit : NetworkBehaviour
{
    public NetworkVariable<ulong> ownerId = new NetworkVariable<ulong>(default);
    public NetworkVariable<float> count = new NetworkVariable<float>(default);
}
