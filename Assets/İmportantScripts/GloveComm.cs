using System;
using System.IO.Ports;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

public class GloveComm : MonoBehaviour
{
    public enum Mode { UDP, Serial, Mock }
    public Mode mode = Mode.UDP;
    public int udpPort = 52000;
    public string serialPortName = "COM3";
    public int baudRate = 115200;
    public bool startOnAwake = true;
    public float mockUpdateHz = 60f;
    public static event Action<GloveSnapshot> OnSnapshot;

    UdpClient _udp;
    Thread _udpThread;
    SerialPort _serial;
    Thread _serialThread;
    volatile bool _running;
    float _mockTimer;

    void Awake() { if (startOnAwake) StartComm(); }
    void OnDestroy() { StopComm(); }

    public void StartComm()
    {
        StopComm();
        _running = true;
        if (mode == Mode.UDP)
        {
            _udp = new UdpClient(udpPort);
            _udpThread = new Thread(UdpLoop) { IsBackground = true };
            _udpThread.Start();
        }
        else if (mode == Mode.Serial)
        {
            try
            {
                _serial = new SerialPort(serialPortName, baudRate);
                _serial.NewLine = "\n";
                _serial.Open();
                _serialThread = new Thread(SerialLoop) { IsBackground = true };
                _serialThread.Start();
            }
            catch (Exception e) { Debug.LogError($"GloveComm Serial start failed: {e}"); }
        }
    }

    public void StopComm()
    {
        _running = false;
        try { _udp?.Close(); } catch { }
        try { if (_udpThread != null && _udpThread.IsAlive) _udpThread.Join(100); } catch { }
        try { if (_serial != null && _serial.IsOpen) _serial.Close(); } catch { }
        try { if (_serialThread != null && _serialThread.IsAlive) _serialThread.Join(100); } catch { }
    }

    void UdpLoop()
    {
        IPEndPoint ep = new IPEndPoint(IPAddress.Any, udpPort);
        while (_running)
        {
            try
            {
                var data = _udp.Receive(ref ep);
                if (data != null && data.Length > 0)
                {
                    string s = Encoding.UTF8.GetString(data);
                    ParseLine(s);
                }
            }
            catch (SocketException) { }
            catch (Exception e) { Debug.LogException(e); }
        }
    }

    void SerialLoop()
    {
        while (_running)
        {
            try
            {
                string line = _serial.ReadLine();
                if (!string.IsNullOrEmpty(line)) ParseLine(line);
            }
            catch (TimeoutException) { }
            catch (Exception e) { Debug.LogException(e); }
        }
    }

    void Update()
    {
        if (mode == Mode.Mock)
        {
            _mockTimer += Time.deltaTime;
            if (_mockTimer >= 1f / mockUpdateHz)
            {
                _mockTimer = 0f;
                var s = CreateMockSnapshot();
                OnSnapshot?.Invoke(s);
            }
        }
    }

    void ParseLine(string line)
    {
        if (string.IsNullOrWhiteSpace(line)) return;
        line = line.Trim();
        GloveSnapshot snap = null;
        try
        {
            if (line.StartsWith("{"))
                snap = GloveSnapshot.FromJson(line);
            else
                snap = GloveSnapshot.FromKeyValue(line);
        }
        catch (Exception e) { Debug.LogWarning($"GloveComm parse error: {e.Message}"); }
        if (snap != null) OnSnapshot?.Invoke(snap);
    }

    // DÜZELTÝLDÝ: Tüm 5 parmak için veri üretir ve el deðiþtirir.
    GloveSnapshot CreateMockSnapshot()
    {
        var s = new GloveSnapshot();

        // Her 3 saniyede bir el deðiþtir
        s.handedness = (Mathf.FloorToInt(Time.time) % 6 < 3) ? "LEFT" : "RIGHT";

        s.flex = new float[5];
        float t = Time.time;
        // 5 parmak için farklý sinyaller
        s.flex[0] = (Mathf.Sin(t * 1.0f) + 1f) * 0.5f;
        s.flex[1] = (Mathf.Sin(t * 1.5f + 0.5f) + 1f) * 0.5f;
        s.flex[2] = (Mathf.Sin(t * 1.3f + 1.0f) + 1f) * 0.5f;
        s.flex[3] = (Mathf.Sin(t * 1.7f + 1.5f) + 1f) * 0.5f;
        s.flex[4] = (Mathf.Sin(t * 1.2f + 2.0f) + 1f) * 0.5f;

        s.wpos = new float[] { 0f, 1.2f, 0.2f };
        s.wrot = new float[] { 0f, 0f, 0f, 1f };
        s.timestamp = DateTime.UtcNow.Ticks / 10;
        return s;
    }
}

[Serializable]
public class GloveSnapshot
{
    public string handedness;
    public float[] flex;
    public float[] wpos;
    public float[] wrot;
    public long timestamp;

    public static GloveSnapshot FromJson(string json)
        => JsonUtility.FromJson<GloveSnapshot>(json);

    public static GloveSnapshot FromKeyValue(string line)
    {
        var s = new GloveSnapshot();
        s.flex = new float[5];
        s.wpos = new float[3];
        s.wrot = new float[4];

        var parts = line.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries);
        foreach (var p in parts)
        {
            var kv = p.Split(new[] { ':', '=' }, 2);
            if (kv.Length != 2) continue;
            var k = kv[0].Trim().ToLower();
            var v = kv[1].Trim();

            if (k == "hand" || k == "h") s.handedness = v.ToUpper();
            else if (k.StartsWith("f"))
            {
                var arr = v.Split('|');
                for (int i = 0; i < Mathf.Min(arr.Length, 5); i++)
                    if (float.TryParse(arr[i], out float fv)) s.flex[i] = fv;
            }
            else if (k == "wpos")
            {
                var arr = v.Split('|');
                for (int i = 0; i < Mathf.Min(arr.Length, 3); i++)
                    if (float.TryParse(arr[i], out float fv)) s.wpos[i] = fv;
            }
            else if (k == "wrot")
            {
                var arr = v.Split('|');
                for (int i = 0; i < Mathf.Min(arr.Length, 4); i++)
                    if (float.TryParse(arr[i], out float fv)) s.wrot[i] = fv;
            }
            else if (k == "ts")
                if (long.TryParse(v, out long tv)) s.timestamp = tv;
        }
        return s;
    }
}