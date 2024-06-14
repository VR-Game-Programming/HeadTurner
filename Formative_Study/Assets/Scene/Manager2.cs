using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Meta.WitAi.Data.Entities;
using TMPro;
using Unity.VisualScripting;
using UnityEditor.ProjectWindowCallback;
using UnityEngine;
using UnityEngine.PlayerLoop;

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

    // Game Control
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
        {0, 50},
        {90, 30},
        {180, 50},
        {270, 30},
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

    // CSV File Setting
    [Header("File Setting")]
    public string MaterialsFolder = @"Materials";
    public string ResultFolder = @"Result";
    private string FullPath = "";
    private FileStream fs;
    private StreamWriter sw;


    void Start() {
        MessageText = Message.GetComponent<TextMeshProUGUI>();
        MessageText.text = "";

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
        FullPath = Path.Combine(ResultFolder, "Formative_O1_P" + ParticipantID.ToString() + "_" + Posture.ToString() + ".csv");
        fs = new FileStream(FullPath, FileMode.OpenOrCreate);
        sw = new StreamWriter(fs);
        string Header = "Participant" + ',' + "Posture" + ',' + "Direction" + "," + "Count" + ","
            + "time" + "," + "HeadRotationAngle" + ',' + "TrunkRotationAngle";
        sw.WriteLine(Header);
    }

    void Update() {
        if (!endtests) {
            if (!testing) {
                if (count >= DirectionList.Count) {
                    endtests = true;
                    MessageText.text = "此輪測試已全部完成\n請通知實驗人員";
                    sw.Close();
                    fs.Close();
                }
                else {
                    if (rest) {
                        MessageText.text = "休息時間，請通知實驗人員\n按下 [A] 鍵來繼續測試";

                        if (OVRInput.GetDown(OVRInput.Button.One)) {
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

                DataRecorder();
            }
        }
    }

    public void CreateNewTrack(float rotationAngle, float viewingRange) {
        if (!gameObject.TryGetComponent<LineRenderer>(out Track)) {
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

        for (int i = 0; i < segments + 1; i++) {
            float x = Mathf.Sin(Mathf.Deg2Rad * currentAngle) * radius;
            float z = Mathf.Cos(Mathf.Deg2Rad * currentAngle) * radius;

            Vector3 rotatedPoint = Quaternion.Euler(0, 0, rotationAngle) * new Vector3(x, 0, z);

            Track.SetPosition(i, rotatedPoint);

            if (i == segments) {
                EndArea.transform.position = rotatedPoint;
            }

            currentAngle += angleStep;
        }

        // Generate Mesh Collider
        if (!gameObject.TryGetComponent<MeshCollider>(out Collider)) {
            Collider = gameObject.AddComponent<MeshCollider>();
        }
        Mesh mesh = new();
        Track.BakeMesh(mesh);
        Collider.sharedMesh = mesh;
    }

    public void HitTrack() {
        hitTrack = true;
    }

    public void HitStartArea() {
        ready = true;
    }

    public void HitEndArea() {
        hitEnd = true;
    }

    void OnApplicationQuit()
    {
        sw.Close();
        fs.Close();
    }

    public void DataRecorder() {
        if (Timer - Time.deltaTime < 0) {
            Timer = Interval;

            float HeadRotationAngle = Vector3.Angle(HeadStartVector, Camera.main.transform.forward);
            float TrunkRotationAngle = enableTrunk ? Vector3.Angle(TrunkStartVector, TrunkAnchor.transform.forward) : 0f;

            string Data = ParticipantID.ToString() + ',' + Posture.ToString() + ',' + DirectionList[count].ToString() + "," + tcount.ToString() + ","
            + Time.time.ToString() + "," + HeadRotationAngle.ToString() + ',' + TrunkRotationAngle.ToString();

            sw.WriteLine(Data);

        } else {
            Timer -= Time.deltaTime;
        }
    }
}