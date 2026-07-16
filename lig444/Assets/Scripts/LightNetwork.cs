using UnityEngine;
using TMPro;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Collections.Generic;

public class LightNetwork : MonoBehaviour
{
    public static LightNetwork Instance;

    [Header("Rede")]
    public bool isServer;
    public string serverIP = "127.0.0.1";
    public int port = 7777;

    [Header("Interface")]
    public TMP_Text logText;

    private TcpListener listener;
    private TcpClient client;
    private NetworkStream stream;

    private Thread networkThread;
    private Thread receiveThread;

    private readonly Queue<string> messages = new();
    private readonly Queue<Action> actions = new();

    public bool IsConnected
    {
        get
        {
            return client != null && client.Connected;
        }
    }

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        GameManager.Instance.OnlineMode = true;

        if (isServer)
        {
            GameManager.Instance.LocalPlayer = Player.Red;

            networkThread = new Thread(StartServer);
            networkThread.IsBackground = true;
            networkThread.Start();
        }
        else
        {
            GameManager.Instance.LocalPlayer = Player.Yellow;

            networkThread = new Thread(StartClient);
            networkThread.IsBackground = true;
            networkThread.Start();
        }
    }

    private void StartServer()
    {
        try
        {
            AddMessage("Servidor iniciado");
            Debug.Log("Servidor iniciado");

            listener = new TcpListener(IPAddress.Any, port);
            listener.Start();

            AddMessage("Esperando conexão...");
            Debug.Log("Esperando conexão...");

            client = listener.AcceptTcpClient();

            stream = client.GetStream();

            AddMessage("Cliente conectado!");
            Debug.Log("Cliente conectado!");

            StartReceiveThread();
        }
        catch (Exception e)
        {
            AddMessage("Erro: " + e.Message);
            Debug.LogError(e);
        }
    }

    private void StartClient()
    {
        try
        {
            AddMessage("Conectando...");
            Debug.Log("Tentando conectar...");

            client = new TcpClient();
            client.Connect(serverIP, port);

            stream = client.GetStream();

            AddMessage("Conectado!");
            Debug.Log("Conectado ao servidor!");

            StartReceiveThread();
        }
        catch (Exception e)
        {
            AddMessage("Falha ao conectar: " + e.Message);
            Debug.LogError(e);
        }
    }

    private void StartReceiveThread()
    {
        receiveThread = new Thread(ReceiveLoop);
        receiveThread.IsBackground = true;
        receiveThread.Start();
    }

    private void ReceiveLoop()
    {
        byte[] buffer = new byte[1024];

        while (true)
        {
            try
            {
                int size = stream.Read(buffer, 0, buffer.Length);

                if (size <= 0)
                    continue;

                string msg = Encoding.UTF8.GetString(buffer, 0, size);

                AddMessage("Recebido: " + msg);
                Debug.Log("Recebido: " + msg);

                // PROCESSA A MENSAGEM
                HandleMessage(msg);
            }
            catch
            {
                break;
            }
        }
    }

    private void HandleMessage(string msg)
    {
        if (msg.StartsWith("MOVE:"))
        {
            int column = int.Parse(msg.Substring(5));

            MainThreadAction(() =>
            {
                GameManager.Instance.TryMakeMove(column, true);
            });
        }
    }

    public void SendMove(int column)
    {
        Send("MOVE:" + column);
    }

    private void Send(string msg)
    {
        if (!IsConnected)
            return;

        byte[] data = Encoding.UTF8.GetBytes(msg);

        stream.Write(data, 0, data.Length);

        AddMessage("Enviado: " + msg);
        Debug.Log("Enviado: " + msg);
    }

    private void MainThreadAction(Action action)
    {
        lock (actions)
        {
            actions.Enqueue(action);
        }
    }

    private void AddMessage(string msg)
    {
        lock (messages)
        {
            messages.Enqueue(msg);
        }
    }

    private void Update()
    {
        lock (messages)
        {
            while (messages.Count > 0)
            {
                string msg = messages.Dequeue();

                if (logText != null)
                    logText.text += "\n" + msg;
            }
        }

        lock (actions)
        {
            while (actions.Count > 0)
            {
                actions.Dequeue().Invoke();
            }
        }
    }

    private void OnDestroy()
    {
        try
        {
            receiveThread?.Abort();
            networkThread?.Abort();
        }
        catch { }

        stream?.Close();
        client?.Close();
        listener?.Stop();
    }
}