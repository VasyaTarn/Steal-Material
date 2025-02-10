using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class PlayerObjectReferences : NetworkBehaviour
{
    public GameObject model;

    public Transform projectileSpawnPoint;

    [Header("Plant Objects")]
    public Transform hookshotTransform;
    public Transform summonedEntitySpawnPoint;

    [Header("Basic Objects")]
    public Transform basicMeleePointPosition;

    [Header("Stone Objects")]
    public Transform stoneMeleePointPosition;
    public Transform stoneDefensePointPosition;
    public Transform[] stoneSpecialSmokePositions;

    private SkinView _skinView;

    private void Start()
    {
        _skinView = GetComponent<SkinView>();

        _skinView.CurrentArmatureNetwork.OnValueChanged += OnArmatureChanged;

        /*if(!IsServer)
        {
            projectileSpawnPoint = _skinView.CurrentArmatureLocal.ProjectileSpawnPoint;
            Debug.Log("Spawn point local start");
        }

        if (_skinView.CurrentArmatureNetwork.Value.TryGet(out NetworkObject armatureNetworkObject))
        {
            projectileSpawnPoint = armatureNetworkObject.GetComponent<PlayerArmature>().ProjectileSpawnPoint;
        }*/

        SetSpawnPointRpc(_skinView.CurrentArmatureNetwork.Value);
    }

    private void OnArmatureChanged(NetworkObjectReference previous, NetworkObjectReference current)
    {
        if (!IsServer)
        {
            projectileSpawnPoint = _skinView.CurrentArmatureLocal.ProjectileSpawnPoint;
        }

        SetSpawnPointRpc(current);
    }

    [Rpc(SendTo.Server)]
    private void SetSpawnPointRpc(NetworkObjectReference current)
    {
        if (current.TryGet(out NetworkObject armatureNetworkObject))
        {
            projectileSpawnPoint = armatureNetworkObject.GetComponent<PlayerArmature>().ProjectileSpawnPoint;
        }
    }

    public override void OnNetworkDespawn()
    {
        _skinView.CurrentArmatureNetwork.OnValueChanged -= OnArmatureChanged;
    }
}
