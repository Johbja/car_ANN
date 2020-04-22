using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

[System.Serializable]
public class AiRay {
    [SerializeField] private float fieldOfView;
    [SerializeField] private Vector2 dir;

    public AiRay(float x, float y, float _fieldOfView) {
        dir = new Vector2(x, y);
        fieldOfView = _fieldOfView;
    }

    public Vector2 GetVector() {
        return dir.normalized;
    }

    public float GetMaxDistance() {
        return fieldOfView;
    }

    public float[] GetData() {
        float[] data = { dir.x, dir.y, fieldOfView};
        return data;
    }
}

[RequireComponent(typeof(Rigidbody2D))]
public class CarController : MonoBehaviour {

    [Header("Car movement settings")]
    [SerializeField] private float rotationSpeed;
    [SerializeField] private float acceleration;
    [SerializeField] private float reverseAcceleration;
    [SerializeField] private float breakePower;
    [SerializeField] private float dragWhenAcc;
    [SerializeField] private float dragWhenIdle;

    [Header("AI settings")]
    public bool isAI;
    [SerializeField] private Brain brain;
    [SerializeField] private float collisionPunish;

    [Header("Ray settings")]
    public AiRay[] aiRays;
    [SerializeField] private Color rayColor;
    [SerializeField] private bool debugRays;

    [Header("Checkpoint settings")]
    [SerializeField] private CheckpointSystem cpSystem;

    [Header("Race settings")]
    [SerializeField] private Text winText;
    [SerializeField] private CarController Opponent;
    [SerializeField] private float resetTimer;
    [SerializeField] private bool isEnabled;

    public Network network;

    public float totalDistance { get; set; }
    [HideInInspector] public Rigidbody2D carRb2d;

    private ParticleSystem particleSystem;
    private Vector2 starPos;
    private float currentDistance;
    private float topSpeed;
    private float collisionCounter;
    private float startTime;
    private float speedCounter;
    private int currentCheckpoint;
    private int goalCheckpoint;
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

    private void OnApplicationQuit() {
        //SaveBrain();
    }

    private void Start() {
        carRb2d = GetComponent<Rigidbody2D>();
        particleSystem = GetComponentInChildren<ParticleSystem>();
        starPos = carRb2d.position;
        ResetCar();
        Invoke("Start2", 0.1f);
    }

    private void Start2() {
        goalCheckpoint = cpSystem.GetGoalCheckpoint();
        currentDistance = cpSystem.GetRelativeDisance(currentCheckpoint, carRb2d.position);
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

        if(topSpeed >= carRb2d.velocity.magnitude - 0.2f) {
            topSpeed = carRb2d.velocity.magnitude;
            speedCounter += Time.deltaTime;
        }


        //calculate curret distance based on checkpoints
        if(currentDistance > cpSystem.GetRelativeDisance(currentCheckpoint, carRb2d.position)) {
            totalDistance += currentDistance - cpSystem.GetRelativeDisance(currentCheckpoint, carRb2d.position);
            currentDistance = cpSystem.GetRelativeDisance(currentCheckpoint, carRb2d.position);
        }

        carRb2d.drag = dragWhenIdle;        //increase drag for faster brake speed when not using the throttle

        if(isAI) {
            //get inputs and put them in a format the AI can read
            List<RaycastHit2D> rays = GetViewInputValues();
            float[] inputsValues = new float[rays.Count];

            string output = "";
            for(int i = 0; i < rays.Count; i++) {
                if(rays[i].collider != null) {
                    inputsValues[i] = rays[i].distance / aiRays[i].GetMaxDistance();

                    output += " ray [" + i + "] has distance of " + (rays[i].distance / aiRays[i].GetMaxDistance()).ToString();
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

    private void OnLose() {
        StartCoroutine(ResetDeley() as IEnumerator);
        GetComponent<SpriteRenderer>().enabled = false;
        particleSystem.Play(false);
    }

    private IEnumerator ResetDeley() {
        yield return new WaitForSeconds(resetTimer);
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private void OnTriggerEnter2D(Collider2D other) {
        ///if car collide with a checkpoint, update current checkpoint to next checkpoint
        if(other.tag == "cp") {
            if(currentCheckpoint + 1 < goalCheckpoint && currentCheckpoint < other.gameObject.GetComponent<CheckpointSlave>().checkpoint && other.gameObject.GetComponent<CheckpointSlave>().checkpoint != goalCheckpoint) {
                currentDistance = cpSystem.GetDistance(currentCheckpoint, currentCheckpoint + 1);
                currentCheckpoint++;
            } else if (currentCheckpoint + 1 == goalCheckpoint) {

                //lose or win stuff
                if(isEnabled) {
                    winText.gameObject.SetActive(true);

                    if(isAI) {
                        winText.text = "YOU LOSE";
                        Opponent.OnLose();
                    } else {
                        winText.text = "YOU WIN";
                        Opponent.OnLose();
                    }
                }

                Debug.Log("you made a lap");
                currentCheckpoint = 0;
            }
        }
    }

    private void OnCollisionEnter2D(Collision2D other) {
        if(other.gameObject.tag == "road") {
            collisionCounter += collisionPunish;
        }
            
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

    private void LoadBrainFromFile() {
        NetworkData data = NetworkLoadSaveSystem.LoadNetwork();
        network = new Network(data.networkStructure);
        network.SetNetworkByGenom(data.netowrkGenom);
    }

    public void ResetCar() {
        totalDistance = 0;
        currentCheckpoint = 0;

        collisionCounter = 0;
        startTime = Time.time;

        carRb2d.position = starPos;

        carRb2d.rotation = 0;
        carRb2d.velocity = new Vector2(0, 0);
        carRb2d.angularVelocity = 0;
    }

    public void RotateCar(float dir) {
        carRb2d.AddTorque(dir * rotationSpeed , ForceMode2D.Force);
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
        return Mathf.Max(1, (totalDistance + (totalDistance/(Time.time-startTime)) - collisionCounter));     //distance is most important, top speed will be added so that i counts (faster = better), 
                                                                                                                            //then subtract a small amount for every time the car collided so that cars that dont collide gets better fittnes
    }

    public void GenerateNewNetwork() {
        if(neuronsInLayers.Length <= 0) {
            Debug.LogError("You need to assing a brain to the car");
            return;
        } else {
            neuronsInLayers[0] = aiRays.Length;     // match first layers neuron to amount of ray inputs no matter what
            neuronsInLayers[neuronsInLayers.Length - 1] = 2; // there should only ever be 2 outputs when used on this car
            network = new Network(neuronsInLayers);
        }
    }

    public List<RaycastHit2D> GetViewInputValues() {
        List<RaycastHit2D> rayList = new List<RaycastHit2D>();

        foreach(AiRay ray in aiRays) {
            Vector2 dir = transform.TransformVector(ray.GetVector()).normalized;
            RaycastHit2D currentRay = Physics2D.Raycast(transform.position, dir, ray.GetMaxDistance(), LayerMask.GetMask("Road"));
            rayList.Add(currentRay);

            

            if(debugRays) {
                if(currentRay.collider != null)
                    Debug.DrawRay(transform.position, dir * ray.GetMaxDistance(), Color.red);
                else
                    Debug.DrawRay(transform.position, dir * ray.GetMaxDistance(), Color.green);
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
                //draw new rays that will be used on a new brain
                foreach(AiRay ray in aiRays) {
                    Debug.DrawRay(transform.position, transform.TransformVector(ray.GetVector()).normalized * ray.GetMaxDistance(), rayColor);
                }
            }
        }
    }
}
