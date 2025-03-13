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
    [SerializeField] private Image _hostFillScore;
    [SerializeField] private Image _clientFillScore;

    [SerializeField] private TMP_Text _hostRoundScore;
    [SerializeField] private TMP_Text _clientRoundScore;

    [SerializeField] private Image _topCapturePointStatus;

    [Header("Other")]
    [SerializeField] private CanvasGroup _roundOverScreen;
    [SerializeField] private TMP_Text _waitingOpponentText;

    [Header("Ability Descriptor")]
    [SerializeField] private GameObject _abilityDescriptor;

    [Header("Crosshair")]
    [SerializeField] private Crosshair _crossHair;

    public Melee Melee => _melee;
    public Movement Movement => _movement;
    public Defense Defense => _defense;
    public Special Special => _special;
    public Steal Steal => _steal;
    public Image HealthbarImage => _healthbarImage;
    public TMP_Text EnemyMaterialDisplay => _enemyMaterialDisplay;
    public Image HostFillScore => _hostFillScore;
    public Image ClientFillScore => _clientFillScore;
    public Image TopCapturePointStatus => _topCapturePointStatus;
    public CanvasGroup RoundOverScreen => _roundOverScreen;
    public TMP_Text WaitingOpponentText => _waitingOpponentText;
    public TMP_Text HostRoundScore => _hostRoundScore;
    public TMP_Text ClientRoundScore => _clientRoundScore;
    public GameObject AbilityDescriptor => _abilityDescriptor;
    public Crosshair Crosshair => _crossHair;


    [Inject]
    private void Construct()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }
}
