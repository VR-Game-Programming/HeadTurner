using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

public class DataRecorder : MonoBehaviour {
    private string path;
    private FileStream fs;
    private StreamWriter sw;

    void Start() {
        string path = "";
        fs = new FileStream(path, FileMode.Create);
        sw = new StreamWriter(fs);
        sw.WriteLine("time, positioin_x, position_y, position_z, rotation_x, rotation_y, rotation_z");

    }
    public void Update() {

        string timeStamp = Time.time.ToString();
        Transform currentTransform = transform;
        Vector3 position = currentTransform.position;
        Vector3 rotation = currentTransform.rotation.eulerAngles;

        string line = $"{timeStamp}, {position.x}, {position.y}, {position.z}, {rotation.x}, {rotation.y}, {rotation.z}";
        sw.WriteLine(line);
    }



    void OnApplicationQuit() {
        if (sw != null) {
            sw.Close();
         }
        if (fs != null) {
            fs.Close();
        }
    }
}