using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class Tenant
{
    public string name;
    
    //Water and waste management variables
    public int[] dwp;

    public int currentWaste = 0;
    public List<int> accumulatedWaste = new List<int>();

    public bool collectMe = false;

    public int currentColInterval = 0;
    public List<int> collectionInterval = new List<int>();

    public string lastCollectionMethod = "No Data";
    public float lastCollectedAmount = 0;

    public int numCollections = 0;

    public int averageInterval => collectionInterval.Count > 0?(Mathf.RoundToInt(collectionInterval.Sum() / collectionInterval.Count)) : 0;

    public List<int> accumulatedAutoCollected = new List<int>();
    public float autoCollectedAVG => accumulatedAutoCollected.Count > 0? (Mathf.RoundToInt(accumulatedAutoCollected.Sum() / accumulatedAutoCollected.Count)) : 0;


    public void CollectWaste(string collectionMethod)
    {
        lastCollectionMethod = collectionMethod;
        lastCollectedAmount = currentWaste;
        accumulatedWaste.Add(currentWaste);
        collectionInterval.Add(currentColInterval);

        if (collectionMethod.Contains("Auto"))
        {
            collectMe = true;
            accumulatedAutoCollected.Add(currentWaste);
        }

        currentWaste = 0;
        currentColInterval = 0;
        numCollections++;
    }

    public void GenerateWaste(int day)
    {
        currentWaste += dwp[day];
        currentColInterval++;
    }
}