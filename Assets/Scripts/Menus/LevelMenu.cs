using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LevelMenu : MonoBehaviour
{
    [SerializeField]
    private Dropdown _levelDropdown;

    private void Start()
    {
        FillLevelDropdown();
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

    public void ChangeLevel()
    {
        int buildIndex = int.Parse(_levelDropdown.options[_levelDropdown.value].text);

        SimulationManager.Instance.UpdateCharactersState();
        SimulationManager.GetGameSave().PlayerLevel = buildIndex;
        
        SceneManager.LoadScene(buildIndex, LoadSceneMode.Single);
    }

    public void ReturnToMainMenu()
    {
        SceneManager.LoadScene(0, LoadSceneMode.Single);
    }
}
