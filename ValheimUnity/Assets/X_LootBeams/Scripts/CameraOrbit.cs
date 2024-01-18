using UnityEngine;

public class CameraOrbit : MonoBehaviour
{
    [SerializeField] private float xSpeed = 120.0f;

    private float yAngle;

    private Vector3 defaultAngles;
    
    private void Start()
    {
        defaultAngles = transform.eulerAngles;
        yAngle = defaultAngles.y;
    }

    private void LateUpdate()
    {
        if (Input.GetMouseButton(0))
        {
            yAngle += Input.GetAxis("Mouse X") * xSpeed;
            Quaternion rotation = Quaternion.Euler(defaultAngles.x, yAngle, 0);
            transform.rotation = rotation;
        }
    }
}