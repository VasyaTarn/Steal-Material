using TMPro;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [SerializeField] private TMP_Text _enemyMaterialDisplay;

    [Header("Skills Icons")]
    [SerializeField] private Melee _melee;
    [SerializeField] private Movement _movement;
    [SerializeField] private Defense _defense;
    [SerializeField] private Special _special;
    [SerializeField] private Steal _steal;

    public Melee Melee => _melee;
    public Movement Movement => _movement;
    public Defense Defense => _defense;
    public Special Special => _special;
    public Steal Steal => _steal;


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

    public TMP_Text GetEnemyMaterialDisplay()
    {
        return _enemyMaterialDisplay;
    }

    
}
