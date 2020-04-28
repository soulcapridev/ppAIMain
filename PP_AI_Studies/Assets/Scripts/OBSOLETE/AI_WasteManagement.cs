//using System.Collections;
//using System.Collections.Generic;
//using UnityEngine;
//using System.Linq;

//public class AI_WasteManagement : MonoBehaviour
//{
//    [SerializeField] GUISkin _skin;

//    Tenant[] _tenants;

//    int _wasteCapacity = 2500; // in grams
//    float _collectionCap = 0.75f; //in percentage, trigger collection

//    int learningCap = 10; //in days, trigger automated collection

//    int _day = 0;
//    float _dayStep = 0.5f; //in seconds

//    List<Rect> toCollect = new List<Rect>();

//    bool _autoCollectionOn = false;
//    bool _runSimulation; //STILL NOT IMPLEMENTED

    
//    void Start()
//    {
//        string[] tenantsNames = CSVReader.ReadNames("Input Data/TENANT_NAMES");

//        _tenants = new Tenant[tenantsNames.Length];

//        //initialize, name the tenants and associate Daily Waste Production (DWP) values
//        for (int i = 0; i < _tenants.Length; i++)
//        {
//            _tenants[i] = new Tenant();
//            _tenants[i].Name = tenantsNames[i];

//            _tenants[i].DWP = CSVReader.ReadDWP($"Input Data/U{i + 1}_DWP");
//        }

//        StartCoroutine(MakeWaste());
//        //StartCoroutine(SaveScreenshot());
      
//    }

//    // Update is called once per frame
//    void Update()
//    {
//        foreach (var tenant in _tenants)
//        {
//            if (tenant.NumCollections >= learningCap)
//            {
//                AutoCollect(tenant);
//            }
//            else
//            {
//                SelfCollect(tenant);
//            }
//        }
//        //SaveScreenshot();
//    }

//    IEnumerator MakeWaste()
//    {
//        while (_day < 999)
//        {

//            foreach (var tenant in _tenants)
//            {
//                if (tenant.CollectMe)
//                {
//                    tenant.CollectMe = false;
//                }
//                tenant.GenerateWaste(_day);
//            }
//            toCollect.Clear();
//            _day++;
//            //StartCoroutine(SaveScreenshot());
//            yield return new WaitForSeconds(_dayStep);
//        }   
//    }

//    void AutoCollect(Tenant tenant)
//    {
//        if (tenant.CcurrentColInterval >= tenant.AverageInterval)
//        {
//            tenant.CollectWaste("Auto collection");
//        }
        
//    }

//    void SelfCollect(Tenant tenant)
//    {
//        if (tenant.CurrentWaste >= _wasteCapacity * _collectionCap)
//        {
//            tenant.CollectWaste("Self-collection");
//        }
//    }

//    IEnumerator SaveScreenshot()
//    {
//        string file = $"SavedFrames/Frame_{_day}.png";
//        ScreenCapture.CaptureScreenshot(file);
//        yield return new WaitForEndOfFrame();
//    }

//    private void OnGUI()
//    {
//        GUI.skin = _skin;

//        //Logo
//        //GUI.Box(new Rect(25, 25, 100, 100), Resources.Load<Texture>("Textures/PP_Logo"), "image");
//        GUI.DrawTexture(new Rect(20, -10, 128, 128), Resources.Load<Texture>("Textures/PP_Logo"));
        
//        //Title
//        GUI.Box(new Rect(180, 30, 500, 25), "AI Waste and Water Management Simulation", "title");

//        //Day Counter
//        GUI.Box(new Rect(Screen.width - 125, 30, 100, 25), $"Day: {_day}", "subtitle");

//        //Info Panels
//        var paddingA = 10;
//        var paddingB = 10;

//        var boxWidthA = (Screen.width - (paddingA * (_tenants.Length + 1))) / _tenants.Length;
//        var boxWidthB = (Screen.width - (paddingB * (_tenants.Length + 1))) / _tenants.Length;
        
//        int boxHeightA = 100;
//        int boxHeightB = 120;

//        for (int i = 0; i < _tenants.Length; i++)
//        {
//            var tenant = _tenants[i];

//            //Tenants Panels
//            Rect tenantRect = new Rect((paddingA*(i+1) + (boxWidthA*i)), Screen.height - paddingA - boxHeightA, boxWidthA, boxHeightA);
            
//            GUIContent tenantInfo = new GUIContent();
//            tenantInfo.text = $"Tenant {i}: {tenant.Name}\n" +
//                $"Current DWP: {tenant.DWP[_day]} g\n" +
//                $"Current Accumulated Waste: {tenant.CurrentWaste} g\n" +
//                $"Current Interval: {tenant.CcurrentColInterval} days";

//            GUI.Box(tenantRect, tenantInfo);

//            GUIContent collectionStatus = new GUIContent();
//            float collectedRatio = Mathf.Round((tenant.LastCollectedAmount / _wasteCapacity) * 100);
//            string collectedData = tenant.LastCollectedAmount == 0 ? "" : $"at {(collectedRatio).ToString()}%";
//            collectionStatus.text = $"Last Collection: {tenant.LastCollectionMethod} {collectedData}";

//            Rect statusRect = new Rect(tenantRect.x, tenantRect.y - 20, boxWidthA, 20);
//            GUI.Box(statusRect, collectionStatus, "borderlessText");


//            //AI Panels
//            Rect aiRect = new Rect((paddingB * (i + 1) + (boxWidthB * i)), 110, boxWidthB, boxHeightB);

//            GUIContent aiContent = new GUIContent();

//            var lastAutoCollectedAmount = tenant.LastCollectionMethod.Contains("Auto") ? tenant.LastCollectedAmount : 0;
//            var lastAutoCollectedRatio = Mathf.Round((lastAutoCollectedAmount / _wasteCapacity) * 100);
            
//            var autoCollectedAVGRatio = Mathf.Round((tenant.AutoCollectedAVG / _wasteCapacity) * 100);

//            aiContent.text = $"Tenant {i}\n" +
//                $"Collection Mode: {tenant.LastCollectionMethod}\n" +
//                $"Number of Collections: {tenant.NumCollections}\n" +
//                $"Average Collection Interval: {tenant.AverageInterval} days\n" +
//                $"Last Auto Collected Amount: {lastAutoCollectedAmount} g ({lastAutoCollectedRatio}%)\n" +
//                $"Average Auto Collected Amount: {tenant.AutoCollectedAVG} g ({autoCollectedAVGRatio}%)";

//            GUI.Box(aiRect, aiContent);
//            if (tenant.LastCollectionMethod.Contains("Auto"))
//            {
//                GUI.Box(new Rect(aiRect.xMin, aiRect.yMax + paddingB/2, aiRect.width, 22), "AUTO COLLECTION ON", "autoCollectText");
//            }

//            //Tenant Units Visualization
//            Rect unitRect = new Rect(tenantRect.center.x - 75, tenantRect.center.y - 300, 150, 60);

//            GUI.Box(unitRect,$"Unit {i}", "unitTitle");
//            if (tenant.CollectMe)
//            {
//                toCollect.Add(unitRect);
//            }

//            //Water and Waste capacity Visualization
//            Rect wwRect = new Rect((int) unitRect.xMin + 25, (int) unitRect.yMin - 25, unitRect.width - 25, 25);
//            Texture capacityColor;
//            if (tenant.CurrentWaste <= _wasteCapacity * 0.2f) capacityColor = Resources.Load<Texture>("Textures/PP_WW_20");
//            else if (tenant.CurrentWaste <= _wasteCapacity * 0.4f) capacityColor = Resources.Load<Texture>("Textures/PP_WW_40");
//            else if (tenant.CurrentWaste <= _wasteCapacity * 0.6f) capacityColor = Resources.Load<Texture>("Textures/PP_WW_60");
//            else if (tenant.CurrentWaste <= _wasteCapacity * 0.8f) capacityColor = Resources.Load<Texture>("Textures/PP_WW_80");
//            else  capacityColor = Resources.Load<Texture>("Textures/PP_WW_80");
 
//            GUI.DrawTexture(wwRect, capacityColor);
//        }

//        //WasteBot
//        int wbWidth = 25;
//        int wbHeight = 25;
//        int wbX;
//        int wbY;
//        Rect robotStatRect = new Rect(20, (Screen.height / 2) - 70, 50, 20);
//        string robotStat = "Robot Status: ";
//        if (!toCollect.Any())
//        {
//            robotStat += "Idle";
//            wbX = 20;
//            wbY = (Screen.height / 2) - 50;
//            Rect botRect = new Rect(wbX, wbY, wbWidth, wbHeight);
//            GUI.Box(botRect, "R");
//        }
//        else
//        {
//            robotStat += $"Collecting";
//            foreach (var unit in toCollect)
//            {
//                wbX = (int) unit.xMin;
//                wbY = (int)unit.yMin - wbHeight;
//                Rect botRect = new Rect(wbX, wbY, wbWidth, wbHeight);
//                GUI.Box(botRect, "R");
//            }
//        }
//        GUI.Box(robotStatRect, robotStat, "robotStatus");
//        //StartCoroutine(SaveScreenshot());
//    }
//}