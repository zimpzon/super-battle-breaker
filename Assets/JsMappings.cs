using System.Runtime.InteropServices;
using UnityEngine;
using System;

public class JsMappings : MonoBehaviour
{
    [DllImport("__Internal")]
    private static extern System.IntPtr GetHostInfo();

    public static string GetHostingInfo()
    {
        if (Application.platform != RuntimePlatform.WebGLPlayer)
            return "Not WebGL";

        var ptr = GetHostInfo();
        return Marshal.PtrToStringUTF8(ptr);
    }

    //[DllImport("__Internal")]
    //public static extern void Save(string json);

    //[DllImport("__Internal")]
    //public static extern string Load();

    //// New methods for file operations
    //[DllImport("__Internal")]
    //public static extern void DownloadFile(string filename, string content);

    //[DllImport("__Internal")]
    //public static extern void OpenFileDialog(string gameObjectName, string methodName);

    //// Callback method that will be called from JavaScript
    //public void OnFileLoaded(string fileContent)
    //{
    //    Debug.Log("File loaded: " + fileContent);
    //    // Process your loaded save data here
    //    ProcessImportedSave(fileContent);
    //}

    //private static void ProcessImportedSave(string saveData)
    //{
    //    try
    //    {
    //        // Parse and apply your save data
    //        // Example: JsonUtility.FromJson<SaveData>(saveData);
    //        Debug.Log("Save imported successfully!");
    //    }
    //    catch (Exception e)
    //    {
    //        Debug.LogError("Failed to import save: " + e.Message);
    //    }
    //}

    //// Example usage methods
    //public static string ExportSave()
    //{
    //    string saveData = GetCurrentSaveData(); // Your method to get save data
    //    string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
    //    string filename = $"idle-earl-save_{timestamp}.txt";

    //    DownloadFile(filename, saveData);
    //    return filename;
    //}

    //public static void ImportSave(GameObject callbacktarget)
    //{
    //    // This will open a file dialog
    //    OpenFileDialog(callbacktarget.name, "OnFileLoaded");
    //}

    //private static string GetCurrentSaveData()
    //{
    //    string export = SaveGame.GetObfuscatedSaveGame();
    //    return export;
    //}
}