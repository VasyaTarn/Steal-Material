using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class Wisp : SummonedEntity
{
    /*private void OnTriggerEnter(Collider other)
    {
        if(other.CompareTag("Player"))
        {
            if (isNetworkObject)
            {
                NetworkObject networkObject = other.GetComponent<NetworkObject>();
                if (networkObject != null)
                {
                    if (networkObject.OwnerClientId == owner.OwnerClientId)
                    {
                        onDeathCallback?.Invoke();
                    }
                }
            }
            else
            {
                onDeathCallback?.Invoke();
            }
        }
    }*/
}
