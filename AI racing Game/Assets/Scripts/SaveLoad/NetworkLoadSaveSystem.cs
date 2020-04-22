using UnityEngine;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

public static class NetworkLoadSaveSystem {

    public static void SaveNetwork(Network network) {
        BinaryFormatter formatter = new BinaryFormatter();
        string path = Application.persistentDataPath + "/networkData.learned";
        FileStream stream = new FileStream(path, FileMode.Create);

        NetworkData data = new NetworkData(network);

        formatter.Serialize(stream, data);
        stream.Close();
    }

    public static void SaveNetwork(Network network, string path) {
        BinaryFormatter formatter = new BinaryFormatter();
        FileStream stream = new FileStream(path, FileMode.Create);

        NetworkData data = new NetworkData(network);

        formatter.Serialize(stream, data);
        stream.Close();
    }

    public static NetworkData LoadNetwork() {

        string path = Application.persistentDataPath + "/networkData.learned";
        if(File.Exists(path)) {

            BinaryFormatter formatter = new BinaryFormatter();
            FileStream stream = new FileStream(path, FileMode.Open);

            NetworkData data = formatter.Deserialize(stream) as NetworkData;
            stream.Close();

            return data;

        } else {
            Debug.LogError("File not found at: " + path);
            return null;
        }

    }

    public static NetworkData LoadNetwork(string path) {

        if(File.Exists(path)) {

            BinaryFormatter formatter = new BinaryFormatter();
            FileStream stream = new FileStream(path, FileMode.Open);

            NetworkData data = formatter.Deserialize(stream) as NetworkData;
            stream.Close();

            return data;

        } else {
            Debug.LogError("File not found at: " + path);
            return null;
        }

    }
}
