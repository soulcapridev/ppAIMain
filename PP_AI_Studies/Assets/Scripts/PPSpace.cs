using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System;

public class PPSpace : IEquatable<PPSpace>
{
    //public List<Voxel> Voxels = new List<Voxel>();
    public HashSet<Voxel> Voxels = new HashSet<Voxel>();
    

    //Boudary voxels are voxels which have as a face neighbour 
    //at least one which isn't part of its ParentSpace
    public IEnumerable<Voxel> BoundaryVoxels => Voxels.Where(v => v.GetFaceNeighbours().Any(n => !Voxels.Contains(n)) || v.GetFaceNeighbours().ToList().Count < 4);

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
