using UnityEngine;
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Threading;

/// <summary>
/// BCI 科创平台 IPC 通信客户端 (Unity C# 版)
/// 协议: 4字节 BigEndian 长度头 + UTF-8 JSON
/// 参照: 基础篇案例开发文档 + ipc通信协议 + ipcsocket Python 示例
/// </summary>
public class IPC : MonoBehaviour
{
    [Header("连接配置")]
    public string ip = "127.0.0.1";
    public int port = 8000;
    public bool autoConnectOnStart = true;

    // 事件
    public event Action OnConnected;
    public event Action OnDisconnected;
    public event Action<string> OnRawMessageReceived;
    public event Action<AlgorithmTestData> OnAlgorithmTest;

    public bool IsConnected => client != null && client.Connected;

    [Header("在线数据（只读）")]
    public int AttentionValue;           // attention 算法输出 0~100
    public Vector3 GyroscopeValue;       // gyroscope 算法输出 (x=偏航, y=俯仰, z=翻滚)
    public string LastAlgorithmName = "";

    TcpClient client;
    NetworkStream stream;
    Thread receiveThread;
    readonly object queueLock = new object();
    readonly List<byte> recvBuffer = new List<byte>(4096);
    readonly Queue<string> msgQueue = new Queue<string>();
    volatile bool isRunning;

    void Start() { if (autoConnectOnStart) Connect(); }
    void Update() { ProcessQueue(); }
    void OnDestroy() { Disconnect(); }

    // ----------------- 连接管理 -----------------

    public void Connect()
    {
        if (IsConnected) return;
        try
        {
            client = new TcpClient(ip, port);
            stream = client.GetStream();
            isRunning = true;
            receiveThread = new Thread(ReceiveLoop) { IsBackground = true };
            receiveThread.Start();
            Debug.Log($"[IPC] 已连接到 {ip}:{port}");
            OnConnected?.Invoke();
        }
        catch (Exception e) { Debug.LogError($"[IPC] 连接失败: {e.Message}"); }
    }

    public void Disconnect()
    {
        isRunning = false;
        try { stream?.Close(); } catch { }
        try { client?.Close(); } catch { }
        stream = null; client = null;
        lock (queueLock) { msgQueue.Clear(); }
        recvBuffer.Clear();
        Debug.Log("[IPC] 已断开连接");
        OnDisconnected?.Invoke();
    }

    // ----------------- 发送方法 -----------------

    public void SendJson(string json)
    {
        if (!IsConnected) { Debug.LogWarning("[IPC] 未连接，无法发送"); return; }
        try
        {
            byte[] payload = Encoding.UTF8.GetBytes(json);
            byte[] pkt = new byte[4 + payload.Length];
            int len = payload.Length;
            pkt[0] = (byte)(len >> 24); pkt[1] = (byte)(len >> 16);
            pkt[2] = (byte)(len >> 8);  pkt[3] = (byte)len;
            Buffer.BlockCopy(payload, 0, pkt, 4, payload.Length);
            stream.Write(pkt, 0, pkt.Length);
        }
        catch (Exception e) { Debug.LogError($"[IPC] 发送失败: {e.Message}"); }
    }

    /// <summary>发送启动算法测试指令</summary>
    public void SendStartTest(string algorithmArgs)
    {
        SendJson($"{{\"msg\":\"ipc_algorithm_start_test\",\"algorithm_args\":{algorithmArgs}}}");
    }

    /// <summary>发送停止算法测试指令</summary>
    public void SendStopTest()
    {
        SendJson("{\"msg\":\"ipc_algorithm_stop_test\"}");
    }

    /// <summary>回复用户信息（分屏模式收到 ipc_user_info 后必须回复）</summary>
    public void SendUserInfo(int windowHandle = 0)
    {
        SendJson($"{{\"msg\":\"ipc_user_info\",\"window\":{windowHandle}}}");
    }

    /// <summary>打标</summary>
    public void SendEvent(int eventId)
    {
        SendJson($"{{\"msg\":\"ipc_event\",\"event\":{eventId}}}");
    }

    // ----------------- 接收循环 -----------------

    void ReceiveLoop()
    {
        byte[] tmp = new byte[4096];
        while (isRunning)
        {
            try
            {
                if (stream == null || !stream.CanRead) break;
                int read = stream.Read(tmp, 0, tmp.Length);
                if (read <= 0) break;

                lock (recvBuffer)
                {
                    for (int i = 0; i < read; i++) recvBuffer.Add(tmp[i]);

                    // 循环解析缓冲区中的完整数据包（处理粘包）
                    while (recvBuffer.Count >= 4)
                    {
                        int payloadLen = (recvBuffer[0] << 24) | (recvBuffer[1] << 16) | (recvBuffer[2] << 8) | recvBuffer[3];
                        int totalLen = 4 + payloadLen;
                        if (recvBuffer.Count < totalLen) break; // 数据不完整，等待下次接收

                        byte[] payload = new byte[payloadLen];
                        for (int i = 0; i < payloadLen; i++) payload[i] = recvBuffer[4 + i];
                        recvBuffer.RemoveRange(0, totalLen);

                        lock (queueLock)
                            msgQueue.Enqueue(Encoding.UTF8.GetString(payload));
                    }
                }
            }
            catch (Exception e) { Debug.LogError($"[IPC] 接收异常: {e.Message}"); break; }
        }
    }

    // ----------------- 主线程消息处理 -----------------

    void ProcessQueue()
    {
        if (isRunning && (client == null || !client.Connected)) { Disconnect(); return; }

        while (true)
        {
            string json;
            lock (queueLock) { if (msgQueue.Count == 0) break; json = msgQueue.Dequeue(); }
            Debug.Log($"[IPC] 收到: {json}");
            OnRawMessageReceived?.Invoke(json);
            Dispatch(json);
        }
    }

    void Dispatch(string json)
    {
        string msg = ExtractField(json, "msg");
        if (msg == null) return;

        switch (msg)
        {
            case "ipc_user_info":
                int layout = ParseInt(ExtractField(json, "layout_type"));
                if (layout == 1) SendUserInfo(0); // 分屏模式必须回复窗口句柄
                break;

            case "ipc_algorithm_test":
                string algoName = ExtractField(json, "algorithm_name") ?? "";
                LastAlgorithmName = algoName;

                if (string.Equals(algoName, "attention", StringComparison.OrdinalIgnoreCase))
                {
                    string dataStr = ExtractNestedField(json, "result_args", "data");
                    if (int.TryParse(dataStr, out int attVal))
                        AttentionValue = Mathf.Clamp(attVal, 0, 100);
                }
                else if (string.Equals(algoName, "gyroscope", StringComparison.OrdinalIgnoreCase))
                {
                    string dataObj = ExtractNestedField(json, "result_args", "data");
                    if (!string.IsNullOrEmpty(dataObj))
                    {
                        string gx = ExtractField(dataObj, "gyroscope_x");
                        string gy = ExtractField(dataObj, "gyroscope_y");
                        string gz = ExtractField(dataObj, "gyroscope_z");
                        if (float.TryParse(gx, out float fx) &&
                            float.TryParse(gy, out float fy) &&
                            float.TryParse(gz, out float fz))
                        {
                            GyroscopeValue = new Vector3(fx, fy, fz);
                        }
                    }
                }

                var result = new AlgorithmTestData
                {
                    algorithm_name = algoName,
                    rawData = ExtractNestedField(json, "result_args", "data")
                };
                if (result.rawData != null)
                {
                    if (int.TryParse(result.rawData, out int iv)) { result.isInt = true; result.dataInt = iv; }
                    else result.dataString = result.rawData;
                }
                OnAlgorithmTest?.Invoke(result);
                break;

            case "ipc_algorithm_start_test":
                bool startOk = ExtractField(json, "result")?.ToLower() == "true";
                string startFm = ExtractField(json, "fail_message") ?? "";
                Debug.Log($"[IPC] 启动测试响应: result={startOk}, msg={startFm}");
                break;

            case "ipc_algorithm_stop_test":
                bool stopOk = ExtractField(json, "result")?.ToLower() == "true";
                string stopFm = ExtractField(json, "fail_message") ?? "";
                Debug.Log($"[IPC] 停止测试响应: result={stopOk}, msg={stopFm}");
                break;

            case "ipc_set_visible":
                bool vis = ExtractField(json, "visible")?.ToLower() == "true";
                Debug.Log($"[IPC] 设置窗口可见性: {vis}");
                break;

            case "ipc_exit":
                Debug.Log("[IPC] 收到退出指令");
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
#else
                Application.Quit();
#endif
                break;
        }
    }

    // ----------------- JSON 辅助解析（零外部依赖）-----------------

    static string ExtractField(string json, string key)
    {
        string p = $"\"{key}\"";
        int i = json.IndexOf(p, StringComparison.OrdinalIgnoreCase);
        if (i < 0) return null;
        i = json.IndexOf(':', i + p.Length);
        if (i < 0) return null;
        i++; while (i < json.Length && char.IsWhiteSpace(json[i])) i++;
        if (i >= json.Length) return null;

        if (json[i] == '"')
        {
            i++; var sb = new StringBuilder();
            for (int j = i; j < json.Length; j++)
                if (json[j] == '\\' && j + 1 < json.Length) { sb.Append(json[j + 1]); j++; }
                else if (json[j] == '"') return sb.ToString();
                else sb.Append(json[j]);
            return null;
        }

        // 处理对象 { ... } 和数组 [ ... ]
        if (json[i] == '{' || json[i] == '[')
        {
            char open = json[i];
            char close = open == '{' ? '}' : ']';
            int depth = 0;
            int objStart = i;
            for (int j = i; j < json.Length; j++)
            {
                if (json[j] == open) depth++;
                else if (json[j] == close) { depth--; if (depth == 0) return json.Substring(objStart, j - objStart + 1).Trim(); }
            }
            return null;
        }

        int s = i;
        while (i < json.Length && json[i] != ',' && json[i] != '}' && json[i] != ']') i++;
        return json.Substring(s, i - s).Trim();
    }

    static string ExtractNestedField(string json, string parent, string child)
    {
        string p = $"\"{parent}\"";
        int i = json.IndexOf(p, StringComparison.OrdinalIgnoreCase);
        if (i < 0) return null;
        i = json.IndexOf(':', i + p.Length);
        if (i < 0) return null;
        i++; while (i < json.Length && char.IsWhiteSpace(json[i])) i++;
        if (i >= json.Length || json[i] != '{') return null;
        int bc = 0, start = -1, end = -1;
        for (int j = i; j < json.Length; j++)
        {
            if (json[j] == '{') { if (bc == 0) start = j; bc++; }
            else if (json[j] == '}') { bc--; if (bc == 0) { end = j; break; } }
        }
        if (start < 0 || end < 0) return null;
        return ExtractField(json.Substring(start, end - start + 1), child);
    }

    static int ParseInt(string s) => int.TryParse(s, out int v) ? v : 0;
}

// ----------------- 数据结构 -----------------

[System.Serializable]
public class AlgorithmTestData
{
    public string algorithm_name;
    public string rawData;
    public bool isInt;
    public int dataInt;
    public string dataString;
}
