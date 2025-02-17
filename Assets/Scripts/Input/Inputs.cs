using System.Globalization;
using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode;

public class Inputs : NetworkBehaviour
{
    [Header("Character Input Values")]
    public Vector2 move;
    public Vector2 look;
    public bool jump;
    public bool steal;
    public bool meleeAttack;
    public bool aim;
    public bool shoot;
    public bool movementSkill;
    public bool defense;
    public bool special;

    private bool _stealTriggered;
    private bool _meleeAttackTriggered;
    private bool _movementSkillTriggered;
    private bool _defenseTriggered;
    private bool _specialTriggered;

    private void Update()
    {
        if (!IsOwner) return;
        ResetTriggers();
    }

    private void ResetTriggers()
    {
        steal = _stealTriggered;
        meleeAttack = _meleeAttackTriggered;
        movementSkill = _movementSkillTriggered;
        defense = _defenseTriggered;
        special = _specialTriggered;

        _stealTriggered = false;
        _meleeAttackTriggered = false;
        _movementSkillTriggered = false;
        _defenseTriggered = false;
        _specialTriggered = false;
    }

    public void OnMove(InputValue value)
    {
        if (!IsOwner) return;
        move = value.Get<Vector2>();
        SendMoveServerRpc(move);
    }

    public void OnLook(InputValue value)
    {
        if (!IsOwner) return;
        look = value.Get<Vector2>();
        SendLookServerRpc(look);
    }

    public void OnJump(InputValue value)
    {
        if (!IsOwner) return;
        if (!PauseScreen.isPause)
        {
            jump = value.isPressed;
            SendJumpServerRpc(jump);
        }
    }

    public void OnSteal(InputValue value)
    {
        if (!IsOwner) return;
        if (value.isPressed)
        {
            _stealTriggered = true;
            SendStealServerRpc();
        }
    }

    public void OnMeleeAttack(InputValue value)
    {
        if (!IsOwner) return;
        if (value.isPressed)
        {
            _meleeAttackTriggered = true;
            SendMeleeAttackServerRpc();
        }
    }

    public void OnAim(InputValue value)
    {
        if (!IsOwner) return;
        aim = value.isPressed;
        SendAimServerRpc(aim);
    }

    public void OnShoot(InputValue value)
    {
        if (!IsOwner) return;
        shoot = value.isPressed;
        SendShootServerRpc(shoot);
    }

    public void OnMovementSkill(InputValue value)
    {
        if (!IsOwner) return;
        if (value.isPressed)
        {
            _movementSkillTriggered = true;
            SendMovementSkillServerRpc();
        }
    }

    public void OnDefense(InputValue value)
    {
        if (!IsOwner) return;
        if (value.isPressed)
        {
            _defenseTriggered = true;
            SendDefenseServerRpc();
        }
    }

    public void OnSpecial(InputValue value)
    {
        if (!IsOwner) return;
        if (value.isPressed)
        {
            _specialTriggered = true;
            SendSpecialServerRpc();
        }
    }

    // --- ServerRpc методы ---

    [ServerRpc]
    private void SendMoveServerRpc(Vector2 moveValue)
    {
        move = moveValue;
    }

    [ServerRpc]
    private void SendLookServerRpc(Vector2 lookValue)
    {
        look = lookValue;
    }

    [ServerRpc]
    private void SendJumpServerRpc(bool isJumping)
    {
        jump = isJumping;
    }

    [ServerRpc]
    private void SendStealServerRpc()
    {
        steal = true;
    }

    [ServerRpc]
    private void SendMeleeAttackServerRpc()
    {
        meleeAttack = true;
    }

    [ServerRpc]
    private void SendAimServerRpc(bool isAiming)
    {
        aim = isAiming;
    }

    [ServerRpc]
    private void SendShootServerRpc(bool isShooting)
    {
        shoot = isShooting;
    }

    [ServerRpc]
    private void SendMovementSkillServerRpc()
    {
        movementSkill = true;
    }

    [ServerRpc]
    private void SendDefenseServerRpc()
    {
        defense = true;
    }

    [ServerRpc]
    private void SendSpecialServerRpc()
    {
        special = true;
    }







    /*[Header("Character Input Values")]
    public Vector2 move;
    public Vector2 look;
    public bool jump;
    public bool steal;
    public bool meleeAttack;
    public bool aim;
    public bool shoot;
    public bool movementSkill;
    public bool defense;
    public bool special;

    private bool _stealTriggered;
    private bool _meleeAttackTriggered;
    private bool _movementSkillTriggered;
    private bool _defenseTriggered;
    private bool _specialTriggered;

    private void Update()
    {
        if (!IsOwner) return;

        ResetTriggers();
    }

    private void ResetTriggers()
    {
        steal = _stealTriggered;
        meleeAttack = _meleeAttackTriggered;
        movementSkill = _movementSkillTriggered;
        defense = _defenseTriggered;
        special = _specialTriggered;

        _stealTriggered = false;
        _meleeAttackTriggered = false;
        _movementSkillTriggered = false;
        _defenseTriggered = false;
        _specialTriggered = false;
    }

    public void OnMove(InputValue value)
    {
        if (!IsOwner) return;
        move = value.Get<Vector2>();
    }

    public void OnLook(InputValue value)
    {
        if (!IsOwner) return;
        look = value.Get<Vector2>();
    }

    public void OnJump(InputValue value)
    {
        if (!IsOwner) return;
        if (!PauseScreen.isPause)
        {
            jump = value.isPressed;
        }
    }    

    public void OnSteal(InputValue value)
    {
        if (!IsOwner) return;
        if (value.isPressed)
            _stealTriggered = true;
    }

    public void OnMeleeAttack(InputValue value)
    {
        if (!IsOwner) return;
        if (value.isPressed)
            _meleeAttackTriggered = true;
    }

    public void OnAim(InputValue value)
    {
        if (!IsOwner) return;
        aim = value.isPressed;
    }

    public void OnShoot(InputValue value)
    {
        if (!IsOwner) return;
        shoot = value.isPressed;
    }

    public void OnMovementSkill(InputValue value)
    {
        if (!IsOwner) return;
        if (value.isPressed)
            _movementSkillTriggered = true;
    }

    public void OnDefense(InputValue value)
    {
        if (!IsOwner) return;
        if (value.isPressed)
            _defenseTriggered = true;
    }

    public void OnSpecial(InputValue value)
    {
        if (!IsOwner) return;
        if (value.isPressed)
            _specialTriggered = true;
    }*/
}
