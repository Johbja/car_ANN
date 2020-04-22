using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CheckpointSystem : MonoBehaviour {

    public GameObject checkpointParent;

    private Dictionary<int, Transform> checkpoints;

    private void Start() {
        checkpoints = new Dictionary<int, Transform>();

        int counter = 0;
        foreach(Transform t in checkpointParent.transform) {
            checkpoints.Add(counter, t);
            t.gameObject.GetComponent<CheckpointSlave>().checkpoint = counter;
            counter++;
        }
    }

    public float GetDistance(int key1, int key2) {
        return Vector2.Distance(checkpoints[key1].position, checkpoints[key2].position);
    }

    public float GetRelativeDisance(int key1, Vector2 pos) {
        return Vector2.Distance(checkpoints[key1].position, pos);
    }

    public int GetGoalCheckpoint() {
        return checkpoints.Count - 1;
    }

    public void PrintCheckPoints() {
        for(int i = 0; i < checkpoints.Count; i++) {
            Debug.Log("checkpoint at index " + i + " " + checkpoints[i].name);
        }
    }

}
