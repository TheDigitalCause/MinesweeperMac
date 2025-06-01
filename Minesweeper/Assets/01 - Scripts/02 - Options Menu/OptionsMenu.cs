using System.Collections;
using System.Collections.Generic;
using System.Xml.Linq;
using Unity.VisualScripting.FullSerializer;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class OptionsMenu : MonoBehaviour
{
    [Header("Game Settings")]
    private string textInput;
    private int width;
    private int height;
    private float numMines;
    private bool hintsEnabled;
    private int hints;
    private float nMResults;
    private int numFlags;

    [Header("Camera Settings")]
    private float panSpeed;
    private float zoomSpeed;
    private float minZoom;
    private float maxZoom;

    [Header("Important Stuff")]
    //Game Settings
    [SerializeField] public TMPro.TMP_InputField widthInputField;
    [SerializeField] public TMPro.TMP_InputField heightInputField;
    [SerializeField] public TMPro.TMP_InputField numMinesInputField;
    [SerializeField] public Toggle hintsToggle;
    [SerializeField] public TMPro.TMP_InputField hintsInputField;
    [SerializeField] public TMPro.TMP_InputField nMQuestion;
    [SerializeField] public TMPro.TMP_InputField nMResultText;
    [SerializeField] public TMPro.TMP_InputField fadeSpeedInputField;

    [SerializeField] public TMPro.TMP_Text fullscreenText;
    [SerializeField] public TMPro.TMP_Text highlightOption;
    [SerializeField] public TMPro.TMP_Text endTile;

    //Camera Settings
    [SerializeField] public TMPro.TMP_InputField zoomspeedInputField;
    [SerializeField] public TMPro.TMP_InputField panspeedInputField;
    [SerializeField] public TMPro.TMP_InputField zoomminInputField;
    [SerializeField] public TMPro.TMP_InputField zoommaxInputField;

    [SerializeField] public GameObject Left;
    [SerializeField] public GameObject Middle;
    [SerializeField] public GameObject Right;

    [SerializeField] public GameObject MiddleDefault;
    [SerializeField] public GameObject MiddleDefaultSave;
    [SerializeField] public GameObject fadeSpeedShow;


    private void Start()
    {
        LoadOptions();
    }

    private void LoadOptions()
    {
        //Game
        widthInputField.text = SettingsManager.Current.width.ToString();
        widthInputField.onEndEdit.AddListener(ReadWidthStringInput);

        heightInputField.text = SettingsManager.Current.height.ToString();
        heightInputField.onEndEdit.AddListener(ReadHeightStringInput);

        numMinesInputField.text = SettingsManager.Current.numMines.ToString() + "%";
        numMinesInputField.onEndEdit.AddListener(ReadNumMinesStringInput);

        if (SettingsManager.Current.hintsEnabled)
        {
            hintsInputField.text = SettingsManager.Current.hints.ToString();
        }
        else
        {
            hintsInputField.text = "...".ToString();
        }
        
        if (SettingsManager.Current.showEndTiles == 3)
        {
            fadeSpeedShow.SetActive(true);
            fadeSpeedInputField.text = SettingsManager.Current.fadeSpeed.ToString();
        }
        else
        {
            fadeSpeedShow.SetActive(false);
        }

        hintsInputField.onEndEdit.AddListener(ReadHintsStringInput);

        nMQuestion.text = "...".ToString();
        nMQuestion.onEndEdit.AddListener(ConvertNumMines);

        nMResultText.text = "...".ToString();
        nMResultText.onEndEdit.AddListener(ConvertDensity);

        //Cam
        zoomspeedInputField.text = SettingsManager.Current.zoomSpeed.ToString();
        zoomspeedInputField.onEndEdit.AddListener(ReadZoomSpeedStringInput);

        panspeedInputField.text = SettingsManager.Current.panSpeed.ToString();
        panspeedInputField.onEndEdit.AddListener(ReadPanSpeedStringInput);

        zoomminInputField.text = SettingsManager.Current.minZoom.ToString();
        zoomminInputField.onEndEdit.AddListener(ReadZoomMinSpeedStringInput);

        zoommaxInputField.text = SettingsManager.Current.maxZoom.ToString();
        zoommaxInputField.onEndEdit.AddListener(ReadZoomMaxSpeedStringInput);

        if (SettingsManager.Current.hintsEnabled)
        {
            hintsToggle.isOn = true;
        }

        if (!SettingsManager.Current.hintsEnabled)
        {
            hintsToggle.isOn = false;
        }

        switch (SettingsManager.Current.highlight)
        {
            case 0:
                highlightOption.text = "Off";
                break;
                
            case 1:
                highlightOption.text = "Minimal";
                break;

            case 2:
                highlightOption.text = "Everything";
                break;
        }

        switch (SettingsManager.Current.showEndTiles)
        {
            case 0:
                endTile.text = "Off";
                endTile.fontSize = 5000;
                break;

            case 1:
                endTile.text = "Show All";
                endTile.fontSize = 5000;
                break;

            case 2:
                endTile.text = "Draw";
                endTile.fontSize = 5000;
                break;
            case 3:
                endTile.text = "Fade In/Out";
                endTile.fontSize = 4500;
                break;
        }
    }

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

    public void ReadWidthStringInput(string w)
    {
        if (string.IsNullOrEmpty(w))
        {
            widthInputField.text = SettingsManager.Current.width.ToString();
            return;
        }

        if (int.TryParse(w, out int result))
        {
            width = result;
            widthInputField.text = width.ToString();
            SettingsManager.Current.width = width;

            if (width < 1)
            {
                SettingsManager.Current.width = 1;
                widthInputField.text = ("1").ToString();
            }
        }
    }

    public void ReadHeightStringInput(string h)
    {
        if (string.IsNullOrEmpty(h))
        {
            heightInputField.text = SettingsManager.Current.height.ToString();
            return;
        }

        if (int.TryParse(h, out int result))
        {
            height = result;
            heightInputField.text = height.ToString();
            SettingsManager.Current.height = height;

            if (height < 1)
            {
                SettingsManager.Current.height = 1;
                heightInputField.text = ("1").ToString();
            }
        }

    }

    public void ReadNumMinesStringInput(string nM)
    {
        if (string.IsNullOrEmpty(nM))
        {
            numMinesInputField.text = (SettingsManager.Current.numMines.ToString()+"%");
            return;
        }

        string cleanInput = nM.Replace("%.", "").Replace("...", "");


        if (float.TryParse(cleanInput, out float result))
        {
            numMines = result;

            numMinesInputField.text = (numMines.ToString() + "%");
            SettingsManager.Current.numMines = numMines;

            if (numMines < 0)
            {
                SettingsManager.Current.numMines = 0;
                numMinesInputField.text = ("0%").ToString();
            }

            else if (numMines > 100)
            {
                SettingsManager.Current.numMines = 100;
                numMinesInputField.text = ("100%").ToString();
            }

            else
            {
                return;
            }
        }
    }

    public void AreHintsEnabled(bool tog)
    {
        if (tog)
        {
            SettingsManager.Current.hintsEnabled = true;
            hintsInputField.interactable = true;
            hintsInputField.text = SettingsManager.Current.hints.ToString();
        }
        else if (!tog)
        {
            SettingsManager.Current.hintsEnabled = false;
            hintsInputField.interactable = false;
            hintsInputField.text = ("...").ToString();
        }
    }

    public void ReadHintsStringInput(string H)
    {
        if (string.IsNullOrEmpty(H))
        {
            hintsInputField.text = SettingsManager.Current.hints.ToString();
            return;
        }

        if (int.TryParse(H, out int result))
        {
            hints = result;
            hintsInputField.text = hints.ToString();
            SettingsManager.Current.hints = hints;

            if (hints <= 0)
            {
                SettingsManager.Current.hints = 1;
                hintsInputField.text = ("1").ToString();
            }
        }
    }
    
    public void ReadFadeSpeedStringInput(string fdSpD)
    {
        if (string.IsNullOrEmpty(fdSpD))
        {
            fadeSpeedInputField.text = SettingsManager.Current.fadeSpeed.ToString();
            return;
        }

        if (float.TryParse(fdSpD, out float result))
        {
            if (result < 0)
            {
                result = 0;
            }
            SettingsManager.Current.fadeSpeed = result;
            fadeSpeedInputField.text = SettingsManager.Current.fadeSpeed.ToString();
        }
}

    public void ConvertNumMines(string nMC)
    {
        if (string.IsNullOrEmpty(nMC))
        {
            nMQuestion.text = ("...").ToString();
            nMResultText.text = ""; // Clear result when input is empty
            return;
        }

        // Clean the input - remove any non-numeric characters
        string cleanInput = nMC.Replace("%.", "").Replace("...", "");

        if (int.TryParse(cleanInput, out int desiredMines))
        {
            // Calculate total cells in the grid
            float totalCells = SettingsManager.Current.width * SettingsManager.Current.height;

            // Convert number of mines to percentage
            float percentage = (desiredMines / totalCells) * 100f;

            // Clamp percentage to reasonable bounds (0-100%)
            percentage = Mathf.Clamp(percentage, 0, 100);

            nMResults = percentage;
            nMResultText.text = percentage.ToString("F9") + "%"; // Display with 9 decimal place
        }
        else
        {
            // If parsing fails, show error
            nMResultText.text = "...";
        }
    }

    public void ConvertDensity(string nMC)
    {
        if (string.IsNullOrEmpty(nMC))
        {
            nMQuestion.text = ("...").ToString();
            nMResultText.text = ""; // Clear result when input is empty
            return;
        }

        // Clean the input - remove any non-numeric characters
        string cleanInput = nMC.Replace("%.", "").Replace("...", "");

        if (float.TryParse(cleanInput, out float desiredDensity))
        {
            // Calculate total cells in the grid
            float totalCells = SettingsManager.Current.width * SettingsManager.Current.height;

            // Convert number of percentage to mine
            float mineCount = (desiredDensity * totalCells / 100);

            // Clamp mines
            int tmpMineCount = Mathf.RoundToInt(mineCount);
            nMResultText.text = cleanInput.ToString() + "%";
            nMQuestion.text = tmpMineCount.ToString();

        }
        else
        {
            // If parsing fails, show error
            nMResultText.text = "...";
        }
    }

    public void ReadZoomSpeedStringInput(string zmSpD)
    {
        if (string.IsNullOrEmpty(zmSpD))
        {
            zoomspeedInputField.text = SettingsManager.Current.zoomSpeed.ToString();
            return;
        }

        if (float.TryParse(zmSpD, out float result))
        {
            zoomSpeed = result;
            zoomspeedInputField.text = zoomSpeed.ToString();
            SettingsManager.Current.zoomSpeed = zoomSpeed;

        }
    }

    public void ReadPanSpeedStringInput(string pnSpD)
    {
        if (string.IsNullOrEmpty(pnSpD))
        {
            panspeedInputField.text = SettingsManager.Current.panSpeed.ToString();
            return;
        }

        if (float.TryParse(pnSpD, out float result))
        {
            panSpeed = result;
            panspeedInputField.text = panSpeed.ToString();
            SettingsManager.Current.panSpeed = panSpeed;
        }
    }

    public void ReadZoomMinSpeedStringInput(string zmMinSpD)
    {
        if (string.IsNullOrEmpty(zmMinSpD))
        {
            zoomminInputField.text = SettingsManager.Current.minZoom.ToString();
            return;
        }

        if (float.TryParse(zmMinSpD, out float result))
        {
            minZoom = result;
            zoomminInputField.text = minZoom.ToString();
            SettingsManager.Current.minZoom = minZoom;

        }
    }

    public void ReadZoomMaxSpeedStringInput(string zmMaxSpD)
    {
        if (string.IsNullOrEmpty(zmMaxSpD))
        {
            zoommaxInputField.text = SettingsManager.Current.maxZoom.ToString();
            return;
        }

        if (float.TryParse(zmMaxSpD, out float result))
        {
            maxZoom = result;
            zoommaxInputField.text = maxZoom.ToString();
            SettingsManager.Current.maxZoom = maxZoom;

        }
    }

    public void DefaultOrSave(bool Default, bool Save, bool Yes)
    {
        Left.SetActive(Default);
        Middle.SetActive(Default);
        Right.SetActive(Default);

        MiddleDefault.SetActive(!Default && !Save);
        MiddleDefaultSave.SetActive(!Default && Save);

        if (Default && !Save && Yes)
        {
            SettingsManager.ResetToDefaults();
            LoadOptions();
        }
        if (Default && Save && Yes)
        {
            SettingsManager.SaveSettings();
            LoadOptions();
        }

    }

    public void ConfirmDefault()
    {
        DefaultOrSave(false, false, false);
    }

    public void YesDefault()
    {
        DefaultOrSave(true, false, true);
    }

    public void NoDefault()
    {
        DefaultOrSave(true, false, false);
    }

    public void ConfirmSave()
    {
        DefaultOrSave(false, true, false);
    }

    public void YesSave()
    {
        DefaultOrSave(true, true, true);
    }

    public void NoSave()
    {
        DefaultOrSave(true, true, false);
    }


    public void BackButton()
    {
        SceneManager.LoadScene("mainMenu");
    }

    public void Highlights()
    {
        switch (SettingsManager.Current.highlight)
        {
            case 0:
                highlightOption.text = "Minimal";
                SettingsManager.Current.highlight = 1;
                break;

            case 1:
                highlightOption.text = "Everything";
                SettingsManager.Current.highlight = 2;
                break;

            case 2:
                highlightOption.text = "Off";
                SettingsManager.Current.highlight = 0;
                break;
        }
    }

    public void ShowEndTiles()
    {
        switch (SettingsManager.Current.showEndTiles)
        {
            case 0:
                fadeSpeedShow.SetActive(false);
                endTile.text = "Show All";
                endTile.fontSize = 5000;
                SettingsManager.Current.showEndTiles = 1;
                break;

            case 1:
                fadeSpeedShow.SetActive(false);
                endTile.text = "Draw";
                endTile.fontSize = 5000;
                SettingsManager.Current.showEndTiles = 2;
                break;

            case 2:
                fadeSpeedShow.SetActive(true);
                fadeSpeedInputField.text = SettingsManager.Current.fadeSpeed.ToString();
                endTile.text = "Fade In/Out";
                endTile.fontSize = 4500;
                SettingsManager.Current.showEndTiles = 3;
                break;
            case 3:
                fadeSpeedShow.SetActive(false);
                endTile.text = "Off";
                endTile.fontSize = 5000;
                SettingsManager.Current.showEndTiles = 0;
                break;
        }
    }
}
