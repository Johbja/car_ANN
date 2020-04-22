using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class TrailController : MonoBehaviour {

    [SerializeField] private float angularVTrigger;
    [SerializeField] private float vMagnitudeTrigger;
    [SerializeField] private float vectorDiffTrigger;

    private TrailRenderer[] trails;
    private Rigidbody2D rb2d;

    private void Start() {
        trails = GetComponentsInChildren<TrailRenderer>();
        rb2d = GetComponent<Rigidbody2D>();
    }

    private void Update() {
        float angle = Vector2.Angle(transform.right, rb2d.velocity);

        if(Mathf.Abs(rb2d.angularVelocity) >= angularVTrigger && rb2d.velocity.magnitude >= vMagnitudeTrigger || angle >= vectorDiffTrigger)
            if(Vector2.Dot(rb2d.velocity, transform.right) < 1)
                DisableTrail();
            else
                EnableTrail();
        else
            DisableTrail();
    }


    private void DisableTrail() {
        foreach(TrailRenderer t in trails) {
            if(t.tag != "flame")
                t.emitting = false;
        } 
    }

    private void EnableTrail() {
        foreach(TrailRenderer t in trails) {
            if(t.tag != "flame")
                t.emitting = true;
        }
    }

}
