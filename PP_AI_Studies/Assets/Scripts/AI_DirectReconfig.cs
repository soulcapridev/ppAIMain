using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System.Diagnostics;
using QuickGraph;
using QuickGraph.Algorithms;
using QuickGraph.Algorithms.KernighanLinAlgoritm;

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

    List<Voxel> _boundaries = new List<Voxel>();
    List<Voxel> _spaceable = new List<Voxel>();
    List<Voxel> _visualize = new List<Voxel>();

    

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
        DefineBoundaries_T02();
        //GetSpaceable();
        //GetBoundaries();
        //BruteForceSpaces();

        //RemoveSmallSpaces();
    }

    void Update()
    {
        DrawState();
        if (Input.GetKeyDown(KeyCode.T)) _drawTags = !_drawTags;
        if (Input.GetKeyDown(KeyCode.B)) _drawBoundaries = !_drawBoundaries;
        DrawVisualize();

        //DrawSpaceable();
        //DrawGeneralBoundaries();

        //if (_drawPaths) Drawing.DrawMesh(false, _meshes);
        //Drawing.DrawVoxelColor(_toDraw, _toColor, _voxelSize);

        //if (_drawBoundaries) DrawSpaceBoundaries();
        //else Drawing.DrawSpaces(_toRemove, _grid);  

        //if (_drawBoundaries) DrawSpaceBoundaries();
        //else Drawing.DrawSpaces(_spaces, _grid);
    }

    void DefineBoundaries_T02()
    {
        Stopwatch stopwatch = new Stopwatch();
        stopwatch.Start();

        //List of walkable voxels
        var walkable = _grid.ActiveVoxelsAsList().Where(v => !v.IsOccupied);

        //Algorithm constraints
        int breadthLevels = 10;
        int pathMaximumLength = 8;

        //Paralell lists containing the connected parts and the paths lenghts
        //This is later used to make that only the shortest connection between 2 parts is maintained
        List<Part[]> connectedParts = new List<Part[]>();
        List<HashSet<Voxel>> connectionPaths = new List<HashSet<Voxel>>();
        List<int> connectionLenghts = new List<int>();

        //Iterate through every existing part that is not structural 
        foreach (var part in _existingParts.Where(p => p.Type != PartType.Structure))
        {
            var t1 = part.OccupiedVoxels.First();
            var t2 = part.OccupiedVoxels.Last();

            var origins = new Voxel[] { t1, t2 };

            //BFS (inspired) algorithm, exploring the grid through levels
            foreach (var origin in origins)
            {
                //Queue used to explore levels
                Queue<Voxel> currentLevel = new Queue<Voxel>();
                currentLevel.Enqueue(origin);

                //List to keep track of voxels that have been visited
                List<Voxel> visited = new List<Voxel>();
                List<Voxel> toProcess = new List<Voxel>();
                //List to store the parts that have been found
                List<Part> foundParts = new List<Part>();

                for (int i = 0; i < breadthLevels; i++)
                {
                    Queue<Voxel> nextLevel = new Queue<Voxel>();
                    while (currentLevel.Count > 0)
                    {
                        var voxel = currentLevel.Dequeue();
                        visited.Add(voxel);
                        var neighbours = voxel.GetFaceNeighbours().Where(n => n.IsActive && !visited.Contains(n));
                        foreach (var neighbour in neighbours)
                        {
                            nextLevel.Enqueue(neighbour);
                            if (!toProcess.Contains(neighbour) && neighbour.IsOccupied && neighbour.Part != part && !foundParts.Contains(neighbour.Part))
                            {
                                var foundPart = neighbour.Part;
                                foundParts.Add(foundPart);
                                toProcess.Add(neighbour);
                            }
                        }
                    }
                    currentLevel = nextLevel;
                }
                //Make copy of walkable voxels for this origin voxel
                var localWalkable = new List<Voxel>(walkable);
                localWalkable.Add(origin);

                //Find the closest voxel in the neighbouring parts
                //Add it to the localWalkable list
                List<Voxel> partsTargets = new List<Voxel>();
                foreach (var nPart in foundParts)
                {
                    var nIndices = nPart.OccupiedIndexes;
                    var closestIndex = new Vector3Int();
                    float minDistance = Mathf.Infinity;
                    foreach (var index in nIndices)
                    {
                        var distance = Vector3Int.Distance(origin.Index, index);
                        if (distance < minDistance)
                        {
                            closestIndex = index;
                            minDistance = distance;
                        }
                    }
                    if (closestIndex == null) print("distance measurement failed");
                    var closestVoxel = _grid.Voxels[closestIndex.x, closestIndex.y, closestIndex.z];
                    partsTargets.Add(closestVoxel);
                    localWalkable.Add(closestVoxel);
                }

                //Construct graph with walkable voxels and targets to be processed
                var faces = _grid.GetFaces().Where(f => localWalkable.Contains(f.Voxels[0]) && localWalkable.Contains(f.Voxels[1]));
                var graphFaces = faces.Select(f => new TaggedEdge<Voxel, Face>(f.Voxels[0], f.Voxels[1], f));
                var graph = graphFaces.ToUndirectedGraph<Voxel, TaggedEdge<Voxel, Face>>();

                var start = origin;
                foreach (var v in partsTargets)
                {
                    var end = v;
                    var shortest = graph.ShortestPathsDijkstra(e => 1.0, start);
                    if(shortest(end, out var endPath))
                    {
                        var endPathVoxels = new HashSet<Voxel>(endPath.SelectMany(e => new[] { e.Source, e.Target }));
                        var pathLength = endPathVoxels.Count;
                        if (pathLength <= pathMaximumLength
                            && !endPathVoxels.All(ev => ev.IsOccupied)
                            && endPathVoxels.Count(ev => ev.GetFaceNeighbours().Any(evn => evn.IsOccupied)) > 2
                            && true)
                        {
                            var isUnique = !connectedParts.Any(cp => cp.Contains(part) && cp.Contains(end.Part));
                            if (!isUnique)
                            {
                                print("found duplicated path");
                                var existingConnection = connectedParts.First(cp => cp.Contains(part) && cp.Contains(end.Part));
                                var index = connectedParts.IndexOf(existingConnection);
                                var existingLength = connectionLenghts[index];
                                if (pathLength > existingLength) continue;
                                else
                                {
                                    //Replace existing conection
                                    connectedParts[index] = new Part[] { part, end.Part };
                                    connectionLenghts[index] = pathLength;
                                    connectionPaths[index] = endPathVoxels;  
                                }
                            }
                            else
                            {
                                //Create new connection
                                connectedParts.Add(new Part[] { part, end.Part });
                                connectionLenghts.Add(pathLength);
                                connectionPaths.Add(endPathVoxels);
                            }
                        }
                    }
                }
            }
        }

        foreach (var path in connectionPaths)
        {
            foreach (var voxel in path)
            {
                if(!voxel.IsOccupied && !_visualize.Contains(voxel)) _visualize.Add(voxel);
            }
        }
        stopwatch.Stop();
        print($"Took {stopwatch.ElapsedMilliseconds}ms to Process");
    }





    void DefineBoundaries_T01()
    {
        Stopwatch stopwatch = new Stopwatch();
        stopwatch.Start();

        //List of walkable voxels
        var walkable = _grid.ActiveVoxelsAsList().Where(v => !v.IsOccupied);

        int breadthLevels = 6;
        foreach (var part in _existingParts.Where(p => p.Type != PartType.Structure))
        {
            var t1 = part.OccupiedVoxels.First();
            var t2 = part.OccupiedVoxels.Last();

            var origins = new Voxel[] { t1, t2 };

            //BFS (inspired) algorithm  [THIS SHOULD BE REFACTORED INTO A METHOD]
            foreach (var origin in origins)
            {
                Queue<Voxel> currentLevel = new Queue<Voxel>();
                currentLevel.Enqueue(origin);

                List<Voxel> visited = new List<Voxel>();
                List<Voxel> toProcess = new List<Voxel>();

                for (int i = 0; i < breadthLevels; i++)
                {
                    Queue<Voxel> nextLevel = new Queue<Voxel>();
                    while (currentLevel.Count > 0)
                    {
                        var voxel = currentLevel.Dequeue();
                        visited.Add(voxel);
                        var neighbours = voxel.GetFaceNeighbours().Where(n => n.IsActive && !visited.Contains(n));
                        foreach (var neighbour in neighbours)
                        {
                            nextLevel.Enqueue(neighbour);
                            if(!toProcess.Contains(neighbour) && neighbour.IsOccupied && neighbour.Part != part) toProcess.Add(neighbour);
                        }
                    }
                    currentLevel = nextLevel;
                }
                var localWalkable = new List<Voxel>(walkable);
                localWalkable.Add(origin);
                foreach (var v in toProcess)
                {
                    localWalkable.Add(v);
                    //_visualize.Add(v);
                }
                
                
                //Construct graph with walkable voxels and targets to process
                var faces = _grid.GetFaces().Where(f => /*f.IsClimbable &&*/ localWalkable.Contains(f.Voxels[0]) && localWalkable.Contains(f.Voxels[1]));
                var graphFaces = faces.Select(f => new TaggedEdge<Voxel, Face>(f.Voxels[0], f.Voxels[1], f));
                var graph = graphFaces.ToUndirectedGraph<Voxel, TaggedEdge<Voxel, Face>>();


                var start = origin;
                foreach (var v in toProcess)
                {
                    var end = v;
                    var shortest = graph.ShortestPathsDijkstra(e => 1.0, start);
                    shortest(end, out var endPath);
                    var endPathVoxels = new HashSet<Voxel>(endPath.SelectMany(e => new[] { e.Source, e.Target }));
                    foreach (var ev in endPathVoxels) _visualize.Add(ev);
                    print($"Found path with {endPathVoxels.Count()} voxels");
                }

            }
        }
        stopwatch.Stop();
        print($"Took {stopwatch.ElapsedMilliseconds}ms to Process");
    }

    void GetSpaceable()
    {
        _spaceable = _grid.ActiveVoxelsAsList().Where(v => !v.IsOccupied && !v.InSpace).ToList();
    }

    void GetBoundaries()
    {
        //var boundaryVoxels = _grid.ActiveVoxelsAsList().Where(v => v.IsBoundary);
        var boundaryVoxels = _grid.ActiveVoxelsAsList().Where(v => !v.IsBoundary && !v.IsOccupied);
        _boundaries = boundaryVoxels.ToList();
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
        //YOU ARE BREAKING ME BRO
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
                        //_spaces.Remove(space);
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


    void DrawVisualize()
    {
        foreach (var voxel in _visualize)
        {
            Drawing.DrawCubeTransparent(voxel.Center + new Vector3(0f, _voxelSize, 0f), _voxelSize);
        }
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

    void DrawSpaceable()
    {
        foreach (var voxel in _spaceable)
        {
            Drawing.DrawCubeTransparent(voxel.Center + new Vector3(0f, _voxelSize, 0f), _voxelSize);
        }
    }

    void DrawGeneralBoundaries()
    {
        foreach (var boundaryVoxel in _boundaries)
        {
            Drawing.DrawCubeTransparent(boundaryVoxel.Center + new Vector3(0f, _voxelSize, 0f), _voxelSize);
        }
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

        //Draw Part tags
        DrawTags();
        //Logo
        GUI.DrawTexture(new Rect(leftPad, -10, 128, 128), Resources.Load<Texture>("Textures/PP_Logo"));

        //Title
        GUI.Box(new Rect(180, 30, 500, 25), "AI Plan Analyser", "title");
    }
}