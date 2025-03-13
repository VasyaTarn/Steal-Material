using System;
using UniRx;
using Unity.Netcode;
using UnityEngine;

public class PlayerAnimationController : NetworkBehaviour
{
    private SkinView _skinView;
    private Inputs _inputs;

    private NetworkObject _lastArmatureNetwork;
    private PlayerArmature _cachedComponentNetwork;

    private PlayerMovementController _movementController;
    private PlayerHealthController _healthController;

    public SkinView SkinView => _skinView;
    public PlayerArmature CachedComponentNetwork => _cachedComponentNetwork;

    private readonly CompositeDisposable _disposables = new CompositeDisposable();

    private void Start()
    {
        _skinView = GetComponent<SkinView>();
        _inputs = GetComponent<Inputs>();
        _movementController = GetComponent<PlayerMovementController>();
        _healthController = GetComponent<PlayerHealthController>();

        //_healthController.OnDeth += HandlePlayerDeath;
        _healthController.OnDeath
            .Subscribe(HandlePlayerDeath)
            .AddTo(_disposables);
    }

    private void Update()
    {
        if (IsClient && !IsServer && IsOwner)
        {
            _skinView.CurrentArmatureLocal.animator.SetFloat("Horizontal", _inputs.move.x, 0.1f, Time.deltaTime);
            _skinView.CurrentArmatureLocal.animator.SetFloat("Vertical", _inputs.move.y, 0.1f, Time.deltaTime);

            if (_inputs.jump && !_movementController.currentMovementStats.isStuned.Value)
            {
                if (_movementController.grounded)
                {
                    _skinView.CurrentArmatureLocal.animator.SetTrigger("Jump");
                    _skinView.CurrentArmatureLocal.animator.ResetTrigger("Jump");
                }
            }

            _skinView.CurrentArmatureLocal.animator.SetBool("IsFalling", !_movementController.grounded);

            PlayClientAnimationByServerRpc(_skinView.CurrentArmatureNetwork.Value, _inputs.move.x, _inputs.move.y, _inputs.jump, _movementController.grounded);
        }
        if (IsServer && IsOwner)
        {
            PlayAnimationServerRpc(_skinView.CurrentArmatureNetwork.Value, _inputs.move.x, _inputs.move.y, _inputs.jump, _movementController.grounded);
        }
    }

    [Rpc(SendTo.Server)]
    private void PlayClientAnimationByServerRpc(NetworkObjectReference armature, float moveX, float moveY, bool jump, bool grounded)
    {
        if (armature.TryGet(out NetworkObject armatureNetworkObject))
        {
            if (_lastArmatureNetwork != armatureNetworkObject)
            {
                _lastArmatureNetwork = armatureNetworkObject;
                _cachedComponentNetwork = armatureNetworkObject.GetComponent<PlayerArmature>();
            }

            _cachedComponentNetwork.animator.SetFloat("Horizontal", moveX, 0.1f, Time.deltaTime);
            _cachedComponentNetwork.animator.SetFloat("Vertical", moveY, 0.1f, Time.deltaTime);

            if (jump && !_movementController.currentMovementStats.isStuned.Value)
            {
                if (_movementController.grounded)
                {
                    _cachedComponentNetwork.animator.SetTrigger("Jump");
                    _cachedComponentNetwork.animator.ResetTrigger("Jump");
                }
            }

            _cachedComponentNetwork.animator.SetBool("IsFalling", !grounded);
        }
    }

    [Rpc(SendTo.Server)]
    private void PlayAnimationServerRpc(NetworkObjectReference armature, float moveX, float moveY, bool jump, bool grounded)
    {
        PlayAnimationClientRpc(armature, moveX, moveY, jump, grounded);
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void PlayAnimationClientRpc(NetworkObjectReference armature, float moveX, float moveY, bool jump, bool grounded)
    {
        if (armature.TryGet(out NetworkObject armatureNetworkObject))
        {
            if (_lastArmatureNetwork != armatureNetworkObject)
            {
                _lastArmatureNetwork = armatureNetworkObject;
                _cachedComponentNetwork = armatureNetworkObject.GetComponent<PlayerArmature>();
            }

            _cachedComponentNetwork.animator.SetFloat("Horizontal", moveX, 0.1f, Time.deltaTime);
            _cachedComponentNetwork.animator.SetFloat("Vertical", moveY, 0.1f, Time.deltaTime);

            if (jump && !_movementController.currentMovementStats.isStuned.Value)
            {
                if (_movementController.grounded)
                {
                    _cachedComponentNetwork.animator.SetTrigger("Jump");
                    _cachedComponentNetwork.animator.ResetTrigger("Jump");
                }
            }

            _cachedComponentNetwork.animator.SetBool("IsFalling", !grounded);
        }
    }

    private void HandlePlayerDeath(ulong playerId)
    {
        PlayDeathAnimationClientRpc(playerId);

        if (playerId == 0)
        {
            _movementController.disablingPlayerMove = true;
        }
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void PlayDeathAnimationClientRpc(ulong playerId)
    {
        if (IsClient && !IsServer && IsOwner)
        {
            _skinView.CurrentArmatureLocal.animator.SetBool("IsDeath", true);
            _skinView.CurrentArmatureLocal.isEnabledAnimatorIK = false;
            _movementController.disablingPlayerMove = true;
        }

        if (playerId == 0)
        {
            _cachedComponentNetwork.animator.SetBool("IsDeath", true);
            _cachedComponentNetwork.isEnabledAnimatorIK = false;
        }
        else
        {
            EnableDeathAnimationRpc();
        }
    }

    [Rpc(SendTo.Server)]
    private void EnableDeathAnimationRpc()
    {
        _cachedComponentNetwork.animator.SetBool("IsDeath", true);
        _cachedComponentNetwork.isEnabledAnimatorIK = false;
    }

    public override void OnNetworkDespawn()
    {
        _disposables.Dispose();
    }
}
