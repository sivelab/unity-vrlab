
//https://answers.unity.com/questions/125049/is-there-any-way-to-view-the-console-in-a-build.html

using UnityEngine;
using UnityEngine.UI;

public class ConsoleToGUI : MonoBehaviour
{
    public Camera sourceCamera;

    string myLog = "*begin log";
    string filename = "";
    bool doShow = true;
    int kChars = 1000;
    public float guiDistance = 1.0f;
    public Text Text;

    void OnEnable() {
        Application.logMessageReceived += Log;
    }
    void OnDisable() { Application.logMessageReceived -= Log; }
    void Update() { if (Input.GetKeyDown(KeyCode.Space)) { doShow = !doShow; } }
    public void Log(string logString, string stackTrace, LogType type)
    {
        // for onscreen...
        myLog = myLog + "\n" + logString;
        if (myLog.Length > kChars) { myLog = myLog.Substring(myLog.Length - kChars); }

        // for the file ...
        if (filename == "")
        {
            string d = System.Environment.GetFolderPath(
               System.Environment.SpecialFolder.Desktop) + "/YOUR_LOGS";
            System.IO.Directory.CreateDirectory(d);
            // lol
            string r = Random.Range(1000, 9999).ToString();
            filename = d + "/log-" + r + ".txt";
        }
        try { System.IO.File.AppendAllText(filename, logString + "\n"); }
        catch { }
    }

    void OnGUI()
    {
        if (!doShow) { return; }
        /*
        GUI.matrix = Matrix4x4.TRS(new Vector3(0, 0, -10.0), Quaternion.identity,
           new Vector3(Screen.width / 1200.0f, Screen.height / 800.0f, 1.0f));
        GUI.TextArea(new Rect(10, 10, 540, 370), myLog);
        */
        Text.text = myLog;
    }
}