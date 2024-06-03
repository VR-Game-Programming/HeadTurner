using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;

using System.IO.Ports;
using System.Threading;
using System;

public class EMGLogger_O
{
    private SerialPort _serialPort;
    private StreamWriter _dataWriter;
    private StreamWriter _timestampWriter;
    private Thread _thread;
    private bool _startLogging;
    private bool _endLogging;
    private string _cur_range;
    private string _cur_posture;

    private string _cur_start_time;
    private bool _running = true;

    public enum Status { Start, Waiting, Back, End }


    public EMGLogger_O(string portName = "COM5", int baudRate = 9600, string dirname = "Result")
    {
        _serialPort = new SerialPort(portName, baudRate);
        _serialPort.Open();

        Directory.CreateDirectory(dirname);
        string currentTime = System.DateTime.Now.ToString("HHmmss");
        string data_path = Path.Combine(dirname, $"data_{currentTime}");
        string timestamp_path = Path.Combine(dirname, $"timestamp_{currentTime}.csv");

        _dataWriter = new StreamWriter(data_path);
        _timestampWriter = new StreamWriter(timestamp_path);
        string header = "range,posture,start,end,";
        _timestampWriter.WriteLine(header);

        _thread = new Thread(ReadSerialPort);
        _thread.Start();
    }

    private void ReadSerialPort()
    {
        while (_running)
        {
            string data = _serialPort.ReadLine();
            _dataWriter.WriteLine(data);
            if (_endLogging)
            {
                string end_time = data.Split(',')[0];
                data = _cur_range + ","
                    + _cur_posture + ","
                    + _cur_start_time + ","
                    + end_time + ",";
                _timestampWriter.WriteLine(data);
                _endLogging = false;
            }
            else if (_startLogging)
            {
                string timestamp = data.Split(',')[0];
                _cur_start_time = timestamp;
                _startLogging = false;
            }

        }
    }

    public void start_logging(string range, string posture)
    {
        _startLogging = true;
        _cur_range = range;
        _cur_posture = posture;
    }

    public void end_logging()
    {
        _endLogging = true;
    }

    public void close()
    {
        _running = false;
        _dataWriter.Close();
        _timestampWriter.Close();
    }
}


public class Manager2 : MonoBehaviour
{
    // Track Setting
    private LineRenderer Track;
    private readonly int segments = 50;
    private readonly float radius = 10f;
    private readonly float lineWidth = 0.8f;
    private Color lineColor = new(0.1f, 0.5f, 0.9f, 0.8f);
    private Color triggerColor = new(0.2f, 0.9f, 0.2f, 0.8f);
    private Color endColor = new(0.9f, 0.1f, 0.1f, 0.8f);
    public Material lineMaterial;
    private MeshCollider Collider;
    private bool hitTrack = false;

    // Objects
    public GameObject EndArea;
    public GameObject ShoulderAnchor;
    public GameObject Message;
    private TextMeshProUGUI MessageText;

    // Game Control
    private bool ready = false; // if is ready to start new task (enter start area)
    private bool waiting = false; // if is ready to waiting(enter end area)
    private float TimeRemain = 5f;
    private int count = 0;
    private bool testing = false; // if task is running
    private bool endtests = false; // if all tasks are done

    // Data Setting
    public enum PostureE { Standing, Sitting, Reclining, Lying }
    public enum RangePercentageE { P25, P50, P75, P100 }
    public float EtoRange(RangePercentageE r)
    {
        switch (r)
        {
            case RangePercentageE.P25:
                return 0.25f;
            case RangePercentageE.P50:
                return 0.50f;
            case RangePercentageE.P75:
                return 0.75f;
            case RangePercentageE.P100:
                return 1f;
            default:
                return 0f;
        }
    }
    public enum DirectionE { D0, D45, D90, D135, D180, D225, D270, D315 }
    public float EtoDirection(DirectionE d)
    {
        switch (d)
        {
            case DirectionE.D0:
                return 0f;
            case DirectionE.D45:
                return 45f;
            case DirectionE.D90:
                return 90f;
            case DirectionE.D135:
                return 135f;
            case DirectionE.D180:
                return 180f;
            case DirectionE.D225:
                return 225f;
            case DirectionE.D270:
                return 270f;
            case DirectionE.D315:
                return 315f;
            default:
                return 0f;
        }
    }
    public float EtoMaxViewingRange(DirectionE d)
    {
        switch (d)
        {
            case DirectionE.D0:
                return MaxViewingRange[0];
            case DirectionE.D45:
                return MaxViewingRange[1];
            case DirectionE.D90:
                return MaxViewingRange[2];
            case DirectionE.D135:
                return MaxViewingRange[3];
            case DirectionE.D180:
                return MaxViewingRange[4];
            case DirectionE.D225:
                return MaxViewingRange[5];
            case DirectionE.D270:
                return MaxViewingRange[6];
            case DirectionE.D315:
                return MaxViewingRange[7];
            default:
                return 0f;
        }
    }

    private float[] MaxViewingRange = new float[8] { 100f, 100f, 100f, 100f, 100f, 100f, 100f, 100f };

    [Header("Task Data")]
    public int ParticipantID = 0;
    public PostureE Posture = PostureE.Standing;

    [System.Serializable]
    public class Task
    {
        public DirectionE direction;
        public RangePercentageE percentage;
    }
    public List<Task> TaskList = new();

    // Data Recording
    private float Interval = 0.1f;
    private float Timer = 0f;
    private Vector3 HeadStartVector;
    private Vector3 ShoulderStartVector;

    [Header("Saving Path")]
    public string Folder = @"";
    private string FullPath = "";
    private FileStream fs;
    private StreamWriter sw;

    private EMGLogger_O _emg_logger;


    void Start()
    {
        MessageText = Message.GetComponent<TextMeshProUGUI>();
        MessageText.text = "";

        EndArea.transform.position = new Vector3(0, 20, 0);
        EndArea.GetComponent<Renderer>().enabled = false;

        string emg_folder = Path.Combine(Folder, "emg_data", "Formative_O1_P" + ParticipantID.ToString());
        _emg_logger = new EMGLogger_O(dirname: emg_folder);

        // Reading MaxViewingRange from CSV
        FullPath = Path.Combine(Folder, "Formative_T1_P" + ParticipantID.ToString() + "_" + Posture.ToString() + ".csv");
        if (File.Exists(FullPath))
        {
            using (var reader = new StreamReader(FullPath))
            {
                reader.ReadLine(); // skip header
                while (!reader.EndOfStream)
                {
                    var line = reader.ReadLine();
                    var values = line.Split(',');
                    if (values.Length != 4)
                    {
                        continue;
                    }

                    int direction = int.Parse(values[2]) / 45;
                    float range = float.Parse(values[3]);

                    MaxViewingRange[direction] = range;
                }
            }
        }

        // Setting CSV File
        FullPath = Path.Combine(Folder, "Formative_O1_P" + ParticipantID.ToString() + "_" + Posture.ToString() + ".csv");
        if (File.Exists(FullPath))
        {
            fs = new FileStream(FullPath, FileMode.Append, FileAccess.Write);
            sw = new StreamWriter(fs);
        }
        else
        {
            fs = new FileStream(FullPath, FileMode.Create, FileAccess.Write);
            sw = new StreamWriter(fs);
            string Header = "Participant" + ',' + "Posture" + ',' +
            "Direction" + ',' + "RangePercentage" + "," + "ViewingRange" + "," + "time" + "," +
            "HeadRotationAngle" + ',' + "GazingAngle" + ',' + "ShoulderRotationAngle";
            sw.WriteLine(Header);
        }





    }

    void Update()
    {
        if (endtests) { return; }
        if (!testing)
        {
            if (count >= TaskList.Count)
            {
                endtests = true;
                MessageText.text = "此輪測試已全部完成\n請通知實驗人員";
                _emg_logger.close();
            }
            else
            {
                if (ready)
                {
                    MessageText.text = "按下 [A] 鍵來開始新測試";
                    // start new test
                    if (OVRInput.GetDown(OVRInput.Button.One))
                    {
                        float rotationAngle = EtoDirection(TaskList[count].direction);
                        float viewingRange = EtoMaxViewingRange(TaskList[count].direction) * EtoRange(TaskList[count].percentage);

                        CreateNewTrack(rotationAngle, viewingRange);

                        MessageText.text = "請沿著軌道方向旋轉身體直到終點";

                        count++;
                        testing = true;

                        HeadStartVector = Camera.main.transform.forward;
                        ShoulderStartVector = ShoulderAnchor.transform.forward;
                        _emg_logger.start_logging(viewingRange.ToString(), Posture.ToString());
                    }
                    ready = false;
                }
                else
                {
                    MessageText.text = "請回到起始區域";
                }
            }
        }
        else
        {
            // change color if collided
            if (Track != null)
            {
                if (hitTrack)
                {
                    Track.startColor = triggerColor;
                    Track.endColor = triggerColor;
                    hitTrack = false;
                }
                else
                {
                    Track.startColor = lineColor;
                    Track.endColor = lineColor;
                }
            }

            // timer countdown
            if (waiting)
            {
                float seconds = Mathf.FloorToInt(TimeRemain % 60);
                MessageText.text = "請維持此姿勢\n還剩" + seconds + "秒";
                // end test
                if (TimeRemain > 0)
                {
                    TimeRemain -= Time.deltaTime;
                }
                else
                {
                    Track.startColor = endColor;
                    Track.endColor = endColor;

                    EndArea.transform.position = new Vector3(0, 20, 0);

                    testing = false;
                    ready = false;

                    TimeRemain = 5f;
                    _emg_logger.end_logging();
                }

                waiting = false;
            }
            else
            {
                MessageText.text = "請沿著軌道方向旋轉身體直到終點";
            }

            DataRecorder();
        }

    }

    public void CreateNewTrack(float rotationAngle, float viewingRange)
    {
        if (!gameObject.TryGetComponent<LineRenderer>(out Track))
        {
            Track = gameObject.AddComponent<LineRenderer>();
        }

        Track.positionCount = segments + 1;
        Track.startWidth = lineWidth;
        Track.endWidth = lineWidth;
        Track.startColor = lineColor;
        Track.endColor = lineColor;
        Track.material = lineMaterial;

        // Draw curve track
        float angleStep = viewingRange / segments;
        float currentAngle = 0f;

        for (int i = 0; i < segments + 1; i++)
        {
            float x = Mathf.Sin(Mathf.Deg2Rad * currentAngle) * radius;
            float z = Mathf.Cos(Mathf.Deg2Rad * currentAngle) * radius;

            Vector3 rotatedPoint = Quaternion.Euler(0, 0, rotationAngle) * new Vector3(x, 0, z);

            Track.SetPosition(i, rotatedPoint);

            if (i == segments)
            {
                EndArea.transform.position = rotatedPoint;
            }

            currentAngle += angleStep;
        }

        // Generate Mesh Collider
        if (!gameObject.TryGetComponent<MeshCollider>(out Collider))
        {
            Collider = gameObject.AddComponent<MeshCollider>();
        }
        Mesh mesh = new();
        Track.BakeMesh(mesh);
        Collider.sharedMesh = mesh;
    }

    public void HitTrack()
    {
        hitTrack = true;
    }

    public void HitStartArea()
    {
        ready = true;
    }

    public void HitEndArea()
    {
        waiting = true;
    }

    void OnApplicationQuit()
    {
        sw.Close();
        fs.Close();
    }

    public void DataRecorder()
    {
        if (Timer - Time.deltaTime < 0)
        {
            Timer = Interval;

            float r = EtoDirection(TaskList[count - 1].direction);
            float v = EtoMaxViewingRange(TaskList[count - 1].direction) * EtoRange(TaskList[count - 1].percentage);
            float t = Time.time;
            float h = Vector3.Angle(HeadStartVector, Camera.main.transform.forward);
            float g = 0f;
            float sh = Vector3.Angle(ShoulderStartVector, ShoulderAnchor.transform.forward);

            string Data = ParticipantID.ToString() + ',' + Posture.ToString() + ',' +
            r.ToString() + ',' + TaskList[count - 1].percentage.ToString() + "," + v.ToString() + "," + t.ToString() + "," +
            h.ToString() + ',' + g.ToString() + ',' + sh.ToString();

            sw.WriteLine(Data);

        }
        else
        {
            Timer -= Time.deltaTime;
        }
    }
}