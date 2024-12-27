using System;
using UnityEngine;
using Unity.Netcode;
using Cinemachine;
using Zenject;

public class SkinContoller : NetworkBehaviour
{
    public GameObject skinMaterial;
    [HideInInspector] public MaterialSkills skills;

    private Inputs input;

    private Outline outline;
    private Outline previousOutline;

    private GameObject mainCamera;

    [HideInInspector] public NetworkVariable<NetworkObjectReference> skinMaterialNetworkVar = new NetworkVariable<NetworkObjectReference>();

    private void Awake()
    {
        Camera camera = GetComponentInChildren<Camera>();
        mainCamera = camera.gameObject;
    }

    private void Start()
    {
        if (!IsOwner)
        {
            mainCamera.SetActive(false);
            GetComponentInChildren<CinemachineVirtualCamera>().gameObject.SetActive(false);
            return;
        }    

        input = GetComponent<Inputs>();

        changeSkin(StarterMaterialManager.Instance.GetStarterMaterial());

        skills.ownerId = OwnerClientId;

        skinMaterialNetworkVar.OnValueChanged += onSkinMaterialChanged;
    }

    private void Update()
    {
        if (!IsOwner)
            return;

        RaycastHit hit;
        if (Physics.Raycast(mainCamera.transform.position, mainCamera.transform.forward, out hit, 8f))
        {
            if (hit.collider.gameObject.CompareTag("Material"))
            {
                if(input.steal && (skinMaterial != hit.collider.gameObject))
                {
                    changeSkin(hit.collider.gameObject);
                    skills.ownerId = OwnerClientId;
                }
                outline = hit.collider.GetComponent<Outline>();

                if (outline != null)
                {
                    outline.enable();
                    previousOutline = outline;
                }
            }
        }
        else
        {
            if (previousOutline != null)
            {
                previousOutline.disable();
                previousOutline = null;
            }
        }
    }

    public void changeSkin(GameObject materialObject)
    {
        setSkinMaterial(materialObject);

        if (IsServer)
        {
            skinMaterialNetworkVar.Value = new NetworkObjectReference(materialObject.GetComponent<NetworkObject>());
        }
        else
        {
            requestSkinMaterialChangeServerRpc(materialObject.GetComponent<NetworkObject>().NetworkObjectId);
        }

        skills.player = gameObject;
    }

    private void setSkinMaterial(GameObject skinMaterial)
    {
        this.skinMaterial = skinMaterial;
        skills = skinMaterial.GetComponent<MaterialSkills>();
    }

    private void onSkinMaterialChanged(NetworkObjectReference previousValue, NetworkObjectReference newValue)
    {
        if (newValue.TryGet(out NetworkObject newNetworkObject))
        {
            setSkinMaterial(newNetworkObject.gameObject);
        }
    }

    [ServerRpc]
    private void requestSkinMaterialChangeServerRpc(ulong skinMaterialNetworkId)
    {
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(skinMaterialNetworkId, out NetworkObject networkObject))
        {
            skinMaterialNetworkVar.Value = new NetworkObjectReference(networkObject);
        }
    }
}
