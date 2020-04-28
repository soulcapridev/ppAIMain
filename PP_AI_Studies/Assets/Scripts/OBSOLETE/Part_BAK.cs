//using System.Collections;
//using System.Collections.Generic;
//using UnityEngine;
//using System.Linq;

//[System.Serializable]
//public class Part_BAK : System.IEquatable<Part_BAK>
//{
//    public PartType Type;
//    public Vector2Int Size;
//    public bool IsStatic;
//    public Vector3Int ReferenceIndex;
//    public float Height;
//    public Vector3Int[] OccupiedIndexes;
//    public Voxel[] OccupiedVoxels;
//    public PartOrientation Orientation;

//    public string TypeName;
//    public int ReferenceX;
//    public int ReferenceY;
//    public int ReferenceZ;
//    public string OrientationName;
//    public List<int[]> OCIndexes;

//    int nVoxels => Size.x * Size.y;
    
//    VoxelGrid _grid;
//    public Voxel OriginVoxel => _grid.Voxels[ReferenceIndex.x, ReferenceIndex.y, ReferenceIndex.z];

//    public Vector3 Center;
//    //
//    //THIS SHOULD BE REWORKED TO IMPLEMENT POLYMORPHISM
//    //
//    public Part_BAK NewPart(VoxelGrid grid)
//    {
//        Part_BAK p = new Part_BAK();
//        p.Type = (PartType)System.Enum.Parse(typeof(PartType), TypeName, false);
//        p.ReferenceIndex = new Vector3Int(ReferenceX, ReferenceY, ReferenceZ);
//        p.Orientation = (PartOrientation)System.Enum.Parse(typeof(PartOrientation), OrientationName, false);
//        p._grid = grid;
//        p.Size = SizeByType2[p.Type];
//        p.IsStatic = MoveableByType[p.Type];
//        p.GetOccupiedIndexes();
//        p.OccupyVoxels();

//        return p;
//    }

//    public Part_BAK NewStructuralPart(VoxelGrid grid)
//    {
//        Part_BAK p = new Part_BAK();
//        p.Type = PartType.Structure;
//        p.IsStatic = false;
//        //p.ReferenceIndex = new Vector3Int(ReferenceX, ReferenceY, ReferenceZ);
//        p.Orientation = (PartOrientation)System.Enum.Parse(typeof(PartOrientation), OrientationName, false);
//        p._grid = grid;
//        p.OccupiedIndexes = new Vector3Int[p.OCIndexes.Count];
 
//        //p.GetOccupiedIndexes();
//        p.OccupyVoxels();

//        return p;
//    }

//    public Part_BAK NewRandomPart(VoxelGrid grid)
//    {
//        _grid = grid;
//        Part_BAK p = new Part_BAK();
//        bool validPart = false;

//        while (!validPart)
//        {
//            Type = (PartType)Random.Range(0, 10);
//            Orientation  = (PartOrientation)Random.Range(0, 2);

//            int randomX = Random.Range(0, _grid.Size.x - 1);
//            int randomY = Random.Range(0, _grid.Size.y - 1);
//            int randomZ = Random.Range(0, _grid.Size.z - 1);
//            ReferenceIndex = new Vector3Int(randomX, randomY, randomZ);

//            bool allInside = true;

//            //SizeByType();
//            Size = SizeByType2[Type];
//            IsStatic = MoveableByType[Type];
//            GetOccupiedIndexes();
//            foreach (var index in OccupiedIndexes)
//            {
//                if (index.x >= _grid.Size.x || index.y >= _grid.Size.y || index.z >= _grid.Size.z)
//                {
//                    allInside = false;
//                    break;
//                }
//                else if (_grid.Voxels[index.x, index.y, index.z].IsOccupied)
//                {
//                    allInside = false;
//                    break;
//                }
//            }
//            if (allInside) validPart = true;
//            else continue;
//        }
//        OccupyVoxels();
//        return p;
//    }

//    public Part_BAK NewRandomConfigurable(VoxelGrid grid, List<Part_BAK> existingParts)
//    {
//        Random.InitState(5);
//        _grid = grid;
//        Part_BAK p = new Part_BAK();
//        bool validPart = false;

//        int minimumDistance = 6; //In voxels

//        while (!validPart)
//        {
//            Type = PartType.Configurable;
//            Orientation = (PartOrientation)Random.Range(0, 2);

//            int randomX = Random.Range(0, _grid.Size.x - 1);
//            int randomY = Random.Range(0, _grid.Size.y - 1);
//            int randomZ = Random.Range(0, _grid.Size.z - 1);
//            ReferenceIndex = new Vector3Int(randomX, randomY, randomZ);

//            bool allInside = true;

//            //SizeByType();
//            Size = SizeByType2[Type];
//            IsStatic = MoveableByType[Type];
//            GetOccupiedIndexes();
//            if (!CheckDistance(existingParts, minimumDistance)) continue;

//            foreach (var index in OccupiedIndexes)
//            {
//                if (index.x >= _grid.Size.x || index.y >= _grid.Size.y || index.z >= _grid.Size.z)
//                {
//                    allInside = false;
//                    break;
//                }
//                else if (_grid.Voxels[index.x, index.y, index.z].IsOccupied || !_grid.Voxels[index.x, index.y, index.z].IsActive)
//                {
//                    allInside = false;
//                    break;
//                }
//            }
//            if (allInside) validPart = true;
//            else continue;
//        }
//        OccupyVoxels();
//        return p;
//    }

//    bool CheckDistance(List<Part_BAK> existingParts, int minimumDistance)
//    {
//        //SHOULD BE RE-EVALUATED TO KEEP DISTANCE CONSIDERING THE THICKNESS OF THE PARTS
//        if (existingParts.Any())
//        {
//            foreach (var ePart in existingParts)
//            {
//                if (Orientation == ePart.Orientation)
//                {
//                    if (Orientation == PartOrientation.Horizontal)
//                    {
//                        foreach (var x in OccupiedIndexes.Select(i => i.x))
//                        {
//                            if (ePart.OccupiedIndexes.Any(e => e.x == x))
//                            {
//                                if (Mathf.Abs(ReferenceIndex.z - ePart.ReferenceIndex.z) <= minimumDistance) return false;
//                            }
//                        }

//                    }
//                    else if (Orientation == PartOrientation.Vertical)
//                    {
//                        foreach (var z in OccupiedIndexes.Select(i => i.z))
//                        {
//                            if (ePart.OccupiedIndexes.Any(ee => ee.z == z))
//                            {
//                                if (Mathf.Abs(ReferenceIndex.x - ePart.ReferenceIndex.x) <= minimumDistance) return false;
//                            }
//                        }

//                    }
//                }
//            }
//        }
//        return true;
//    }

//    void OccupyVoxels()
//    {
//        OccupiedVoxels = new Voxel[nVoxels];
//        for (int i = 0; i < nVoxels; i++)
//        {
//            var index = OccupiedIndexes[i];
//            Voxel voxel = _grid.Voxels[index.x, index.y, index.z];
//            voxel.IsOccupied = true;
//            //voxel.Part = this;
//            OccupiedVoxels[i] = voxel;
//        }
//        CalculateCenter();
//    }

//    void GetOccupiedIndexes()
//    {
//        //THIS NEEDS TO BE REVISED AND SIMPLIFIED
//        if (Type != PartType.Configurable)
//        {
//            OccupiedIndexes = new Vector3Int[nVoxels];
//            for (int i = 0; i < nVoxels; i++)
//            {

//                if (Orientation == PartOrientation.Horizontal)
//                {
//                    OccupiedIndexes[i] = new Vector3Int(ReferenceIndex.x + i, ReferenceIndex.y, ReferenceIndex.z);
//                }
//                else if (Orientation == PartOrientation.Vertical)
//                {
//                    OccupiedIndexes[i] = new Vector3Int(ReferenceIndex.x, ReferenceIndex.y, ReferenceIndex.z + i);
//                }
//                else if (Orientation == PartOrientation.Agnostic)
//                {
//                    OccupiedIndexes[i] = new Vector3Int(ReferenceIndex.x, ReferenceIndex.y, ReferenceIndex.z);
//                }
//            }
//        }
//        else
//        {
//            OccupiedIndexes = new Vector3Int[Size.x * Size.y];
//            if (Orientation == PartOrientation.Horizontal)
//            {
//                int i = 0;
//                for (int x = 0; x < Size.x; x++)
//                {
//                    for (int z = 0; z < Size.y; z++)
//                    {
//                        OccupiedIndexes[i++] = new Vector3Int(ReferenceIndex.x + x, ReferenceIndex.y, ReferenceIndex.z + z);
//                    }
//                }

//            }
//            else if (Orientation == PartOrientation.Vertical)
//            {
//                int i = 0;
//                for (int x = 0; x < Size.y; x++)
//                {
//                    for (int z = 0; z < Size.x; z++)
//                    {
//                        OccupiedIndexes[i++] = new Vector3Int(ReferenceIndex.x + x, ReferenceIndex.y, ReferenceIndex.z + z);
//                    }
//                }
//            }

//        }

//    }

//    void CalculateCenter()
//    {
//        float avgX = OccupiedVoxels.Select(v => v.Center).Average(c => c.x);
//        float avgY = OccupiedVoxels.Select(v => v.Center).Average(c => c.y);
//        float avgZ = OccupiedVoxels.Select(v => v.Center).Average(c => c.z);
//        Center = new Vector3(avgX, avgY, avgZ);
//    }

//    Dictionary<PartType, Vector2Int> SizeByType2 = new Dictionary<PartType, Vector2Int>() 
//    {
//        { PartType.Structure, new Vector2Int(1, 1) },
//        { PartType.Configurable, new Vector2Int(6, 2) },
//        //These are obsolete
//        { PartType.Bedroom, new Vector2Int(3, 1) },
//        { PartType.Shower, new Vector2Int(1, 1) },
//        { PartType.WCSink, new Vector2Int(1, 1) },
//        { PartType.Toilet, new Vector2Int(1, 1) },
//        { PartType.KitchenOven, new Vector2Int(1, 1) },
//        { PartType.KitchenSink, new Vector2Int(1, 1) },
//        { PartType.KitchenStove, new Vector2Int(1, 1) },
//        { PartType.KitchenTop, new Vector2Int(1, 1) },
//        { PartType.Laundry, new Vector2Int(3, 1) },
//        { PartType.Dumb, new Vector2Int(3, 1) }
//    };

//    Dictionary<PartType, bool> MoveableByType = new Dictionary<PartType, bool>()
//    {
//        { PartType.Structure, true },
//        { PartType.Configurable, false },
//        //These are obsolete
//        { PartType.Bedroom, false },
//        { PartType.Shower, false },
//        { PartType.WCSink, false },
//        { PartType.Toilet, false },
//        { PartType.KitchenOven, false },
//        { PartType.KitchenSink, false },
//        { PartType.KitchenStove, false },
//        { PartType.KitchenTop, false },
//        { PartType.Laundry, false },
//        { PartType.Dumb, false }
//    };

//    public bool Equals(Part_BAK other)
//    {
//        return (other != null) && (Type == other.Type) && (ReferenceIndex == other.ReferenceIndex);
//    }

//    public override int GetHashCode()
//    {
//        return Type.GetHashCode() + ReferenceIndex.GetHashCode() + Orientation.GetHashCode();
//    }
//}
//public class PartCollection_BAK
//{
//    public Part_BAK[] Parts;
//}
