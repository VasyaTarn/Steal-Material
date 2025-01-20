using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public abstract class SummonedEntity : NetworkBehaviour
{
    public Action onDeathCallback;

    public PlayerSkillsController owner { get; set; }

    public bool isNetworkObject { get; private set; }


    public override void OnNetworkSpawn()
    {
        isNetworkObject = true;
    }

    public void SetDeathAction(Action action)
    {
        onDeathCallback = action;
    }
}
