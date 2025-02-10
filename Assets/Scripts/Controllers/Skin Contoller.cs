using System;
using UnityEngine;
using Unity.Netcode;
using Cinemachine;
using System.Collections;
using Zenject;

public class SkinContoller : NetworkBehaviour
{
    public GameObject skinMaterial;
    [HideInInspector] public MaterialSkills skills;

    private Inputs _input;
    private PlayerHealthController _playerHealthController;
    private PlayerMovementController _playerMovementController;

    private OutlineCustom _outline;
    private OutlineCustom _previousOutline;

    private GameObject _mainCamera;

    private float _lastStealTime = 0.0f;
    private float _stealCooldown = 3f;

    [HideInInspector] public NetworkVariable<NetworkObjectReference> skinMaterialNetworkVar = new NetworkVariable<NetworkObjectReference>();

    public bool disablingPlayerSkills { get; private set; }
    public SkinView skinView { get; private set; }

    private void Awake()
    {
        Camera camera = GetComponentInChildren<Camera>();
        _mainCamera = camera.gameObject;
    }

    private void Start()
    {
        if (!IsOwner)
        {
            _mainCamera.SetActive(false);
            GetComponentInChildren<CinemachineVirtualCamera>().gameObject.SetActive(false);
            return;
        }

        _input = GetComponent<Inputs>();
        _playerHealthController = GetComponent<PlayerHealthController>();
        _playerMovementController = GetComponent<PlayerMovementController>();

        skinView = GetComponent<SkinView>();

        skinView.Initialize();

        ChangeSkin(StarterMaterialManager.Instance.GetStarterMaterial());

        skills.ownerId = OwnerClientId;

        skinMaterialNetworkVar.OnValueChanged += OnSkinMaterialChanged;
    }

    private void Update()
    {
        if (!IsOwner)
            return;

        RaycastHit hit;
        if (Physics.Raycast(_mainCamera.transform.position, _mainCamera.transform.forward, out hit, 8f))
        {
            if (hit.collider.gameObject.CompareTag("Material"))
            {
                if (_input.steal && skinMaterial != hit.collider.gameObject)
                {
                    if (Time.time >= _lastStealTime + _stealCooldown)
                    {
                        if (skills is ISkinMaterialChanger skin)
                        {
                            skin.ChangeSkinAction();
                        }

                        ChangeSkin(hit.collider.gameObject);
                        skills.ownerId = OwnerClientId;
                        _playerHealthController.OnDamageTaken = null;

                        UIReferencesManager.Instance.Steal.ActivateCooldown(_stealCooldown);
                        _lastStealTime = Time.time;
                    }

                }

                if (_outline == null || hit.collider.transform.position != _outline.transform.position)
                {
                    if (hit.collider.TryGetComponent<OutlineCustom>(out var newOutline))
                    {
                        _outline = newOutline;
                    }
                }

                if (_outline != null)
                {
                    _outline.Enable();
                    if(_previousOutline == null)
                    {
                        _previousOutline = _outline;
                    }
                    else
                    {
                        if (_outline != _previousOutline)
                        {
                            _previousOutline.Disable();
                            _previousOutline = null;
                        }
                    }
                }
            }
        }
        else
        {
            if (_outline != null)
            {
                _outline.Disable();
                _outline = null;
                _previousOutline = null;
            }
        }
    }

    public void ChangeSkin(GameObject materialObject)
    {
        if(skinMaterial == null)
        {
            SetSkinMaterial(materialObject);
        }
        else
        {
            SetSkinMaterial(materialObject);

            if(!IsServer)
            {
                skinView.ChangeArmatureLocal(materialObject.GetComponent<MaterialSkills>().MaterialType);
            }

            skinView.ChangeArmatureNetwork(materialObject.GetComponent<MaterialSkills>().MaterialType);
            StartCoroutine(DisablePlayerMove(0.8f));
        }


        if (IsServer)
        {
            skinMaterialNetworkVar.Value = new NetworkObjectReference(materialObject.GetComponent<NetworkObject>());
        }
        else
        {
            RequestSkinMaterialChangeRpc(materialObject.GetComponent<NetworkObject>().NetworkObjectId);
        }

        skills.Player = gameObject;
    }

    private void SetSkinMaterial(GameObject skinMaterial)
    {
        this.skinMaterial = skinMaterial;
        skills = skinMaterial.GetComponent<MaterialSkills>();
    }

    private void OnSkinMaterialChanged(NetworkObjectReference previousValue, NetworkObjectReference newValue)
    {
        if (newValue.TryGet(out NetworkObject newNetworkObject))
        {
            SetSkinMaterial(newNetworkObject.gameObject);
        }
    }

    [Rpc(SendTo.Server)]
    private void RequestSkinMaterialChangeRpc(ulong skinMaterialNetworkId)
    {
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(skinMaterialNetworkId, out NetworkObject networkObject))
        {
            skinMaterialNetworkVar.Value = new NetworkObjectReference(networkObject);
        }
    }

    private IEnumerator DisablePlayerMove(float delay)
    {
        _playerMovementController.disablingPlayerMove = true;
        _playerMovementController.disablingPlayerJumpAndGravity = true;
        disablingPlayerSkills = true;

        yield return new WaitForSeconds(delay);

        _playerMovementController.disablingPlayerMove = false;
        _playerMovementController.disablingPlayerJumpAndGravity = false;
        disablingPlayerSkills = false;
    }
}
