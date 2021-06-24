using System;
using BehaviorDesigner.Runtime;
using UnityEngine;

[Serializable]
public struct CharacterSettings
{
    public string Name;
    public GameObject Prefab;
    public ExternalBehavior PrefabBehavior;
    public ExternalBehavior SimplifiedBehavior;
}

[CreateAssetMenu(fileName = "CharactersSetting", menuName = "Game State/Settings/Characters Settings")]
public class CharactersSettings : ScriptableObject
{
    public CharacterSettings[] Settings;

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
