using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public enum ArmatureChangeSource
{
    Unknown,
    Server,
    Client
}

public class SkinView : NetworkBehaviour
{
    [SerializeField] private PlayerArmature[] _armatures;

    private PlayerArmature _currentArmatureLocal;
    private NetworkVariable<NetworkObjectReference> _currentArmature = new NetworkVariable<NetworkObjectReference>();

    private Dictionary<Type, NetworkObject> _armaturesCollecionNetwork = new Dictionary<Type, NetworkObject>();
    private Dictionary<Type, PlayerArmature> _armaturesCollecionLocal = new Dictionary<Type, PlayerArmature>();

    private NetworkObject _armatureNetworkObject = null;

    private GameObject _transformationSmoke;

    private int _initialTransformationSmokePoolSize = 1;
    private LocalObjectPool _transformationSmokePool;

    private ArmatureChangeSource _changeSource = ArmatureChangeSource.Unknown;

    public NetworkVariable<NetworkObjectReference> CurrentArmatureNetwork => _currentArmature;
    public PlayerArmature CurrentArmatureLocal => _currentArmatureLocal;

    public Dictionary<Type, NetworkObject> ArmaturesCollecionNetwork => _armaturesCollecionNetwork;
    public Dictionary<Type, PlayerArmature> ArmaturesCollecionLocal => _armaturesCollecionLocal;

    private void Awake()
    {
        _transformationSmoke = Resources.Load<GameObject>("General/Transformation_Smoke");

        _currentArmature.OnValueChanged += HandleArmatureChange;
    }

    private void HandleArmatureChange(NetworkObjectReference previous, NetworkObjectReference current)
    {
        _changeSource = IsServer ? ArmatureChangeSource.Server : ArmatureChangeSource.Client;

        if ((OwnerClientId == 0 && _changeSource == ArmatureChangeSource.Client) ||
            (OwnerClientId == 0 && _changeSource == ArmatureChangeSource.Server) ||
            (OwnerClientId > 0 && _changeSource == ArmatureChangeSource.Server))
        {
            if (current.TryGet(out NetworkObject armatureNetworkObject))
            {
                if (_armatureNetworkObject != null)
                {
                    _armatureNetworkObject.gameObject.SetActive(false);
                }

                if (!previous.TryGet(out NetworkObject previousObj))
                {
                    _armatureNetworkObject = armatureNetworkObject;
                    armatureNetworkObject.gameObject.SetActive(true);
                }
                else
                {
                    if (IsServer)
                    {
                        SpawnTransformationSmokeServerRpc(transform.position, OwnerClientId);
                    }

                    StartCoroutine(ActivateAramture(0.5f));
                    _armatureNetworkObject = armatureNetworkObject;
                }
            }
        }
    }

    public override void OnNetworkSpawn()
    {
        if(OwnerClientId == 0 && !IsServer)
        {
            SpawnStartingArmatureServerRpc();
        }
    }

    [Rpc(SendTo.Server)]
    private void SpawnStartingArmatureServerRpc()
    {
        SpawnStartingArmatureClientRpc(_currentArmature.Value);
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void SpawnStartingArmatureClientRpc(NetworkObjectReference armatureReference)
    {
        if(!IsServer)
        {
            if (armatureReference.TryGet(out NetworkObject armatureNetworkObject))
            {
                armatureNetworkObject.gameObject.SetActive(true);
                _armatureNetworkObject = armatureNetworkObject;
            }
        }
    }

    public void Initialize()
    {
        if (!IsServer)
        {
            PlayerArmature[] localArmatures = this.GetComponentsInChildren<PlayerArmature>(true);

            foreach(PlayerArmature localArmature in localArmatures)
            {
                _armaturesCollecionLocal.Add(localArmature.Type, localArmature);
            }  
        }

        SpawnArmaturesRpc(OwnerClientId);

        if(!IsServer)
        {
            ChangeArmatureLocal(StarterMaterialManager.Instance.GetStarterMaterial().GetComponent<MaterialSkills>().MaterialType);
        }

        ChangeArmatureNetwork(StarterMaterialManager.Instance.GetStarterMaterial().GetComponent<MaterialSkills>().MaterialType);

        
    }

    public void ChangeArmatureLocal(Type newLocalArmature)
    {
        if(_currentArmatureLocal != null)
        {
            _currentArmatureLocal.gameObject.SetActive(false);
        }

        if(_currentArmatureLocal == null)
        {
            _currentArmatureLocal = _armaturesCollecionLocal[newLocalArmature];
            _currentArmatureLocal.gameObject.SetActive(true);
        }
        else
        {
            SpawnTransformationSmokeLocal(transform.position);

            _currentArmatureLocal = _armaturesCollecionLocal[newLocalArmature];
            StartCoroutine(ActiveteLocalArmature(0.5f));
        }
    }

    public void ChangeArmatureNetwork(Type newArmature)
    {
        SetCurrentArmatureRpc(newArmature);
    }

    [Rpc(SendTo.Server)]
    private void SetCurrentArmatureRpc(Type newArmature)
    {
        _currentArmature.Value = _armaturesCollecionNetwork[newArmature].GetComponent<NetworkObject>();
    }

    [Rpc(SendTo.Server)]
    private void SpawnArmaturesRpc(ulong ownerId)
    {
        foreach (var armature in _armatures)
        {
            NetworkObject armatureNetObj = Instantiate(armature).GetComponent<NetworkObject>();

            if (!armatureNetObj.IsSpawned)
            {
                armatureNetObj.Spawn();
                armatureNetObj.TrySetParent(NetworkManager.Singleton.ConnectedClients[ownerId].PlayerObject.GetComponent<NetworkObject>(), true);
            }

            if (!_armaturesCollecionNetwork.ContainsKey(armature.Type) && armatureNetObj.IsSpawned)
            {
                _armaturesCollecionNetwork.Add(armature.Type, armatureNetObj);
            }
        }
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void OnArmaturesSpawnedClientRpc()
    {
        if (!IsServer)
        {
            PlayerArmature[] armatures = this.GetComponentsInChildren<PlayerArmature>(true);

            foreach (PlayerArmature armature in armatures)
            {
                _armaturesCollecionLocal.Add(armature.Type, armature);
            }
        }
    }

    private void SpawnTransformationSmokeLocal(Vector3 transformationSmokeSpawnPoint)
    {
        if (_transformationSmokePool == null)
        {
            if (_transformationSmoke != null)
            {
                _transformationSmokePool = new LocalObjectPool(_transformationSmoke, _initialTransformationSmokePoolSize);
            }
        }

        GameObject spawnedTransformationSmoke = _transformationSmokePool.Get(transformationSmokeSpawnPoint);

        StartCoroutine(ReleaseSmoke(1f, () => _transformationSmokePool.Release(spawnedTransformationSmoke)));
    }

    [Rpc(SendTo.Server)]
    private void SpawnTransformationSmokeServerRpc(Vector3 transformationSmokeSpawnPoint, ulong ownerId)
    {
        NetworkObject transformationSmokeNetwork = NetworkObjectPool.Singleton.GetNetworkObject(_transformationSmoke, transformationSmokeSpawnPoint);
        transformationSmokeNetwork.Spawn();


        if (ownerId != 0)
        {
            transformationSmokeNetwork.NetworkHide(ownerId);
        }

        StartCoroutine(ReleaseSmoke(1f, () =>
        {
            if (IsServer)
            {
                if (transformationSmokeNetwork.IsSpawned)
                {
                    transformationSmokeNetwork.Despawn();
                }
            }
        }));
    }

    private IEnumerator ReleaseSmoke(float duration, Action releaseAction)
    {
        yield return new WaitForSeconds(duration);

        releaseAction?.Invoke();
    }

    private IEnumerator ActivateAramture(float delay)
    {
        yield return new WaitForSeconds(delay);

        if (_currentArmature.Value.TryGet(out NetworkObject armatureNetworkObject))
        {
            armatureNetworkObject.gameObject.SetActive(true);
        }
    }

    private IEnumerator ActiveteLocalArmature(float delay)
    {
        yield return new WaitForSeconds(delay);
        _currentArmatureLocal.gameObject.SetActive(true);
    }

    public override void OnNetworkDespawn()
    {
        foreach (var kvp in _armaturesCollecionNetwork)
        {
            if (kvp.Value.IsSpawned)
            {
                kvp.Value.Despawn();
            }
        }

        _currentArmature.OnValueChanged -= HandleArmatureChange;
    }
}
