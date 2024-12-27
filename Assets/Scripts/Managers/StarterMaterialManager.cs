using UnityEngine;

public class StarterMaterialManager : MonoBehaviour
{
    public static StarterMaterialManager Instance { get; private set; }

    [SerializeField] private GameObject starterMaterial;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public GameObject GetStarterMaterial()
    {
        return starterMaterial;
    }
}
