using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class NetworkData {

    public float[] netowrkGenom;
    public int[] networkStructure;

    public NetworkData(Network network) {

        //store the structure of the network for reconstruction later
        networkStructure = new int[network.networkStructure.Length];
        for(int i = 0; i < networkStructure.Length; i++) {
            networkStructure[i] = network.networkStructure[i];
        }

        //deep copy genom
        float[] genom = network.GetNetworkGenom();
        netowrkGenom = new float[genom.Length];
        for(int i = 0; i < netowrkGenom.Length; i++) {
            netowrkGenom[i] = genom[i];
        }
    }
}
