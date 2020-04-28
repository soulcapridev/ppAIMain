using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraControl : MonoBehaviour
{

    //Script based on https://pastebin.com/2RX8fpJ3, modified to achieve desired camera behaviour
    public Vector3 target = new Vector3(0f, 0f, 0f);
    public bool RotateAroundPivot = true;
    public float RotationSpeed = 5.0f;

    private Transform _Camera;
    private Transform _Parent;

    private Vector3 _LocalRotation;
    private float _CameraDistance = 25f;

    public float MouseSensitivity = 4f;
    public float ScrollSensitvity = 2f;
    public float OrbitDampening = 10f;
    public float ScrollDampening = 6f;
    float CamMinAngle = 2f;
    float CamMaxAngle = 90f;


    public bool RotationDisabled = true;
    public bool PYDisabled = true;

    // Start is called before the first frame update
    void Start()
    {
        _Camera = transform;
        _Parent = transform.parent;
        _LocalRotation.y = 30f;
    }

    // LateUpdate is called once per frame after Update
    void LateUpdate()
    {
        //Orbit with MouseWheel Button
        if (Input.GetMouseButton(2))
        {
            RotationDisabled = false;
            PYDisabled = false;
        }
        else
        {
            RotationDisabled = true;
            PYDisabled = true;
        }

        if (!RotationDisabled)
        {
            //Rotation of the Camera based on Mouse Coordinates
            if (Input.GetAxis("Mouse X") != 0)
            {
                _LocalRotation.x += Input.GetAxis("Mouse X") * MouseSensitivity;
            }
        }
        if (!PYDisabled)
        {
            //Pitch and Yaw of the Camera based on Mouse Coordinates
            if (Input.GetAxis("Mouse Y") != 0)
            {
                _LocalRotation.y -= Input.GetAxis("Mouse Y") * MouseSensitivity;
                _LocalRotation.y = Mathf.Clamp(_LocalRotation.y, CamMinAngle, CamMaxAngle);
            }

        }
        //Zooming Input from our Mouse Scroll Wheel
        if (Input.GetAxis("Mouse ScrollWheel") != 0f)
        {
            float ScrollAmount = Input.GetAxis("Mouse ScrollWheel") * ScrollSensitvity;
            ScrollAmount *= (_CameraDistance * 0.3f);
            _CameraDistance += ScrollAmount * -1f;
            _CameraDistance = Mathf.Clamp(_CameraDistance, 1.5f, 100f);
        }

        //Actual Camera Rig Transformations

        if (true)
        {
            Quaternion QT = Quaternion.Euler(_LocalRotation.y, _LocalRotation.x, 0);
            _Parent.rotation = Quaternion.Lerp(_Parent.rotation, QT, Time.deltaTime * OrbitDampening);
        }

        if (_Camera.localPosition.z != _CameraDistance * -1f)
        {
            var updatePosition = new Vector3(0f, 0f, Mathf.Lerp(_Camera.localPosition.z, _CameraDistance * -1f, Time.deltaTime * ScrollDampening));
            _Camera.localPosition = updatePosition;
        }

    }
}
