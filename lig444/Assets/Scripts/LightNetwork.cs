using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

public class LightNetwork : MonoBehaviour
{
    public static LightNetwork Instance { get; private set; }

    [Header("Network Settings")]
    [SerializeField] private string serverIP = "127.0.0.1";
    [SerializeField] private int port = 5555;
    [SerializeField] private bool isHost = true;

    private TcpListener tcpListener;
    private TcpClient socketConnection;
    private NetworkStream stream;
    private Thread clientReceiveThread;

    // Fila para transferir execuções da Thread do Socket para a Main Thread da Unity
    private readonly ConcurrentQueue<Action> mainThreadActions = new ConcurrentQueue<Action>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        // Ativa o modo online no GameManager
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnlineMode = true;
        }

        if (isHost)
        {
            // Host joga de Vermelho e inicia o servidor
            if (GameManager.Instance != null) GameManager.Instance.LocalPlayer = Player.Red;
            StartServer();
        }
        else
        {
            // Cliente joga de Amarelo e se conecta ao host
            if (GameManager.Instance != null) GameManager.Instance.LocalPlayer = Player.Yellow;
            ConnectToServer();
        }
    }

    private void Update()
    {
        // Executa todas as mensagens/jogadas pendentes na Thread principal da Unity
        while (mainThreadActions.TryDequeue(out Action action))
        {
            action?.Invoke();
        }
    }

    #region --- SERVIDOR & CONEXÃO ---

    private void StartServer()
    {
        try
        {
            clientReceiveThread = new Thread(() =>
            {
                tcpListener = new TcpListener(IPAddress.Any, port);
                tcpListener.Start();
                Debug.Log($"Servidor aguardando conexões na porta {port}...");

                socketConnection = tcpListener.AcceptTcpClient();
                Debug.Log("Cliente conectado!");

                stream = socketConnection.GetStream();
                ListenForData();
            });

            clientReceiveThread.IsBackground = true;
            clientReceiveThread.Start();
        }
        catch (Exception e)
        {
            Debug.LogError($"Erro ao iniciar o servidor TCP: {e.Message}");
        }
    }

    private void ConnectToServer()
    {
        try
        {
            clientReceiveThread = new Thread(() =>
            {
                socketConnection = new TcpClient(serverIP, port);
                Debug.Log("Conectado com sucesso ao Servidor!");

                stream = socketConnection.GetStream();
                ListenForData();
            });

            clientReceiveThread.IsBackground = true;
            clientReceiveThread.Start();
        }
        catch (Exception e)
        {
            Debug.LogError($"Erro ao conectar ao servidor TCP: {e.Message}");
        }
    }

    #endregion

    #region --- RECEBIMENTO & ENVIO ---

    private void ListenForData()
    {
        byte[] bytes = new byte[1024];
        while (socketConnection != null && socketConnection.Connected)
        {
            try
            {
                int length;
                if (stream != null && (length = stream.Read(bytes, 0, bytes.Length)) != 0)
                {
                    string incomingMessage = Encoding.UTF8.GetString(bytes, 0, length).Trim();
                    ProcessMessage(incomingMessage);
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Conexão encerrada ou erro de leitura: {ex.Message}");
                break;
            }
        }
    }

    private void ProcessMessage(string message)
    {
        // Formato esperado de jogada: "MOVE:coluna" (Ex: "MOVE:3")
        if (message.StartsWith("MOVE:"))
        {
            string[] parts = message.Split(':');
            if (parts.Length > 1 && int.TryParse(parts[1], out int columnIndex))
            {
                // Enfileira a jogada para rodar dentro da Unity Main Thread
                mainThreadActions.Enqueue(() =>
                {
                    GameManager.Instance.TryMakeMove(columnIndex, receivedFromNetwork: true);
                });
            }
        }
    }

    public void SendMove(int columnIndex)
    {
        if (socketConnection == null || !socketConnection.Connected)
        {
            Debug.LogWarning("Não há conexão ativa para enviar o movimento.");
            return;
        }

        try
        {
            if (stream.CanWrite)
            {
                string clientMessage = $"MOVE:{columnIndex}\n";
                byte[] clientMessageAsByteArray = Encoding.UTF8.GetBytes(clientMessage);
                stream.Write(clientMessageAsByteArray, 0, clientMessageAsByteArray.Length);
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Erro ao enviar dados via TCP: {e.Message}");
        }
    }

    #endregion

    private void OnApplicationQuit()
    {
        CloseConnection();
    }

    private void OnDestroy()
    {
        CloseConnection();
    }

    private void CloseConnection()
    {
        try
        {
            if (clientReceiveThread != null && clientReceiveThread.IsAlive)
                clientReceiveThread.Abort();

            stream?.Close();
            socketConnection?.Close();
            tcpListener?.Stop();
        }
        catch (Exception e)
        {
            Debug.LogWarning($"Erro ao fechar conexão: {e.Message}");
        }
    }
}