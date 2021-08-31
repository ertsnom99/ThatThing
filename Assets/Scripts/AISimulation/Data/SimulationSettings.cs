using System.IO;
using BehaviorDesigner.Runtime;
using UnityEngine;
using System.Collections.Generic;
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
    private SimulationState _initialSimulationState;

    public SimulationState InitialSimulationState
    {
        get { return _initialSimulationState; }
        private set { _initialSimulationState = value; }
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

    // Error texts
    private const string _emptyFieldsError = "Some fields are empty";
    private const string _invalidCharacterSettingsError = "CharacterSettings used is invalid";
    private const string _invalidInitialSimulationStateError = "Initial SimulationState is invalid";
    private const string _missingBehaviorTreeError = "Simplified AI prefab doesn't have a BehaviorTree";
    private const string _missingMovementError = "Simplified AI prefab doesn't have a SimplifiedCharacterMovement";
    private const string _AITickRateError = "AI tick rate can't be smaller than 0";

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

    public bool IsValid(out string[] errors)
    {
        List<string> errorList = new List<string>();

        if ((!_charactersSettingsUsed || !_initialSimulationState || !_simplifiedAIPrefab) && !errorList.Contains(_emptyFieldsError))
        {
            errorList.Add(_emptyFieldsError);

            errors = errorList.ToArray();
            return errors.Length == 0;
        }

        string[] characterSettingsErrors;

        if (!_charactersSettingsUsed.IsValid(out characterSettingsErrors))
        {
            errorList.Add(_invalidCharacterSettingsError);
        }

        string[] simulationStateErrors;

        if (!_initialSimulationState.IsValid(_charactersSettingsUsed, out simulationStateErrors))
        {
            errorList.Add(_invalidInitialSimulationStateError);
        }

        if (!_simplifiedAIPrefab.GetComponent<BehaviorTree>())
        {
            errorList.Add(_missingBehaviorTreeError);
        }

        if (!_simplifiedAIPrefab.GetComponent<SimplifiedCharacterMovement>())
        {
            errorList.Add(_missingMovementError);
        }

        if (_simplifiedAITickRate < 0)
        {
            errorList.Add(_AITickRateError);
        }

        errors = errorList.ToArray();
        return errors.Length == 0;
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

        string[] simualtionSettingsErrors;

        if (!simulationSettings.IsValid(out simualtionSettingsErrors))
        {
            throw new BuildFailedException("The simulationSettings file is not valid!");
        }
    }
}
#endif