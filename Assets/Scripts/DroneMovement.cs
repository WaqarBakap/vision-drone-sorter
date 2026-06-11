using UnityEngine;
using System.Collections;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Collections.Generic;

public class DroneMovement : MonoBehaviour
{
    //public Transform greenCardPosition; // Assign this in the editor
    public float speed = 5f;
    public float liftHeight = 10f;
    public Transform greenBoxPosition;
    public Transform redBoxPosition;
    public Transform blueBoxPosition;
    

    private TcpListener listener;
    private Thread listenerThread;
    private readonly Queue<System.Action> executeOnMainThread = new Queue<System.Action>();
    private GameObject heldObject = null; // The object currently being held by the drone
    private Rigidbody droneRigidbody;
     public bool isBusy = false; // Add this line
      public Transform greenDropPosition;
    public Transform redDropPosition;
    public Transform blueDropPosition;

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
        while (executeOnMainThread.Count > 0)
        {
            executeOnMainThread.Dequeue().Invoke();
        }
    }

    void ListenForCommands()
    {
        while (true)
        {
            using (TcpClient client = listener.AcceptTcpClient())
            {
                using (NetworkStream stream = client.GetStream())
                {
                    byte[] buffer = new byte[1024];
                    int bytesRead = stream.Read(buffer, 0, buffer.Length);
                    string command = Encoding.ASCII.GetString(buffer, 0, bytesRead);
                    if (!isBusy){
                    if (command.ToLower() == "green")
                    {
                        QueueMainThreadAction(() =>
                        {
                            StartCoroutine(MoveDrone(greenBoxPosition.position,greenDropPosition.position));
                        });
                    }
                    else if (command.ToLower() == "red")
                    {
                        QueueMainThreadAction(() =>
                        {
                            StartCoroutine(MoveDrone(redBoxPosition.position,redDropPosition.position));
                        });
                    }
                    else if (command.ToLower() == "blue")
                    {
                        QueueMainThreadAction(() =>
                        {
                            StartCoroutine(MoveDrone(blueBoxPosition.position,blueDropPosition.position));
                        });
                    }
                    }
                }
                
            }
        }
    }

    IEnumerator MoveDrone(Vector3 targetPosition,Vector3 dropPosition)
    {
        isBusy=true;
        
        Vector3 startPosition = transform.position;
    Vector3 liftPosition = new Vector3(startPosition.x, startPosition.y + liftHeight, startPosition.z);
    Vector3 horizontalPosition = new Vector3(targetPosition.x, liftPosition.y, targetPosition.z);
    Vector3 descentPosition = new Vector3(targetPosition.x, targetPosition.y, targetPosition.z);
        // Move up
        yield return MoveToPosition(liftPosition);
        // Move horizontally
    yield return MoveToPosition(horizontalPosition);
        // Move horizontally
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
  
        // Move down
        yield return MoveToPosition(targetPosition);
        
        // Move up again
        yield return MoveToPosition(liftPosition);
        // Dropping
        yield return MoveToPosition(dropPosition);
        // Move down to drop the box
        yield return MoveToPosition(new Vector3(dropPosition.x, startPosition.y, dropPosition.z));
        
 if (heldObject != null)
    {
        heldObject.GetComponent<Rigidbody>().isKinematic = false;
        heldObject.transform.parent = null;
        heldObject = null;
    }

    isBusy=false;
        
    }

    IEnumerator MoveToPosition(Vector3 target)
    {
        // Check if the movement is a vertical descent
        bool isDescending = (transform.position.y > target.y);

        while (Vector3.Distance(transform.position, target) > 0.1f)
        {
            // Move towards the target
            transform.position = Vector3.MoveTowards(transform.position, target, speed * Time.deltaTime);

            // If descending, stop the movement when the drone reaches the card's y-coordinate
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
        listener.Stop();
        listenerThread.Abort();
    }

  
}
