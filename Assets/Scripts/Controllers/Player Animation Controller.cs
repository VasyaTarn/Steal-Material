using System;
using UniRx;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;

public class PlayerAnimationController : NetworkBehaviour
{
    private SkinView _skinView;
    private Inputs _inputs;

    private NetworkObject _lastArmatureNetwork;
    private PlayerArmature _cachedComponentNetwork;

    private PlayerMovementController _movementController;
    private PlayerHealthController _healthController;
    private PlayerSkillsController _skillsController;

    private bool _sprintStatus;
    private bool _blockStatus;
    private bool _climbStatus;
    private bool _movementSkillStatus;
    private bool _rollStatus;
    private bool _stunStatus;

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
        _skillsController = GetComponent<PlayerSkillsController>();

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

                if (!_climbStatus && !_movementSkillStatus)
                {
                    _skinView.CurrentArmatureLocal.animator.SetBool("IsFalling", !_movementController.grounded);
                }

                if (_sprintStatus)
                {
                    _skinView.CurrentArmatureLocal.animator.SetFloat("SpeedMultiplier", 1.3f);
                }
                else if (_blockStatus)
                {
                    _skinView.CurrentArmatureLocal.animator.SetFloat("SpeedMultiplier", 0.5f);
                }
                else
                {
                    _skinView.CurrentArmatureLocal.animator.SetFloat("SpeedMultiplier", 1f);
                }

                _skinView.CurrentArmatureLocal.animator.SetBool("IsBlock", _blockStatus);
                _skinView.CurrentArmatureLocal.animator.SetBool("IsClimbing", _climbStatus);
                _skinView.CurrentArmatureLocal.animator.SetBool("IsRolling", _rollStatus);
                _skinView.CurrentArmatureLocal.animator.SetBool("IsStunning", _stunStatus);

                PlayClientAnimationByServerRpc(_skinView.CurrentArmatureNetwork.Value, _inputs.move.x, _inputs.move.y, _inputs.jump, _movementController.grounded, _blockStatus, _sprintStatus, _climbStatus, _movementSkillStatus, _rollStatus, _stunStatus);
            }
            else
            {
                _inputs.move.x = 0f;
                _inputs.move.y = 0f;

                _skinView.CurrentArmatureLocal.animator.SetFloat("Horizontal", _inputs.move.x, 0.1f, Time.deltaTime);
                _skinView.CurrentArmatureLocal.animator.SetFloat("Vertical", _inputs.move.y, 0.1f, Time.deltaTime);

                PlayClientAnimationByServerRpc(_skinView.CurrentArmatureNetwork.Value, _inputs.move.x, _inputs.move.y, _inputs.jump, _movementController.grounded, _blockStatus, _sprintStatus, _climbStatus, _movementSkillStatus, _rollStatus, _stunStatus);
            }
        }
        if (IsServer && IsOwner)
        {
            PlayAnimationServerRpc(_skinView.CurrentArmatureNetwork.Value, _inputs.move.x, _inputs.move.y, _inputs.jump, _movementController.grounded, _blockStatus, _sprintStatus, _climbStatus, _movementSkillStatus, _rollStatus, _stunStatus);
        }
    }

    [Rpc(SendTo.Server)]
    private void PlayClientAnimationByServerRpc(NetworkObjectReference armature, float moveX, float moveY, bool jump, bool grounded, bool block, bool sprint, bool climb, bool movementSkill, bool roll, bool stun)
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

                if (!climb && !movementSkill)
                {
                    _cachedComponentNetwork.animator.SetBool("IsFalling", !grounded);
                }

                if (sprint)
                {
                    _cachedComponentNetwork.animator.SetFloat("SpeedMultiplier", 1.3f);
                }
                else if (block)
                {
                    _cachedComponentNetwork.animator.SetFloat("SpeedMultiplier", 0.5f);
                }
                else
                {
                    _cachedComponentNetwork.animator.SetFloat("SpeedMultiplier", 1f);
                }

                _cachedComponentNetwork.animator.SetBool("IsBlock", block);
                _cachedComponentNetwork.animator.SetBool("IsClimbing", climb);
                _cachedComponentNetwork.animator.SetBool("IsRolling", roll);
                _cachedComponentNetwork.animator.SetBool("IsStunning", stun);
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
    private void PlayAnimationServerRpc(NetworkObjectReference armature, float moveX, float moveY, bool jump, bool grounded, bool block, bool sprint, bool climb, bool movementSkill, bool roll, bool stun)
    {
        PlayAnimationClientRpc(armature, moveX, moveY, jump, grounded, block, sprint, climb, movementSkill, roll, stun);
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void PlayAnimationClientRpc(NetworkObjectReference armature, float moveX, float moveY, bool jump, bool grounded, bool block, bool sprint, bool climb, bool movementSkill, bool roll, bool stun)
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

                if (!climb && !movementSkill)
                {
                    _cachedComponentNetwork.animator.SetBool("IsFalling", !grounded);
                }

                if (sprint)
                {
                    _cachedComponentNetwork.animator.SetFloat("SpeedMultiplier", 1.3f);
                }
                else if(block)
                {
                    _cachedComponentNetwork.animator.SetFloat("SpeedMultiplier", 0.5f);
                }
                else
                {
                    _cachedComponentNetwork.animator.SetFloat("SpeedMultiplier", 1f);
                }

                _cachedComponentNetwork.animator.SetBool("IsBlock", block);
                _cachedComponentNetwork.animator.SetBool("IsClimbing", climb);
                _cachedComponentNetwork.animator.SetBool("IsRolling", roll);
                _cachedComponentNetwork.animator.SetBool("IsStunning", stun);
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

    #region trigger_animation
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

    #region bool_animation

    public void PlayBoolAnimation(string name, bool status)
    {
        if (IsClient && !IsServer && IsOwner)
        {
            _skinView.CurrentArmatureLocal.animator.SetBool(name, status);

            PlayClientBoolAnimationByServerRpc(_skinView.CurrentArmatureNetwork.Value, name, status);
        }
        if (IsServer && IsOwner)
        {
            PlayBoolAnimationServerRpc(_skinView.CurrentArmatureNetwork.Value, name, status);
        }
    }

    [Rpc(SendTo.Server)]
    private void PlayClientBoolAnimationByServerRpc(NetworkObjectReference armature, string name, bool status)
    {
        if (armature.TryGet(out NetworkObject armatureNetworkObject))
        {
            if (_lastArmatureNetwork != armatureNetworkObject)
            {
                _lastArmatureNetwork = armatureNetworkObject;
                _cachedComponentNetwork = armatureNetworkObject.GetComponent<PlayerArmature>();
            }

            _cachedComponentNetwork.animator.SetBool(name, status);
        }
    }

    [Rpc(SendTo.Server)]
    private void PlayBoolAnimationServerRpc(NetworkObjectReference armature, string name, bool status)
    {
        PlayBoolAnimationClientRpc(armature, name, status);
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void PlayBoolAnimationClientRpc(NetworkObjectReference armature, string name, bool status)
    {
        if (armature.TryGet(out NetworkObject armatureNetworkObject))
        {
            if (_lastArmatureNetwork != armatureNetworkObject)
            {
                _lastArmatureNetwork = armatureNetworkObject;
                _cachedComponentNetwork = armatureNetworkObject.GetComponent<PlayerArmature>();
            }

            _cachedComponentNetwork.animator.SetBool(name, status);
        }
    }

    #endregion

    #endregion


    private void HandlePlayerDeath(ulong playerId)
    {
        PlayDeathAnimationClientRpc(playerId);

        if (playerId == 0)
        {
            _movementController.disablingPlayerMove = true;
            _skillsController.SetDisablePlayerSkillsStatus(true);
            DisablingPlayerAnimator = true;
        }
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void PlayDeathAnimationClientRpc(ulong playerId)
    {
        if (IsClient && !IsServer && IsOwner)
        {
            _skinView.CurrentArmatureLocal.animator.SetBool("IsDeath", true);
            _skinView.CurrentArmatureLocal.isEnabledAnimatorIK = false;
            _skillsController.SetDisablePlayerSkillsStatus(true);
            _movementController.disablingPlayerMove = true;
            DisablingPlayerAnimator = true;
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

    public void SetBlockStatus(bool status)
    {
        _blockStatus = status;
    }

    public void SetClimbStatus(bool status)
    {
        _climbStatus = status;
    }

    public void SetMovementSkillStatus(bool status)
    {
        _movementSkillStatus = status;
    }

    public void SetRollStatus(bool status)
    {
        _rollStatus = status;
    }

    public void SetStunStatus(bool status)
    {
        if (_healthController.currentHp.Value > 0)
        {
            _stunStatus = status;
        }
        else
        {
            _stunStatus = false;
        }
    }

    public override void OnNetworkDespawn()
    {
        _disposables.Dispose();
    }
}
