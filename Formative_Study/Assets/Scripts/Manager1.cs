using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using System.IO.Ports;
using System.Threading;

public class EMGLogger_T
{
    private SerialPort _serialPort;
    private StreamWriter _dataWriter;
    private StreamWriter _timestampWriter;
    private Thread _thread;
    private bool _startLogging;
    private bool _endLogging;
    private bool _running=true;
    private string _cur_angle;
    private string _cur_posture;
    private string _cur_start_time;


    public EMGLogger_T(string portName = "COM4", int baudRate = 9600, string dirname = "Result")
    {
        _serialPort = new SerialPort(portName, baudRate);
        _serialPort.Open();

        Directory.CreateDirectory(dirname);
        string data_path = Path.Combine(dirname, "emgdata");
        string timestamp_path = Path.Combine(dirname, "timestamp.csv");

        _dataWriter = new StreamWriter(data_path);
        _timestampWriter = new StreamWriter(timestamp_path);
        string header = "angle,posture,start_time,end_time,";
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
            if (_startLogging)
            {
                string timestamp = data.Split(',')[0];
                _cur_start_time = timestamp;
                _startLogging = false;
            }
            if (_endLogging)
            {
                string end_time = data.Split(',')[0];
                data = _cur_angle + ","
                    + _cur_posture + ","
                    + _cur_start_time + ","
                    + end_time + ",";
                _timestampWriter.WriteLine(data);
                _endLogging = false;
            }
        }
    }

    public void start_logging(string angle, string posture)
    {
        _startLogging = true;
        _cur_angle = angle;
        _cur_posture = posture;
    }

    public void end_logging()
    {
        _endLogging = true;
    }

    public void close()
    {
        while(_endLogging);
        _running = false;
        _dataWriter.Close();
        _timestampWriter.Close();
    }
}

public class Manager1 : MonoBehaviour
{
    // Track Setting
    private LineRenderer Track;
    private readonly int segments = 50;
    private readonly float radius = 10f;
    private readonly float lineWidth = 0.6f;
    private Color lineColor = new Color(0.1f, 0.5f, 0.9f, 0.8f);
    private Color triggerColor = new Color(0.2f, 0.9f, 0.2f, 0.8f);
    private Color endColor = new Color(0.9f, 0.1f, 0.1f, 0.8f);
    public Material lineMaterial;

    private MeshCollider Collider;
    private bool hitTrack = false;

    // Obkects
    public GameObject Message;
    private TextMeshProUGUI MessageText;
    public GameObject Cam;
    public GameObject Viewport;
    private Quaternion StartRotation;

    // Game Control
    private bool redirect = false;
    private bool ready = false;
    private int count = 0;
    private bool testing = false;
    private bool endtests = false;

    // Data Setting
    public enum PostureE { Standing, Sitting, Reclining, Lying }
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

    [Header("Task Data")]
    public int ParticipantID = 0;
    public PostureE Posture = PostureE.Standing;
    public List<DirectionE> DirectionList = new();

    // Data Recording
    private Vector3 StartVector;
    private Vector3 EndVector;
    private float MaxViewingRange;

    [Header("Saving Path")]
    public string Folder = @"";
    private string FullPath = "";
    private FileStream fs;
    private StreamWriter sw;

    private EMGLogger_T emg_logger;


    void Start()
    {
        MessageText = Message.GetComponent<TextMeshProUGUI>();
        MessageText.text = "按下 [A] 鍵來重新定向";

        Debug.Log("load csv object");

        // Setting CSV File
        string filename = "Formative_T1_P" + ParticipantID.ToString() + "_" + Posture.ToString() + ".csv";
        FullPath = Path.Combine(Folder, filename);
        fs = new FileStream(FullPath, FileMode.OpenOrCreate);
        sw = new StreamWriter(fs);
        string Header = "Participant" + ',' + "Posture" + ',' + "Direction" + ',' + "MaxViewingRange";
        sw.WriteLine(Header);

        Debug.Log("load emg object");

        string emg_folder = Path.Combine(Folder, "emg_data", "Formative_T1_P" + ParticipantID.ToString()+ "_" + Posture.ToString() + ".csv");
        emg_logger = new EMGLogger_T(dirname: emg_folder);
    }

    void Update()
    {
        if (redirect) {
            if (!endtests)
            {
                if (!testing)
                {
                    if (count >= DirectionList.Count)
                    {
                        endtests = true;
                        MessageText.text = "此輪測試已全部完成\n請通知實驗人員";
                        emg_logger.close();
                    }
                    else
                    {
                        if (!ready)
                        {
                            MessageText.text = "請回到起始區域";
                        }
                        else
                        {
                            MessageText.text = "按下 [A] 鍵來開始新測試";
                            // start new test
                            if (OVRInput.GetDown(OVRInput.Button.One))
                            {
                                CreateNewTrack(EtoDirection(DirectionList[count]), 180);
                                MessageText.text = "請沿著軌道方向旋轉身體到最大距離\n按下 [A] 鍵來結束測試";

                                StartVector = Camera.main.transform.forward;

                                float angle = EtoDirection(DirectionList[count]);
                                count++;
                                testing = true;
                                Debug.Log(angle.ToString() + " " + Posture.ToString());
                                emg_logger.start_logging(angle.ToString(), Posture.ToString());
                            }
                            ready = false;
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
                    // end test
                    if (OVRInput.GetDown(OVRInput.Button.One))
                    {
                        Track.startColor = endColor;
                        Track.endColor = endColor;

                        testing = false;
                        ready = false;

                        EndVector = Camera.main.transform.forward;
                        MaxViewingRange = Vector3.Angle(StartVector, EndVector);

                        string NewLine = ParticipantID.ToString() + ',' + Posture.ToString() + ','
                        + EtoDirection(DirectionList[count - 1]).ToString() + ',' + MaxViewingRange.ToString();
                        sw.WriteLine(NewLine);
                        emg_logger.end_logging();
                    }
                }
            }
        }
        else {
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

    public void HitArea()
    {
        ready = true;
    }

    public void Redirect()
    {
        redirect = true;
    }

    void OnApplicationQuit()
    {
        sw.Close();
        fs.Close();
    }
}