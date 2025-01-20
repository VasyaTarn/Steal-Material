using System;
using UnityEngine;
using Unity.Netcode;
using Cinemachine;
using Zenject;

public class SkinContoller : NetworkBehaviour
{
    public GameObject skinMaterial;
    [HideInInspector] public MaterialSkills skills;

    private Inputs _input;
    private PlayerHealthController _playerHealthController;

    public OutlineCustom outline;
    public OutlineCustom previousOutline;

    private GameObject _mainCamera;

    [HideInInspector] public NetworkVariable<NetworkObjectReference> skinMaterialNetworkVar = new NetworkVariable<NetworkObjectReference>();

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
                if(_input.steal && (skinMaterial != hit.collider.gameObject))
                {
                    if(skills is ISkinMaterialChanger skin)
                    {
                        skin.ChangeSkinAction();
                    }

                    ChangeSkin(hit.collider.gameObject);
                    skills.ownerId = OwnerClientId;
                    _playerHealthController.OnDamageTaken = null;
                    
                }

                if (outline == null || hit.collider.transform.position != outline.transform.position)
                {
                    if (hit.collider.TryGetComponent<OutlineCustom>(out var newOutline))
                    {
                        outline = newOutline;
                    }
                }

                if (outline != null)
                {
                    outline.Enable();
                    if(previousOutline == null)
                    {
                        previousOutline = outline;
                    }
                    else
                    {
                        if (outline != previousOutline)
                        {
                            previousOutline.Disable();
                            previousOutline = null;
                        }
                    }
                }
            }
        }
        else
        {
            if (outline != null)
            {
                outline.Disable();
                outline = null;
                previousOutline = null;
            }
        }
    }

    public void ChangeSkin(GameObject materialObject)
    {
        SetSkinMaterial(materialObject);

        if (IsServer)
        {
            skinMaterialNetworkVar.Value = new NetworkObjectReference(materialObject.GetComponent<NetworkObject>());
        }
        else
        {
            RequestSkinMaterialChangeServerRpc(materialObject.GetComponent<NetworkObject>().NetworkObjectId);
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

    [ServerRpc]
    private void RequestSkinMaterialChangeServerRpc(ulong skinMaterialNetworkId)
    {
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(skinMaterialNetworkId, out NetworkObject networkObject))
        {
            skinMaterialNetworkVar.Value = new NetworkObjectReference(networkObject);
        }
    }
}
