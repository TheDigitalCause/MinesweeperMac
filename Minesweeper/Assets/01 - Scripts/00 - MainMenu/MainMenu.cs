using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    [SerializeField] public TMPro.TMP_Text fullscreenText;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F11))
        {
            ReadButtonInput();

        }
        switch (Screen.fullScreenMode)
        {
            case FullScreenMode.Windowed:
                fullscreenText.text = "E".ToString();
                break;
            case FullScreenMode.ExclusiveFullScreen:
                fullscreenText.text = "F".ToString();
                break;
            case FullScreenMode.FullScreenWindow:
                fullscreenText.text = "W".ToString();
                break;

        }
    }

    public void ReadButtonInput()
    {
        switch (Screen.fullScreenMode)
        {
            case FullScreenMode.Windowed:
                Screen.fullScreenMode = FullScreenMode.ExclusiveFullScreen;
                break;
            case FullScreenMode.ExclusiveFullScreen:
                Screen.fullScreenMode = FullScreenMode.FullScreenWindow;
                break;
            case FullScreenMode.FullScreenWindow:
                Screen.fullScreenMode = FullScreenMode.Windowed;
                break;

        }
    }

    public void StartGame()
    {
        SceneManager.LoadScene("mainGame");
    }

    public void OptionMenu()
    {
        SceneManager.LoadScene("optionMenu");
    }

    public void AchievementMenu()
    {
        SceneManager.LoadScene("achievementMenu");
    }

    public void ChallengesMenu()
    {
        SceneManager.LoadScene("challengesMenu");
    }

    public void LeaderboardMenu()
    {
        SceneManager.LoadScene("leaderboardMenu");
    }

    public void QuitGame()
    {
        Application.Quit();
    }
}
