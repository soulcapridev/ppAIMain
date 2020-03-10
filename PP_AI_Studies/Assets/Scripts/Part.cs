using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

[System.Serializable]
public class Part : System.IEquatable<Part>
{
    public PartType Type;
    public Vector2Int Size;
    public bool IsStatic;
    public Vector3Int ReferenceIndex;
    public float Height;
    public Vector3Int[] OccupiedIndexes;
    public Voxel[] OccupiedVoxels;
    public PartOrientation Orientation;

    public string TypeName;
    public int ReferenceX;
    public int ReferenceY;
    public int ReferenceZ;
    public string OrientationName;

    int nVoxels => Size.x > Size.y ? Size.x : Size.y;
    
    VoxelGrid _grid;
    public Voxel OriginVoxel => _grid.Voxels[ReferenceIndex.x, ReferenceIndex.y, ReferenceIndex.z];

    public Vector3 Center;


    public Part NewPart(VoxelGrid grid)
    {
        Part p = new Part();
        p.Type = (PartType)System.Enum.Parse(typeof(PartType), TypeName, false);
        p.ReferenceIndex = new Vector3Int(ReferenceX, ReferenceY, ReferenceZ);
        p.Orientation = (PartOrientation)System.Enum.Parse(typeof(PartOrientation), OrientationName, false);
        p._grid = grid;
        p.SizeByType();
        p.GetOccupiedIndexes();
        p.OccupyVoxels();

        return p;
    }

    public Part NewRandomPart(VoxelGrid grid)
    {
        _grid = grid;
        Part p = new Part();
        bool validPart = false;

        while (!validPart)
        {
            Type = (PartType)Random.Range(0, 10);
            Orientation  = (PartOrientation)Random.Range(0, 2);

            int randomX = Random.Range(0, _grid.Size.x - 1);
            int randomY = Random.Range(0, _grid.Size.y - 1);
            int randomZ = Random.Range(0, _grid.Size.z - 1);
            ReferenceIndex = new Vector3Int(randomX, randomY, randomZ);

            bool allInside = true;

            SizeByType();
            GetOccupiedIndexes();
            foreach (var index in OccupiedIndexes)
            {
                if (index.x >= _grid.Size.x || index.y >= _grid.Size.y || index.z >= _grid.Size.z)
                {
                    allInside = false;
                    break;
                }
                else if (_grid.Voxels[index.x, index.y, index.z].IsOccupied)
                {
                    allInside = false;
                    break;
                }
            }
            if (allInside) validPart = true;
            else continue;
        }
        OccupyVoxels();
        return p;
    }

    public Part NewRandomConfigurable(VoxelGrid grid, List<Part> existingParts)
    {
        _grid = grid;
        Part p = new Part();
        bool validPart = false;

        int minimumDistance = 4; //In voxels

        while (!validPart)
        {
            Type = PartType.Configurable;
            Orientation = (PartOrientation)Random.Range(0, 2);

            int randomX = Random.Range(0, _grid.Size.x - 1);
            int randomY = Random.Range(0, _grid.Size.y - 1);
            int randomZ = Random.Range(0, _grid.Size.z - 1);
            ReferenceIndex = new Vector3Int(randomX, randomY, randomZ);

            bool allInside = true;

            SizeByType();
            GetOccupiedIndexes();
            if (!CheckDistance(existingParts, minimumDistance)) continue;

            foreach (var index in OccupiedIndexes)
            {
                if (index.x >= _grid.Size.x || index.y >= _grid.Size.y || index.z >= _grid.Size.z)
                {
                    allInside = false;
                    break;
                }
                else if (_grid.Voxels[index.x, index.y, index.z].IsOccupied || !_grid.Voxels[index.x, index.y, index.z].IsActive)
                {
                    allInside = false;
                    break;
                }
            }
            if (allInside) validPart = true;
            else continue;
        }
        OccupyVoxels();
        return p;
    }

    bool CheckDistance(List<Part> existingParts, int minimumDistance)
    {
        if (existingParts.Any())
        {
            foreach (var ePart in existingParts)
            {
                if (Orientation == ePart.Orientation)
                {
                    if (Orientation == PartOrientation.Horizontal)
                    {
                        foreach (var x in OccupiedIndexes.Select(i => i.x))
                        {
                            if (ePart.OccupiedIndexes.Any(e => e.x == x))
                            {
                                if (Mathf.Abs(ReferenceIndex.z - ePart.ReferenceIndex.z) <= minimumDistance) return false;
                            }
                        }

                    }
                    else if (Orientation == PartOrientation.Vertical)
                    {
                        foreach (var z in OccupiedIndexes.Select(i => i.z))
                        {
                            if (ePart.OccupiedIndexes.Any(ee => ee.z == z))
                            {
                                if (Mathf.Abs(ReferenceIndex.x - ePart.ReferenceIndex.x) <= minimumDistance) return false;
                            }
                        }

                    }
                }
            }
        }
        return true;
    }

    void OccupyVoxels()
    {
        OccupiedVoxels = new Voxel[nVoxels];
        for (int i = 0; i < nVoxels; i++)
        {
            var index = OccupiedIndexes[i];
            Voxel voxel = _grid.Voxels[index.x, index.y, index.z];
            voxel.IsOccupied = true;
            voxel.Part = this;
            OccupiedVoxels[i] = voxel;
        }
        CalculateCenter();
    }

    void GetOccupiedIndexes()
    {
        OccupiedIndexes = new Vector3Int[nVoxels];
        for (int i = 0; i < nVoxels; i++)
        {
            if (Orientation == PartOrientation.Horizontal)
            {
                OccupiedIndexes[i] = new Vector3Int(ReferenceIndex.x + i, ReferenceIndex.y, ReferenceIndex.z);
            }
            else if (Orientation == PartOrientation.Vertical)
            {
                OccupiedIndexes[i] = new Vector3Int(ReferenceIndex.x, ReferenceIndex.y, ReferenceIndex.z + i);
            }
            else if (Orientation == PartOrientation.Agnostic)
            {
                OccupiedIndexes[i] = new Vector3Int(ReferenceIndex.x, ReferenceIndex.y, ReferenceIndex.z);
            }
        }
    }

    void CalculateCenter()
    {
        float avgX = OccupiedVoxels.Select(v => v.Center).Average(c => c.x);
        float avgY = OccupiedVoxels.Select(v => v.Center).Average(c => c.y);
        float avgZ = OccupiedVoxels.Select(v => v.Center).Average(c => c.z);
        Center = new Vector3(avgX, avgY, avgZ);
    }

    void SizeByType()
    {
        if (Type == PartType.Bedroom)
        {
            IsStatic = false;
            Size = new Vector2Int(3, 1);
        }
        else if (Type == PartType.Shower || Type == PartType.WCSink || Type == PartType.Toilet)
        {
            IsStatic = false;
            Size = new Vector2Int(1, 1);
        }
        else if (Type == PartType.KitchenOven || Type == PartType.KitchenSink || Type == PartType.KitchenStove || Type == PartType.KitchenTop)
        {
            IsStatic = false;
            Size = new Vector2Int(1, 1);
        }
        else if (Type == PartType.Laundry)
        {
            IsStatic = true;
            Size = new Vector2Int(3, 1);
        }
        else if (Type == PartType.Structure)
        {
            IsStatic = true;
            Size = new Vector2Int(1, 1);
        }

        else if (Type == PartType.Dumb)
        {
            IsStatic = false;
            Size = new Vector2Int(3, 1);
        }

        else if (Type == PartType.Configurable)
        {
            IsStatic = false;
            Size = new Vector2Int(6, 1);
        }
    }

    public bool Equals(Part other)
    {
        return (other != null) && (Type == other.Type) && (ReferenceIndex == other.ReferenceIndex);
    }

    public override int GetHashCode()
    {
        return Type.GetHashCode() + ReferenceIndex.GetHashCode() + Orientation.GetHashCode();
    }
}
public class PartCollection
{
    public Part[] Parts;
}
public enum PartType { Structure, Shower, WCSink, Toilet, Laundry, Dumb, Bedroom, KitchenOven, KitchenStove, KitchenSink, KitchenTop, Configurable };
public enum PartOrientation { Vertical, Horizontal, Agnostic };
