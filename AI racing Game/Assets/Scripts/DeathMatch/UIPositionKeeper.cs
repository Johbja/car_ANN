using UnityEngine;
using UnityEngine.UI;

public class UIPositionKeeper : MonoBehaviour {

    private Vector2 pos;

    private void Start() {
        pos = transform.position - transform.parent.position;
    }

    private void Update() {
        transform.position = (Vector2)transform.parent.position + pos;
        //Vector3 rot = transform.rotation.eulerAngles;
        //rot = Vector3.zero;
        transform.rotation = Quaternion.Euler(Vector3.zero);
    }
}
