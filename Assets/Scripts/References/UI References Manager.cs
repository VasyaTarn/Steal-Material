using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
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
    [SerializeField] private Volume _vignette;
    [SerializeField] private CanvasGroup _damageIndicator;

    [Header("Abilities")]
    [SerializeField] private GameObject _abilityDescriptor;
    [SerializeField] private GameObject _fireChargeDisplayer;
    [SerializeField] private GameObject[] _fillChargeObjects;

    [Header("Crosshair")]
    [SerializeField] private Crosshair _crossHair;

    [Header("Settings")]
    [SerializeField] private Slider _sensitivitySlider;
    [SerializeField] private TMP_Text _sensitivityValueText;

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
    public Volume Vignette => _vignette;
    public GameObject[] FillChargeObjects => _fillChargeObjects;
    public GameObject FireChargeDisplayer => _fireChargeDisplayer;
    public Slider SensitivitySlider => _sensitivitySlider;
    public TMP_Text SensitivityValueText => _sensitivityValueText;
    public CanvasGroup DamageIndicator => _damageIndicator;


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
