using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RaycastPerformer : MonoBehaviour
{
    private Camera _playerCamera;
    [SerializeField] private LayerMask _aimCollaiderLayerMask;

    private void Start()
    {
        _playerCamera = GetComponent<PlayerMovementController>().mainCamera;
    }

    public bool PerformRaycast(out RaycastHit raycastHit)
    {
        Vector2 screenCenterPoint = new Vector2(Screen.width / 2f, Screen.height / 2f);
        Ray ray = _playerCamera.ScreenPointToRay(screenCenterPoint);

        return Physics.Raycast(ray, out raycastHit, Mathf.Infinity, _aimCollaiderLayerMask);
    }
}
