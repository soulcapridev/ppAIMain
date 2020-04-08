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
    Vector3Int _gridSize = new Vector3Int(40, 1, 82);
    
    float _voxelSize = 0.5f;
    int _spaceMinimumArea = 15; //in voxel ammount
    int _ammountOfComponents = 26;
    int _day = 0;

    List<Part> _existingParts = new List<Part>();
    List<PPSpace> _spaces = new List<PPSpace>();
    List<Voxel> _partsBoundaries = new List<Voxel>();
    List<Voxel> _toDraw = new List<Voxel>();
    List<Color> _toColor = new List<Color>();

    bool _drawTags = false;
    bool _populated = false;
    bool _analyzed = false;

    string _outputMessage = "Adjust the slider to define how many parts should be created." +
        '\n' + "\nPress Populate button to create configurable parts on the floor";

    void Start()
    {
        _cam = Camera.main;
        _cameraPivot.position = new Vector3(_gridSize.x / 2, _gridSize.y / 2, _gridSize.z / 2) * _voxelSize;
        _grid = new VoxelGrid(_gridSize, _voxelSize, Vector3.zero);
        //Read CSV to create the floor
        CSVReader.SetGridState(_grid, "Input Data/FloorLayout");
        //Read JSON to create structural parts
        _existingParts = JSONReader.ReadPartsAsList(_grid, "Input Data/StructureParts");
    }

    void Update()
    {
        DrawState();
        DrawPartsBoundaries();
        //Use T to toggle the visibility of the components type tags
        //StartCoroutine(SaveScreenshot());
        if (Input.GetKeyDown(KeyCode.T)) _drawTags = !_drawTags;
        Drawing.DrawVoxelColor(_toDraw, _toColor, _voxelSize);
    }

    IEnumerator SaveScreenshot()
    {
        string file = $"SavedFrames/SpaceAnalysis/Frame_{_day}.png";
        ScreenCapture.CaptureScreenshot(file, 2);
        _day++;
        yield return new WaitForEndOfFrame();
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

    void DefinePartsBoundaries()
    {
        Stopwatch mainStopwatch = new Stopwatch();
        mainStopwatch.Start();

        int partsProcessing = 0;
        int graphProcessing = 0;
        //List of walkable voxels
        var walkable = _grid.ActiveVoxelsAsList().Where(v => !v.IsOccupied);

        //Algorithm constraints
        int breadthLevels = 20;
        int pathMaximumLength = 25;

        //Paralell lists containing the connected parts and the paths lenghts
        //This is later used to make that only the shortest connection between 2 parts is maintained
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

            //BFS (inspired) algorithm, exploring the grid through levels
            foreach (var origin in origins)
            {
                //Queue used to explore levels
                Queue<Voxel> currentLevel = new Queue<Voxel>();
                currentLevel.Enqueue(origin);

                //List to keep track of voxels that have been visited
                List<Voxel> visited = new List<Voxel>();
                List<Voxel> toProcess = new List<Voxel>();
                //List to store search range
                //This is used to reduce the area the ShortestPath algorithm needs to look
                

                //List to store the parts that have been found
                List<Part> foundParts = new List<Part>();




                //Navigate through the neighbours through levels ORIGINAL ATTEMPT (16SEG)
                //List<Voxel> searchRange = new List<Voxel>() { origin };
                //for (int i = 0; i < breadthLevels; i++)
                //{
                //    Queue<Voxel> nextLevel = new Queue<Voxel>();
                //    while (currentLevel.Count > 0)
                //    {
                //        var voxel = currentLevel.Dequeue();
                //        visited.Add(voxel);
                //        var neighbours = voxel.GetFaceNeighbours().Where(n => n.IsActive && !visited.Contains(n));
                //        foreach (var neighbour in neighbours)
                //        {
                //            if (!searchRange.Contains(neighbour)) searchRange.Add(neighbour);
                //            nextLevel.Enqueue(neighbour);
                //            //Only add neighbour(and its part) if it hasn't been processed before
                //            if (!toProcess.Contains(neighbour) && neighbour.IsOccupied && neighbour.Part != part && !foundParts.Contains(neighbour.Part))
                //            {
                //                var foundPart = neighbour.Part;
                //                foundParts.Add(foundPart);
                //                toProcess.Add(neighbour);
                //            }
                //        }
                //    }
                //    currentLevel = nextLevel;
                //}

                //Navigate through the neighbours through levels SECOND ATTEMPT (2ms)
                var neighbours = origin.GetNeighboursInRange(breadthLevels);
                foreach (var neighbour in neighbours)
                {
                    if (neighbour.IsOccupied && neighbour.Part != part && !foundParts.Contains(neighbour.Part))
                    {
                        foundParts.Add(neighbour.Part);
                    }
                }
                var searchRange = neighbours.Where(n => !n.IsOccupied).ToList();
                searchRange.Add(origin);

                //Make copy of walkable voxels for this origin voxel
                var localWalkable = new List<Voxel>(searchRange);

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
                    var closestVoxel = _grid.Voxels[closestIndex.x, closestIndex.y, closestIndex.z];
                    partsTargets.Add(closestVoxel);
                    localWalkable.Add(closestVoxel);
                }
                
                partStopwatch.Stop();
                partsProcessing += (int)partStopwatch.ElapsedMilliseconds;


                //Construct graph with walkable voxels and targets to be processed
                Stopwatch graphStopwatch = new Stopwatch();
                graphStopwatch.Start();
                var faces = _grid.GetFaces().Where(f => localWalkable.Contains(f.Voxels[0]) && localWalkable.Contains(f.Voxels[1]));
                var graphFaces = faces.Select(f => new TaggedEdge<Voxel, Face>(f.Voxels[0], f.Voxels[1], f));
                var start = origin;

                //var graph = graphFaces.ToUndirectedGraph<Voxel, TaggedEdge<Voxel, Face>>();
                //var shortest = graph.ShortestPathsDijkstra(e => 1.0, start);
                var graph = graphFaces.ToBidirectionalGraph<Voxel, TaggedEdge<Voxel, Face>>();
                var shortest = graph.ShortestPathsAStar(e => 1.0, v => VoxelDistance(v, start), start);
                //var shortest = graph.ShortestPathsAStar(e => 1.0, v => 1.0, start);

                foreach (var v in partsTargets)
                {
                    var end = v;
                    //Check if the shortest path is valid
                    if(shortest(end, out var endPath))
                    {
                        var endPathVoxels = new HashSet<Voxel>(endPath.SelectMany(e => new[] { e.Source, e.Target }));
                        var pathLength = endPathVoxels.Count;
                        //Check if path length is under minimum
                        if (pathLength <= pathMaximumLength
                            && !endPathVoxels.All(ev => ev.IsOccupied)
                            && endPathVoxels.Count(ev => ev.GetFaceNeighbours().Any(evn => evn.IsOccupied)) > 2
                            && true)
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
                graphStopwatch.Stop();
                graphProcessing += (int)graphStopwatch.ElapsedMilliseconds;
            }
        }
        //Feed the general boundaries list
        foreach (var path in connectionPaths)
        {
            foreach (var voxel in path)
            {
                if(!voxel.IsOccupied && !_partsBoundaries.Contains(voxel)) _partsBoundaries.Add(voxel);
            }
        }
        mainStopwatch.Stop();
        print($"Took {mainStopwatch.ElapsedMilliseconds}ms to Process");
        print($"Took {partsProcessing}ms to Process Parts");
        print($"Took {graphProcessing}ms to Process Graphs");
    }

    void GenerateSingleSpace()
    {
        //Generate spaces on the voxels that are not inside the parts boudaries, or space or part
        //The method is inspired by a BFS algorithm, continuously checking the neighbours of the
        //processed voxels until the minimum area is reached

        int minimumArea = 1000; //in voxel ammount
        var availableVoxels = _grid.ActiveVoxelsAsList().Where(v => !_partsBoundaries.Contains(v) && !v.IsOccupied && !v.InSpace).ToList();
        if (availableVoxels.Count == 0) return;
        Voxel originVoxel = availableVoxels[0];
        
        //Initiate a new space
        PPSpace space = new PPSpace();
        originVoxel.InSpace = true;
        originVoxel.ParentSpace = space;
        space.Voxels.Add(originVoxel);
        //Keep running until the space area is under the minimum
        while (space.Voxels.Count < minimumArea)
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
        stopwatch.Stop();
        print($"Took {stopwatch.ElapsedMilliseconds}ms to Generate {_spaces.Count} Spaces");
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

    void DrawPartsBoundaries()
    {
        foreach (var voxel in _partsBoundaries)
        {
            Drawing.DrawCubeTransparent(voxel.Center + new Vector3(0f, _voxelSize, 0f), _voxelSize);
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

    double VoxelDistance(Voxel s, Voxel t)
    {
        var dif = s.Center - t.Center;
        double distance = dif.sqrMagnitude;
        return distance;
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

        //Part counter slider
        _ammountOfComponents = Mathf.RoundToInt(GUI.HorizontalSlider(new Rect(leftPad, topPad, fieldTitleWidth, fieldHeight), _ammountOfComponents, 20f, 28f));
        GUI.Box(new Rect((leftPad * 2) + fieldTitleWidth, topPad, textFieldWidth, fieldHeight), $"Ammount of Parts: {_ammountOfComponents}", "fieldTitle");

        //Populate Button
        if (GUI.Button(new Rect(leftPad, topPad + ((fieldHeight + 10) * i++), (fieldTitleWidth + leftPad + textFieldWidth), fieldHeight), "Populate Parts"))
        {
            _populated = true;
            _grid.ClearGrid();
            _existingParts = new List<Part>();
            PopulateRandomConfigurable(_ammountOfComponents);
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
                //GenerateSpaces();
                //StartCoroutine(AnimateGeneration());
                _analyzed = true;
                _outputMessage = $"{_spaces.Count} Spaces created!";
            }
        }
        //Output Message
        GUI.Box(new Rect(leftPad, (topPad) + ((fieldHeight + 10) * i++), (fieldTitleWidth + leftPad + textFieldWidth), fieldHeight), _outputMessage, "outputMessage");


    }
}