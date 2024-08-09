using System.Collections;
using System.Collections.Generic;
using System.IO;
using JetBrains.Annotations;
using Unity.PlasticSCM.Editor.WebApi;
using UnityEngine;
using UnityEngine.UI;

public class HeadMotionRecords : MonoBehaviour
{
    private FileStream fs;
    private StreamWriter sw;
    OrientationUtility orientationUtility;
    bool isRecording = false;
    public string dirname = "Summative Result";
    void Start()
    {
        string currentTime = System.DateTime.Now.ToString("HHmmss");
        string path = Path.Combine(dirname, $"timestamp_{currentTime}.csv");
        fs = new FileStream(path, FileMode.Create);
        sw = new StreamWriter(fs);
        sw.WriteLine("time, Pitch, Yaw, Roll");
        orientationUtility = FindObjectOfType<OrientationUtility>();
    }
    public void Update()
    {
        string timeStamp = Time.time.ToString();

        if (orientationUtility.IsCalibrated == false)
        {
            return;
        }
        if (Input.GetKeyDown(KeyCode.Space))
        {
            isRecording = !isRecording;
        }
        if (isRecording)
        {
            string line = $"{timeStamp}, {orientationUtility.PitchAngle}, {orientationUtility.YawAngle}, {orientationUtility.RollAngle}";
            sw.WriteLine(line);
        }
    }
    void OnApplicationQuit()
    {
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