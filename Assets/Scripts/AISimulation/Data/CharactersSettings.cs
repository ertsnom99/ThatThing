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
public class CharactersSettings : ScriptableObject
{
    public CharacterSettings[] Settings;

    //[HideInInspector]
    public List<bool> SettingsFolded;

    public bool IsValid()
    {
        foreach (CharacterSettings setting in Settings)
        {
            if (setting.Name == "" || setting.Prefab == null || setting.PrefabBehavior == null || setting.SimplifiedBehavior == null)
            {
                return false;
            }

            if (!setting.Prefab.GetComponent<BehaviorTree>())
            {
                return false;
            }
        }

        return true;
    }

    public string[] GetSettingsNames()
    {
        string[] settingsNames = new string[Settings.Length];

        for(int i = 0; i < Settings.Length; i++)
        {
            settingsNames[i] = Settings[i].Name;
        }

        return settingsNames;
    }
}
