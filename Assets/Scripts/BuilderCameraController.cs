// BuilderCameraController.cs
using UnityEngine;

public class BuilderCameraController : MonoBehaviour
{
    public float panSpeed = 20f;
    public float zoomSpeed = 20f;
    public float minZoom = 5f;
    public float maxZoom = 50f;

    // Optional: Define pan limits
    // public Vector2 minPanLimit;
    // public Vector2 maxPanLimit;

    private Camera _camera;
    private Vector3 _lastPanPosition;
    private bool _isMovementActive = false;
    private Vector3 _initialPosition;
    private float _initialZoom;


    void Awake()
    {
        _camera = GetComponent<Camera>();
        _initialPosition = transform.position;
        _initialZoom = _camera.orthographic ? _camera.orthographicSize : _camera.fieldOfView;
    }

    public void SetMovementActive(bool isActive)
    {
        _isMovementActive = isActive;
        if (!isActive)
        {
            // Optionally reset position/zoom if movement is deactivated
            // ResetView();
        }
    }

    public void ResetView()
    {
        transform.position = _initialPosition;
        if (_camera.orthographic) _camera.orthographicSize = _initialZoom;
        else _camera.fieldOfView = _initialZoom;
    }


    void Update()
    {
        if (!_isMovementActive || !_camera.gameObject.activeInHierarchy) return;

        if (Input.GetMouseButtonDown(2)) 
        {
            _lastPanPosition = Input.mousePosition;
        }
        if (Input.GetMouseButton(2)) 
        {
            Vector3 delta = Input.mousePosition - _lastPanPosition;
            float currentPanSpeed = panSpeed * (_camera.orthographic ? _camera.orthographicSize / _initialZoom : 1f);
            transform.Translate(-delta.x * currentPanSpeed * Time.unscaledDeltaTime, -delta.y * currentPanSpeed * Time.unscaledDeltaTime, 0);
            _lastPanPosition = Input.mousePosition;
        }

        // Zooming (Mouse Scroll Wheel)
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll != 0f)
        {
            if (_camera.orthographic)
            {
                _camera.orthographicSize -= scroll * zoomSpeed * (_camera.orthographicSize /10f) * Time.unscaledDeltaTime ; // Make zoom feel more natural
                _camera.orthographicSize = Mathf.Clamp(_camera.orthographicSize, minZoom, maxZoom);
            }
            else // Perspective camera (less common for this type of builder)
            {
                _camera.fieldOfView -= scroll * zoomSpeed;
                _camera.fieldOfView = Mathf.Clamp(_camera.fieldOfView, minZoom, maxZoom);
            }
            // ApplyPanLimits(); // Re-check limits after zoom
        }
    }

    // void ApplyPanLimits()
    // {
    //     if (!_camera.orthographic) return; // Pan limits are easier with ortho
    //     Vector3 pos = transform.position;
    //     pos.x = Mathf.Clamp(pos.x, minPanLimit.x + (_camera.aspect * _camera.orthographicSize), maxPanLimit.x - (_camera.aspect * _camera.orthographicSize));
    //     pos.y = Mathf.Clamp(pos.y, minPanLimit.y + _camera.orthographicSize, maxPanLimit.y - _camera.orthographicSize);
    //     transform.position = pos;
    // }
}