using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using System.IO.Ports;
using System.Threading;

public class Manager1 : MonoBehaviour
{
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

    // Objects
    public GameObject Message;
    private TextMeshProUGUI MessageText;
    public GameObject Cam;
    public GameObject Viewport;
    private Quaternion StartRotation;

    // Game Control
    private bool redirect = false;
    private bool ready = false;
    private int tcount = 0;
    private int count = 0;
    private bool testing = false;
    private bool endtests = false;

    // Data Setting
    public enum PostureE { Standing, Sitting, Lying }

    [Header("Task Setting")]
    public int ParticipantID = 0;
    public PostureE Posture = PostureE.Standing;
    private List<int> DirectionList = new();

    // Data Recording
    private Vector3 StartVector;
    private float MaxViewingRange;

    // CSV File Setting
    [Header("File Setting")]
    public string MaterialsFolder = @"Materials";
    public string ResultFolder = @"Result";
    private string FullPath = "";
    private FileStream fs;
    private StreamWriter sw;

    void Start()
    {
        MessageText = Message.GetComponent<TextMeshProUGUI>();
        MessageText.text = "按下 [A] 鍵來重新定向";

        // Reading Task Order
        FullPath = Path.Combine(MaterialsFolder, "Formative_T1_Order.csv");
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
        FullPath = Path.Combine(ResultFolder, "Formative_T1_P" + ParticipantID.ToString() + "_" + Posture.ToString() + ".csv");
        fs = new FileStream(FullPath, FileMode.OpenOrCreate);
        sw = new StreamWriter(fs);
        string Header = "Participant,Posture,tcount,Direction,MaxViewingRange";
        sw.WriteLine(Header);
    }

    void Update()
    {
        if (redirect) {
            if (!endtests) {
                if (!testing) {
                    if (count >= DirectionList.Count) {
                        endtests = true;
                        MessageText.text = "此輪測試已全部完成\n請通知實驗人員";

                        sw.Close();
                        fs.Close();
                    }
                    else {
                        if (!ready) {
                            MessageText.text = "請回到起始區域";
                        } else {
                            MessageText.text = "按下 [A] 鍵來開始新測試";
                            // start new test
                            if (OVRInput.GetDown(OVRInput.Button.One)) {
                                float rotationAngle = DirectionList[count];

                                CreateNewTrack(rotationAngle, 180);
                                MessageText.text = "請沿著軌道方向旋轉身體到最大距離\n按下 [A] 鍵來結束測試";

                                StartVector = Camera.main.transform.forward;
                                
                                tcount ++;
                                testing = true;
                            }
                            ready = false;
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
                    // end test
                    if (OVRInput.GetDown(OVRInput.Button.One)) {
                        Track.startColor = endColor;
                        Track.endColor = endColor;

                        testing = false;
                        ready = false;

                        Vector3 EndVector = Camera.main.transform.forward;
                        MaxViewingRange = Vector3.Angle(StartVector, EndVector);

                        string NewLine = ParticipantID.ToString() + ',' + Posture.ToString() + ',' + tcount.ToString() + ','
                        + DirectionList[count].ToString() + ',' + MaxViewingRange.ToString();

                        sw.WriteLine(NewLine);

                        Debug.Log("tcount: " + tcount.ToString() + " count: " + count.ToString());

                        if (tcount >= 3) {
                            tcount = 0;
                            count ++;
                        }
                    }
                }
            }
            else {
                return;
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