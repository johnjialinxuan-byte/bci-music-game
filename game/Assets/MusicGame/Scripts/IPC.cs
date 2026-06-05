using UnityEngine;
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using MusicGame.Core;


/// <summary>
/// BCI platform IPC client for Unity.
/// Packet format: 4-byte big-endian payload length + UTF-8 JSON payload.
/// </summary>
public class IPC : MonoBehaviour
{
    [Header("Connection")]
    public string ip = CommunicationSettings.DefaultRemoteIp;
    public int port = 8000;
    public bool autoConnectOnStart = true;

    public event Action OnConnected;
    public event Action OnDisconnected;
    public event Action<string> OnRawMessageReceived;
    public event Action<AlgorithmTestData> OnAlgorithmTest;

    public bool IsConnected => client != null && client.Connected;

    [Header("Runtime Data")]
    public int AttentionValue;
    public Vector3 GyroscopeValue;
    public string LastAlgorithmName = "";

    private TcpClient client;
    private NetworkStream stream;
    private Thread receiveThread;
    private readonly object queueLock = new object();
    private readonly List<byte> recvBuffer = new List<byte>(4096);
    private readonly Queue<string> msgQueue = new Queue<string>();
    private volatile bool isRunning;

    private void Start()
    {
        if (autoConnectOnStart)
            Connect();
    }

    private void Update()
    {
        ProcessQueue();
    }

    private void OnDestroy()
    {
        Disconnect();
    }

    public void Connect()
    {
        ip = CommunicationSettings.CurrentIp;

        if (IsConnected) return;

        try
        {
            client = new TcpClient(ip, port);
            stream = client.GetStream();
            isRunning = true;
            receiveThread = new Thread(ReceiveLoop) { IsBackground = true };
            receiveThread.Start();

            Debug.Log($"[IPC] Connected to {ip}:{port}");
            OnConnected?.Invoke();
        }
        catch (Exception e)
        {
            Debug.LogError($"[IPC] Connect failed: {e.Message}");
        }
    }

    public void Disconnect()
    {
        isRunning = false;

        try { stream?.Close(); } catch { }
        try { client?.Close(); } catch { }

        stream = null;
        client = null;

        lock (queueLock)
        {
            msgQueue.Clear();
        }

        recvBuffer.Clear();
        Debug.Log("[IPC] Disconnected");
        OnDisconnected?.Invoke();
    }

    public void SendJson(string json)
    {
        if (!IsConnected)
        {
            Debug.LogWarning("[IPC] Not connected; send skipped.");
            return;
        }

        try
        {
            byte[] payload = Encoding.UTF8.GetBytes(json);
            byte[] packet = new byte[4 + payload.Length];
            int len = payload.Length;
            packet[0] = (byte)(len >> 24);
            packet[1] = (byte)(len >> 16);
            packet[2] = (byte)(len >> 8);
            packet[3] = (byte)len;
            Buffer.BlockCopy(payload, 0, packet, 4, payload.Length);
            stream.Write(packet, 0, packet.Length);
        }
        catch (Exception e)
        {
            Debug.LogError($"[IPC] Send failed: {e.Message}");
        }
    }

    public void SendStartTest(string algorithmArgs)
    {
        SendJson($"{{\"msg\":\"ipc_algorithm_start_test\",\"algorithm_args\":{algorithmArgs}}}");
    }

    public void SendStopTest()
    {
        SendJson("{\"msg\":\"ipc_algorithm_stop_test\"}");
    }

    public void SendUserInfo(int windowHandle = 0)
    {
        SendJson($"{{\"msg\":\"ipc_user_info\",\"window\":{windowHandle}}}");
    }

    public void SendEvent(int eventId)
    {
        SendJson($"{{\"msg\":\"ipc_event\",\"event\":{eventId}}}");
    }

    private void ReceiveLoop()
    {
        byte[] temp = new byte[4096];

        while (isRunning)
        {
            try
            {
                if (stream == null || !stream.CanRead) break;

                int read = stream.Read(temp, 0, temp.Length);
                if (read <= 0) break;

                lock (recvBuffer)
                {
                    for (int i = 0; i < read; i++)
                        recvBuffer.Add(temp[i]);

                    while (recvBuffer.Count >= 4)
                    {
                        int payloadLen = (recvBuffer[0] << 24) | (recvBuffer[1] << 16) | (recvBuffer[2] << 8) | recvBuffer[3];
                        int totalLen = 4 + payloadLen;
                        if (payloadLen < 0 || payloadLen > 16 * 1024 * 1024)
                        {
                            Debug.LogError($"[IPC] Invalid packet length: {payloadLen}");
                            recvBuffer.Clear();
                            break;
                        }

                        if (recvBuffer.Count < totalLen)
                            break;

                        byte[] payload = new byte[payloadLen];
                        for (int i = 0; i < payloadLen; i++)
                            payload[i] = recvBuffer[4 + i];

                        recvBuffer.RemoveRange(0, totalLen);

                        lock (queueLock)
                        {
                            msgQueue.Enqueue(Encoding.UTF8.GetString(payload));
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[IPC] Receive failed: {e.Message}");
                break;
            }
        }
    }

    private void ProcessQueue()
    {
        if (isRunning && (client == null || !client.Connected))
        {
            Disconnect();
            return;
        }

        while (true)
        {
            string json;
            lock (queueLock)
            {
                if (msgQueue.Count == 0) break;
                json = msgQueue.Dequeue();
            }

            Debug.Log($"[IPC] Received: {json}");
            OnRawMessageReceived?.Invoke(json);
            Dispatch(json);
        }
    }

    private void Dispatch(string json)
    {
        string msg = ExtractField(json, "msg");
        if (string.IsNullOrEmpty(msg)) return;

        switch (msg)
        {
            case "ipc_user_info":
                int layout = ParseInt(ExtractField(json, "layout_type"));
                if (layout == 1)
                    SendUserInfo(0);
                break;

            case "ipc_algorithm_test":
                HandleAlgorithmTest(json);
                break;

            case "ipc_algorithm_start_test":
                string startResult = ExtractField(json, "result");
                string startMessage = ExtractField(json, "fail_message") ?? "";
                Debug.Log($"[IPC] Start test response: result={startResult}, msg={startMessage}");
                break;

            case "ipc_algorithm_stop_test":
                string stopResult = ExtractField(json, "result");
                string stopMessage = ExtractField(json, "fail_message") ?? "";
                Debug.Log($"[IPC] Stop test response: result={stopResult}, msg={stopMessage}");
                break;

            case "ipc_set_visible":
                string visible = ExtractField(json, "visible");
                Debug.Log($"[IPC] Set visible: {visible}");
                break;

            case "ipc_exit":
                Debug.Log("[IPC] Exit requested");
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
#else
                Application.Quit();
#endif
                break;
        }
    }

    private void HandleAlgorithmTest(string json)
    {
        string algoName = ExtractField(json, "algorithm_name") ?? "";
        LastAlgorithmName = algoName;
        string rawData = ExtractNestedField(json, "result_args", "data");

        if (string.Equals(algoName, "attention", StringComparison.OrdinalIgnoreCase))
        {
            if (int.TryParse(rawData, out int attention))
                AttentionValue = Mathf.Clamp(attention, 0, 100);
        }
        else if (string.Equals(algoName, "gyroscope", StringComparison.OrdinalIgnoreCase))
        {
            string gx = ExtractField(rawData, "gyroscope_x") ?? ExtractField(rawData, "gyroscopeX");
            string gy = ExtractField(rawData, "gyroscope_y") ?? ExtractField(rawData, "gyroscopeY");
            string gz = ExtractField(rawData, "gyroscope_z") ?? ExtractField(rawData, "gyroscopeZ");

            if (float.TryParse(gx, out float fx) &&
                float.TryParse(gy, out float fy) &&
                float.TryParse(gz, out float fz))
            {
                GyroscopeValue = new Vector3(fx, fy, fz);
            }
        }

        AlgorithmTestData result = new AlgorithmTestData
        {
            algorithm_name = algoName,
            rawData = rawData
        };

        if (rawData != null)
        {
            if (int.TryParse(rawData, out int intValue))
            {
                result.isInt = true;
                result.dataInt = intValue;
            }
            else
            {
                result.dataString = rawData;
            }
        }

        OnAlgorithmTest?.Invoke(result);
    }

    private static string ExtractField(string json, string key)
    {
        if (string.IsNullOrEmpty(json) || string.IsNullOrEmpty(key)) return null;

        string pattern = $"\"{key}\"";
        int index = json.IndexOf(pattern, StringComparison.OrdinalIgnoreCase);
        if (index < 0) return null;

        index = json.IndexOf(':', index + pattern.Length);
        if (index < 0) return null;

        index++;
        while (index < json.Length && char.IsWhiteSpace(json[index]))
            index++;
        if (index >= json.Length) return null;

        if (json[index] == '"')
        {
            index++;
            StringBuilder builder = new StringBuilder();
            for (int i = index; i < json.Length; i++)
            {
                if (json[i] == '\\' && i + 1 < json.Length)
                {
                    builder.Append(json[i + 1]);
                    i++;
                }
                else if (json[i] == '"')
                {
                    return builder.ToString();
                }
                else
                {
                    builder.Append(json[i]);
                }
            }

            return null;
        }

        if (json[index] == '{' || json[index] == '[')
        {
            char open = json[index];
            char close = open == '{' ? '}' : ']';
            int depth = 0;
            int start = index;
            bool inString = false;

            for (int i = index; i < json.Length; i++)
            {
                if (json[i] == '"' && (i == 0 || json[i - 1] != '\\'))
                    inString = !inString;
                if (inString) continue;

                if (json[i] == open) depth++;
                else if (json[i] == close)
                {
                    depth--;
                    if (depth == 0)
                        return json.Substring(start, i - start + 1).Trim();
                }
            }

            return null;
        }

        int valueStart = index;
        while (index < json.Length && json[index] != ',' && json[index] != '}' && json[index] != ']')
            index++;
        return json.Substring(valueStart, index - valueStart).Trim();
    }

    private static string ExtractNestedField(string json, string parent, string child)
    {
        string parentObject = ExtractField(json, parent);
        return ExtractField(parentObject, child);
    }

    private static int ParseInt(string value)
    {
        return int.TryParse(value, out int result) ? result : 0;
    }
}

[System.Serializable]
public class AlgorithmTestData
{
    public string algorithm_name;
    public string rawData;
    public bool isInt;
    public int dataInt;
    public string dataString;
}

