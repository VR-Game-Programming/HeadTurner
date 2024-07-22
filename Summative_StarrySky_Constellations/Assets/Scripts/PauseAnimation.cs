using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PauseAnimation : MonoBehaviour
{
    // Start is called before the first frame update
    public Animator stars;
    public bool pause=false, play=false;
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (pause)
        {
            Time.timeScale = 0;
            pause = false;
        }
        if (play)
        {
            Time.timeScale = 1;
            play = false;
        }
        if (Input.GetKey(KeyCode.Mouse1))
        {
            Camera.main.transform.RotateAround(Camera.main.transform.position, Camera.main.transform.right, Input.GetAxis("Mouse Y"));
            Camera.main.transform.RotateAround(Camera.main.transform.position, Vector3.up, -Input.GetAxis("Mouse X"));
        }
    }
}
