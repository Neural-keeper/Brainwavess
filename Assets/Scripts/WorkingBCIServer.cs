using System;
using UnityEngine;
using System.Net;
using System.Text;
using System.Threading;
using System.IO;
using System.Collections;

/// <summary>
/// Working BCI HTTP Server - No threading issues
/// </summary>
public class WorkingBCIServer : MonoBehaviour
{
    [Header("Server Settings")]
    public int port = 8080;
    public bool startOnPlay = true;
    
    private HttpListener httpListener;
    private Thread httpListenerThread;
    private bool isRunning = false;
    
    // Command queue for main thread processing
    private System.Collections.Generic.Queue<CommandData> commandQueue = new System.Collections.Generic.Queue<CommandData>();
    private readonly object queueLock = new object();
    
    // Events for commands
    public static event System.Action<string, float> OnCommandReceived;
    
    void Start()
    {
        if (startOnPlay)
        {
            StartHttpServer();
        }
    }
    
    void Update()
    {
        // Process commands on main thread
        ProcessQueuedCommands();
    }
    
    void StartHttpServer()
    {
        try
        {
            httpListener = new HttpListener();
            httpListener.Prefixes.Add($"http://localhost:{port}/");
            httpListener.Start();
            isRunning = true;
            
            Debug.Log($"✅ Working BCI Server started on http://localhost:{port}/");
            Debug.Log("Ready to receive mental commands!");
            
            httpListenerThread = new Thread(new ThreadStart(ListenForRequests));
            httpListenerThread.Start();
        }
        catch (Exception e)
        {
            Debug.LogError($"❌ Failed to start HTTP server: {e.Message}");
        }
    }
    
    void ListenForRequests()
    {
        while (isRunning && httpListener != null)
        {
            try
            {
                var context = httpListener.GetContext();
                ProcessRequest(context);
            }
            catch (Exception e)
            {
                if (isRunning)
                {
                    Debug.LogError($"HTTP Request Error: {e.Message}");
                }
            }
        }
    }
    
    void ProcessRequest(HttpListenerContext context)
    {
        var request = context.Request;
        var response = context.Response;
        
        // Log the incoming request
        Debug.Log($"🌐 HTTP Request: {request.HttpMethod} {request.Url.AbsolutePath}");
        
        // Add CORS headers
        response.Headers.Add("Access-Control-Allow-Origin", "*");
        response.Headers.Add("Access-Control-Allow-Methods", "GET, POST, OPTIONS");
        response.Headers.Add("Access-Control-Allow-Headers", "Content-Type");
        
        string responseText = "";
        
        if (request.HttpMethod == "OPTIONS")
        {
            response.StatusCode = 200;
        }
        else if (request.Url.AbsolutePath == "/status")
        {
            responseText = "Unity BCI Server Running";
            response.StatusCode = 200;
        }
        else if (request.Url.AbsolutePath == "/ping")
        {
            responseText = "pong";
            response.StatusCode = 200;
        }
        else if (request.Url.AbsolutePath == "/health")
        {
            responseText = "{\"status\":\"ok\",\"server\":\"Unity BCI Server\",\"port\":" + port + "}";
            response.ContentType = "application/json";
            response.StatusCode = 200;
        }
        else if (request.Url.AbsolutePath == "/command" && request.HttpMethod == "POST")
        {
            using (StreamReader reader = new StreamReader(request.InputStream))
            {
                string jsonData = reader.ReadToEnd();
                QueueCommand(jsonData);
                responseText = "Command queued";
                response.StatusCode = 200;
            }
        }
        else
        {
            responseText = "404 - Not Found";
            response.StatusCode = 404;
        }
        
        // Send response
        byte[] buffer = Encoding.UTF8.GetBytes(responseText);
        response.ContentLength64 = buffer.Length;
        response.OutputStream.Write(buffer, 0, buffer.Length);
        response.OutputStream.Close();
    }
    
    void QueueCommand(string jsonData)
    {
        try
        {
            var data = JsonUtility.FromJson<CommandData>(jsonData);
            
            if (data != null && !string.IsNullOrEmpty(data.command))
            {
                lock (queueLock)
                {
                    commandQueue.Enqueue(data);
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"❌ Error queuing command: {e.Message}");
        }
    }
    
    void ProcessQueuedCommands()
    {
        lock (queueLock)
        {
            while (commandQueue.Count > 0)
            {
                var command = commandQueue.Dequeue();
                
                Debug.Log($"Processing command: {command.command} (strength: {command.strength})");
                
                // Trigger event - this happens on main thread
                OnCommandReceived?.Invoke(command.command, command.strength);
                
                Debug.Log($"✅ Command sent to player: {command.command} ({command.strength})");
            }
        }
    }
    
    void OnDestroy()
    {
        StopServer();
    }
    
    void OnApplicationQuit()
    {
        StopServer();
    }
    
    void StopServer()
    {
        isRunning = false;

        if (httpListener != null)
        {
            try
            {
                if (httpListener.IsListening)
                {
                    httpListener.Stop();
                }
                httpListener.Close();
                Debug.Log("🔴 Working BCI Server stopped");
            }
            catch (ObjectDisposedException)
            {
                Debug.LogWarning("HttpListener already disposed.");
            }
            catch (Exception e)
            {
                Debug.LogError($"Error stopping server: {e.Message}");
            }
            finally
            {
                httpListener = null;
            }
        }

        if (httpListenerThread != null && httpListenerThread.IsAlive)
        {
            httpListenerThread.Abort();
        }
    }
    
    [System.Serializable]
    public class CommandData
    {
        public string command;
        public float strength;
        public long timestamp;
    }
}