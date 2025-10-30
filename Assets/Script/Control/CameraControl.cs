using UnityEngine;
using UnityEngine.UI;

public class CameraControl : MonoBehaviour
{
    public float moveSpeed = 10f;
    public float zoomSpeed = 20f;
    public float minZoom = 1f;
    public float maxZoom = 10f;

    private Camera cam;
    private Vector3 defaultPosition;
    private float defaultFOV;
    private float defaultOrthoSize;

    public static CameraControl Instance;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }



    void Start()
    {
        cam = GetComponent<Camera>();
        defaultPosition = transform.position;
        defaultFOV = cam.fieldOfView;
        defaultOrthoSize = cam.orthographicSize;
    }

    void Update()
    {
        float moveX = Input.GetAxis("Horizontal");
        float moveZ = Input.GetAxis("Vertical");
        Vector3 move = new Vector3(moveX, 0, moveZ) * moveSpeed * Time.deltaTime;
        transform.Translate(move, Space.World);

        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (cam.orthographic == false)
        {
            cam.fieldOfView -= scroll * zoomSpeed;
            cam.fieldOfView = Mathf.Clamp(cam.fieldOfView, minZoom, maxZoom);
        }
        else
        {
            cam.orthographicSize -= scroll * zoomSpeed * 0.1f;
            cam.orthographicSize = Mathf.Clamp(cam.orthographicSize, minZoom, maxZoom);
        }
    }

    public void ResetCamera()
    {
        transform.position = defaultPosition;
        if (cam.orthographic == false)
            cam.fieldOfView = defaultFOV;
        else
            cam.orthographicSize = defaultOrthoSize;
    }
}
