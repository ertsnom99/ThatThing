using System.Collections.Generic;
using GraphCreator;
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
        int currentBuildIndex = SceneManager.GetActiveScene().buildIndex;

        foreach (int buildIndex in SimulationManager.GetBuildIndexes())
        {
            if (currentBuildIndex == buildIndex)
            {
                continue;
            }

            levelOptions.Add(new Dropdown.OptionData(buildIndex.ToString()));
        }

        _levelDropdown.options = levelOptions;
    }

    public void ChangeLevel()
    {
        int buildIndex = int.Parse(_levelDropdown.options[_levelDropdown.value].text);

        SimulationManager.Instance.UpdateCharacterStatesInCurrentLevel();
        
        SceneManager.LoadScene(buildIndex, LoadSceneMode.Single);
    }

    public void ReturnToMainMenu()
    {
        SceneManager.LoadScene(0, LoadSceneMode.Single);
    }
}
