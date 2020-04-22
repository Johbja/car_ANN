using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class CameraFollower : MonoBehaviour {

    [Header("Player follow settings")]
    [SerializeField] private bool IsEnabled;
    [SerializeField] private Transform followObject;
    [SerializeField] private float maxZoom;
    [SerializeField] private float minZoom;
    [SerializeField] private float startZoom;
    [SerializeField] private float zoomSpeed;


    [Header("Mouse follow settings")]
    [SerializeField] private bool moveWithMouse;
    [SerializeField] private float mouseFollowSpeed;
    [SerializeField] private float mouseFollowTrigger;


    private Rigidbody2D carRb2d;
    private Camera cam;

    private int carIndex;

    private void Start() {

        if(IsEnabled) {
            cam = GetComponent<Camera>();
            carRb2d = followObject.GetComponent<Rigidbody2D>();

            carIndex = 0;

            cam.orthographicSize = minZoom;
        }

    }

    void Update() {

        if(Input.GetKeyDown(KeyCode.F)) {
            moveWithMouse = !moveWithMouse;
        }

        if(IsEnabled && !moveWithMouse) {
            cam.orthographicSize = Mathf.MoveTowards(cam.orthographicSize, Mathf.Clamp(carRb2d.velocity.magnitude * startZoom, minZoom, maxZoom), zoomSpeed * Time.fixedDeltaTime);

            transform.position = new Vector3(followObject.position.x, followObject.position.y, -1);
        }

        if(moveWithMouse) {
            if(Vector3.Distance(transform.position, Camera.main.ScreenToWorldPoint(Input.mousePosition)) > mouseFollowTrigger)
                transform.position = Vector3.MoveTowards(transform.position, Camera.main.ScreenToWorldPoint(Input.mousePosition), mouseFollowSpeed);
        }
    }
}
