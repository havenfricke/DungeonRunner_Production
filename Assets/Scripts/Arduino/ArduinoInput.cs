using System;
using System.IO.Ports;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.Windows;

/*
---------ARDUINO INPUT TO VIRTUAL GAMEPAD----------
 
Description:

This script reads input from an Arduino connected via 
serial (USB) and maps it to a virtual gamepad device.
It supports a joystick with X and Y axes and a button (Z click).
It uses the new Unity Input System package to create 
and manage the virtual gamepad.

--------------ARDUINO CONFIGURATION----------------

*Elegoo Uno R3 Analog Joystick Input Module

Joystick
GND  -> GND
5V   -> 5V
VRx  -> A0
VRy  -> A1
SW   -> D2 

Button
GND  -> (-) Breadboard
3.3v -> (+) Breadboard

---ARDUINO IDE CODE CORRESPONDING TO THIS SCRIPT---

Note: This code is for the Arduino IDE, not C#. 
When uploading to your Arduino, close the IDE after 
uploading to free the serial port for Unity.

int xAxisInput = A0;
int yAxisInput = A1;
int zClickInput = 2;

int xAxisValue;
int yAxisValue;
int zClickValue;

void setup() {
  Serial.begin(9600);
  pinMode(xAxisInput, INPUT);
  pinMode(yAxisInput, INPUT);
  pinMode(zClickInput, INPUT);

  // WAKE THE DIGITAL INPUT UP
  digitalWrite(zClickInput, HIGH);
}

void loop() {
  // READ X AND Y VALUES
  xAxisValue = analogRead(xAxisInput);
  yAxisValue = analogRead(yAxisInput);

  // READ DIGITAL INPUT Z-CLICK
  zClickValue = digitalRead(zClickInput);

  // FLIP CLICK VALUES AROUND SINCE WE HAD TO WAKE IT UP
  switch(zClickValue)
  {
    case 1:
      zClickValue = 0;
      break;
    case 0:
      zClickValue = 1;
      break;
  }

  //DEBUG - Used by Unity to get input
  Serial.println("x" + String(xAxisValue));
  Serial.println("y" + String(yAxisValue));
  Serial.println("z" + String(zClickValue));
  
  delay(50);
}


--------------READ BEFORE PROCEEDING---------------

To use the libraries System.IO.Ports and System.Threading,
Unity needs to be set to use .NET Framework by changing settings found in:
Edit > Project Settings > Player > Other Settings > Configuration > Api Compatability Level
*/




public class ArduinoInput : MonoBehaviour
{
    [Header("Serial Settings")]
    [Tooltip("Windows: COM3, COM4...  |  macOS: /dev/tty.usbmodemXXXX or /dev/tty.usbserialXXXX")]
    public string portName = "COM3";
    public int baudRate = 9600;
    public int readTimeoutMs = 25;

    [Header("Calibration (raw at rest)")]
    [Tooltip("Center (idle) values from your Arduino at rest")]
    public int centerX = 496;
    public int centerY = 507;

    [Tooltip("Raw input min/max from analogRead")]
    public int rawMin = 0;
    public int rawMax = 1023;

    [Header("Processing")]
    [Range(0f, 0.3f)] public float deadzone = 0.09f;
    [Range(0f, 1f)] public float smooth = 0.15f; // 0 = off

    [Header("Button Mapping")]
    [Tooltip("Which button the Z-click should press on the virtual gamepad")]
    public GamepadButton zMapsTo = GamepadButton.North;  
    [Tooltip("Which button the extra 'b' input should press on the virtual gamepad")]
    public GamepadButton bMapsTo = GamepadButton.RightTrigger;  
    [Tooltip("If your wiring uses pull-ups (HIGH when released, LOW when pressed), leave this ON.")]
    public bool bIsActiveLow = true;
    [Tooltip("If your Z input comes in active-low, set this accordingly.")]
    public bool zIsActiveLow = false; // your Arduino flips z already; leave false unless you change Arduino side

    [Header("Debug (read-only)")]
    public int rawX;
    public int rawY;
    public int rawZ; // 0 or 1 (raw from serial)
    public int rawB; // 0 or 1 (raw from serial)
    public float normX; // -1..1
    public float normY; // -1..1
    public bool zPressed; // processed (after active-low)
    public bool bPressed; // processed (after active-low)

    private SerialPort _sp;
    private float _smoothedX, _smoothedY;
    private Gamepad _virtualPad;

    // previous states for edge detection
    private bool _prevBPressed;

    void Awake()
    {
        if (_sp == null)
        {
            _virtualPad = InputSystem.AddDevice<Gamepad>("Arduino Gamepad");
            Debug.Log("[ArduinoInput] Virtual Gamepad added: " + _virtualPad.name);
        }
    }

    void Start()
    {
        try
        {
            _sp = new SerialPort(portName, baudRate) { NewLine = "\n", ReadTimeout = readTimeoutMs };
            _sp.Open();
            Debug.Log("[ArduinoInput] Serial opened on " + portName);
        }
        catch (Exception e)
        {
            Debug.Log("[ArduinoInput] Failed to open serial: " + e.Message);
        }
    }

    void Update()
    {
        if (_sp != null && _sp.IsOpen)
        {
            try
            {
                while (_sp.BytesToRead > 0)
                {
                    var line = _sp.ReadLine().Trim();
                    if (string.IsNullOrEmpty(line)) continue;

                    // Expect "x512", "y487", "z1", "b1"
                    switch (line[0])
                    {
                        case 'x': if (int.TryParse(line.AsSpan(1), out var vx)) rawX = vx; break;
                        case 'y': if (int.TryParse(line.AsSpan(1), out var vy)) rawY = vy; break;
                        case 'z': if (int.TryParse(line.AsSpan(1), out var vz)) rawZ = Mathf.Clamp(vz, 0, 1); break;
                        case 'b': if (int.TryParse(line.AsSpan(1), out var vb)) rawB = Mathf.Clamp(vb, 0, 1); break;
                    }
                }
            }
            catch (TimeoutException) { /* no new data */ }
            catch (Exception e)
            {
                Debug.LogWarning("[ArduinoInput] Serial read error: " + e.Message);
            }
        }

        // Normalize & smooth
        normX = NormalizeAxis(rawX, centerX, rawMin, rawMax, deadzone);
        normY = NormalizeAxis(rawY, centerY, rawMin, rawMax, deadzone);

        if (smooth > 0f)
        {
            float a = 1f - Mathf.Pow(1f - smooth, Time.deltaTime * 60f);
            _smoothedX = Mathf.Lerp(_smoothedX, normX, a);
            _smoothedY = Mathf.Lerp(_smoothedY, normY, a);
        }
        else
        {
            _smoothedX = normX;
            _smoothedY = normY;
        }

        // Process digital inputs with active-low options
        zPressed = zIsActiveLow ? (rawZ == 0) : (rawZ != 0);
        bPressed = bIsActiveLow ? (rawB == 0) : (rawB != 0);

        Debug.Log($"x: {_smoothedX} y: {_smoothedY} / z: {rawZ} b:{rawB}");

        // Edge detection
        _prevBPressed = bPressed;

        if (_virtualPad != null)
        {
            // Buttons (for anything that really IS a button)
            uint buttons = 0;
            if (zPressed) buttons |= (uint)zMapsTo; // ok if zMapsTo is a real button (South/North/etc.)

            // IMPORTANT: Right trigger is an AXIS (0..1), not a button
            var state = new GamepadState
            {
                leftStick = new Vector2(_smoothedX, -_smoothedY),
                rightTrigger = bPressed ? 1f : 0f,   // map Arduino 'b' to RT axis
                buttons = buttons
            };

            InputSystem.QueueStateEvent(_virtualPad, state);
        }
    }

    static float NormalizeAxis(int raw, int center, int min, int max, float dz)
    {
        float range = raw >= center ? (max - center) : (center - min);
        if (range <= 0.0001f) return 0f;

        float v = Mathf.Clamp((raw - center) / range, -1f, 1f);
        float mag = Mathf.Abs(v);
        if (mag < dz) return 0f;

        float rescaled = Mathf.InverseLerp(dz, 1f, mag) * Mathf.Sign(v);
        return Mathf.Clamp(rescaled, -1f, 1f);
    }

    void OnDisable()
    {
        if (_virtualPad != null)
        {
            InputSystem.RemoveDevice(_virtualPad);
            _virtualPad = null;
            Debug.Log("[ArduinoInput] Virtual Gamepad removed.");
        }

        if (_sp != null)
        {
            try { if (_sp.IsOpen) _sp.Close(); } catch { }
            _sp.Dispose();
            _sp = null;
        }
    }

    void OnApplicationQuit() => OnDisable();
}

