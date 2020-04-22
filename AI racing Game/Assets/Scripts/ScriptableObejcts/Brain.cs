using UnityEngine;

[System.Serializable, CreateAssetMenu(fileName = "Brain", menuName = "Brain/New Brain", order = 1)]
public class Brain : ScriptableObject {
    [Header("Image in editor")]
    [SerializeField] public Texture2D previewImage;

    [Header("New brain settings")]
    public bool generateNewNetwork;
    public int[] neuronsInLayers = { 8, 7, 6, 8 };

    [Header("Do not touch, data is stored from network here")]
    [SerializeField] private float[] networkGenom;
    [SerializeField] private int[] networkStructure;
    [SerializeField] private float[] rayValues;

    public void StoreNetowrk(Network network, float[] rays) {
        //store the structure of the network for reconstruction later
        networkStructure = new int[network.networkStructure.Length];
        for(int i = 0; i < networkStructure.Length; i++) {
            networkStructure[i] = network.networkStructure[i];
        }

        //deep copy genom
        float[] genom = network.GetNetworkGenom();
        networkGenom = new float[genom.Length];
        for(int i = 0; i < networkGenom.Length; i++) {
            networkGenom[i] = genom[i];
        }

        //deep copy ray data
        rayValues = new float[rays.Length];
        for(int i = 0; i < rayValues.Length; i++) {
            rayValues[i] = rays[i];
        }

    }

    public Network GetNetwork() {
        Network net = new Network(networkStructure);
        net.SetNetworkByGenom(networkGenom);
        return net;
    }

    public AiRay[] GetRays() {
        if(rayValues == null)
            return null;

        AiRay[] rays = new AiRay[rayValues.Length/3];       //every ai ray will take 3 arguments from the array thus a 3rd of the length is needed

        int pointer = 0;
        for(int i = 0; i < rays.Length; i++) {
            rays[i] = new AiRay(rayValues[pointer], rayValues[pointer + 1], rayValues[pointer + 2]);
            pointer += 3;                                   //add 3 as an offset every time due to every Airay needing the 3 next values in the array
        }

        return rays;
    }

}
