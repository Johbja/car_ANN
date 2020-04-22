using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CarControllerDeathM : MonoBehaviour {
    [Header("Car movement settings")]
    [SerializeField] private float rotationSpeed;
    [SerializeField] private float acceleration;
    [SerializeField] private float reverseAcceleration;
    [SerializeField] private float breakePower;
    [SerializeField] private float dragWhenAcc;
    [SerializeField] private float dragWhenIdle;

    [Header("Battle Settings")]
    [SerializeField] private float health;
    [SerializeField] private float otherCarDamage;
    [SerializeField] private float wallSelfDamage;
    [SerializeField] private Image hpImage;

    [Header("Collision settings")]
    [SerializeField] private float maxVelocity;
    [SerializeField] private float wallCollisionForceMulti;
    [SerializeField] private float carCollisionForceMulti;

    [Header("AI settings")]
    public bool isAI;
    [SerializeField] private bool isOn;
    [SerializeField] private Brain brain;

    [Header("Ray settings")]
    public AiRay[] aiRays;
    [SerializeField] private LayerMask carMask;
    [SerializeField] private LayerMask roadMask;
    [SerializeField] private bool useCarRays;
    [SerializeField] private Color rayColor;
    [SerializeField] private bool debugRays;

    public Network network;

    [HideInInspector] public Rigidbody2D carRb2d;

    private ParticleSystem particleSystem;
    private Vector2 starPos;
    private Vector2 lastPos;
    private float currenHp;
    private float LongestAliveTime;
    private float CarCollisions;
    private float timerAtReset;
    private float totalDistance;
    private int DeathCount;
    private int[] neuronsInLayers;
    

    private void Awake() {
        if(isAI) {
            if(brain != null) {
                if(brain.generateNewNetwork) {

                    // deep copy ints from brain scripteble object
                    neuronsInLayers = new int[brain.neuronsInLayers.Length];
                    for(int i = 0; i < neuronsInLayers.Length; i++) {
                        neuronsInLayers[i] = brain.neuronsInLayers[i];
                    }

                    GenerateNewNetwork();
                } else {

                    //LoadBrainFromFile();

                    //get network stored in brain
                    network = brain.GetNetwork();

                    //copy over rays from brain so that the inputs rays will be the same for a given brain
                    AiRay[] raysToLoad = brain.GetRays();
                    aiRays = new AiRay[raysToLoad.Length];
                    for(int i = 0; i < aiRays.Length; i++) {
                        aiRays[i] = raysToLoad[i];
                    }
                }
            } else
                GenerateNewNetwork();
        }
    }

    private void Start() {
        carRb2d = GetComponent<Rigidbody2D>();
        particleSystem = GetComponentInChildren<ParticleSystem>(true);
        starPos = carRb2d.position;
        ResetCar();
    }

    private void Update() {
        if(!isAI) {

            if(Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.RightArrow))
                RotateCar(-Input.GetAxis("Horizontal"));

            if(Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.DownArrow))
                AccelerateCar(Input.GetAxis("Vertical"));

            if(Input.GetKey(KeyCode.Space)) {
                UseBreak();
            }

        }
    }

    private void FixedUpdate() {

        totalDistance += Vector2.Distance(lastPos, carRb2d.position);
        lastPos = carRb2d.position;

        carRb2d.drag = dragWhenIdle;        //increase drag for faster brake speed when not using the throttle

        if(isAI && isOn) {
            //get inputs and put them in a format the AI can read
            List<RaycastHit2D> rays = GetViewInputValues();
            float[] inputsValues = new float[rays.Count];

            string output = "";
            for(int i = 0; i < rays.Count; i++) {
                if(rays[i].collider != null) {
                    inputsValues[i] = rays[i].distance / aiRays[i % aiRays.Length].GetMaxDistance();

                    output += " ray [" + i + "] has distance of " + (rays[i].distance / aiRays[i % aiRays.Length].GetMaxDistance()).ToString();
                } else {
                    output += " ray [" + i + "] has distance of 1";
                    inputsValues[i] = 1;
                }
            }

            //send inputs to AI and recive outputs
            float[] outputs = network.SendInputs(inputsValues);
            AccelerateCar(outputs[0]);
            RotateCar(outputs[1]);
        }
    }

    private void OnCollisionEnter2D(Collision2D collision) {
        if(collision.gameObject.tag == "car") {
            Rigidbody2D collRb2d = collision.gameObject.GetComponent<Rigidbody2D>();

            if(collRb2d.velocity.magnitude < maxVelocity) {
                Vector3 f = collision.GetContact(0).normal * carCollisionForceMulti * carRb2d.velocity.magnitude;
                collRb2d.AddForce(f, ForceMode2D.Impulse);
            }

            Vector2 contact = collision.GetContact(0).point;

            if(Vector2.Distance(contact, transform.position) < Vector2.Distance(contact, collision.transform.position)) {
                CarCollisions++;
                collRb2d.gameObject.GetComponent<CarControllerDeathM>().DealDamage(otherCarDamage);
                //Debug.Log("coll car distance: " + Vector2.Distance(contact, transform.position));
                //Debug.Log("other car distance: " + Vector2.Distance(contact, collision.transform.position));
            }

        }

        if(collision.gameObject.tag == "wall") {
            if(carRb2d.velocity.magnitude < maxVelocity) {
                carRb2d.AddForce(collision.GetContact(0).normal * wallCollisionForceMulti * carRb2d.velocity.magnitude, ForceMode2D.Impulse);
                DealDamage(wallSelfDamage);
            }
        }

    }

    private void LoadBrainFromFile() {
        NetworkData data = NetworkLoadSaveSystem.LoadNetwork();
        network = new Network(data.networkStructure);
        network.SetNetworkByGenom(data.netowrkGenom);
    }

    private void UpdateUi() {
        hpImage.fillAmount = currenHp / health;
    }

    public void DealDamage(float dmg) {
        if(currenHp - dmg > 0) {
            currenHp -= dmg;
        } else {
            currenHp = 0;
            DeathCount++;
            if(Time.time - timerAtReset > LongestAliveTime)
                LongestAliveTime = Time.time - timerAtReset;

            timerAtReset = Time.time;

            ResetCar();
        }
        UpdateUi();
    }

    public void SaveBrain() {
        //extract all data from rays so that it can be stored in brain
        float[] rays = new float[aiRays.Length * 3];        //times 3 due to every ray consisting of 3 values

        int pointer = 0;
        for(int i = 0; i < aiRays.Length; i++) {
            rays[pointer] = aiRays[i].GetData()[0];
            rays[pointer + 1] = aiRays[i].GetData()[1];
            rays[pointer + 2] = aiRays[i].GetData()[2];
            pointer += 3;
        }

        //send data to brain for stroing
        brain.StoreNetowrk(network, rays);
    }

    public void ResetCar() {

        currenHp = health;
        LongestAliveTime = 0;
        CarCollisions = 0;
        DeathCount = 0;
        timerAtReset = Time.time;

        carRb2d.position = starPos;
        lastPos = starPos;

        carRb2d.rotation = 0;
        carRb2d.velocity = new Vector2(0, 0);
        carRb2d.angularVelocity = 0;

        UpdateUi();
    }

    public void RotateCar(float dir) {
        carRb2d.AddTorque(dir * rotationSpeed, ForceMode2D.Force);
    }

    public void AccelerateCar(float dir) {
        if(Mathf.Sign(dir) > 0) {
            carRb2d.drag = dragWhenAcc;
            carRb2d.AddRelativeForce(Vector2.right * acceleration * dir, ForceMode2D.Force);
        } else {
            if(Vector2.Dot(carRb2d.velocity, transform.right) < 1)
                carRb2d.AddRelativeForce(Vector2.right * reverseAcceleration * dir, ForceMode2D.Force);
        }
    }

    public void UseBreak() {
        carRb2d.drag = breakePower;
    }

    public float GetFitness() {
        return Mathf.Max(1, LongestAliveTime + CarCollisions* 10 + currenHp - DeathCount * 10 + totalDistance * 0.1f);                                                                                                           
    }

    public void GenerateNewNetwork() {
        if(neuronsInLayers == null) {
            Debug.Log("You need to assing a brain to the car");
            return;
        } else {
            if(useCarRays)
                neuronsInLayers[0] = aiRays.Length * 2;     // match first layers neuron to amount of ray inputs no matter what
            else
                neuronsInLayers[0] = aiRays.Length;     // match first layers neuron to amount of ray inputs no matter what

            neuronsInLayers[neuronsInLayers.Length - 1] = 2; // there should only ever be 2 outputs when used on this car
            network = new Network(neuronsInLayers);
        }
    }

    public List<RaycastHit2D> GetViewInputValues() {
        List<RaycastHit2D> rayList = new List<RaycastHit2D>();

        foreach(AiRay ray in aiRays) {
            Vector2 dir = transform.TransformVector(ray.GetVector()).normalized;
            RaycastHit2D currentRay = Physics2D.Raycast(transform.position, dir, ray.GetMaxDistance(), roadMask);
            rayList.Add(currentRay);

            if(debugRays) {
                if(currentRay.collider != null)
                    Debug.DrawRay(transform.position, dir * ray.GetMaxDistance(), Color.red);
                else
                    Debug.DrawRay(transform.position, dir * ray.GetMaxDistance(), Color.green);
            }

            //add collisions against cars
            if(useCarRays) {
                currentRay = Physics2D.Raycast(transform.position, dir, ray.GetMaxDistance(), carMask);
                rayList.Add(currentRay);

                if(debugRays) {
                    if(currentRay.collider != null)
                        Debug.DrawRay(transform.position, dir * ray.GetMaxDistance(), Color.red);
                    else
                        Debug.DrawRay(transform.position, dir * ray.GetMaxDistance(), Color.yellow);
                }
            }
        }

        return rayList;
    }

    private void OnDrawGizmosSelected() {
        if(!Application.isPlaying && isAI) {
            if(brain != null && !brain.generateNewNetwork && brain.GetRays() != null) {
                //draw rays from brain that will be loaded on runtime
                AiRay[] rays = brain.GetRays();
                foreach(AiRay ray in rays) {
                    Debug.DrawRay(transform.position, transform.TransformVector(ray.GetVector()).normalized * ray.GetMaxDistance(), rayColor);
                }
            } else {
                if(aiRays != null) {
                    //draw new rays that will be used on a new brain
                    foreach(AiRay ray in aiRays) {
                        Debug.DrawRay(transform.position, transform.TransformVector(ray.GetVector()).normalized * ray.GetMaxDistance(), rayColor);
                    }
                }
            }
        }
    }
}
