using System.Collections;
using System.Collections.Generic;
using System.IO.Ports;
using System;
using UnityEngine;
using System.Threading;

public class ArduinoCommunication : MonoBehaviour
{
    public string portName = "COM8";
    public int baudRate = 115200;
    const int motorRange = 660;

    private SerialPort port;
    int targetLinearMotor = 0;
    public int TargetLinearMotor{
        get{
            return targetLinearMotor;
        }
        set{
            targetLinearMotor = value;
        }
    }
    int currentMotor = 0;
    float threshold = 15f;
    public float motorSpeed = 0.15f;
    Thread arduinoCommander;
    Thread arduinoReader;
    void Awake()
    {
        port = new SerialPort(portName, baudRate);
        try
        {
            port.Open();
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed opening port. Log: \n{e}");
        }

        if (!port.IsOpen) return;
    }
    private void Start()
    {
        /* StartCoroutine(ActuatorCommand());
        StartCoroutine(EncoderRead()); */
        arduinoCommander = new Thread(ArduinoCommand);
        arduinoReader = new Thread(ArduinoRead);
        arduinoCommander.Start();
        arduinoReader.Start();
    }
    protected void Command(char mode, int pos)
    {
        if (!port.IsOpen) return;
        port.WriteLine(mode + pos.ToString());
        //Debug.Log(mode + pos.ToString());
    }
    IEnumerator ActuatorCommand()
    {
        while (true)
        {
            currentMotor = (int)Mathf.Lerp(currentMotor, targetLinearMotor, motorSpeed);
            currentMotor = Mathf.Clamp(currentMotor, -motorRange / 2, motorRange / 2);
            try
            {
                Command('p', currentMotor);
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed sending command. Log: \n{e}");
            }
            yield return new WaitForSeconds(0.5f);
        }
    }
    IEnumerator EncoderRead()
    {
        while (true)
        {
            try
            {
                string message = port.ReadLine();
                //Debug.Log(message);
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed reading message. Log: \n{e}");
            }
            yield return new WaitForSeconds(0.1f);
        }
    }

    // Update is called once per frame
    void ArduinoCommand()
    {
        while (true)
        {
            if (Math.Abs(currentMotor - targetLinearMotor) > threshold)
            {
                currentMotor = (int)Mathf.Lerp(currentMotor, targetLinearMotor, motorSpeed);
                currentMotor = Mathf.Clamp(currentMotor, -motorRange / 2, motorRange / 2);
                try
                {
                    Command('p', currentMotor);
                }
                catch (Exception e)
                {
                    Debug.LogError($"Failed sending command. Log: \n{e}");
                }
            }
            else
            {
                Command('s', 0);
            }
            Thread.Sleep(100); // 100 ms
        }
    }
    void ArduinoRead()
    {
        while (true)
        {
            try
            {
                string message = port.ReadLine();
                //Debug.Log(message);
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed reading message. Log: \n{e}");
            }
        }
    }
    void OnDestroy()
    {
        arduinoCommander.Abort();
        arduinoReader.Abort();
        Command('p', 0);
        if (port.IsOpen)
        {
            port.Close();
        }
    }
}
