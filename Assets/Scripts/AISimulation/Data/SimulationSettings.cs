using System.IO;
using BehaviorDesigner.Runtime;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
#endif

public partial class SimulationSettings : ScriptableObject
{
    [SerializeField]
    private CharactersSettings _charactersSettingsUsed;

    public CharactersSettings CharactersSettingsUsed
    {
        get { return _charactersSettingsUsed; }
        private set { _charactersSettingsUsed = value; }
    }

    [SerializeField]
    private GameState _initialGameState;

    public GameState InitialGameState
    {
        get { return _initialGameState; }
        private set { _initialGameState = value; }
    }

    [SerializeField]
    private GameObject _simplifiedAIPrefab;

    public GameObject SimplifiedAIPrefab
    {
        get { return _simplifiedAIPrefab; }
        private set { _simplifiedAIPrefab = value; }
    }

    [SerializeField]
    private float _simplifiedAITickRate = 1.0f;

    public float SimplifiedAITickRate
    {
        get { return _simplifiedAITickRate; }
        private set { _simplifiedAITickRate = value; }
    }

    private const string _simulationSettingsFileName = "SimulationSettings";

    public static SimulationSettings LoadFromResources()
    {
        return Resources.Load<SimulationSettings>(_simulationSettingsFileName);
    }
}

#if UNITY_EDITOR
public partial class SimulationSettings
{
    private const string _pathToSimulationSetting = "Assets/Data/AISimulation/Resources/";

    public static SimulationSettings CreateAsset()
    {
        string directoryFullPath = Application.dataPath + _pathToSimulationSetting.Remove(0, 6);
        if (!Directory.Exists(directoryFullPath))
        {
            Directory.CreateDirectory(directoryFullPath);
        }

        SimulationSettings simulationSettings = ScriptableObject.CreateInstance<SimulationSettings>();
        AssetDatabase.CreateAsset(simulationSettings, _pathToSimulationSetting + _simulationSettingsFileName + ".asset");
        return simulationSettings;
    }

    public static SimulationSettings LoadFromAsset()
    {
        return (SimulationSettings)AssetDatabase.LoadAssetAtPath(_pathToSimulationSetting + _simulationSettingsFileName + ".asset", typeof(SimulationSettings));
    }

    public bool IsValid()
    {
        if (!_charactersSettingsUsed || !_charactersSettingsUsed.IsValid())
        {
            return false;
        }

        if (!_initialGameState || (_charactersSettingsUsed && !_initialGameState.IsValid(_charactersSettingsUsed)))
        {
            return false;
        }

        if (!_simplifiedAIPrefab || !_simplifiedAIPrefab.GetComponent<BehaviorTree>() || !_simplifiedAIPrefab.GetComponent<SimplifiedCharacterMovement>())
        {
            return false;
        }

        if (_simplifiedAITickRate < 0)
        {
            return false;
        }

        return true;
    }
}

public class SimulationSettingsBuildProcessor : IPreprocessBuildWithReport
{
    public int callbackOrder { get { return 0; } }
    public void OnPreprocessBuild(BuildReport report)
    {
        SimulationSettings simulationSettings = SimulationSettings.LoadFromResources();

        if (!simulationSettings)
        {
            throw new BuildFailedException("No simulationSettings file exist!");
        }

        if (!simulationSettings.IsValid())
        {
            throw new BuildFailedException("The simulationSettings file is not valid!");
        }
    }
}
#endif