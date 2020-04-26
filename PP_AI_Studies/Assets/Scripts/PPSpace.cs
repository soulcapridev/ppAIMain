using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;

[System.Serializable]
public class PPSpace : IEquatable<PPSpace>
{
    protected VoxelGrid _grid;
    public HashSet<Voxel> Voxels = new HashSet<Voxel>();
    public HashSet<Vector3Int> Indices = new HashSet<Vector3Int>();
    public string OCIndexes;
    public int nVoxels => Voxels.Count;
    public Vector3 Center => new Vector3(
        Indices.Average(i => (float)i.x), 
        0, 
        Indices.Average(i => (float)i.z));
    
    //Boudary voxels are voxels which have at least
    //one face neighbour which isn't part of its ParentSpace
    public IEnumerable<Voxel> BoundaryVoxels => Voxels.Where(v => v.GetFaceNeighbours().Any(n => !Voxels.Contains(n)) || v.GetFaceNeighbours().ToList().Count < 4);

    public PPSpace NewSpace(VoxelGrid grid)
    {
        PPSpace s = new PPSpace();
        s.OCIndexes = OCIndexes;
        s._grid = grid;

        var indexes = s.OCIndexes.Split(';');
        int len = indexes.Length;
        s.Indices = new HashSet<Vector3Int>();
        s.Voxels = new HashSet<Voxel>();
        for (int i = 0; i < len; i++)
        {
            var index = indexes[i];
            var coords = index.Split('_');
            Vector3Int vector = new Vector3Int(int.Parse(coords[0]), int.Parse(coords[1]), int.Parse(coords[2]));
            var voxel = grid.Voxels[vector.x, vector.y, vector.z];
            voxel.ParentSpace = s;
            s.Indices.Add(vector);
            s.Voxels.Add(voxel);
        }
        return s;
    }


    public List<Voxel> DestroySpace()
    {
        List<Voxel> orphans = new List<Voxel>();
        foreach (var voxel in Voxels)
        {
            voxel.InSpace = false;
            voxel.ParentSpace = null;
            orphans.Add(voxel);
        }
        Voxels = new HashSet<Voxel>();
        return orphans;
    }
    public bool Equals(PPSpace other)
    {
        return (other != null && Voxels.Count == other.Voxels.Count && Voxels.All(other.Voxels.Contains));
    }
    public override int GetHashCode()
    {
        return Voxels.Sum(v => v.GetHashCode());
    }
}

public class PPSpaceCollection
{
    public PPSpace[] Spaces;
}
