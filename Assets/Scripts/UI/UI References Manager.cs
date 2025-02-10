using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

public class UIReferencesManager : MonoBehaviour
{
    public static UIReferencesManager Instance { get; private set; }

    [SerializeField] private TMP_Text _enemyMaterialDisplay;

    [Header("Skills Icons")]
    [SerializeField] private Melee _melee;
    [SerializeField] private Movement _movement;
    [SerializeField] private Defense _defense;
    [SerializeField] private Special _special;
    [SerializeField] private Steal _steal;

    [Header("Health Bar")]
    [SerializeField] private Image _healthbarImage;

    [Header("Score Bar")]
    [SerializeField] private TMP_Text _hostScore;
    [SerializeField] private TMP_Text _clientScore;

    public Melee Melee => _melee;
    public Movement Movement => _movement;
    public Defense Defense => _defense;
    public Special Special => _special;
    public Steal Steal => _steal;
    public Image HealthbarImage => _healthbarImage;
    public TMP_Text EnemyMaterialDisplay => _enemyMaterialDisplay;
    public TMP_Text HostScore => _hostScore;
    public TMP_Text ClientScore => _clientScore;


    [Inject]
    public void Construct()
    {
        Instance = this;
    }
}
