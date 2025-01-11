using System;
using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using Zenject;
using Unity.Multiplayer.Samples.Utilities.ClientAuthority;

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

    private Dictionary<GameObject, (Inputs inputs, PlayerMovementController movement, PlayerHealthController health, PlayerSkillsController skills, SkinContoller skin, ClientNetworkTransform networkTransform)> playerComponents = new Dictionary<GameObject, (Inputs, PlayerMovementController, PlayerHealthController, PlayerSkillsController, SkinContoller, ClientNetworkTransform)>();

    protected Inputs inputs;
    protected PlayerMovementController playerMovementController;
    protected PlayerHealthController playerHealthController;
    protected PlayerSkillsController playerSkillsController;
    protected SkinContoller skinContoller;
    protected ClientNetworkTransform playerNetworkTransform;

    public virtual string projectilePrefabKey { get; }

    public virtual float meleeAttackCooldown { get; }

    public virtual float rangeAttackCooldown { get; }

    public virtual float movementCooldown { get; }

    public virtual float defenseCooldown { get; }

    public virtual float specialCooldown { get; }

    [HideInInspector] public float lastMeleeAttackTime = 0.0f;
    [HideInInspector] public float lastRangeAttackTime = 0.0f;
    [HideInInspector] public float lastMovementTime = 0.0f;
    [HideInInspector] public float lastDefenseTime = 0.0f;
    [HideInInspector] public float lastSpecialTime = 0.0f;

    [HideInInspector] public bool disablingPlayerMove = false;
    [HideInInspector] public bool disablingPlayerShootingDuringMovementSkill = false;
    [HideInInspector] public bool disablingPlayerJumpAndGravity = false;

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
                player.GetComponent<SkinContoller>(),
                player.GetComponent<ClientNetworkTransform>()
            );

            playerComponents[player] = components;
        }

        inputs = components.inputs;
        playerMovementController = components.movement;
        playerHealthController = components.health;
        playerSkillsController = components.skills;
        skinContoller = components.skin;
        playerNetworkTransform = components.networkTransform;
    }
}