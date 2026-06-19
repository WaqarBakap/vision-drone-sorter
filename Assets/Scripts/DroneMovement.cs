using UnityEngine;
using System.Collections;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Collections.Generic;

public class DroneMovement : MonoBehaviour
{
    public float speed = 5f;
    public float liftHeight = 10f;
    public Transform greenBoxPosition;
    public Transform redBoxPosition;
    public Transform blueBoxPosition;
    
    public Transform greenDropPosition;
    public Transform redDropPosition;
    public Transform blueDropPosition;

    private TcpListener listener;
    private Thread listenerThread;
    private readonly Queue<System.Action> executeOnMainThread = new Queue<System.Action>();
    private GameObject heldObject = null; 
    private Rigidbody droneRigidbody;
    
    [HideInInspector] 
    public bool isBusy = false; 

    void Start()
    {
        listener = new TcpListener(System.Net.IPAddress.Parse("127.0.0.1"), 65431);
        listener.Start();
        listenerThread = new Thread(ListenForCommands);
        listenerThread.Start();

        droneRigidbody = GetComponent<Rigidbody>();
    }

    void Update()
    {
        // Execute all queued actions on the main thread
        lock (executeOnMainThread)
        {
            while (executeOnMainThread.Count > 0)
            {
                executeOnMainThread.Dequeue().Invoke();
            }
        }
    }

    void ListenForCommands()
    {
        while (true)
        {
            try
            {
                // Accept incoming connection
                TcpClient client = listener.AcceptTcpClient();
                NetworkStream stream = client.GetStream();
                
                byte[] buffer = new byte[1024];
                int bytesRead = stream.Read(buffer, 0, buffer.Length);
                string command = Encoding.ASCII.GetString(buffer, 0, bytesRead).Trim().ToLower();

                // Check if drone is available and command is valid
                if (!isBusy && (command == "red" || command == "green" || command == "blue"))
                {
                    QueueMainThreadAction(() =>
                    {
                        Vector3 target = Vector3.zero;
                        Vector3 drop = Vector3.zero;

                        if (command == "green") { target = greenBoxPosition.position; drop = greenDropPosition.position; }
                        else if (command == "red") { target = redBoxPosition.position; drop = redDropPosition.position; }
                        else if (command == "blue") { target = blueBoxPosition.position; drop = blueDropPosition.position; }

                        // Hand off the client connection to the coroutine
                        StartCoroutine(MoveDrone(target, drop, client));
                    });
                }
                else
                {
                    // If busy or invalid command, reject it immediately so Python doesn't hang
                    byte[] rejectMsg = Encoding.ASCII.GetBytes(isBusy ? "busy" : "invalid");
                    stream.Write(rejectMsg, 0, rejectMsg.Length);
                    client.Close();
                }
            }
            catch (SocketException)
            {
                // Thrown when listener.Stop() is called on quit, safely exit thread loop
                break;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Network Thread Error: {e.Message}");
            }
        }
    }

    // CHANGED: Added TcpClient parameter to handle reporting back when finished
    IEnumerator MoveDrone(Vector3 targetPosition, Vector3 dropPosition, TcpClient client)
    {
        isBusy = true;
        
        Vector3 startPosition = transform.position;
        Vector3 liftPosition = new Vector3(startPosition.x, startPosition.y + liftHeight, startPosition.z);
        Vector3 horizontalPosition = new Vector3(targetPosition.x, liftPosition.y, targetPosition.z);
        Vector3 descentPosition = new Vector3(targetPosition.x, targetPosition.y, targetPosition.z);
        
        // Move up
        yield return MoveToPosition(liftPosition);
        // Move horizontally
        yield return MoveToPosition(horizontalPosition);
        // Move down
        yield return MoveToPosition(descentPosition);

        // Pick up the cube
        if (heldObject == null)
        {
            Collider[] colliders = Physics.OverlapSphere(transform.position, 1f);
            foreach (Collider collider in colliders)
            {
                if (collider.gameObject.CompareTag("Cube"))
                {
                    heldObject = collider.gameObject;
                    heldObject.GetComponent<Rigidbody>().isKinematic = true;
                    heldObject.transform.parent = transform;
                    break;
                }
            }
        }
  
        // Re-align / Ensure target positioning
        yield return MoveToPosition(targetPosition);
        
        // Move up again with the box
        yield return MoveToPosition(liftPosition);
        // Move horizontally to dropping zone
        yield return MoveToPosition(dropPosition);
        // Move down to drop the box
        yield return MoveToPosition(new Vector3(dropPosition.x, startPosition.y, dropPosition.z));
        
        // Drop logic
        if (heldObject != null)
        {
            heldObject.GetComponent<Rigidbody>().isKinematic = false;
            heldObject.transform.parent = null;
            heldObject = null;
        }

        // --- NEW: NOTIFY PYTHON WE ARE DONE ---
        if (client != null && client.Connected)
        {
            try
            {
                NetworkStream stream = client.GetStream();
                byte[] response = Encoding.ASCII.GetBytes("idle");
                stream.Write(response, 0, response.Length);
                Debug.Log("Sent 'idle' status back to Python client.");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to send completion status: {e.Message}");
            }
            finally
            {
                client.Close(); // Clean up socket connection
            }
        }

        isBusy = false;
    }

    IEnumerator MoveToPosition(Vector3 target)
    {
        bool isDescending = (transform.position.y > target.y);

        while (Vector3.Distance(transform.position, target) > 0.1f)
        {
            transform.position = Vector3.MoveTowards(transform.position, target, speed * Time.deltaTime);

            if (isDescending && transform.position.y <= target.y)
            {
                transform.position = new Vector3(transform.position.x, target.y, transform.position.z);
                break;
            }

            yield return null;
        }
    }

    private void QueueMainThreadAction(System.Action action)
    {
        lock (executeOnMainThread)
        {
            executeOnMainThread.Enqueue(action);
        }
    }

    void OnApplicationQuit()
    {
        // Safe thread cleanup 
        if (listener != null)
        {
            listener.Stop();
        }
        
        if (listenerThread != null && listenerThread.IsAlive)
        {
            listenerThread.Join(500); // Wait smoothly for the thread to exit
        }
    }
}
