using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System.Diagnostics;
using QuickGraph;
using QuickGraph.Algorithms;

public class AI_PlanAnalyzer : MonoBehaviour
{
    [SerializeField] GUISkin _skin;
    [SerializeField] Transform _cameraPivot;
    Camera _cam;
    VoxelGrid _grid;
    bool _bigGrid;
    string _slabFile;
    string _structureFile;
    Vector3Int _gridSize;

    bool _testFloor = true;
    
    float _voxelSize = 0.5f;
    int _spaceMinimumArea = 20; //in voxel ammount
    int _ammountOfComponents = 10;
    int _day = 0;

    List<Part> _existingParts = new List<Part>();
    List<PPSpace> _spaces = new List<PPSpace>();
    List<Voxel> _partsBoundaries = new List<Voxel>();
    List<Voxel> _toDraw = new List<Voxel>();
    List<Color> _toColor = new List<Color>();

    bool _drawTags = false;
    bool _populated = false;
    bool _analyzed = false;

    string _outputMessage;

    //Debugging
    bool _showDebug = true;
    string _debugMessage;
    string[] _compiledMessage = new string[2];

    List<Voxel[]> _origins = new List<Voxel[]>();
    List<PartType> _foundParts = new List<PartType>();
    List<Part[]> _foundPairs = new List<Part[]>();
    List<Voxel[]> _prospectivePairs = new List<Voxel[]>();
    List<Voxel> _usedWalkables = new List<Voxel>();
    List<Voxel> _usedTargets = new List<Voxel>();
    List<Voxel> _boudaryVoxels = new List<Voxel>();

    bool _showTime;
    bool _showRawBoundaries = true;
    bool _showSpaces = true;

    int _boundaryMainTime;
    int _boundaryGraphTime;
    int _boundaryPartsTime;
    int _singleSpaceTime;
    int _allSpacesTime;

    void Start()
    {
        _cam = Camera.main;

        if (!_testFloor)
        {
            if (_bigGrid)
            {
                _gridSize = new Vector3Int(40, 1, 82);
                _grid = new VoxelGrid(_gridSize, _voxelSize, Vector3.zero);
                _slabFile = "Input Data/BigSlab";
                _structureFile = "StructureParts_BigSlab";
            }
            else
            {
                _gridSize = new Vector3Int(20, 1, 45);
                _grid = new VoxelGrid(_gridSize, _voxelSize, Vector3.zero);
                _slabFile = "Input Data/SmallSlab";
                _structureFile = "Input Data/StructureParts_SmallSlab";
            }
        }
        else
        {
            _gridSize = new Vector3Int(20, 1, 45);
            _grid = new VoxelGrid(_gridSize, _voxelSize, Vector3.zero);
            _slabFile = "Input Data/TestingData/SlabStates";
            _structureFile = "Input Data/TestingData/Structure";
            string configurablesFile = "Input Data/TestingData/Configurables";
            string spacesFile = "Input Data/TestingData/Spaces";
            ReadConfigurables(configurablesFile);
            ReadSpaces(spacesFile);
        }
        
        
        _cameraPivot.position = new Vector3(_gridSize.x / 2, _gridSize.y / 2, _gridSize.z / 2) * _voxelSize;
        
        //Read CSV to create the floor
        CSVReader.SetGridState(_grid, _slabFile);
        
        //Read JSON to create structural parts
        ReadStructure(_structureFile);
    }

    void Update()
    {
        DrawState();

        DrawOrigins();
        //DrawVoxelList(_usedWalkables);
        //DrawVoxelList(_usedTargets);
        //DrawVoxelList(_boudaryVoxels);
        //DrawConnections();
        //DrawProspective();

        if (_showRawBoundaries)
        {
            DrawPartsBoundaries();
        }

        if (_showSpaces)
        {
            DrawSpaces();
        }

        //Use T to toggle the visibility of the components type tags
        if (Input.GetKeyDown(KeyCode.T)) _drawTags = !_drawTags;

        //Use D to toggle the visibility of the Debug Window
        if (Input.GetKeyDown(KeyCode.D)) _showDebug = !_showDebug;

        Drawing.DrawVoxelColor(_toDraw, _toColor, _voxelSize);
        //StartCoroutine(SaveScreenshot());
    }

    void DefinePartsBoundaries()
    {
        Stopwatch mainStopwatch = new Stopwatch();
        mainStopwatch.Start();

        int partsProcessing = 0;
        int graphProcessing = 0;

        //Algorithm constraints
        int searchRadius = 15;
        int maximumPathLength = 15;

        //Paralell lists containing the connected parts and the paths lenghts
        //This is later used to make sure that only the shortest connection between 2 parts is maintained
        List<Part[]> connectedParts = new List<Part[]>();
        List<HashSet<Voxel>> connectionPaths = new List<HashSet<Voxel>>();
        List<int> connectionLenghts = new List<int>();

        //Iterate through every existing part that is not structural 
        foreach (var part in _existingParts.Where(p => p.Type != PartType.Structure))
        {
            Stopwatch partStopwatch = new Stopwatch();
            partStopwatch.Start();
            var t1 = part.OccupiedVoxels.First();
            var t2 = part.OccupiedVoxels.Last();

            var origins = new Voxel[] { t1, t2 };
            _origins.Add(origins);

            //Finding the neighbouring parts in a given radius from a voxel
            foreach (var origin in origins)
            {
                //List to store the parts that have been found
                List<Part> foundParts = new List<Part>();
                List<Voxel> foundBoudaryVoxels = new List<Voxel>();

                //Navigate through the neighbours in a given range
                var neighbours = origin.GetNeighboursInRange(searchRadius);
                foreach (var neighbour in neighbours)
                {
                    if (neighbour.IsOccupied && neighbour.Part != part && !foundParts.Contains(neighbour.Part))
                    {
                        foundParts.Add(neighbour.Part);
                        _foundParts.Add(neighbour.Part.Type);
                    }
                    else if (neighbour.IsBoundary) foundBoudaryVoxels.Add(neighbour);
                }
                
                //var searchRange = neighbours.Where(n => !n.IsOccupied).ToList();
                var searchRange = _grid.ActiveVoxelsAsList().Where(n => !n.IsOccupied).ToList();
                searchRange.Add(origin);

                //Make copy of walkable voxels for this origin voxel
                var localWalkable = new List<Voxel>(searchRange);

                //Find the closest voxel in the neighbouring parts
                //Add it to the localWalkable list
                List<Voxel> targets = new List<Voxel>();
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
                    var closestVoxel = _grid.Voxels[closestIndex.x, closestIndex.y, closestIndex.z];
                    localWalkable.Add(closestVoxel);
                    targets.Add(closestVoxel);
                }

                //Find the closest boundary voxel -> this is broken!
                //var b_closestIndex = new Vector3Int();
                //float b_minDistance = Mathf.Infinity;
                //foreach (var voxel in foundBoudaryVoxels)
                //{
                //    var distance = Vector3Int.Distance(origin.Index, voxel.Index);
                //    if (distance < b_minDistance)
                //    {
                //        b_closestIndex = voxel.Index;
                //        b_minDistance = distance;
                //    }
                //}
                //var b_closestVoxel = _grid.Voxels[b_closestIndex.x, b_closestIndex.y, b_closestIndex.z];
                //localWalkable.Add(b_closestVoxel);
                //targets.Add(b_closestVoxel);

                foreach (var voxel in foundBoudaryVoxels)
                {
                    //this will add all found boudary voxels to the targets
                    //More processing but closer to defining actual boudaries 
                    targets.Add(voxel);
                }

                partStopwatch.Stop();
                partsProcessing += (int)partStopwatch.ElapsedMilliseconds;

                foreach (var item in targets)
                {
                    _usedTargets.Add(item);
                }

                //Construct graph with walkable voxels and targets to be processed
                Stopwatch graphStopwatch = new Stopwatch();
                graphStopwatch.Start();
                var faces = _grid.GetFaces().Where(f => localWalkable.Contains(f.Voxels[0]) && localWalkable.Contains(f.Voxels[1]));
                var graphFaces = faces.Select(f => new TaggedEdge<Voxel, Face>(f.Voxels[0], f.Voxels[1], f));
                var start = origin;

                //var graph = graphFaces.ToBidirectionalGraph<Voxel, TaggedEdge<Voxel, Face>>();
                //var shortest = graph.ShortestPathsAStar(e => 1.0, v => VoxelDistance(v, start), start);
                var graph = graphFaces.ToUndirectedGraph<Voxel, TaggedEdge<Voxel, Face>>();
                var shortest = graph.ShortestPathsDijkstra(e => 1.0, start);

                HashSet<Voxel> closest2boudary = new HashSet<Voxel>();
                int shortestLength = 1_000_000;
                foreach (var v in targets)
                {
                    var end = v;
                    if (!end.IsBoundary)
                    {
                        //Check if the shortest path is valid
                        if (shortest(end, out var endPath))
                        {
                            Voxel[] pair = new Voxel[] { origin, end };
                            _prospectivePairs.Add(pair);

                            var endPathVoxels = new HashSet<Voxel>(endPath.SelectMany(e => new[] { e.Source, e.Target }));
                            var pathLength = endPathVoxels.Count;
                            //Check if path length is under minimum
                            if (pathLength <= maximumPathLength
                                && !endPathVoxels.All(ev => ev.IsOccupied)
                                && endPathVoxels.Count(ev => ev.GetFaceNeighbours().Any(evn => evn.IsOccupied)) > 2)
                            {
                                //Check if the connection between the parts is unique
                                var isUnique = !connectedParts.Any(cp => cp.Contains(part) && cp.Contains(end.Part));
                                if (!isUnique)
                                {
                                    //If it isn't unique, only replace if the current length is smaller
                                    var existingConnection = connectedParts.First(cp => cp.Contains(part) && cp.Contains(end.Part));
                                    var index = connectedParts.IndexOf(existingConnection);
                                    var existingLength = connectionLenghts[index];
                                    if (pathLength > existingLength) continue;
                                    else
                                    {
                                        //Replace existing conection pair
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
                    else
                    {
                        
                        if (shortest(end, out var endPath))
                        {
                            var endPathVoxels = new HashSet<Voxel>(endPath.SelectMany(e => new[] { e.Source, e.Target }));
                            var pathLength = endPathVoxels.Count;
                            if (pathLength <= maximumPathLength
                                && endPathVoxels.Count(ev => ev.GetFaceNeighbours().Any(evn => evn.IsOccupied)) > 2)
                            {
                                if (pathLength < shortestLength)
                                {
                                    closest2boudary = endPathVoxels;
                                    shortestLength = pathLength;
                                }
                                //connectionPaths.Add(endPathVoxels);
                            }
                        }
                    }
                    
                }
                if (closest2boudary.Count > 0) connectionPaths.Add(closest2boudary);
                graphStopwatch.Stop();
                graphProcessing += (int)graphStopwatch.ElapsedMilliseconds;
            }
        }
        //Feed the general boundaries list
        foreach (var path in connectionPaths)
        {
            foreach (var voxel in path)
            {
                if (!voxel.IsOccupied
                    && !_partsBoundaries.Contains(voxel)) _partsBoundaries.Add(voxel);
            }
        }
        mainStopwatch.Stop();
        int mainProcessing = (int) mainStopwatch.ElapsedMilliseconds;
        //print($"Took {mainStopwatch.ElapsedMilliseconds}ms to Process");
        //print($"Took {partsProcessing}ms to Process Parts");
        //print($"Took {graphProcessing}ms to Process Graphs");

        _boundaryPartsTime = partsProcessing;
        _boundaryGraphTime = graphProcessing;
        _boundaryMainTime = mainProcessing;
        
        foreach (var t in connectedParts)
        {
            _foundPairs.Add(t);
        }
    }

    void GenerateSingleSpace()
    {
        //Generate spaces on the voxels that are not inside the parts boudaries, or space or part
        //The method is inspired by a BFS algorithm, continuously checking the neighbours of the
        //processed voxels until the minimum area is reached

        Stopwatch singleSpace = new Stopwatch();
        singleSpace.Start();
        int maximumArea = 1000; //in voxel ammount
        var availableVoxels = _grid.ActiveVoxelsAsList().Where(v => !_partsBoundaries.Contains(v) && !v.IsOccupied && !v.InSpace).ToList();
        if (availableVoxels.Count == 0) return;
        Voxel originVoxel = availableVoxels[0];
        
        //Initiate a new space
        PPSpace space = new PPSpace();
        originVoxel.InSpace = true;
        originVoxel.ParentSpace = space;
        space.Voxels.Add(originVoxel);
        //Keep running until the space area is under the minimum
        while (space.Voxels.Count < maximumArea)
        {
            List<Voxel> temp = new List<Voxel>();
            foreach (var voxel in space.Voxels)
            {
                //Get the face neighbours which are available
                var newNeighbours = voxel.GetFaceNeighbours().Where(n => availableVoxels.Contains(n));
                foreach (var neighbour in newNeighbours)
                {
                    var nIndex = neighbour.Index;
                    var gridVoxel = _grid.Voxels[nIndex.x, nIndex.y, nIndex.z];
                    //Only add the nighbour it its not already in the space 
                    //or temp list, is active, not occupied(in a part), or another space
                    if (!space.Voxels.Contains(neighbour) && !temp.Contains(neighbour))
                    {
                        if (gridVoxel.IsActive && !gridVoxel.IsOccupied && !gridVoxel.InSpace) temp.Add(neighbour);

                    }
                }
            }
            //Break if the temp list returned empty
            if (temp.Count == 0) break;
            //Add found neighbours to the space until it reaches maximum capacity
            foreach (var v in temp)
            {
                if (space.Voxels.Count <= maximumArea)
                {
                    v.InSpace = true;
                    v.ParentSpace = space;
                    space.Voxels.Add(v);
                }
            }
        }
        _spaces.Add(space);
        singleSpace.Stop();
        _singleSpaceTime = (int)singleSpace.ElapsedMilliseconds;
    }

    void GenerateSpaces()
    {
        Stopwatch stopwatch = new Stopwatch();
        stopwatch.Start();
        //Generate spaces on vacant voxels inside boundaries
        while (_grid.ActiveVoxelsAsList().Any(v => !_partsBoundaries.Contains(v) && !v.IsOccupied && !v.InSpace))
        {
            GenerateSingleSpace();
        }
        
        
        //Allocate boundary voxel to the smallest neighbouring space
        while (_partsBoundaries.Any(b => !b.InSpace))
        {
            Voxels2SmallestNeighbour(_partsBoundaries.Where(b => !b.InSpace));
        }
        

        //Destroy the spaces that are too small 
        Queue<Voxel>  orphanVoxels = new Queue<Voxel>();
        foreach (var space in _spaces)
        {
            if (space.Voxels.Count < _spaceMinimumArea)
            {
                var spaceOrphans = space.DestroySpace();
                foreach (var voxel in spaceOrphans)
                {
                    orphanVoxels.Enqueue(voxel);
                }
            } 
        }
        //Remove empty spaces from main list of spaces
        _spaces = _spaces.Where(s => s.Voxels.Count != 0).ToList();

        stopwatch.Stop();
        _allSpacesTime = (int)stopwatch.ElapsedMilliseconds;
        return;


        while (orphanVoxels.Count > 0)
        {
            //Get first orphan
            var orphan = orphanVoxels.Dequeue();
            //Get its neighbours
            var neighbours = orphan.GetFaceNeighbours();
            //Check if any of the neighbours is a space
            if (neighbours.Any(n => n.InSpace))
            {
                //Get the closest smallest space
                var closestSpace = neighbours.Where(v => v.InSpace).MinBy(s => s.ParentSpace.Voxels.Count).ParentSpace;
                //Check, for safety, if the space is valid
                if (closestSpace != null)
                {
                    closestSpace.Voxels.Add(orphan);
                    orphan.ParentSpace = closestSpace;
                    orphan.InSpace = true;
                }
            }
            else
            {
                //If it doesn't have a space as neighbour, add to the back of the queue
                orphanVoxels.Enqueue(orphan);
            }
        }
        
        //print($"Took {stopwatch.ElapsedMilliseconds}ms to Generate {_spaces.Count} Spaces");
    }

    void Voxels2SmallestNeighbour(IEnumerable<Voxel> voxels2Allocate)
    {
        //This method tries to allocate the voxels in a list 
        //to the smallest neighbouring space
        var boundaryNonAllocated = voxels2Allocate;
        foreach (var voxel in boundaryNonAllocated)
        {
            var neighbours = voxel.GetFaceNeighbours();
            if (neighbours.Any(v => v.InSpace))
            {
                var closestSpace = neighbours.Where(v => v.InSpace).MinBy(s => s.ParentSpace.Voxels.Count).ParentSpace;
                if (closestSpace != null)
                {
                    closestSpace.Voxels.Add(voxel);
                    voxel.ParentSpace = closestSpace;
                    voxel.InSpace = true;
                }
            }
        }
    }

    void ReadStructure(string file)
    {
        var newParts = JSONReader.ReadStructureAsList(_grid, file);
        foreach (var item in newParts)
        {
            _existingParts.Add(item);
        }
    }

    void ReadConfigurables(string file)
    {
        var newParts = JSONReader.ReadConfigurablesAsList(_grid, file);
        foreach (var item in newParts)
        {
            _existingParts.Add(item);
        }
    }

    void ReadSpaces(string file)
    {
        var newParts = JSONReader.ReadSpacesAsList(_grid, file);
        foreach (var item in newParts)
        {
            _spaces.Add(item);
        }
    }

    void CountStructure()
    {
        var n = _existingParts.Count(p => p.Type == PartType.Structure);
        var nV = _existingParts.Where(p => p.Type == PartType.Structure).Select(st => st.OccupiedIndexes.Length);
        print($"{n} Structural Parts");
        foreach (var st in nV)
        {
            print($"Part with {st} voxels");
        }
    }

    void PopulateRandomConfigurable(int amt)
    {
        for (int i = 0; i < amt; i++)
        {
            ConfigurablePart p = new ConfigurablePart(_grid, _existingParts);
            _existingParts.Add(p);
        }
    }

    double VoxelDistance(Voxel s, Voxel t)
    {
        var dif = s.Center - t.Center;
        double distance = dif.sqrMagnitude;
        return distance;
    }

    IEnumerator AnimateGeneration()
    {
        var flatVoxels = _spaces.SelectMany(v => v.Voxels).ToList();
        var flatParents = flatVoxels.Select(v => v.ParentSpace).ToList();

        for (int i = 0; i < flatVoxels.Count; i++)
        {
            var voxel = flatVoxels[i];
            var parent = flatParents[i].Voxels.Sum(v => v.Index.x);

            Random.InitState(parent);
            float r = Random.value;

            Random.InitState(2 * parent);
            float g = Random.value;

            Random.InitState(3 * parent);
            float b = Random.value;
            var color = new Color(r, g, b, 0.70f);

            _toDraw.Add(voxel);
            _toColor.Add(color);
            yield return new WaitForSeconds(0.005f);
        }
    }

    IEnumerator SaveScreenshot()
    {
        string file = $"SavedFrames/SpaceAnalysis/Frame_{_day}.png";
        ScreenCapture.CaptureScreenshot(file, 2);
        _day++;
        yield return new WaitForEndOfFrame();
    }

    //Drawing

    void DrawConnections()
    {
        foreach (var pair in _foundPairs)
        {
            Vector3 height = new Vector3(0, 8f, 0) * _voxelSize;
            Drawing.DrawBar(pair[0].Center + height, pair[1].Center + height, 0.1f, 1);
        }
    }

    void DrawProspective()
    {
        foreach (var pair in _prospectivePairs)
        {
            Vector3 height = new Vector3(0, 8f, 0) * _voxelSize;
            Drawing.DrawBar(pair[0].Center + height, pair[1].Center + height, 0.1f, 1);
        }
    }

    void DrawSpaces()
    {
        Drawing.DrawSpaces(_spaces, _grid);
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
                            var voxel = _grid.Voxels[x, y, z];
                            if (voxel.Part.Type == PartType.Configurable)
                            {
                                Drawing.DrawConfigurable(_grid.Voxels[x, y, z].Center + new Vector3(0, (i + 1) * _voxelSize, 0), _grid.VoxelSize, 1);
                            }
                            else
                            {
                                Drawing.DrawCube(_grid.Voxels[x, y, z].Center + new Vector3(0, (i + 1) * _voxelSize, 0), _grid.VoxelSize, 1);
                            }
                            
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
    void DrawWalkable()
    {
        foreach (var voxel in _usedWalkables)
        {
            Drawing.DrawCube(voxel.Center, _grid.VoxelSize, 0.25f);
        }
    }

    void DrawVoxelList(List<Voxel> input)
    {
        foreach (var voxel in input)
        {
            Drawing.DrawCube(voxel.Center + new Vector3(0,0.5f,0), _grid.VoxelSize, 0.25f);
        }
    }

    void DrawPartsBoundaries()
    {
        foreach (var voxel in _partsBoundaries)
        {
            Drawing.DrawCubeTransparent(voxel.Center + new Vector3(0f, _voxelSize, 0f), _voxelSize);
        }
    }

    void DrawOrigins()
    {
        foreach (var origins in _origins)
        {
            foreach (var voxel in origins)
            {
                Drawing.DrawCube(voxel.Center + new Vector3(0, 7 * _voxelSize, 0), _grid.VoxelSize, 1);
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
        int i = 1;

        //Draw Part tags
        DrawTags();
        //Logo
        GUI.DrawTexture(new Rect(leftPad, -10, 128, 128), Resources.Load<Texture>("Textures/PP_Logo"));

        //Background Transparency
        GUI.Box(new Rect(leftPad, topPad - 75, (fieldTitleWidth * 2) + (leftPad * 3), (fieldHeight * 25) + 10), Resources.Load<Texture>("Textures/PP_TranspBKG"), "backgroundTile");

        //Setup title
        GUI.Box(new Rect(leftPad, topPad - 40, fieldTitleWidth, fieldHeight + 10), "Control Panel", "partsTitle");

        //Title
        GUI.Box(new Rect(180, 30, 500, 25), "AI Plan Analyser", "title");

        if (!_testFloor)
        {
            //Output message to be displayed out of test mode
            _outputMessage = "Adjust the slider to define how many parts should be created." + 
                '\n' + "\nPress Populate button to create configurable parts on the floor";
            //Part counter slider
            _ammountOfComponents = Mathf.RoundToInt(GUI.HorizontalSlider(new Rect(leftPad, topPad, fieldTitleWidth, fieldHeight), _ammountOfComponents, 5f, 15f));
            GUI.Box(new Rect((leftPad * 2) + fieldTitleWidth, topPad, textFieldWidth, fieldHeight), $"Ammount of Parts: {_ammountOfComponents}", "fieldTitle");
            
            //Populate Button
            if (GUI.Button(new Rect(leftPad, topPad + ((fieldHeight + 10) * i++), (fieldTitleWidth + leftPad + textFieldWidth), fieldHeight), "Populate Parts"))
            {
                _populated = true;
                _grid.ClearGrid();
                _existingParts = new List<Part>();
                if (_bigGrid) ReadStructure("StructureParts_BigSlab");

                PopulateRandomConfigurable(_ammountOfComponents);

                _boudaryVoxels = _grid.ActiveVoxelsAsList().Where(v => v.IsBoundary).ToList();
                _outputMessage = $"{_ammountOfComponents} parts created! " +
                    $"\n \nClick populate again to generate a different layout or Make Spaces to proceed" +
                    $"\n \nYou can press T to visualize type of each part";
            }
            //Make Button
            if (_populated && !_analyzed)
            {
                if (GUI.Button(new Rect(leftPad, topPad + ((fieldHeight + 10) * i++), (fieldTitleWidth + leftPad + textFieldWidth), fieldHeight), "Make Spaces"))
                {
                    _outputMessage = $"Please wait...";
                    DefinePartsBoundaries();
                    //GenerateSingleSpace();
                    GenerateSpaces();
                    //StartCoroutine(AnimateGeneration());
                    _analyzed = true;
                    _outputMessage = $"{_spaces.Count} Spaces created!";
                }
            }
        }
        else
        {
            _outputMessage = "Test mode is active.";
        }
        
       
        //Output Message
        GUI.Box(new Rect(leftPad, (topPad) + ((fieldHeight + 10) * i++), (fieldTitleWidth + leftPad + textFieldWidth), fieldHeight), _outputMessage, "outputMessage");

        //Debug pop-up window
        if (_showDebug)
        {
            GUI.Window(0, new Rect(Screen.width - leftPad - 300, topPad - 75, 300, (fieldHeight * 25) + 10), DebugWindow, "Debug_Summary");
        }
    }

    void DebugWindow(int windowID)
    {
        GUIStyle style = _skin.GetStyle("debugWindow");
        int leftPad = 10;
        int topPad = 10;
        int fieldWidth = 300 - (leftPad*2);
        int fieldHeight = 25;
        //int buttonWidth = 50;
        int windowSize = (fieldHeight * 25) + 10;

        int count = 1;

        _compiledMessage[0] = "Debug output";

        //Time Button
        if (GUI.Button(new Rect(leftPad, windowSize - ((fieldHeight + topPad) * count++), fieldWidth, fieldHeight), "Processing Durations"))
        {
            _showTime = !_showTime;
            if (_showTime)
            {
                _compiledMessage[1] = "Time: \n" + $"Parts: {_boundaryPartsTime}ms \n"
                    + $"Graphs: {_boundaryGraphTime}ms \n"
                    + $"Boudaries Total: {_boundaryMainTime}ms \n"
                    + $"Single Space: {_singleSpaceTime}ms \n"
                    + $"All Spaces: {_allSpacesTime}ms";
            }
            else
            {
                _compiledMessage[1] = "escape";
            }  
        }

        //Show Raw Boundaries
        if (GUI.Button(new Rect(leftPad, windowSize - ((fieldHeight + topPad) * count++), fieldWidth, fieldHeight), "Raw Boundaries"))
        {
            _showRawBoundaries = !_showRawBoundaries;
        }
        
        //Show Spaces
        if (GUI.Button(new Rect(leftPad, windowSize - ((fieldHeight + topPad) * count++), fieldWidth, fieldHeight), "Spaces"))
        {
            _showSpaces = !_showSpaces;
        }

        //Debug Message
        _debugMessage = "";
        for (int i = 0; i < _compiledMessage.Length; i++)
        {
            var line = _compiledMessage[i];
            if (line != "escape")
            {
                _debugMessage += line + '\n';
            }
        }

        GUI.Box(new Rect(leftPad, topPad, fieldWidth, fieldHeight), _debugMessage, "outputMessage");
    }
}