using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Description", menuName = "Descriptions/Ability Description")]
public class AbilityDescription : ScriptableObject
{
    [SerializeField] private string _name;

    [SerializeField, TextArea] private string _rangeDescription;
    [SerializeField, TextArea] private string _passiveDescription;
    [SerializeField, TextArea] private string _meleeDescription;
    [SerializeField, TextArea] private string _movementDescription;
    [SerializeField, TextArea] private string _defenseDescription;
    [SerializeField, TextArea] private string _specialDescription;

    public string Name => _name;
    public string RangeDescription => _rangeDescription;
    public string PassiveDescription => _passiveDescription;
    public string MeleeDescription => _meleeDescription;
    public string MovementDescription => _movementDescription;
    public string DefenseDescription => _defenseDescription;
    public string SpecialDescription => _specialDescription;

}
