using System;
using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using Zenject;
using Unity.Multiplayer.Samples.Utilities.ClientAuthority;

public abstract class MaterialSkills : NetworkBehaviour
{
    public Animator animator;

    public GameObject player;
    public Action<GameObject> OnPlayerChanged;

    private Dictionary<GameObject, (Inputs inputs, PlayerMovementController movement, PlayerHealthController health, PlayerSkillsController skills, SkinContoller skin, ClientNetworkTransform networkTransform, PlayerObjectReferences playerObjectReferences, PlayerAnimationController playerAnimation)> _playerComponents = new Dictionary<GameObject, (Inputs, PlayerMovementController, PlayerHealthController, PlayerSkillsController, SkinContoller, ClientNetworkTransform, PlayerObjectReferences, PlayerAnimationController)>();

    protected Inputs inputs;
    protected PlayerMovementController playerMovementController;
    protected PlayerHealthController playerHealthController;
    protected PlayerSkillsController playerSkillsController;
    protected SkinContoller skinContoller;
    protected ClientNetworkTransform playerNetworkTransform;
    protected PlayerObjectReferences playerObjectReferences;
    protected PlayerAnimationController playerAnimationController;

    [HideInInspector] public float lastMeleeAttackTime = 0.0f;
    [HideInInspector] public float lastRangeAttackTime = 0.0f;
    [HideInInspector] public float lastMovementTime = 0.0f;
    [HideInInspector] public float lastDefenseTime = 0.0f;
    [HideInInspector] public float lastSpecialTime = 0.0f;

    protected Dictionary<string, GameObject> projectilePrefabs;

    protected Type materialType;

    [SerializeField] protected AbilityDescription abilityDescription;

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

    public Type MaterialType => materialType;
    public AbilityDescription AbilityDescription => abilityDescription;


    [Inject]
    public void Construct(ProjectilePrefabs projectilePrefabsManager)
    {
        projectilePrefabs = projectilePrefabsManager.GetProjectilePrefabs();
    }

    public abstract void MeleeAttack();

    public abstract void RangeAttack(RaycastHit raycastHit);

    public abstract void Movement();

    public abstract void Defense();

    public abstract void Special();

    public abstract void Passive();

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
                player.GetComponent<PlayerObjectReferences>(),
                player.GetComponent<PlayerAnimationController>()
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
        playerAnimationController = components.playerAnimation;
    }
}