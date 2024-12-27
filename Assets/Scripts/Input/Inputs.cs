using UnityEngine;
using UnityEngine.InputSystem;

public class Inputs : MonoBehaviour
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

    private bool stealTriggered;
    private bool meleeAttackTriggered;
    private bool movementSkillTriggered;
    private bool defenseTriggered;
    private bool specialTriggered;

    private void Update()
    {
        ResetTriggers();
    }

    private void ResetTriggers()
    {
        steal = stealTriggered;
        meleeAttack = meleeAttackTriggered;
        movementSkill = movementSkillTriggered;
        defense = defenseTriggered;
        special = specialTriggered;

        stealTriggered = false;
        meleeAttackTriggered = false;
        movementSkillTriggered = false;
        defenseTriggered = false;
        specialTriggered = false;
    }

    public void OnMove(InputValue value) => move = value.Get<Vector2>();

    public void OnLook(InputValue value) => look = value.Get<Vector2>();

    public void OnJump(InputValue value) => jump = value.isPressed;

    public void OnSteal(InputValue value)
    {
        if (value.isPressed)
            stealTriggered = true;
    }

    public void OnMeleeAttack(InputValue value)
    {
        if (value.isPressed)
            meleeAttackTriggered = true;
    }

    public void OnAim(InputValue value) => aim = value.isPressed;

    public void OnShoot(InputValue value) => shoot = value.isPressed;

    public void OnMovementSkill(InputValue value)
    {
        if (value.isPressed)
            movementSkillTriggered = true;
    }

    public void OnDefense(InputValue value)
    {
        if (value.isPressed)
            defenseTriggered = true;
    }

    public void OnSpecial(InputValue value)
    {
        if (value.isPressed)
            specialTriggered = true;
    }























    /*[Header("Character Input Values")]
    public Vector2 move;
    public Vector2 look;
    public bool jump;

    public bool steal;
    private bool stealPressed;

    public bool meleeAttack;
    private bool meleeAttackPressed;

    public bool aim;

    public bool shoot;

    public bool movementSkill;
    private bool movementSkillPressed;

    public bool defense;
    private bool defensePressed;

    public bool special;
    private bool specialPressed;

    private void Update()
    {
        steal = false;

        if (stealPressed)
        {
            steal = true;
            stealPressed = false;
        }

        meleeAttack = false;

        if (meleeAttackPressed)
        {
            meleeAttack = true;
            meleeAttackPressed = false;
        }

        movementSkill = false;

        if (movementSkillPressed)
        {
            movementSkill = true;
            movementSkillPressed = false;
        }

        defense = false;

        if (defensePressed)
        {
            defense = true;
            defensePressed = false;
        }

        special = false;

        if(specialPressed)
        {
            special = true;
            specialPressed = false;
        }
    }

    public void OnMove(InputValue value)
    {
        MoveInput(value.Get<Vector2>());
    }
    public void OnLook(InputValue value)
    {
        LookInput(value.Get<Vector2>());
    }

    public void OnJump(InputValue value)
    {
        JumpInput(value.isPressed);
    }

    public void OnSteal(InputValue value)
    {
        StealInput(value.isPressed);
    }

    public void OnMeleeAttack(InputValue value)
    {
        MeleeAttackInput(value.isPressed);
    }

    public void OnAim(InputValue value)
    {
        AimInput(value.isPressed);
    }

    public void OnShoot(InputValue value)
    {
        ShootInput(value.isPressed);
    }

    public void OnMovementSkill(InputValue value)
    {
        MovementSkillInput(value.isPressed);
    }

    public void OnDefense(InputValue value)
    {
        DefenseInput(value.isPressed);
    }

    public void OnSpecial(InputValue value)
    {
        SpecialInput(value.isPressed);
    }

    public void MoveInput(Vector2 newMoveDirection)
    {
        move = newMoveDirection;
    }

    public void LookInput(Vector2 newLookDirection)
    {
        look = newLookDirection;
    }

    public void JumpInput(bool newJumpState)
    {
        jump = newJumpState;
    }

    public void StealInput(bool newStealState)
    {
        if (newStealState)
        {
            stealPressed = true;
        }
    }

    public void MeleeAttackInput(bool newStealState)
    {
        if (newStealState)
        {
            meleeAttackPressed = true;
        }
    }

    public void AimInput(bool newAimState)
    {
        aim = newAimState;
    }

    public void ShootInput(bool newShootState)
    {
        shoot = newShootState;
    }

    public void MovementSkillInput(bool newMovementSkillState)
    {
        if (newMovementSkillState)
        {
            movementSkillPressed = true;
        }
    }

    public void DefenseInput(bool newDefenseState)
    {
        if(newDefenseState)
        {
            defensePressed = true;
        }
    }

    public void SpecialInput(bool newSpecialState)
    {
        if(newSpecialState)
        {
            specialPressed = true;
        }
    }*/
}
