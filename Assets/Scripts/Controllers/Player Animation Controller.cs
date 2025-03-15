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

    public bool _sprintStatus;

    public SkinView SkinView => _skinView;
    public PlayerArmature CachedComponentNetwork => _cachedComponentNetwork;

    private readonly CompositeDisposable _disposables = new CompositeDisposable();

    private bool _disablingPlayerAnimator;
    public bool DisablingPlayerAnimator
    {
        get 
        {
            return _disablingPlayerAnimator;
        }

        set
        {
            _disablingPlayerAnimator = value;

            if (value)
            {
                if (IsClient && !IsServer && IsOwner)
                {
                    _skinView.CurrentArmatureLocal.isEnabledAnimatorIK = false;
                }
                else if (IsServer && IsOwner)
                {
                    _cachedComponentNetwork.isEnabledAnimatorIK = false;
                }
            }
            else
            {
                if (IsClient && !IsServer && IsOwner)
                {
                    _skinView.CurrentArmatureLocal.isEnabledAnimatorIK = true;
                }
                else if (IsServer && IsOwner)
                {
                    _cachedComponentNetwork.isEnabledAnimatorIK = true;
                }
            }
        }
    }

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

    #region main_animations

    private void Update()
    {
        if (IsClient && !IsServer && IsOwner)
        {
            if (!_disablingPlayerAnimator)
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

                _skinView.CurrentArmatureLocal.animator.SetBool("IsRunning", _sprintStatus);

                PlayClientAnimationByServerRpc(_skinView.CurrentArmatureNetwork.Value, _inputs.move.x, _inputs.move.y, _inputs.jump, _movementController.grounded);
            }
            else
            {
                _inputs.move.x = 0f;
                _inputs.move.y = 0f;

                _skinView.CurrentArmatureLocal.animator.SetFloat("Horizontal", _inputs.move.x, 0.1f, Time.deltaTime);
                _skinView.CurrentArmatureLocal.animator.SetFloat("Vertical", _inputs.move.y, 0.1f, Time.deltaTime);

                PlayClientAnimationByServerRpc(_skinView.CurrentArmatureNetwork.Value, _inputs.move.x, _inputs.move.y, _inputs.jump, _movementController.grounded);
            }
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

            if (!_disablingPlayerAnimator)
            {
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

                _cachedComponentNetwork.animator.SetBool("IsRunning", _sprintStatus);
            }
            else
            {
                moveX = 0f;
                moveY = 0f;

                _cachedComponentNetwork.animator.SetFloat("Horizontal", moveX, 0.1f, Time.deltaTime);
                _cachedComponentNetwork.animator.SetFloat("Vertical", moveY, 0.1f, Time.deltaTime);
            }
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

            if (!_disablingPlayerAnimator)
            {
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

                _cachedComponentNetwork.animator.SetBool("IsRunning", _sprintStatus);
            }
            else
            {
                moveX = 0f;
                moveY = 0f;

                _cachedComponentNetwork.animator.SetFloat("Horizontal", moveX, 0.1f, Time.deltaTime);
                _cachedComponentNetwork.animator.SetFloat("Vertical", moveY, 0.1f, Time.deltaTime);
            }
        }
    }

    #endregion

    #region external_methods

    public void PlayTriggerAnimation(string name)
    {
        if (IsClient && !IsServer && IsOwner)
        {
            _skinView.CurrentArmatureLocal.animator.SetTrigger(name);

            PlayClientTriggerAnimationByServerRpc(_skinView.CurrentArmatureNetwork.Value, name);
        }
        if (IsServer && IsOwner)
        {
            PlayTriggerAnimationServerRpc(_skinView.CurrentArmatureNetwork.Value, name);
        }
    }

    [Rpc(SendTo.Server)]
    private void PlayClientTriggerAnimationByServerRpc(NetworkObjectReference armature, string name)
    {
        if (armature.TryGet(out NetworkObject armatureNetworkObject))
        {
            if (_lastArmatureNetwork != armatureNetworkObject)
            {
                _lastArmatureNetwork = armatureNetworkObject;
                _cachedComponentNetwork = armatureNetworkObject.GetComponent<PlayerArmature>();
            }

            _cachedComponentNetwork.animator.SetTrigger(name);
        }
    }

    [Rpc(SendTo.Server)]
    private void PlayTriggerAnimationServerRpc(NetworkObjectReference armature, string name)
    {
        PlayTriggerAnimationClientRpc(armature, name);
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void PlayTriggerAnimationClientRpc(NetworkObjectReference armature, string name)
    {
        if (armature.TryGet(out NetworkObject armatureNetworkObject))
        {
            if (_lastArmatureNetwork != armatureNetworkObject)
            {
                _lastArmatureNetwork = armatureNetworkObject;
                _cachedComponentNetwork = armatureNetworkObject.GetComponent<PlayerArmature>();
            }

            _cachedComponentNetwork.animator.SetTrigger(name);
        }
    }

    #endregion


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

    public void SetSprintStatus(bool status)
    {
        _sprintStatus = status;
    }

    public override void OnNetworkDespawn()
    {
        _disposables.Dispose();
    }
}
