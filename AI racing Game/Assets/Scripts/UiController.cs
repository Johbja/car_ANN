using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UiController : MonoBehaviour {

    [SerializeField] private InputField carSpawn;
    [SerializeField] private InputField traningTime;
    [SerializeField] private Slider mutationRange;
    [SerializeField] private Slider splice;
    [SerializeField] private Slider uniformProb;
    [SerializeField] private Slider dominatnGene;
    [SerializeField] private Toggle random;
    [SerializeField] private Toggle spliceBased;

    private AiEvolver aiEvo;

    void Start() {
        aiEvo = GetComponent<AiEvolver>();

        //carSpawn.text = aiEvo.inizialCarSpawn.ToString();
        //traningTime.text = aiEvo.traniningTime.ToString();
        //mutationRange.value = aiEvo.randomMutationRange;
        //splice.value = aiEvo.spliceCount;
        //uniformProb.value = aiEvo.uniformProbability;
        //dominatnGene.value = aiEvo.dominantGenomePresistancyProbaility;
        //random.isOn = aiEvo.randomEvulution;
        //spliceBased.isOn = aiEvo.isSpliceBased;

    }


    void Update() {
        //aiEvo.inizialCarSpawn = int.Parse(carSpawn.text);
        //aiEvo.traniningTime = float.Parse(traningTime.text);

        //aiEvo.randomMutationRange = mutationRange.value;
        //aiEvo.spliceCount = (int)splice.value;
        //aiEvo.uniformProbability = (int)uniformProb.value;
        //aiEvo.dominantGenomePresistancyProbaility = (int)dominatnGene.value;
        //aiEvo.randomEvulution = random.isOn;
        //aiEvo.isSpliceBased = spliceBased.isOn;
    }
}
