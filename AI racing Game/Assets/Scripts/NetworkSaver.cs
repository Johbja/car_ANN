using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.IO;

public class NetworkSaver : MonoBehaviour {

    [SerializeField] private InputField inputFieldSave;
    [SerializeField] private InputField inputFieldSaveName;
    [SerializeField] private InputField inputFieldLoad;
    [SerializeField] private InputField inputFieldLoadName;
    [SerializeField] private CarController car;

    public void SaveNetworkToFile() {
        NetworkData data = new NetworkData(car.network);
        string path = inputFieldSave.text + "\\" + inputFieldSaveName.text +  ".txt";

        Debug.Log(path);

        if(File.Exists(path)) {
            Debug.LogError("file already exsists");
            return;
        }

        NetworkLoadSaveSystem.SaveNetwork(car.network, path);
    }

    public void LoadNetworkFromFile() {
        string path = inputFieldLoad.text + "\\" + inputFieldLoadName.text + ".txt";

        if(!File.Exists(path)) {
            Debug.LogError("file does not exsists");
            return;
        }

        NetworkData data = NetworkLoadSaveSystem.LoadNetwork(path);

        car.network = new Network(data.networkStructure);
        car.network.SetNetworkByGenom(data.netowrkGenom);
        car.ResetCar();
    }
}
