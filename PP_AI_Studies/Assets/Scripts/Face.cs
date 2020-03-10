using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;

public class Face
{
    //Implemented and adapted from https://github.com/ADRC4/Voxel
    public enum BoundaryType { Inside = 0, Left = 1, Right = 2, Outside = 3 };

    public Voxel[] Voxels;
    public Vector3Int Index;
    public Vector3 Center;
    public Axis Direction;

    VoxelGrid _grid;

    public bool IsActive => Voxels.Count(v => v != null && v.IsActive) == 2;
    public bool IsClimbable => IsActive && Voxels.Count(v => v != null && !v.IsOccupied) == 2;

    public BoundaryType Boundary
    {
        get
        {
            bool left = Voxels[0]?.IsActive == true;
            bool right = Voxels[1]?.IsActive == true;

            if (!left && right) return BoundaryType.Left;
            if (left && !right) return BoundaryType.Right;
            if (left && right) return BoundaryType.Inside;
            return BoundaryType.Outside;
        }
    }

    public Vector3 Normal
    {
        get
        {
            int f = (int)Boundary;
            if (Boundary == BoundaryType.Outside) f = 0;

            if (Index.y == 0 && Direction == Axis.Y)
            {
                f = Boundary == BoundaryType.Outside ? 1 : 0;
            }

            switch (Direction)
            {
                case Axis.X:
                    return Vector3.right * f;
                case Axis.Y:
                    return Vector3.up * f;
                case Axis.Z:
                    return Vector3.forward * f;
                default:
                    throw new Exception("Wrong direction.");
            }
        }
    }

    public bool IsSkin
    {
        get
        {
            if (Index.y == 0 && Direction == Axis.Y)
            {
                return Boundary == BoundaryType.Outside;
            }

            return Boundary == BoundaryType.Left || Boundary == BoundaryType.Right;
        }
    }

    public Face(int x, int y, int z, Axis direction, VoxelGrid grid)
    {
        _grid = grid;
        Index = new Vector3Int(x, y, z);
        Direction = direction;
        Voxels = GetVoxels();

        foreach (var v in Voxels.Where(v => v != null))
            v.Faces.Add(this);

        Center = GetCenter();
    }

    Voxel[] GetVoxels()
    {
        int x = Index.x;
        int y = Index.y;
        int z = Index.z;

        switch (Direction)
        {
            case Axis.X:
                return new[]
                {
                   x == 0 ? null : _grid.Voxels[x - 1, y, z],
                   x == _grid.Size.x ? null : _grid.Voxels[x, y, z]
                };
            case Axis.Y:
                return new[]
                {
                   y == 0 ? null : _grid.Voxels[x, y - 1, z],
                   y == _grid.Size.y ? null : _grid.Voxels[x, y, z]
                };
            case Axis.Z:
                return new[]
                {
                   z == 0 ? null : _grid.Voxels[x, y, z - 1],
                   z == _grid.Size.z ? null : _grid.Voxels[x, y, z]
                };
            default:
                throw new Exception("Wrong direction.");
        }
    }

    Vector3 GetCenter()
    {
        int x = Index.x;
        int y = Index.y;
        int z = Index.z;

        switch (Direction)
        {
            case Axis.X:
                return _grid.Origin + new Vector3(x, y + 0.5f, z + 0.5f) * _grid.VoxelSize;
            case Axis.Y:
                return _grid.Origin + new Vector3(x + 0.5f, y, z + 0.5f) * _grid.VoxelSize;
            case Axis.Z:
                return _grid.Origin + new Vector3(x + 0.5f, y + 0.5f, z) * _grid.VoxelSize;
            default:
                throw new Exception("Wrong direction.");
        }
    }
}
