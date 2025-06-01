using System.IO;
using UnityEngine;

public static class DefaultSettings
{
    public static int width = 16;
    public static int height = 16;
    public static float numMines = 10;
    public static bool hintsEnabled = false;
    public static int hints = 1;
    public static float numFlags = 0;
    //Cam
    public static float panSpeed = 40f;
    public static float maxZoom = 1000f;
    public static float minZoom = 0.3f;
    public static float zoomSpeed = 4f;
    public static int highlight = 0;
    public static int showEndTiles = 0;

    public static float fadeSpeed = 0f;
}

[System.Serializable]
public class GameSettings
{
    // Game settings
    public int width;
    public int height;
    public float numMines;
    public bool hintsEnabled;
    public int hints;
    public float numFlags;
    // Camera settings
    public float panSpeed;
    public float maxZoom;
    public float minZoom;
    public float zoomSpeed;
    public int highlight;

    public int showEndTiles;
    public float fadeSpeed;

    // Constructor to initialize with defaults
    public GameSettings()
    {
        width = DefaultSettings.width;
        height = DefaultSettings.height;
        numMines = DefaultSettings.numMines;
        hintsEnabled = DefaultSettings.hintsEnabled;
        hints = DefaultSettings.hints;
        numFlags = DefaultSettings.numFlags;
        panSpeed = DefaultSettings.panSpeed;
        maxZoom = DefaultSettings.maxZoom;
        minZoom = DefaultSettings.minZoom;
        zoomSpeed = DefaultSettings.zoomSpeed;

        highlight = DefaultSettings.highlight;
        showEndTiles = DefaultSettings.showEndTiles;
        fadeSpeed = DefaultSettings.fadeSpeed;
    }
}

public static class SettingsManager
{
    private static GameSettings _current;
    private static string fileName = "settings.json";

    // Auto-initialize on first access
    public static GameSettings Current
    {
        get
        {
            if (_current == null)
            {
                LoadSettings();
            }
            return _current;
        }
        set
        {
            _current = value;
        }
    }

    public static void SaveSettings()
    {
        try
        {
            string json = JsonUtility.ToJson(Current, true);
            string path = Path.Combine(Application.persistentDataPath, fileName);
            File.WriteAllText(path, json);
            Debug.Log("Settings saved! Pan Speed: " + Current.panSpeed);
        }
        catch (System.Exception e)
        {
            Debug.LogError("Failed to save settings: " + e.Message);
        }
    }

    public static void LoadSettings()
    {
        string path = Path.Combine(Application.persistentDataPath, fileName);
        if (File.Exists(path))
        {
            try
            {
                string json = File.ReadAllText(path);
                GameSettings loadedSettings = JsonUtility.FromJson<GameSettings>(json);

                if (loadedSettings != null)
                {
                    _current = loadedSettings;
                    Debug.Log("Custom settings loaded! Pan Speed: " + Current.panSpeed);
                }
                else
                {
                    Debug.LogWarning("Loaded settings were null, using defaults");
                    ResetToDefaults();
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("Failed to load settings, using defaults: " + e.Message);
                ResetToDefaults();
            }
        }
        else
        {
            Debug.Log("No custom settings found, using default settings");
            ResetToDefaults();
        }
    }

    public static void ResetToDefaults()
    {
        _current = new GameSettings();
        Debug.Log("Settings reset to defaults");
    }

    public static void ResetToDefaultsAndSave()
    {
        ResetToDefaults();
        SaveSettings();
    }

    public static string GetSaveFilePath()
    {
        return Path.Combine(Application.persistentDataPath, fileName);
    }
}