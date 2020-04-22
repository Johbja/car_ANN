using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RecordingData {
    public List<float> inputs;
    public List<float> outputs;

    public RecordingData(List<float> _inputs, List<float> _outputs) {
        inputs = _inputs;
        outputs = _outputs;
    }

}

public class AiEvolver : MonoBehaviour {

    [Header("Setup settings")]
    [SerializeField] private GameObject car;
    [SerializeField] private int inizialCarSpawn;
    [SerializeField] private float traniningTime;
    [SerializeField, Range(1, 100)] private float timeScale;

    [Header("Saving")]
    [SerializeField] private bool saveBestBrain;

    [Header("Imitation Learning")]
    [SerializeField] private bool useImitation;
    [SerializeField] private float recodingDeley;
    [SerializeField] private float recordingTime;

    [Header("Evulution Settings")]
    [SerializeField] private bool keepBestCar;
    [SerializeField] private bool isSpliceBased;
    [SerializeField] private float randomMutationRange;
    [SerializeField, Range(1, 2)] private int spliceCount;
    [SerializeField, Range(0, 100)] private int uniformProbability;
    [SerializeField, Range(0, 100)] private int dominantGenomePresistancyProbaility;
    [SerializeField, Range(0, 100)] private int mutationChance;


    public List<CarController> cars { get; private set; }
    private CarController carController;
    private List<RecordingData> recordedData;
    private Network traningNetwork;
    private int generationCount;
    private int genomVariationCount;
    private bool isRecording;

    private void Start() {
        // only activate if cars are set to AI
        if(useImitation) {
            recordedData = new List<RecordingData>();
            carController = car.GetComponent<CarController>();
            carController.isAI = false;
            StartCoroutine(RecordingDeley() as IEnumerator);

        } else {

            if(car.GetComponent<CarController>().isAI) {
                cars = new List<CarController>();
                car.SetActive(false);

                Time.timeScale = timeScale;

                for(int i = 0; i < inizialCarSpawn; i++) {
                    GameObject ins = Instantiate(car, transform) as GameObject;
                    ins.SetActive(true);
                    cars.Add(ins.gameObject.GetComponent<CarController>());
                }

                StartCoroutine(TraningTime() as IEnumerator);
            }
        }

    }

    private void OnApplicationQuit() {
        if(saveBestBrain) {

            float fittnes = 0;
            int index = 0;

            //find the car with best fittnes and save it in the current generation
            for(int i = 0; i < cars.Count; i++) {
                if(cars[i].GetFitness() >= fittnes) {
                    fittnes = cars[i].GetFitness();
                    index = i;
                }
            }

            //NetworkLoadSaveSystem.SaveNetwork(cars[index].network);
            cars[index].SaveBrain();
            Debug.Log("Best car saved");
        }
    }

    private IEnumerator TraningTime() {
        yield return new WaitForSeconds(traniningTime);
        GenerateNewGeneration();
        generationCount++;
        Debug.Log("Generation Reset, gen count = " + generationCount);
        StartCoroutine(TraningTime() as IEnumerator);
    }

    private IEnumerator RecordingDeley() {
        yield return new WaitForSeconds(recodingDeley);
        Debug.Log("start recoding");
        isRecording = true;
        StartCoroutine(Record() as IEnumerator);
        StartCoroutine(RecordTimer() as IEnumerator);
    }

    private IEnumerator Record() {
        while(isRecording) {
            //cast rays and get every ray casted in a list
            List<RaycastHit2D> inputValues = carController.GetViewInputValues();

            List<float> outputs = new List<float>();
            List<float> inputs = new List<float>();

            //add all the outputs based on what player presses
            outputs.Add(Input.GetAxis("Vertical"));
            outputs.Add(-Input.GetAxis("Horizontal"));

            // add all the inputs values as the ai sees it
            for(int i = 0; i < inputValues.Count; i++){
                if(inputValues[i].collider != null) {
                    inputs.Add(inputValues[i].distance / carController.aiRays[i].GetMaxDistance());
                } else {
                    inputs.Add(1);
                }
            }

            recordedData.Add(new RecordingData(inputs, outputs));

            yield return new WaitForFixedUpdate();
        }
        
    }

    private IEnumerator RecordTimer() {
        yield return new WaitForSeconds(recordingTime);
        isRecording = false;
        Debug.Log("recoding Complite");
        StartCoroutine(TrainNetwork() as IEnumerator);
    }

    private IEnumerator TrainNetwork() {
        car.SetActive(false);
        //traningNetwork = new Network(carController.network);
        Debug.Log(recordedData.Count);
        yield return null;
    }

    private CarController SelectCar(float fitnessSum) {
        float rand = Random.Range(0, fitnessSum);
        float counter = 0;

        foreach(CarController car in cars) {
            if(counter < rand) {
                counter += car.GetFitness();
            } else {
                return car;
            }
        }

        return cars[0];
    }

    private CarController SelectCar(int index) {
        int rand = Random.Range(0, index);
        return cars[rand];
    }

    private float[][] PointSpliceCrossover(CarController dominat, CarController submissive) {

        float[][] returnList = new float[2][];
        float[] dominatGenom = dominat.network.GetNetworkGenom();
        float[] submissiveGenom = submissive.network.GetNetworkGenom();


        if(spliceCount > 1) {
            //get a random split point
            int rand = Random.Range(0, dominatGenom.Length - 1);

            for(int i = 0; i < dominatGenom.Length; i++) {
                if(i > rand) {
                    //sawp genom values to the rigth of the splitting point
                    float holder = dominatGenom[i];
                    dominatGenom[i] = submissiveGenom[i];
                    submissiveGenom[i] = holder;
                }
            }

        } else {

            //get 2 split points, one random and the other one based on what is left
            int rand = Random.Range(0, dominatGenom.Length - 3);
            int rand2 = Random.Range(rand + 2, dominatGenom.Length - 1);

            for(int i = 0; i < dominatGenom.Length; i++) {
                if(i > rand && i < rand2) {
                    //sawp genom values in between the 2 random generated values
                    float holder = dominatGenom[i];
                    dominatGenom[i] = submissiveGenom[i];
                    submissiveGenom[i] = holder;
                }
            }
        }

        returnList[0] = dominatGenom;
        returnList[1] = submissiveGenom;
        return returnList;
    }

    private float[][] UniformSpliceCrossover(CarController dominat, CarController submissive) {

        float[][] returnList = new float[2][];
        float[] dominatGenom = dominat.network.GetNetworkGenom();
        float[] submisive = submissive.network.GetNetworkGenom();

        //do the combination, works for 1 and 2 splits points
        for(int i = 0; i < dominatGenom.Length; i++) {
            if(Random.Range(0, 100) >= uniformProbability) {
                //sawp genom values in a given range
                float holder = dominatGenom[i];
                dominatGenom[i] = submisive[i];
                submisive[i] = holder;
            }
        }

        returnList[0] = dominatGenom;
        returnList[1] = submisive;
        return returnList;
    }

    private float[] MutateGenom(float[] genom) {
        for(int i = 0; i < genom.Length; i++) {
            if(Random.Range(0, 100) < mutationChance) {
                genom[i] += Random.Range(-randomMutationRange, randomMutationRange);
            }
        }
        return genom;
    }

    private float[] GetEvolvedNetworkGenom() {
        //temp vars
        CarController selectedCar1;
        CarController selectedCar2;

        //get sum of fittnes
        float fitnessSum = 0;
        foreach(CarController car in cars) {
            fitnessSum += car.GetFitness();
        }

        selectedCar1 = SelectCar(fitnessSum);
        selectedCar2 = SelectCar(fitnessSum);

        //decide which car is dominat based on fitness and which gene combination method to use
        float[][] combindeGenoms;
        if(selectedCar1.GetFitness() > selectedCar2.GetFitness())
            combindeGenoms = isSpliceBased ? PointSpliceCrossover(selectedCar1, selectedCar2) : UniformSpliceCrossover(selectedCar1, selectedCar2);
        else
            combindeGenoms = isSpliceBased ? PointSpliceCrossover(selectedCar2, selectedCar1) : UniformSpliceCrossover(selectedCar2, selectedCar1);

        //do mutation with x% chance
        for(int i = 0; i < combindeGenoms.Length; i++) {
            combindeGenoms[i] = MutateGenom(combindeGenoms[i]);
        }

        // return dominat genom with % based chanse
        return Random.Range(0, 100) >= dominantGenomePresistancyProbaility ? combindeGenoms[1] : combindeGenoms[0];
    }

    private void GenerateNewGeneration() {

        float sum = 0;
        foreach(CarController car in cars) {
            sum += car.GetFitness();
        }

        Debug.Log("avrage fitness: " + sum / cars.Count);
        

        //new list to keep all new genoms
        List<float[]> newGenoms = new List<float[]>();

        //generate all new genoms
        for(int i = 0; i < inizialCarSpawn; i++) {
            newGenoms.Add(GetEvolvedNetworkGenom());
        }

        if(keepBestCar) {
            int bestCarIndex = 0;
            float currentFitness = 0;
            float worstFittness = cars[0].GetFitness();

            //find the best car and worst car
            for(int i = 0; i < cars.Count; i++) {
                if(cars[i].GetFitness() > currentFitness) {
                    currentFitness = cars[i].GetFitness();
                    bestCarIndex = i;
                }

                if(cars[i].GetFitness() < worstFittness)
                    worstFittness = cars[i].GetFitness();
            }

            //mutate best car
            cars[bestCarIndex].network.SetNetworkByGenom(MutateGenom(cars[bestCarIndex].network.GetNetworkGenom()));

            Debug.Log("best fitness: " + currentFitness);
            Debug.Log("worst fitness: " + worstFittness);

            //overwrite old genoms with new
            for(int i = 0; i < cars.Count; i++) {
                cars[i].ResetCar();

                //overwrite old genoms with new if its not the best car
                if(i != bestCarIndex) {
                    cars[i].network.SetNetworkByGenom(newGenoms[i]);
                }
            }

        } else {

            //overwrite old genoms with new
            for(int i = 0; i < cars.Count; i++) {
                cars[i].ResetCar();
                cars[i].network.SetNetworkByGenom(newGenoms[i]);
            }
        }
        


        //Debug.Log("Generations best fittnes: " + cars[cars.Count - 1].GetFitness());
        //cars[cars.Count - 1].network.PrintNetworkValues();

        //string output = "";
        //int counter = 0;
        //foreach(Neuron n in cars[cars.Count - 1].network.layers[cars[cars.Count - 1].network.layers.Length - 1].neurons) {
        //    output += " neuron activation: " + counter.ToString() + " = " + n.activation;
        //    counter++;
        //}
        //Debug.Log(output);

    }

    private void QuickSort(List<CarController> arr, int start, int end) {
        int i;
        if(start < end) {
            i = Partition(arr, start, end);

            QuickSort(arr, start, i - 1);
            QuickSort(arr, i + 1, end);
        }
    }

    private int Partition(List<CarController> arr, int start, int end) {
        CarController temp;
        CarController p = arr[end];
        int i = start - 1;

        for(int j = start; j <= end - 1; j++) {
            if(arr[j].GetFitness() <= p.GetFitness()) {
                i++;
                temp = arr[i];
                arr[i] = arr[j];
                arr[j] = temp;
            }
        }

        temp = arr[i + 1];
        arr[i + 1] = arr[end];
        arr[end] = temp;
        return i + 1;
    }

    private void OnValidate() {
        Time.timeScale = timeScale;
    }

}
