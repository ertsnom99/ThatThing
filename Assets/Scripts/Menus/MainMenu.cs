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
        SimulationManager.Initialize();
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

    public void Play()
    {
        int buildIndex = int.Parse(_levelDropdown.options[_levelDropdown.value].text);
        SceneManager.LoadScene(buildIndex, LoadSceneMode.Single);
    }

    public void Quit()
    {
        Application.Quit();
    }
}
