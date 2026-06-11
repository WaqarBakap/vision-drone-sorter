using UnityEngine;

public class pickdrop : MonoBehaviour
{
    public GameObject gripper; // The gripper object attached to the drone
    private GameObject currentBox; // Reference to the currently held box

    // Call this method to pick up the box
    public void PickUpBox(GameObject box)
    {
        if (currentBox == null)
        {
            currentBox = box;
            box.transform.SetParent(gripper.transform);
            box.transform.localPosition = Vector3.zero;
            box.GetComponent<Rigidbody>().isKinematic = true;
        }
        else
        {
            Debug.Log("Already holding a box.");
        }
    }

    // Call this method to drop the box
    public void DropBox()
    {
        if (currentBox != null)
        {
            currentBox.transform.SetParent(null);
            currentBox.GetComponent<Rigidbody>().isKinematic = false;
            currentBox = null;
        }
    }
}
