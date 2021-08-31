using System;
using System.Collections.Generic;
using BehaviorDesigner.Runtime;
using UnityEngine;

[Serializable]
public struct CharacterSettings
{
    [SerializeField]
    private string _name;

    public string Name
    {
        get { return _name; }
        private set { _name = value; }
    }

    [SerializeField]
    private GameObject _prefab;

    public GameObject Prefab
    {
        get { return _prefab; }
        private set { _prefab = value; }
    }

    [SerializeField]
    private float _maxWalkSpeed;

    public float MaxWalkSpeed
    {
        get { return _maxWalkSpeed; }
        private set { _maxWalkSpeed = value; }
    }

    [SerializeField]
    private ExternalBehavior _prefabBehavior;

    public ExternalBehavior PrefabBehavior
    {
        get { return _prefabBehavior; }
        private set { _prefabBehavior = value; }
    }

    [SerializeField]
    private ExternalBehavior _simplifiedBehavior;

    public ExternalBehavior SimplifiedBehavior
    {
        get { return _simplifiedBehavior; }
        private set { _simplifiedBehavior = value; }
    }
}

[CreateAssetMenu(fileName = "CharactersSetting", menuName = "AI Simulation/Settings/Characters Settings")]
public partial class CharactersSettings : ScriptableObject
{
    public CharacterSettings[] Settings = new CharacterSettings[0];
}
#if UNITY_EDITOR
public partial class CharactersSettings
{
    public List<bool> SettingsFolded;

    // Error texts
    private const string _emptyFieldsError = "Some fields are empty";
    private const string _duplicateNameError = "Duplicate setting name";
    private const string _missingBehaviorTreeError = "Some prefabs don't have a BehaviorTree script";
    private const string _missingMovementError = "Some prefabs don't have a CharacterMovement script";

    public bool IsValid(out string[] errors)
    {
        List<string> errorList = new List<string>();
        List<string> settingsNames = new List<string>();

        foreach (CharacterSettings setting in Settings)
        {
            if ((setting.Name == "" || setting.Prefab == null || setting.PrefabBehavior == null || setting.SimplifiedBehavior == null) && !errorList.Contains(_emptyFieldsError))
            {
                errorList.Add(_emptyFieldsError);
                break;
            }

            if (!settingsNames.Contains(setting.Name))
            {
                settingsNames.Add(setting.Name);
            }
            else if (!errorList.Contains(_duplicateNameError))
            {
                errorList.Add(_duplicateNameError);
            }

            if (!setting.Prefab.GetComponent<BehaviorTree>() && !errorList.Contains(_missingBehaviorTreeError))
            {
                errorList.Add(_missingBehaviorTreeError);
            }

            if (!setting.Prefab.GetComponent<CharacterMovement>() && !errorList.Contains(_missingMovementError))
            {
                errorList.Add(_missingMovementError);
            }
        }

        errors = errorList.ToArray();
        return errors.Length == 0;
    }

    public string[] GetSettingsNames()
    {
        string[] settingsNames = new string[Settings.Length];

        for (int i = 0; i < Settings.Length; i++)
        {
            settingsNames[i] = Settings[i].Name;
        }

        return settingsNames;
    }
}
#endif