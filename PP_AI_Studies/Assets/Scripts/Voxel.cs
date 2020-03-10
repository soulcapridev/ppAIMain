using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class Voxel : IEquatable<Voxel>
{
    public Vector3Int Index;
    public Vector3 Center;
    public bool IsOccupied;
    public bool IsActive;
    public List<Face> Faces = new List<Face>(6);
    public Part Part;
    public bool InSpace;
    public PPSpace ParentSpace;

    VoxelGrid _grid;

    public Voxel(Vector3Int index, VoxelGrid grid)
    {
        _grid = grid;
        Index = index;
        Center = _grid.Origin + new Vector3(index.x + 0.5f, index.y + 0.5f, index.z + 0.5f) * _grid.VoxelSize;
        IsOccupied = false;
        IsActive = true;
    }

    public IEnumerable<Voxel> GetFaceNeighbours()
    {
        int x = Index.x;
        int y = Index.y;
        int z = Index.z;
        var s = _grid.Size;

        if (x != 0) yield return _grid.Voxels[x - 1, y, z];
        if (x != s.x - 1) yield return _grid.Voxels[x + 1, y, z];

        if (y != 0) yield return _grid.Voxels[x, y - 1, z];
        if (y != s.y - 1) yield return _grid.Voxels[x, y + 1, z];

        if (z != 0) yield return _grid.Voxels[x, y, z - 1];
        if (z != s.z - 1) yield return _grid.Voxels[x, y, z + 1];
    }

    public PPSpace MoveToSpace(PPSpace target)
    {
        target.Voxels.Add(this);
        this.ParentSpace.Voxels.Remove(this);
        this.ParentSpace = target;

        return target;
    }

    public bool Equals(Voxel other)
    {
        return (other != null) && (Index == other.Index) && (IsOccupied == other.IsOccupied) && (IsActive == other.IsActive);
    }

    public override int GetHashCode()
    {
        return Index.GetHashCode() + IsActive.GetHashCode() + IsOccupied.GetHashCode();
    }

}
