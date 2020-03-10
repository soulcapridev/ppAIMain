using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System.Diagnostics;
using QuickGraph;
using QuickGraph.Algorithms;

public class AI_DirectReconfig : MonoBehaviour
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


    VoxelGrid _grid;
    Vector3Int _gridSize = new Vector3Int(40, 1, 82);
    float _voxelSize = 0.5f;

    List<Part> _existingParts = new List<Part>();
    List<Part> _partsInEvent = new List<Part>();
    List<PPMoveTask> _eventTasks = new List<PPMoveTask>();

    List<PPSpace> _spaces = new List<PPSpace>();
    List<PPSpace> _isolated = new List<PPSpace>();
    List<PPSpace> _toRemove = new List<PPSpace>();
    List<PPSpace> _clean = new List<PPSpace>();

    //List<Voxel> _toDraw = new List<Voxel>();
    //List<Color> _toColor = new List<Color>();

    bool _drawTags = false;
    bool _drawPaths = false;
    bool _drawBoundaries = false;

    Mesh[] _meshes;

    void Start()
    {
        _ai.name = "PublicParts_AI";
        _cam = Camera.main;

        _cameraPivot.position = new Vector3(_gridSize.x / 2, _gridSize.y / 2, _gridSize.z / 2) * _voxelSize;
        _grid = new VoxelGrid(_gridSize, _voxelSize, Vector3.zero);
        CSVReader.SetGridState(_grid, "Input Data/FloorLayout");
        _existingParts = JSONReader.ReadPartsAsList(_grid, "Input Data/StructureParts");

        PopulateRandomConfigurable(50);
        BruteForceSpaces();

        //RemoveSmallSpaces();
    }

    void Update()
    {
        DrawState();
        if (Input.GetKeyDown(KeyCode.T)) _drawTags = !_drawTags;
        if (Input.GetKeyDown(KeyCode.B)) _drawBoundaries = !_drawBoundaries;

        if (_drawPaths) Drawing.DrawMesh(false, _meshes);
        //Drawing.DrawVoxelColor(_toDraw, _toColor, _voxelSize);

        //if (_drawBoundaries) DrawSpaceBoundaries();
        //else Drawing.DrawSpaces(_toRemove, _grid);  

        if (_drawBoundaries) DrawSpaceBoundaries();
        else Drawing.DrawSpaces(_spaces, _grid);
    }

    //IEnumerator SaveScreenshot()
    //{
    //    string file = $"SavedFrames/DirectReconfig/Frame_{_day}.png";
    //    ScreenCapture.CaptureScreenshot(file);
    //    _day++;
    //    yield return new WaitForEndOfFrame();
    //}



    //IEnumerator AnimateGeneration()
    //{
    //    var flatVoxels = _spaces.SelectMany(v => v.Voxels).ToList();
    //    var flatParents = flatVoxels.Select(v => v.ParentSpace).ToList();


    //    for (int i = 0; i < flatVoxels.Count; i++)
    //    {
    //        var voxel = flatVoxels[i];
    //        var parent = flatParents[i].Voxels.Sum(v => v.Index.x);

    //        Random.InitState(parent);
    //        float r = Random.value;

    //        Random.InitState(2 * parent);
    //        float g = Random.value;

    //        Random.InitState(3 * parent);
    //        float b = Random.value;
    //        var color = new Color(r, g, b, 0.70f);

    //        _toDraw.Add(voxel);
    //        _toColor.Add(color);
    //        yield return new WaitForSeconds(0.01f);
    //    }   
    //}


    void RemoveSmallSpaces()
    {
        // NEEDS WORK ON IT! DETECTION IS OK, MODIFICATION IS NOT
        _clean = new List<PPSpace>(_spaces);
        int count = 0;
        int minimumArea = 36; //In Voxel units

        for (int i = 1; i < minimumArea; i++)
        {
            for (int a = 0; a < _spaces.Count; a++)
            {
                var space = _spaces[a];

                if (space.Voxels.Count == i)
                {
                    var index = _spaces.IndexOf(space);
                    //This removes / ignores isolated rooms. NEEDS REFACTORING
                    var isConnected = space.BoundaryVoxels.Where(v => v.GetFaceNeighbours().Any(vv => vv.IsActive 
                    && !vv.IsOccupied 
                    && vv.ParentSpace != null 
                    && vv.ParentSpace != space)).Any();

                    if (!isConnected)
                    {
                        print("isolated found");
                        _isolated.Add(space);
                        _spaces.Remove(space);
                        a--;
                        continue;
                    }

                    var tempVoxels = new List<Voxel>(space.Voxels);
                    foreach (var voxel in tempVoxels)
                    {
                        var smallestNeighbour = voxel.GetFaceNeighbours().
                            Where(n => n.IsActive && n.ParentSpace != null).
                            Select(s => s.ParentSpace).
                            MinBy(m => m.Voxels.Count);

                        voxel.MoveToSpace(smallestNeighbour);
                    }
                    a--;
                    _spaces.RemoveAt(index);
                    _clean.Remove(space);
                    _toRemove.Add(space);
                    count++;
                }
            }
        }
        print($"{count} spaces removed");
        
    }


    void BruteForceSpaces()
    {
        Stopwatch stopwatch = new Stopwatch();
        stopwatch.Start();
        while (_grid.ActiveVoxelsAsList().Any(v => !v.IsOccupied && !v.InSpace))
        {
            GenerateSpace();
        }
        stopwatch.Stop();
        print($"Took {stopwatch.ElapsedMilliseconds}ms to Generate {_spaces.Count} Spaces");
    }

  

    void GenerateSpace()
    {
        
        int minimumArea = 100; //in voxel ammount
        var availableVoxels = _grid.ActiveVoxelsAsList().Where(v => !v.IsOccupied && !v.InSpace).ToList();
        if (availableVoxels.Count == 0) return;
        Voxel originVoxel = availableVoxels[Random.Range(0, availableVoxels.Count)];

        PPSpace space = new PPSpace();
        originVoxel.InSpace = true;
        originVoxel.ParentSpace = space;

        space.Voxels.Add(originVoxel);

        while (space.Voxels.Count < minimumArea)
        {
            List<Voxel> temp = new List<Voxel>();
            foreach (var voxel in space.Voxels)
            {
                var newNeighbours = voxel.GetFaceNeighbours();
                foreach (var neighbour in newNeighbours)
                {
                    var nIndex = neighbour.Index;
                    var gridVoxel = _grid.Voxels[nIndex.x, nIndex.y, nIndex.z];
                    if (!space.Voxels.Contains(neighbour) && !temp.Contains(neighbour))
                    {
                        if (gridVoxel.IsActive && !gridVoxel.IsOccupied && !gridVoxel.InSpace) temp.Add(neighbour);

                    }
                }
            }
            if (temp.Count == 0) break;

            foreach (var v in temp)
            {
                if (space.Voxels.Count <= minimumArea)
                {
                    v.InSpace = true;
                    v.ParentSpace = space;
                    space.Voxels.Add(v);
                }
            }   
        }
        _spaces.Add(space);        
    }

    void DrawSpaceBoundaries()
    {
        foreach (var space in _spaces)
        {
            if (space.Voxels.Count > 0)
            {
                foreach (var boundaryVoxel in space.BoundaryVoxels)
                {
                    Drawing.DrawCubeTransparent(boundaryVoxel.Center + new Vector3(0f, _voxelSize, 0f), _voxelSize);
                }
            }

        }
        
    }

    void PopulateRandomConfigurable(int amt)
    {
        for (int i = 0; i < amt; i++)
        {
            Part p = new Part();
            p.NewRandomConfigurable(_grid, _existingParts);
            _existingParts.Add(p);
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
                        for (int i = 0; i < 6; i++)
                        {
                            Drawing.DrawCube(_grid.Voxels[x, y, z].Center + new Vector3(0, (i + 1)* _voxelSize, 0), _grid.VoxelSize, 1);
                        }

                    }
                    if (_grid.Voxels[x, y, z].IsActive)
                    {
                        Drawing.DrawCube(_grid.Voxels[x, y, z].Center, _grid.VoxelSize, 0);
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
        //GUI.Box(new Rect(leftPad, topPad - 75, (fieldTitleWidth * 2) + (leftPad * 3), (fieldHeight * 25) + 10), Resources.Load<Texture>("Textures/PP_TranspBKG"), "backgroundTile");

        //Title
        GUI.Box(new Rect(180, 30, 500, 25), "AI Plan Analyser", "title");

        //Setup title
        //GUI.Box(new Rect(leftPad, topPad - 40, fieldTitleWidth, fieldHeight + 10), "Event Setup", "partsTitle");

        //Event Name field
        //GUI.Box(new Rect(leftPad, topPad, fieldTitleWidth, fieldHeight), "Event Name", "fieldTitle");
        //_eventName = GUI.TextField(new Rect((leftPad * 2) + fieldTitleWidth, topPad, textFieldWidth, fieldHeight), _eventName, 100);

        //Number of People field
        //GUI.Box(new Rect(leftPad, topPad + ((fieldHeight + 10) * i), fieldTitleWidth, fieldHeight), "Population", "fieldTitle");
        //_eventPopulation = GUI.TextField(new Rect((leftPad * 2) + fieldTitleWidth, topPad + ((fieldHeight + 10) * 1), numberFieldWidth, fieldHeight), _eventPopulation, 100);

        //i += 2;
        //Parts fields
        //GUI.Box(new Rect(leftPad, topPad + ((fieldHeight + 10) * i++), fieldTitleWidth, fieldHeight + 10), "Parts Required", "partsTitle");

        //GUI.Box(new Rect(leftPad, topPad + ((fieldHeight + 10) * i), fieldTitleWidth, fieldHeight), "Toilet #", "fieldTitle");
        //_eventToiletCount = GUI.TextField(new Rect((leftPad * 2) + fieldTitleWidth, topPad + ((fieldHeight + 10) * i++), numberFieldWidth, fieldHeight), _eventToiletCount, 100);

        //GUI.Box(new Rect(leftPad, topPad + ((fieldHeight + 10) * i), fieldTitleWidth, fieldHeight), "Toilet Sink #", "fieldTitle");
        //_eventWCSinkCount = GUI.TextField(new Rect((leftPad * 2) + fieldTitleWidth, topPad + ((fieldHeight + 10) * i++), numberFieldWidth, fieldHeight), _eventWCSinkCount, 100);

        //GUI.Box(new Rect(leftPad, topPad + ((fieldHeight + 10) * i), fieldTitleWidth, fieldHeight), "Shower #", "fieldTitle");
        //_eventShowerCount = GUI.TextField(new Rect((leftPad * 2) + fieldTitleWidth, topPad + ((fieldHeight + 10) * i++), numberFieldWidth, fieldHeight), _eventShowerCount, 100);

        //GUI.Box(new Rect(leftPad, topPad + ((fieldHeight + 10) * i), fieldTitleWidth, fieldHeight), "Oven #", "fieldTitle");
        //_eventKitchenOvenCount = GUI.TextField(new Rect((leftPad * 2) + fieldTitleWidth, topPad + ((fieldHeight + 10) * i++), numberFieldWidth, fieldHeight), _eventKitchenOvenCount, 100);

        //GUI.Box(new Rect(leftPad, topPad + ((fieldHeight + 10) * i), fieldTitleWidth, fieldHeight), "Stove #", "fieldTitle");
        //_eventKitchenStoveCount = GUI.TextField(new Rect((leftPad * 2) + fieldTitleWidth, topPad + ((fieldHeight + 10) * i++), numberFieldWidth, fieldHeight), _eventKitchenStoveCount, 100);

        //GUI.Box(new Rect(leftPad, topPad + ((fieldHeight + 10) * i), fieldTitleWidth, fieldHeight), "Kitchen Top #", "fieldTitle");
        //_eventKitchenTopCount = GUI.TextField(new Rect((leftPad * 2) + fieldTitleWidth, topPad + ((fieldHeight + 10) * i++), numberFieldWidth, fieldHeight), _eventKitchenTopCount, 100);

        //GUI.Box(new Rect(leftPad, topPad + ((fieldHeight + 10) * i), fieldTitleWidth, fieldHeight), "Kitchen Sink #", "fieldTitle");
        //_eventKitchenSinkCount = GUI.TextField(new Rect((leftPad * 2) + fieldTitleWidth, topPad + ((fieldHeight + 10) * i++), numberFieldWidth, fieldHeight), _eventKitchenSinkCount, 100);

        //Event pop-up window
        //GUI.Window(0, new Rect(Screen.width - leftPad - 300, topPad - 75, 300, (fieldHeight * 25) + 10), PopUpEventWindow, "Your event Tasks");




        //Run Button
        //if (GUI.Button(new Rect(leftPad, topPad + ((fieldHeight + 10) * i++), (fieldTitleWidth + leftPad + textFieldWidth), fieldHeight), "Generate Event"))
        //{
        //    //GenerateEvent();
        //}

        //Output Message
        //GUI.Box(new Rect(leftPad, (topPad) + ((fieldHeight + 10) * i++), (fieldTitleWidth + leftPad + textFieldWidth), fieldHeight), _outputMessage, "fieldTitle");

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
                cummulativeHeight += bHeight + (topPad * 2);

                Rect pathButton = new Rect(taskRect.xMax + leftPad, taskRect.y, buttonWidth, bHeight);
                if (GUI.Button(pathButton, "Path"))
                {
                    //CalculatePPTaskShortestPath(mTask);
                    _drawPaths = true;
                }
            }
        }
    }
}