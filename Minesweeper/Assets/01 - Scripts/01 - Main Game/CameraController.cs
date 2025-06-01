using UnityEngine;

/// <summary>
/// CameraController allows player to pan and zoom the camera in a 2D game.
/// Attach this script to the Main Camera in your 'main game' scene.
/// </summary>
public class CameraController : MonoBehaviour
{
    [Header("Panning")]
    public float panSpeed;

    [Header("Zooming")]
    private float zoomSpeed;
    private float minZoom;
    private float maxZoom;

    private Camera cam;

    private void Awake()
    {
        cam = GetComponent<Camera>();
        maxZoom = SettingsManager.Current.maxZoom;
        minZoom = SettingsManager.Current.minZoom;
        zoomSpeed = SettingsManager.Current.zoomSpeed;
        panSpeed = SettingsManager.Current.panSpeed;

    }

    private void Update()
    {
        HandlePan();
        HandleZoom();
    }

    private void HandlePan()
    {
        if (GameManager.MouseUsability.isMouseEnabled == false)
        {
            return;
        }

        else if (GameManager.MouseUsability.isMouseEnabled == true)
        {
            Vector3 pos = transform.position;

            // Mouse drag (middle mouse button)
            if (Input.GetMouseButton(2))
            {
                float h = -Input.GetAxis("Mouse X") * panSpeed * Time.deltaTime;
                float v = -Input.GetAxis("Mouse Y") * panSpeed * Time.deltaTime;

                pos.x += h;
                pos.y += v;
            }

            transform.position = pos;
        }
    }

    private void HandleZoom()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) > 0.01f)
        {
            cam.orthographicSize -= scroll * zoomSpeed;
            cam.orthographicSize = Mathf.Clamp(cam.orthographicSize, minZoom, maxZoom);
        }
    }
}
