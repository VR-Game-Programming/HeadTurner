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


    public EMGLogger_O(string portName = "COM4", int baudRate = 115200, string dirname = "Result")
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
    public bool enableTrunk = false;
    public GameObject TrunkAnchor;
    public GameObject Message;
    private TextMeshProUGUI MessageText;
    public GameObject Cam;
    public GameObject Viewport;
    private Quaternion StartRotation;

    // Game Control
    private bool redirect = false;
    private bool ready = false; // if is ready to start new task (enter start area)
    private int tcount = 0; // task countdown
    private int count = 0; // total count
    private bool hitEnd = false; // if task is done
    private bool rest = false; // if is resting
    private bool testing = false; // if task is running
    private bool endtests = false; // if all tasks are done

    // Data Setting
    public enum PostureE { Standing, Sitting, Lying }
    public Dictionary<int, int> RangeDict = new(){
        {0, 55},
        {45, 45},
        {90, 35},
        {135, 45},
        {180, 55},
        {225, 45},
        {270, 35},
        {315, 45},
    };

    [Header("Task Setting")]
    public int ParticipantID = 0;
    public PostureE Posture = PostureE.Standing;
    private List<int> DirectionList = new();

    // Data Recording
    public float Interval = 0.02f;
    private float Timer = 0f;
    private Vector3 HeadStartVector;
    private Vector3 TrunkStartVector;
    private float timestamp, hPolar, hAzimuth, tPolar, tAzimuth;

    // CSV File Setting
    [Header("File Setting")]
    public string MaterialsFolder = @"Materials";
    public string ResultFolder = @"Result";
    private string FullPath = "";
    private FileStream fs;
    private StreamWriter sw;

    // Emg
    private EMGLogger_O _emg_logger;


    void Start()
    {
        MessageText = Message.GetComponent<TextMeshProUGUI>();
        MessageText.text = "按下 [A] 鍵來重新定向";

        EndArea.transform.position = new Vector3(0, 20, 0);
        EndArea.GetComponent<Renderer>().enabled = false;

        // Reading Task Order
        FullPath = Path.Combine(MaterialsFolder, "Formative_T2_Order.csv");
        if (File.Exists(FullPath)) {
            using (var reader = new StreamReader(FullPath)) {
                reader.ReadLine(); // skip header
                while (!reader.EndOfStream) {
                    var line = reader.ReadLine();
                    var values = line.Split(',');

                    int participants = int.Parse(values[0]);
                    string posture = values[1];

                    if(participants == ParticipantID && posture == Posture.ToString()) {
                        for (int i = 2; i < values.Length; i++) {
                            DirectionList.Add(int.Parse(values[i]));
                        }
                    }
                }
            }
        }

        // Setting Result File
        FullPath = Path.Combine(ResultFolder, "Formative_O3_P" + ParticipantID.ToString() + "_" + Posture.ToString() + ".csv");
        fs = new FileStream(FullPath, FileMode.OpenOrCreate);
        sw = new StreamWriter(fs);
        string Header = "Direction,Count,Time,TrunkPolar,TrunkAzimuth,TrunkPolar,TrunkAzimuth";
        sw.WriteLine(Header);

        // Emg
        string emg_folder = Path.Combine(ResultFolder, "emg_data", "Formative_O3_P" + ParticipantID.ToString() + "_" + Posture.ToString());
        _emg_logger = new EMGLogger_O(dirname: emg_folder);
    }

    void Update()
    {
        if (redirect) {
            if (!endtests) {
                if (!testing) {
                    if (count >= DirectionList.Count) {
                        endtests = true;
                        MessageText.text = "全部方向皆測試完成，請通知實驗人員並回答問卷\n請還不要拿下頭盔";
                        _emg_logger.close();
                        sw.Close();
                        fs.Close();
                    }
                    else {
                        if (rest) {
                            MessageText.text = "此方向測試完成，請通知實驗人員並回答問卷\n還有下一項測試，請還不要拿下頭盔";

                            if (OVRInput.GetDown(OVRInput.Button.Two)) {
                                rest = false;
                            }
                        }
                        else {
                            if (ready) {
                                MessageText.text = "按下 [A] 鍵來開始新測試";
                                // start new test
                                if (OVRInput.GetDown(OVRInput.Button.One)) {
                                    int rotationAngle = DirectionList[count];
                                    int viewingRange = RangeDict[rotationAngle];

                                    CreateNewTrack(rotationAngle, viewingRange);

                                    MessageText.text = "請沿著軌道方向旋轉身體直到終點";

                                    tcount ++;
                                    testing = true;

                                    HeadStartVector = Camera.main.transform.forward;
                                    if (enableTrunk){ TrunkStartVector = TrunkAnchor.transform.forward; }

                                    _emg_logger.start_logging(rotationAngle.ToString(), Posture.ToString());
                                }
                                ready = false;
                                hitEnd = false;
                            } else {
                                MessageText.text = "請回到起始區域";
                                hitEnd = false;
                            }
                        }
                    }
                }
                else {
                    // change color if collided
                    if (Track != null) {
                        if (hitTrack) {
                            Track.startColor = triggerColor;
                            Track.endColor = triggerColor;
                            hitTrack = false;
                        } else {
                            Track.startColor = lineColor;
                            Track.endColor = lineColor;
                        }
                    }

                    if (hitEnd) {
                        Track.startColor = endColor;
                        Track.endColor = endColor;

                        if (tcount >= 3) { // rest
                            testing = false;
                            ready = false;
                            rest = true;

                            tcount = 0;
                            count ++;
                        }
                        else { // cointiue next task
                            testing = false;
                            ready = false;
                        }

                        hitEnd = false;
                    }

                    _emg_logger.end_logging();
                    DataRecorder();
                }
            }
            else {
                return;
            }
        }
        else
        {
            if (OVRInput.GetDown(OVRInput.Button.One))
            {
                StartRotation = Cam.transform.rotation;
                Debug.Log("Start Rotation: " + StartRotation.eulerAngles.ToString());
                Viewport.transform.Rotate(StartRotation.eulerAngles.x, StartRotation.eulerAngles.y, StartRotation.eulerAngles.z, Space.World);
                redirect = true;
            }
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

            Vector3 rotatedPoint = StartRotation * Quaternion.Euler(0, 0, rotationAngle) * new Vector3(x, 0, z);

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
        hitEnd = true;
    }

    void OnApplicationQuit()
    {
        sw.Close();
        fs.Close();
        _emg_logger.close();
    }

    public void DataRecorder()
    {
        if (Timer - Time.deltaTime < 0)
        {
            Timer = Interval;

            timestamp = Time.time;
            ForwardToSpherical(Camera.main.transform.forward, out hPolar, out hAzimuth);
            if (enableTrunk) { ForwardToSpherical(TrunkAnchor.transform.forward, out tPolar, out tAzimuth); }

            string Data = DirectionList[count].ToString() + "," + tcount.ToString() + "," + timestamp.ToString() + ","
                + hPolar.ToString() + "," + hAzimuth.ToString() + ","
                + tPolar.ToString() + "," + tAzimuth.ToString() + ",";
            sw.WriteLine(Data);

        }
        else
        {
            Timer -= Time.deltaTime;
        }
    }

    public static void ForwardToSpherical(Vector3 cartCoords, out float outPolar, out float outAzimuth){
        // Radius is 1
        if (cartCoords.z == 0)
            cartCoords.z = Mathf.Epsilon;
        outPolar = Mathf.Atan(cartCoords.x / cartCoords.z) * Mathf.Rad2Deg;
        if (cartCoords.z < 0)
            outPolar += Mathf.PI;
        outAzimuth = Mathf.Asin(cartCoords.y / 1) * Mathf.Rad2Deg;
    }
}