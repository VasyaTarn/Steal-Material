using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AbilityDescriptor : MonoBehaviour
{
    [SerializeField] private Image _abilitiesDescriptionUI;
    [SerializeField] private TMP_Text _abilitiesDescriptionButton;
    [SerializeField] private TMP_Text _skinMaterialName;

    [Space(20)]

    [SerializeField] private TMP_Text _rangeDiscription;
    [SerializeField] private TMP_Text _passiveDiscription;
    [SerializeField] private TMP_Text _meleeDiscription;
    [SerializeField] private TMP_Text _movementDiscription;
    [SerializeField] private TMP_Text _defenseDiscription;
    [SerializeField] private TMP_Text _specialDiscription;

    private bool descriptorActiveStatus;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F1))
        {
            descriptorActiveStatus = !descriptorActiveStatus;

            _abilitiesDescriptionUI.gameObject.SetActive(descriptorActiveStatus);
            _abilitiesDescriptionButton.gameObject.SetActive(!descriptorActiveStatus);
        }
    }

    public void ChangeAbilitiesDescription(AbilityDescription abilityDescription)
    {
        _skinMaterialName.text = abilityDescription.Name;

        _rangeDiscription.text = abilityDescription.RangeDescription;
        _passiveDiscription.text = abilityDescription.PassiveDescription;
        _meleeDiscription.text = abilityDescription.MeleeDescription;
        _movementDiscription.text = abilityDescription.MovementDescription;
        _defenseDiscription.text = abilityDescription.DefenseDescription;
        _specialDiscription.text = abilityDescription.SpecialDescription;
    }
}
