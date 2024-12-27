using System;
using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using Zenject;

public abstract class MaterialSkills : NetworkBehaviour
{
    public Animator animator;

    public GameObject _player;
    public Action<GameObject> OnPlayerChanged;

    public GameObject player
    {
        get => _player; 
        set
        {
            if (_player != value)
            {
                _player = value;
                OnPlayerChanged?.Invoke(_player);
                HandlePlayerChanged(_player); 
            }
        }
    }

    public ulong ownerId { get; set; }

    private Dictionary<GameObject, (Inputs inputs, PlayerMovementController movement, PlayerHealthController health, PlayerSkillsController skills, SkinContoller skin)> playerComponents = new Dictionary<GameObject, (Inputs, PlayerMovementController, PlayerHealthController, PlayerSkillsController, SkinContoller)>();

    protected Inputs inputs;
    protected PlayerMovementController playerMovementController;
    protected PlayerHealthController playerHealthController;
    protected PlayerSkillsController playerSkillsController;
    protected SkinContoller skinContoller;

    public virtual string projectilePrefabKey { get; }

    public virtual float meleeAttackCooldown { get; }

    public virtual float rangeAttackCooldown { get; }

    [HideInInspector] public bool disablingPlayerMoveDuringMovementSkill = false;
    [HideInInspector] public bool disablingPlayerShootingDuringMovementSkill = false;

    protected Dictionary<string, GameObject> projectilePrefabs;

    [Inject]
    public void Construct(ProjectilePrefabs projectilePrefabsManager)
    {
        projectilePrefabs = projectilePrefabsManager.getProjectilePrefabs();
    }

    public virtual void meleeAttack() {}

    public virtual void rangeAttack(RaycastHit raycastHit) {}

    public virtual void movement() {}

    public virtual void defense() {}

    public virtual void special() {}

    public virtual void passive() {}

    public void HandlePlayerChanged(GameObject player)
    {
        if(!playerComponents.TryGetValue(player, out var components))
        {
            components = (
                player.GetComponent<Inputs>(),
                player.GetComponent<PlayerMovementController>(),
                player.GetComponent<PlayerHealthController>(),
                player.GetComponent<PlayerSkillsController>(),
                player.GetComponent<SkinContoller>()
            );

            playerComponents[player] = components;
        }

        inputs = components.inputs;
        playerMovementController = components.movement;
        playerHealthController = components.health;
        playerSkillsController = components.skills;
        skinContoller = components.skin;
    }
}