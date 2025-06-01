using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameButtonLogic : MonoBehaviour
{
    [SerializeField] public TMPro.TMP_Text numFlagTXT;
    [SerializeField] public TMPro.TMP_Text timerText;
    [SerializeField] public Button fullscreenToggle;
    [SerializeField] public TMPro.TMP_Text fullscreenText;
    private float numFlag;
    private float time = 0f;

    private void Update()
    {
        NumFlagUpdater();
        if (GameManager.MouseUsability.timerEnabled == true)
        {
            TimerUpdater();
        }

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

    private bool isProcessingSceneChange = false;

    public void RestartGame()
    {
        if (isProcessingSceneChange) return;
        isProcessingSceneChange = true;

        // Use immediate restart for best performance
        RestartGameImmediate();
    }

    public void MainMenu()
    {
        if (isProcessingSceneChange) return;
        isProcessingSceneChange = true;

        // Use immediate main menu for best performance
        MainMenuImmediate();
    }

    public void QuitGame()
    {
        // First, cleanup GameManager if it exists
        

        Process.Start("taskkill", "/f /im minesweeper.exe");
        CleanupGameManagerBeforeQuit();
        Application.Quit();
    }

    // Optimized immediate restart - works with GameManager's FastCleanup
    private void RestartGameImmediate()
    {
        // GameManager will handle its own cleanup through OnDestroy/OnDisable
        // Just load the scene - Unity will trigger cleanup automatically
        SceneManager.LoadScene("mainGame");
    }

    // Optimized immediate main menu - works with GameManager's FastCleanup  
    private void MainMenuImmediate()
    {
        // GameManager will handle its own cleanup through OnDestroy/OnDisable
        // Just load the scene - Unity will trigger cleanup automatically
        SceneManager.LoadScene("mainMenu");
    }

    // Optional: Manual cleanup before quit (more thorough)
    private void CleanupGameManagerBeforeQuit()
    {
        try
        {
            GameObject gameHolder = GameObject.Find("Game Holder");
            if (gameHolder != null)
            {
                GameManager gameManager = gameHolder.GetComponentInChildren<GameManager>();
                if (gameManager != null)
                {
                    gameManager.PrepareForSceneChange();
                }
            }
        }
        catch (System.Exception e)
        {
            UnityEngine.Debug.LogWarning($"Error during quit cleanup: {e.Message}");
        }
    }

    // Keep these methods for backward compatibility if needed elsewhere
    [System.Obsolete("Use RestartGame() instead - automatic cleanup is now handled by GameManager")]
    public void RestartGameOptimized()
    {
        RestartGame();
    }

    [System.Obsolete("Use MainMenu() instead - automatic cleanup is now handled by GameManager")]
    public void MainMenuOptimized()
    {
        MainMenu();
    }

    // Alternative: If you want the safest approach with explicit timing (slower but more predictable)
    public void RestartGameSafe()
    {
        if (isProcessingSceneChange) return;
        isProcessingSceneChange = true;

        StartCoroutine(RestartGameSafeCoroutine());
    }

    public void MainMenuSafe()
    {
        if (isProcessingSceneChange) return;
        isProcessingSceneChange = true;

        StartCoroutine(MainMenuSafeCoroutine());
    }

    private IEnumerator RestartGameSafeCoroutine()
    {
        // Explicit cleanup with timing
        GameObject gameHolder = GameObject.Find("Game Holder");
        if (gameHolder != null)
        {
            GameManager gameManager = gameHolder.GetComponentInChildren<GameManager>();
            if (gameManager != null)
            {
                gameManager.PrepareForSceneChange();
                yield return null; // Wait one frame for cleanup
            }
        }

        SceneManager.LoadScene("mainGame");
    }

    private IEnumerator MainMenuSafeCoroutine()
    {
        // Explicit cleanup with timing
        GameObject gameHolder = GameObject.Find("Game Holder");
        if (gameHolder != null)
        {
            GameManager gameManager = gameHolder.GetComponentInChildren<GameManager>();
            if (gameManager != null)
            {
                gameManager.PrepareForSceneChange();
                yield return null; // Wait one frame for cleanup
            }
        }

        SceneManager.LoadScene("mainMenu");
    }

    private void NumFlagUpdater()
    {
        numFlag = SettingsManager.Current.numFlags;
        numFlagTXT.text = ("FLG RMN: ") + numFlag.ToString();
    }

    private void TimerUpdater()
    {
        time += Time.deltaTime;

        int hours = Mathf.FloorToInt(time / 3600f);
        int minutes = Mathf.FloorToInt((time % 3600f) / 60f);
        int seconds = Mathf.FloorToInt(time % 60f);
        int milliseconds = Mathf.FloorToInt((time % 1f) * 1000f);

        timerText.text = string.Format("{0:D3}:{1:D2}:{2:D2}:{3:D3}", hours, minutes, seconds, milliseconds);
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
}