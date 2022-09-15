using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LoginCam : MonoBehaviour
{
    [SerializeField]
    private GameObject SpaceShip;
    private float rotateSpeed = 20.0f;
    private float xRadius = 1500.0f;
    private float angle = 15.0f;

    // Start is called before the first frame update
    void Start()
    {
        transform.position = new Vector3(SpaceShip.transform.position.x, (xRadius / Mathf.Cos(Mathf.Deg2Rad * angle)) * Mathf.Sin(Mathf.Deg2Rad * angle), SpaceShip.transform.position.z + xRadius);
        transform.LookAt(SpaceShip.transform);
    }

    // Update is called once per frame
    void Update()
    {
        transform.RotateAround(SpaceShip.transform.position, Vector3.up, -rotateSpeed * Time.deltaTime);
    }
}
