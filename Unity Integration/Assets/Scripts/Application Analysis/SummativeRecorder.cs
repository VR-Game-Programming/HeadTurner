using System.Collections;
using System.Collections.Generic;
using System.IO;
using JetBrains.Annotations;
using Unity.PlasticSCM.Editor.WebApi;
using UnityEngine;
using UnityEngine.UI;
public enum Condition { NormalBed, ActuatedBed }
public enum Apps { App0, App1 }
public class SummativeRecorder : MonoBehaviour
{
    [Header("Data Pipeline Settings")]
    public int participantID = 0;

    public Apps app = Apps.App0;
    public bool usingEMG = true;
    static EMGLogger_O emgLogger;
    public Condition condition = Condition.NormalBed;
    private FileStream fs;
    private StreamWriter sw;
    OrientationUtility orientationUtility;
    bool isRecording = false;
    public string dirname = "Result S";
    string folder;
    void Start()
    {
        folder = Path.Combine(dirname, app.ToString());
        Directory.CreateDirectory(folder);
        string path = Path.Combine(folder, $"P_{participantID}_{condition}.csv");
        fs = new FileStream(path, FileMode.OpenOrCreate);
        sw = new StreamWriter(fs);
        sw.WriteLine("time, Pitch, Yaw, Roll");
        orientationUtility = FindObjectOfType<OrientationUtility>();
        if (condition == Condition.ActuatedBed)
        {
            FindObjectOfType<HeadController>().enableActuation = true;
        }
        else
        {
            FindObjectOfType<HeadController>().enableActuation = false;
        }
        if (usingEMG)
        {
            // get the EMGLogger_O component from the scene
            if (emgLogger == null)
            {
                string emg_folder = Path.Combine(folder, "emg_data", $"P_{participantID}_{condition}");
                emgLogger = new EMGLogger_O(dirname: emg_folder);
                Debug.Log("EMGLogger_O created");
            }
        }
    }
    public void Update()
    {
        string timeStamp = Time.time.ToString();

        if (orientationUtility.IsCalibrated == false)
        {
            return;
        }
        if (Input.GetKeyDown(KeyCode.Space) && !isRecording)
        {
            Debug.Log("Recording started");
            isRecording = !isRecording;
            if (usingEMG)
            {
                emgLogger.start_logging(app.ToString(), condition.ToString());
                Debug.Log(app.ToString() + " start logging");
            }
        }
        if (isRecording)
        {
            string line = $"{timeStamp}, {orientationUtility.PitchAngle}, {orientationUtility.YawAngle}, {orientationUtility.RollAngle}";
            sw.WriteLine(line);
        }
    }
    void OnApplicationQuit()
    {
        if (usingEMG)
        {
            emgLogger.end_logging();
            emgLogger.close();
        }
        if (sw != null)
        {
            sw.Close();
        }
        if (fs != null)
        {
            fs.Close();
        }
    }
}