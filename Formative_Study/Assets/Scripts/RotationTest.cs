using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RotationTest : MonoBehaviour
{
    // The object that will be rotated, and we are going to monitor its rotation using the Roll, Yaw, Pitch expressions
    public GameObject rb;
    // Start is called before the first frame update
    Quaternion startRotation;
    void Start()
    {
        if (rb == null)
        {
            // Quit
            Debug.LogError("The object to be rotated is not set. Quitting the application.");
            Application.Quit();
        }
        startRotation = rb.transform.rotation;
    }

    // Update is called once per frame
    void Update()
    {
        Quaternion currentRotation = rb.transform.rotation;
        // Calculate the difference between the current rotation and the start rotation
        Quaternion diff = currentRotation * Quaternion.Inverse(startRotation);
        // Extract the Roll, Yaw, Pitch values from the difference
        // Yaw-Y。Pitch-X。Roll-Z
        float yaw = diff.eulerAngles.y;
        float pitch = diff.eulerAngles.x;
        float roll = diff.eulerAngles.z;
        Debug.Log("Yaw: " + yaw + ", Pitch: " + pitch + ", Roll: " + roll);

    }
}
