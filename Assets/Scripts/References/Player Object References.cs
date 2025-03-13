using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class PlayerObjectReferences : NetworkBehaviour
{
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

    [Header("Fire Objects")]
    public GameObject fireModelLocal;
    public NetworkVariable<NetworkObjectReference> fireModelNetwork = new NetworkVariable<NetworkObjectReference>();

    private SkinView _skinView;

    private void Start()
    {
        _skinView = GetComponent<SkinView>();

        SetSpawnPointRpc(_skinView.CurrentArmatureNetwork.Value);

        if (IsOwner)
        {
            _skinView.CurrentArmatureNetwork.OnValueChanged += OnArmatureChanged;

            SetFireModelSerevRpc();
        }
    }

    [Rpc(SendTo.Server)]
    private void SetFireModelSerevRpc()
    {
        fireModelNetwork.Value = _skinView.ArmaturesCollecionNetwork[Type.Fire].gameObject;
    }

    /*[Rpc(SendTo.ClientsAndHost)]
    private void SetFireModelClientRpc(NetworkObjectReference fireModelReference)
    {
        if (IsClient && !IsServer)
        {
            if (fireModelReference.TryGet(out NetworkObject fireModelNetworkObject))
            {
                GetComponent<PlayerSkillsController>().enemyObjectReferences.fireModelNetwork.Value = fireModelNetworkObject.gameObject;
            }
        }
        else
        {
            fireModelNetwork.Value = _skinView.ArmaturesCollecionNetwork[Type.Fire].gameObject;
        }
    }*/

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
