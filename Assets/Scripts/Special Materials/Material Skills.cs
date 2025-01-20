using System;
using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using Zenject;
using Unity.Multiplayer.Samples.Utilities.ClientAuthority;

public abstract class MaterialSkills : NetworkBehaviour
{
    public Animator animator;

    public GameObject player; //Chk
    public Action<GameObject> OnPlayerChanged;

    private Dictionary<GameObject, (Inputs inputs, PlayerMovementController movement, PlayerHealthController health, PlayerSkillsController skills, SkinContoller skin, ClientNetworkTransform networkTransform, PlayerObjectReferences playerObjectReferences)> _playerComponents = new Dictionary<GameObject, (Inputs, PlayerMovementController, PlayerHealthController, PlayerSkillsController, SkinContoller, ClientNetworkTransform, PlayerObjectReferences)>();

    protected Inputs inputs;
    protected PlayerMovementController playerMovementController;
    protected PlayerHealthController playerHealthController;
    protected PlayerSkillsController playerSkillsController;
    protected SkinContoller skinContoller;
    protected ClientNetworkTransform playerNetworkTransform;
    protected PlayerObjectReferences playerObjectReferences;

    [HideInInspector] public float lastMeleeAttackTime = 0.0f;
    [HideInInspector] public float lastRangeAttackTime = 0.0f;
    [HideInInspector] public float lastMovementTime = 0.0f;
    [HideInInspector] public float lastDefenseTime = 0.0f;
    [HideInInspector] public float lastSpecialTime = 0.0f;

    [HideInInspector] public bool disablingPlayerMove = false;
    [HideInInspector] public bool disablingPlayerShootingDuringMovementSkill = false;
    [HideInInspector] public bool disablingPlayerJumpAndGravity = false;

    protected Dictionary<string, GameObject> projectilePrefabs;

    public GameObject Player
    {
        get => player; 
        set
        {
            if (player != value)
            {
                player = value;
                OnPlayerChanged?.Invoke(player);
                HandlePlayerChanged(player); 
            }
        }
    }

    public ulong ownerId { get; set; }

    public virtual string projectilePrefabKey { get; }

    public virtual float meleeAttackCooldown { get; }

    public virtual float rangeAttackCooldown { get; }

    public virtual float movementCooldown { get; }

    public virtual float defenseCooldown { get; }

    public virtual float specialCooldown { get; }


    [Inject]
    public void Construct(ProjectilePrefabs projectilePrefabsManager)
    {
        projectilePrefabs = projectilePrefabsManager.GetProjectilePrefabs();
    }

    public virtual void MeleeAttack() {}

    public virtual void RangeAttack(RaycastHit raycastHit) {}

    public virtual void Movement() {}

    public virtual void Defense() {}

    public virtual void Special() {}

    public virtual void Passive() {}

    public void HandlePlayerChanged(GameObject player)
    {
        if(!_playerComponents.TryGetValue(player, out var components))
        {
            components = (
                player.GetComponent<Inputs>(),
                player.GetComponent<PlayerMovementController>(),
                player.GetComponent<PlayerHealthController>(),
                player.GetComponent<PlayerSkillsController>(),
                player.GetComponent<SkinContoller>(),
                player.GetComponent<ClientNetworkTransform>(),
                player.GetComponent<PlayerObjectReferences>()
            );

            _playerComponents[player] = components;
        }

        inputs = components.inputs;
        playerMovementController = components.movement;
        playerHealthController = components.health;
        playerSkillsController = components.skills;
        skinContoller = components.skin;
        playerNetworkTransform = components.networkTransform;
        playerObjectReferences = components.playerObjectReferences;
    }
}