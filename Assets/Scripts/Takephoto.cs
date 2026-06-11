using System.Collections;
using System.IO;
using UnityEngine;

public class Takephoto : MonoBehaviour
{
    // Reference to the camera that takes the screenshot
    public Camera screenshotCamera;

    // File path to save the screenshot
    public string screenshotPath = "Assets/Screenshots/card_screenshot.png";

    // Method called when the button is clicked
    public void OnCaptureButtonClicked()
    {
        StartCoroutine(CaptureAndSaveScreenshot());
    }

    private IEnumerator CaptureAndSaveScreenshot()
    {
        // Wait for the end of the frame to ensure everything is rendered
        yield return new WaitForEndOfFrame();

        // Capture the screenshot
        RenderTexture renderTexture = new RenderTexture(Screen.width, Screen.height, 24);
        screenshotCamera.targetTexture = renderTexture;
        screenshotCamera.Render();

        // Read the pixels from the render texture
        Texture2D screenshotTexture = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);
        RenderTexture.active = renderTexture;
        screenshotTexture.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0);
        screenshotTexture.Apply();

        // Save the screenshot as an image file
        byte[] bytes = screenshotTexture.EncodeToPNG();
        File.WriteAllBytes(screenshotPath, bytes);

        // Clean up
        screenshotCamera.targetTexture = null;
        RenderTexture.active = null;
        Destroy(renderTexture);
        Destroy(screenshotTexture);

        Debug.Log($"Screenshot saved at {screenshotPath}");
    }
}
