using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
{
    [SerializeField]
    private Dropdown _levelDropdown;

    private void Awake()
    {
        if (SimulationManager.GetGameSave() == null)
        {
            InitializeSimulationManager();
        }

        FillLevelDropdown();
    }

    private void InitializeSimulationManager()
    {
        // Search for a valid SimulationSettings
        SimulationSettings simulationSettings = SimulationSettings.LoadFromResources();
#if UNITY_EDITOR
        if (!simulationSettings)
        {
            Debug.LogError("No SimulationSettings found!");
            return;
        }

        string[] simualtionSettingsErrors;

        if (!simulationSettings.IsValid(out simualtionSettingsErrors))
        {
            Debug.LogError("The SimulationSettings aren't valid!");
            return;
        }
#endif
        GameSave gameSave = new GameSave(simulationSettings.InitialSimulationState);

        SimulationManager.SetSimulationSettings(simulationSettings);
        SimulationManager.SetGameSave(gameSave);
    }

    private void FillLevelDropdown()
    {
        List<Dropdown.OptionData> levelOptions = new List<Dropdown.OptionData>();
        int buildIndex = SceneManager.GetActiveScene().buildIndex;

        foreach (KeyValuePair<int, LevelStateSave> levelState in SimulationManager.GetGameSave().LevelStatesByBuildIndex)
        {
            if (buildIndex == levelState.Key)
            {
                continue;
            }

            levelOptions.Add(new Dropdown.OptionData(levelState.Key.ToString()));
        }

        _levelDropdown.options = levelOptions;
    }

    public void Play()
    {
        int buildIndex = int.Parse(_levelDropdown.options[_levelDropdown.value].text);
        SimulationManager.GetGameSave().PlayerLevel = buildIndex;
        SceneManager.LoadScene(buildIndex, LoadSceneMode.Single);
    }

    public void Quit()
    {
        Application.Quit();
    }
}
