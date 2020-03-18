using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using QuickGraph;
using QuickGraph.Algorithms;

public class AI_EventPlanner : MonoBehaviour
{
    [SerializeField] GUISkin _skin;
    [SerializeField] Transform _cameraPivot;

    Camera _cam;

    Tenant _ai = new Tenant();

    int _day = 0;
    float _dayStep = 0.2f; //in seconds

    string _outputMessage;
    string _eventName = "MyEvent";
    string _eventPopulation = "10";
    string _eventToiletCount = "0";
    string _eventWCSinkCount = "0";
    string _eventShowerCount = "0";
    string _eventKitchenOvenCount = "0";
    string _eventKitchenStoveCount = "0";
    string _eventKitchenSinkCount = "0";
    string _eventKitchenTopCount = "0";
    BuildingZone _zone; 

    //List<Task> _requiredTasks = new List<Task>();
    List<Voxel> _eventVoxels = new List<Voxel>();


    VoxelGrid _grid;
    VoxelGrid _basement; //SHOULD BE IMPLEMENTED AS PART RESOURCE

    Vector3Int _gridSize = new Vector3Int(50, 1, 50);

    List<Part> _existingParts = new List<Part>();
    List<Part> _partsInEvent = new List<Part>();
    List<PPMoveTask> _eventTasks = new List<PPMoveTask>();

    bool _drawTags = false;
    bool _drawPaths = false;

    Mesh[] _meshes;


    void Start()
    {
        _ai.name = "PublicParts_AI";
        _cam = Camera.main;

        _cameraPivot.position = new Vector3(_gridSize.x / 2, _gridSize.y / 2, _gridSize.z / 2);
        _grid = new VoxelGrid(_gridSize, 1f, Vector3.zero);
        //_existingParts = JSONReader.ReadPartsAsList(_grid);
        PopulateRandom(150);

        _zone = BuildingZone.Northeast;
    }

    void Update()
    {
        DrawState();
        DrawEvent();

        if (Input.GetKeyDown(KeyCode.T)) _drawTags = !_drawTags;

        //if (Input.GetKeyDown(KeyCode.P)) _drawPaths = !_drawPaths;
 
        if (_drawPaths) Drawing.DrawMesh(false, _meshes);
    }

    void PopulateRandom(int amt)
    {
        for (int i = 0; i < amt; i++)
        {
            Part p = new Part();
            p.NewRandomPart(_grid);
            _existingParts.Add(p);
        }
    }

    void GenerateEvent()
    {
        int requiredPopulation = int.Parse(_eventPopulation);
        int eventTargetArea = Mathf.CeilToInt(requiredPopulation * 1.5f);

        Vector3Int calculationOrigin;
        Voxel originVoxel;
        if (_zone == BuildingZone.Northeast)
        {
            calculationOrigin = new Vector3Int(Mathf.FloorToInt(_gridSize.x * 0.75f) - 1, _gridSize.y - 1, Mathf.FloorToInt(_gridSize.z * 0.75f) - 1);
            originVoxel = _grid.Voxels[calculationOrigin.x, calculationOrigin.y, calculationOrigin.z];
        }
        else
        {
            //OTHER ORIGINS REMAIN TO BE IMPLEMENTED
            calculationOrigin = new Vector3Int(Mathf.FloorToInt(_gridSize.x * 0.75f), _gridSize.y, Mathf.FloorToInt(_gridSize.z * 0.75f));
            originVoxel = _grid.Voxels[calculationOrigin.x, calculationOrigin.y, calculationOrigin.z];
        }

        _eventVoxels.Add(originVoxel);

        while (_eventVoxels.Count < eventTargetArea)
        {
            List<Voxel> temp = new List<Voxel>();
            foreach (var voxel in _eventVoxels)
            {
                var newNeighbours = voxel.GetFaceNeighbours();
                foreach (var neighbour in newNeighbours)
                {
                    var nIndex = neighbour.Index;
                    var gridVoxel = _grid.Voxels[nIndex.x, nIndex.y, nIndex.z];
                    if (!_eventVoxels.Contains(neighbour) && !temp.Contains(neighbour))
                    {
                        if (!gridVoxel.IsOccupied && gridVoxel.IsActive) temp.Add(neighbour);
                        else if (!gridVoxel.Part.IsStatic && gridVoxel.IsActive && gridVoxel.Part != null)
                        {
                            temp.Add(neighbour);
                            if (!_partsInEvent.Contains(gridVoxel.Part)) _partsInEvent.Add(gridVoxel.Part);
                        }
                    }
                }
            }
            foreach (var v in temp)
            {
                if (_eventVoxels.Count < eventTargetArea + 1) _eventVoxels.Add(v);
            }
        }

        GenerateMoveTasks();
        _outputMessage = $"Event {_eventName} generated with {_eventVoxels.Count} units and {_eventTasks.Count} Tasks";
    }

    void GenerateMoveTasks()
    {
        int requiredToilet = int.Parse(_eventToiletCount);
        //int requiredWCSink = int.Parse(_eventWCSinkCount);
        //int requiredShower = int.Parse(_eventShowerCount);
        //int requiredKitchenOven = int.Parse(_eventKitchenOvenCount);
        //int requiredKitchenStove = int.Parse(_eventKitchenStoveCount);
        //int requiredKitchenSink = int.Parse(_eventKitchenSinkCount);
        //int requiredKitchenTop = int.Parse(_eventKitchenTopCount);

        int toiletInEvent = _partsInEvent.Where(p => p.Type == PartType.Toilet).Count();
        //int wcSinkInEvent = _partsInEvent.Where(p => p.Type == PartType.WCSink).Count();
        //int showerInEvent = _partsInEvent.Where(p => p.Type == PartType.Shower).Count();
        //int kitchenOvenInEvent = _partsInEvent.Where(p => p.Type == PartType.KitchenOven).Count();
        //int kitchenStoveInEvent = _partsInEvent.Where(p => p.Type == PartType.KitchenStove).Count();
        //int kitchenSinkInEvent = _partsInEvent.Where(p => p.Type == PartType.KitchenSink).Count();
        //int kitchenTopInEvent = _partsInEvent.Where(p => p.Type == PartType.KitchenTop).Count();

        int toiletDif = requiredToilet - toiletInEvent;
        //int wcSinkDif = requiredWCSink - wcSinkInEvent;
        //int showerDif = requiredShower - showerInEvent;
        //int kitchenOvenDif = requiredKitchenOven - kitchenOvenInEvent;
        //int kitchenStoveDif = requiredKitchenStove - kitchenStoveInEvent;
        //int kitchenSinkDif = requiredKitchenSink - kitchenSinkInEvent;
        //int kitchenTopDif = requiredKitchenTop - kitchenTopInEvent;
        

        //THIS SHOULD BE FUNCTION-BASED!
        if (toiletDif > 0)
        {
            List<Part> outsideParts = _existingParts.Where(p => !_partsInEvent.Contains(p) && p.Type == PartType.Toilet).ToList();

            for (int i = 0; i < toiletDif; i++)
            {
                //INSTEAD OF BREAKING, IT SHOULD COLLECT NEW PART FROM 'BASEMENT'
                if (outsideParts.Count < toiletDif) break;
                Part outsidePart = outsideParts[i];
                PPMoveTask t = new PPMoveTask();
                t.tenant = _ai;
                t.Part = outsidePart;
                //t.taskDay = _eventDate; STILL TO IMPLEMENT
                //t.taskTime = _eventTime - 2h; STILL TO IMPLEMENT

                var vacantVoxels = _eventVoxels.Where(v => !v.IsOccupied);
                foreach (Voxel vv in vacantVoxels)
                {
                    if (vv.GetFaceNeighbours().Where(v => !v.IsOccupied && v.IsActive).Count() == 4)
                    {
                        t.TargetVoxel = vv;
                        break;
                    }
                }
                _eventTasks.Add(t);
            }
            print($"Event had {toiletInEvent} toilets, {requiredToilet} were requested. {_eventTasks.Count} Tasks were created to resolve the {toiletDif} difference");
        }
        else if(toiletDif < 0)
        {
            List<Part> insideParts = _partsInEvent.Where(p => _partsInEvent.Contains(p) && p.Type == PartType.Toilet).ToList();
            for (int i = 0; i < -toiletDif; i++)
            {
                Part insidePart = insideParts[i];
                PPMoveTask t = new PPMoveTask();
                t.tenant = _ai;
                t.Part = insidePart;
                //t.taskDay = _eventDate; STILL TO IMPLEMENT
                //t.taskTime = _eventTime - 2h; STILL TO IMPLEMENT

                var vacantVoxels = _grid.ActiveVoxelsAsList().Where(v => !v.IsOccupied);
                foreach (Voxel vv in vacantVoxels)
                {
                    if (vv.GetFaceNeighbours().Where(v => !v.IsOccupied && v.IsActive).Count() == 4)
                    {
                        t.TargetVoxel = vv;
                        break;
                    }
                }
                _eventTasks.Add(t);
            }
            print($"Event had {toiletInEvent} toilets, {requiredToilet} were requested. {_eventTasks.Count} Tasks were created to resolve the {toiletDif} difference");
        }
    } 

    void CalculatePPTaskShortestPath(PPMoveTask mTask)
    {
        var originFaces = _eventTasks.SelectMany(t => t.OriginVoxel.Faces);
        var faces = _grid.GetFaces().Where(f => f.IsClimbable || (originFaces.Contains(f)) && f.IsActive);
        var graphFaces = faces.Select(f => new TaggedEdge<Voxel, Face>(f.Voxels[0], f.Voxels[1], f));
        var graph = graphFaces.ToUndirectedGraph<Voxel, TaggedEdge<Voxel, Face>>();
        

        var start = mTask.OriginVoxel;
        var end = mTask.TargetVoxel;
        var shortest = graph.ShortestPathsDijkstra(e => 1.0, start);
        shortest(end, out var endPath);

        var endPathVoxels = new HashSet<Voxel>(endPath.SelectMany(e => new[] { e.Source, e.Target }));

        var faceMeshes = new List<CombineInstance>();
        foreach (var voxel in _grid.ActiveVoxelsAsList().Where(v => endPathVoxels.Contains(v)))
        {
            float t = 1f;
            if (shortest(voxel, out var path))
            {
                t = path.Count() * 0.04f;
                t = Mathf.Clamp01(t);
            }

            Mesh faceMesh;
            faceMesh = Drawing.MakeFace(voxel.Center + (Vector3.up * 1.1f), Axis.Y, 0.5f, 0, 1);
            faceMeshes.Add(new CombineInstance() { mesh = faceMesh });
        }
        var mesh = new Mesh()
        {
            indexFormat = UnityEngine.Rendering.IndexFormat.UInt32
        };
        mesh.CombineMeshes(faceMeshes.ToArray(), true, false, false);
        _meshes = new[] { mesh };


    }

    void DrawEvent()
    {
        if (_eventVoxels.Count > 0)
        {
            foreach (var voxel in _eventVoxels)
            {
                Drawing.DrawCubeTransparent(voxel.Center + new Vector3(0f,1f,0f), 1f);
            }
        }
    }

    void DrawState()
    {
        for (int x = 0; x < _gridSize.x; x++)
        {
            for (int y = 0; y < _gridSize.y; y++)
            {
                for (int z = 0; z < _gridSize.z; z++)
                {
                    if (_grid.Voxels[x, y, z].IsOccupied)
                    {
                        for (int i = 0; i < 3; i++)
                        {
                            Drawing.DrawCube(_grid.Voxels[x, y, z].Center + new Vector3Int(0, (i + 1), 0), 1f, 1);
                        }

                    }
                    if (_grid.Voxels[x, y, z].IsActive)
                    {
                        Drawing.DrawCube(_grid.Voxels[x, y, z].Center, 1f, 0);
                    }
                }
            }
        }
    }

    void DrawTags()
    {
        if (_drawTags)
        {
            float tagHeight = 4.5f;
            Vector2 tagSize = new Vector2(100, 20);
            foreach (var part in _existingParts)
            {
                string partTag = part.Type.ToString();
                Vector3 tagWorldPos = part.Center + (Vector3.up * tagHeight);

                var t = _cam.WorldToScreenPoint(tagWorldPos);
                Vector2 tagPos = new Vector2(t.x - (tagSize.x / 2), Screen.height - t.y);

                GUI.Box(new Rect(tagPos, tagSize), partTag, "partTag");
            }
        }
    }

    private void OnGUI()
    {
        GUI.skin = _skin;
        GUI.depth = 2;
        int leftPad = 20;
        int topPad = 200;
        int fieldHeight = 25;
        int fieldTitleWidth = 110;
        int textFieldWidth = 125;
        int numberFieldWidth = 125;
        int i = 1;

        //Draw Part tags
        DrawTags();
        //Logo
        GUI.DrawTexture(new Rect(leftPad, -10, 128, 128), Resources.Load<Texture>("Textures/PP_Logo"));

        //Background Transparency
        GUI.Box(new Rect(leftPad, topPad - 75, (fieldTitleWidth*2) + (leftPad*3), (fieldHeight * 25) + 10), Resources.Load<Texture>("Textures/PP_TranspBKG"), "backgroundTile");

        //Title
        GUI.Box(new Rect(180, 30, 500, 25), "AI Event Planner", "title");

        //Setup title
        GUI.Box(new Rect(leftPad, topPad-40, fieldTitleWidth, fieldHeight + 10), "Event Setup", "partsTitle");

        //Event Name field
        GUI.Box(new Rect(leftPad, topPad, fieldTitleWidth, fieldHeight), "Event Name", "fieldTitle");
        _eventName = GUI.TextField(new Rect((leftPad*2) + fieldTitleWidth, topPad, textFieldWidth, fieldHeight), _eventName, 100);

        //Number of People field
        GUI.Box(new Rect(leftPad, topPad + ((fieldHeight+10)*i), fieldTitleWidth, fieldHeight), "Population", "fieldTitle");
        _eventPopulation = GUI.TextField(new Rect((leftPad * 2) + fieldTitleWidth, topPad + ((fieldHeight + 10)*1), numberFieldWidth, fieldHeight), _eventPopulation, 100);

        i += 2;
        //Parts fields
        GUI.Box(new Rect(leftPad, topPad + ((fieldHeight + 10)*i++), fieldTitleWidth, fieldHeight+10), "Parts Required", "partsTitle");

        GUI.Box(new Rect(leftPad, topPad + ((fieldHeight + 10) * i), fieldTitleWidth, fieldHeight), "Toilet #", "fieldTitle");
        _eventToiletCount = GUI.TextField(new Rect((leftPad * 2) + fieldTitleWidth, topPad + ((fieldHeight + 10) * i++), numberFieldWidth, fieldHeight), _eventToiletCount, 100);

        GUI.Box(new Rect(leftPad, topPad + ((fieldHeight + 10) * i), fieldTitleWidth, fieldHeight), "Toilet Sink #", "fieldTitle");
        _eventWCSinkCount = GUI.TextField(new Rect((leftPad * 2) + fieldTitleWidth, topPad + ((fieldHeight + 10) * i++), numberFieldWidth, fieldHeight), _eventWCSinkCount, 100);

        GUI.Box(new Rect(leftPad, topPad + ((fieldHeight + 10) * i), fieldTitleWidth, fieldHeight), "Shower #", "fieldTitle");
        _eventShowerCount = GUI.TextField(new Rect((leftPad * 2) + fieldTitleWidth, topPad + ((fieldHeight + 10) * i++), numberFieldWidth, fieldHeight), _eventShowerCount, 100);

        GUI.Box(new Rect(leftPad, topPad + ((fieldHeight + 10) * i), fieldTitleWidth, fieldHeight), "Oven #", "fieldTitle");
        _eventKitchenOvenCount = GUI.TextField(new Rect((leftPad * 2) + fieldTitleWidth, topPad + ((fieldHeight + 10) * i++), numberFieldWidth, fieldHeight), _eventKitchenOvenCount, 100);

        GUI.Box(new Rect(leftPad, topPad + ((fieldHeight + 10) * i), fieldTitleWidth, fieldHeight), "Stove #", "fieldTitle");
        _eventKitchenStoveCount = GUI.TextField(new Rect((leftPad * 2) + fieldTitleWidth, topPad + ((fieldHeight + 10) * i++), numberFieldWidth, fieldHeight), _eventKitchenStoveCount, 100);

        GUI.Box(new Rect(leftPad, topPad + ((fieldHeight + 10) * i), fieldTitleWidth, fieldHeight), "Kitchen Top #", "fieldTitle");
        _eventKitchenTopCount = GUI.TextField(new Rect((leftPad * 2) + fieldTitleWidth, topPad + ((fieldHeight + 10) * i++), numberFieldWidth, fieldHeight), _eventKitchenTopCount, 100);

        GUI.Box(new Rect(leftPad, topPad + ((fieldHeight + 10) * i), fieldTitleWidth, fieldHeight), "Kitchen Sink #", "fieldTitle");
        _eventKitchenSinkCount = GUI.TextField(new Rect((leftPad * 2) + fieldTitleWidth, topPad + ((fieldHeight + 10) * i++), numberFieldWidth, fieldHeight), _eventKitchenSinkCount, 100);

        //Event pop-up window
        GUI.Window(0, new Rect(Screen.width - leftPad - 300, topPad - 75, 300, (fieldHeight * 25) + 10), PopUpEventWindow, "Your event Tasks");
        
        
        
        
        //Run Button
        if (GUI.Button(new Rect(leftPad, topPad + ((fieldHeight + 10) * i++), (fieldTitleWidth + leftPad + textFieldWidth), fieldHeight), "Generate Event"))
        {
            GenerateEvent();
        }

        //Output Message
        GUI.Box(new Rect(leftPad, (topPad) + ((fieldHeight + 10) * i++), (fieldTitleWidth + leftPad + textFieldWidth), fieldHeight), _outputMessage, "fieldTitle");

    }
    void PopUpEventWindow(int windowID)
    {
        
        GUIStyle style = _skin.GetStyle("taskTitle");
        int leftPad = 10;
        int topPad = 10;
        int fieldWidth = 200;
        int fieldHeight = 20;
        int buttonWidth = 50;
        if (_eventTasks.Count == 0)
        {
            GUI.Box(new Rect(leftPad, topPad, fieldWidth, fieldHeight), "You have no Tasks!", style);
        }
        else
        {
            float cummulativeHeight = topPad;
            for (int i = 0; i < _eventTasks.Count; i++)
            {
                var mTask = _eventTasks[i];
                GUIContent tTitle = new GUIContent();
                tTitle.text = mTask.taskTitle;
                float bHeight = style.CalcHeight(tTitle, fieldWidth);
                
                Rect taskRect = new Rect(leftPad, cummulativeHeight, fieldWidth, bHeight);
                GUI.Box(taskRect, tTitle, style);
                cummulativeHeight += bHeight + (topPad*2);

                Rect pathButton = new Rect(taskRect.xMax + leftPad, taskRect.y, buttonWidth, bHeight);
                if (GUI.Button(pathButton, "Path"))
                {
                    CalculatePPTaskShortestPath(mTask);
                    _drawPaths = true;
                }
            }
        }
    }
}