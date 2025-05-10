using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class PlayerObjectReferences : NetworkBehaviour
{
    [Header("General Objects")]
    [SerializeField] private Transform _projectileSpawnPoint;

    [Header("Plant Objects")]
    [SerializeField] private Transform _hookshotTransform;
    [SerializeField] private Transform _summonedEntitySpawnPoint;

    [Header("Basic Objects")]
    [SerializeField] private Transform _basicMeleePointPosition;
    [SerializeField] private GameObject _basicSkillRadius;

    [Header("Stone Objects")]
    [SerializeField] private Transform _stoneMeleePointPosition;
    [SerializeField] private Transform _stoneDefensePointPosition;
    [SerializeField] private Transform[] _stoneSpecialSmokePositions;
    [SerializeField] private GameObject _stoneMovementObject;

    [Header("Fire Objects")]
    [SerializeField] private GameObject _fireModelLocal;
    private NetworkVariable<NetworkObjectReference> _fireModelNetwork = new NetworkVariable<NetworkObjectReference>();
    [SerializeField] private GameObject _fireSkillRadius;
    [SerializeField] private GameObject _astralVisualObject;

    private SkinView _skinView;

    public Transform ProjectileSpawnPoint => _projectileSpawnPoint;
    public Transform HookshotTransform => _hookshotTransform;
    public Transform SummonedEntitySpawnPoint => _summonedEntitySpawnPoint;
    public Transform BasicMeleePointPosition => _basicMeleePointPosition;
    public Transform StoneMeleePointPosition => _stoneMeleePointPosition;
    public Transform StoneDefensePointPosition => _stoneDefensePointPosition;
    public Transform[] StoneSpecialSmokePositions => _stoneSpecialSmokePositions;
    public GameObject FireModelLocal => _fireModelLocal;
    public NetworkVariable<NetworkObjectReference> FireModelNetwork => _fireModelNetwork;
    public GameObject BasicSkillRadius => _basicSkillRadius;
    public GameObject FireSkillRadius => _fireSkillRadius;
    public GameObject StoneMovementObject => _stoneMovementObject;
    public GameObject AstralVisualObject => _astralVisualObject;

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
        _fireModelNetwork.Value = _skinView.ArmaturesCollecionNetwork[Type.Fire].gameObject;
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
            _projectileSpawnPoint = _skinView.CurrentArmatureLocal.ProjectileSpawnPoint;
        }

        SetSpawnPointRpc(current);
    }

    [Rpc(SendTo.Server)]
    private void SetSpawnPointRpc(NetworkObjectReference current)
    {
        if (current.TryGet(out NetworkObject armatureNetworkObject))
        {
            _projectileSpawnPoint = armatureNetworkObject.GetComponent<PlayerArmature>().ProjectileSpawnPoint;
        }
    }

    public override void OnNetworkDespawn()
    {
        _skinView.CurrentArmatureNetwork.OnValueChanged -= OnArmatureChanged;
    }
}
