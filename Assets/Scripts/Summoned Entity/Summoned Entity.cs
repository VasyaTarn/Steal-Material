using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public abstract class SummonedEntity : NetworkBehaviour
{
    public PlayerSkillsController owner { get; set; }

    protected bool isNetworkObject = false;

    protected Action onDeathCallback;

    public override void OnNetworkSpawn()
    {
        isNetworkObject = true;
    }

    public void setDeathAction(Action action)
    {
        onDeathCallback = action;
    }

    protected virtual void attack(float damage) { }
}
