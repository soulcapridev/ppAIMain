using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

[System.Serializable]
public class Part : System.IEquatable<Part>
{
    protected VoxelGrid _grid;

    public PartType Type;
    public Vector2Int Size;
    public bool IsStatic;
    public Vector3Int ReferenceIndex;
    public int Height;
    public Vector3 Center;
    public Vector3Int[] OccupiedIndexes;
    public Voxel[] OccupiedVoxels;
    public PartOrientation Orientation;
    public int nVoxels;

    public string OrientationName;
    public string OCIndexes;

    public Voxel OriginVoxel => _grid.Voxels[ReferenceIndex.x, ReferenceIndex.y, ReferenceIndex.z];

    protected void OccupyVoxels()
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
    protected void CalculateCenter()
    {
        float avgX = OccupiedVoxels.Select(v => v.Center).Average(c => c.x);
        float avgY = OccupiedVoxels.Select(v => v.Center).Average(c => c.y);
        float avgZ = OccupiedVoxels.Select(v => v.Center).Average(c => c.z);
        Center = new Vector3(avgX, avgY, avgZ);
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