using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;

[System.Serializable]
public class PPSpace : IEquatable<PPSpace>
{
    private VoxelGrid _grid;
    public HashSet<Voxel> Voxels = new HashSet<Voxel>();
    public HashSet<Vector3Int> Indices = new HashSet<Vector3Int>();
    public string OCIndexes; // Used to read Space data from Json file

    //Boudary voxels are voxels which have at least
    //one face neighbour which isn't part of its ParentSpace
    public IEnumerable<Voxel> BoundaryVoxels => Voxels.Where(v =>
        v.GetFaceNeighbours()
        .Any(n => !Voxels.Contains(n))
        || v.GetFaceNeighbours().ToList().Count < 4);


    //Size | Scale Parameters
    public int nVoxels => Voxels.Count; //This represents the Area of the space.
    //Average dimensions in the X and Z directions. 
    //Does not ignore jagged edges / broken lengths of the space
    //Use is still unclear, might help later
    public int AverageXWidth => (int) Voxels.GroupBy(v => v.Index.z).Select(r => r.ToList().Count).Average();
    public int AverageZWidth => (int)Voxels.GroupBy(v => v.Index.x).Select(r => r.ToList().Count).Average();

    //Connectivity Parameters
    //Get from the boundary voxels, the ones that represent connections
    //to other spaces
    public IEnumerable<Voxel> ConnectionVoxels => 
        BoundaryVoxels.Where(v => 
        v.GetFaceNeighbours()
        .Any(n => n.ParentSpace != this && n.InSpace));

    //The number of voxels connecting this space to others
    public int NumberOfConnections => ConnectionVoxels.Count();
    
    //The ratio (0.00 -> 1.00) between the number of voxels on the 
    //boundary of the space and the amount of voxels that
    //are connected to other spaces
    public float ConnectionRatio => NumberOfConnections / BoundaryVoxels.Count();
    
    //The spaces that are connected to this one
    public IEnumerable<PPSpace> NeighbourSpaces
    {
        get
        {
            HashSet<PPSpace> tempSpaces = new HashSet<PPSpace>();
            foreach (var voxel in ConnectionVoxels)
            {
                var neighbours = voxel.GetFaceNeighbours();
                foreach (var neighbour in neighbours)
                {
                    var nSpace = neighbour.ParentSpace;
                    if (nSpace != this)
                    {
                        tempSpaces.Add(nSpace);
                    }
                }
            }
            return tempSpaces.Distinct();
        }
    }
    
    //A Dictionary representing the connection lenght
    //in voxel units between this space and its neighbours
    public Dictionary<PPSpace,int> ConnectionLenghts
    {
        get
        {
            Dictionary<PPSpace, int> tempDictionary = new Dictionary<PPSpace, int>();
            foreach (var space in NeighbourSpaces)
            {
                var t = ConnectionVoxels.Count(v => v.GetFaceNeighbours().Any(n => n.ParentSpace == space));
                tempDictionary[space] = t;
            }
            return tempDictionary;
        }
    }

    //The average center of this space, used to create the data exposer
    Vector3 _center => new Vector3(
        Indices.Average(i => (float)i.x), 
        0, 
        Indices.Average(i => (float)i.z)) * _grid.VoxelSize;

    GameObject _infoArrow;
    

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
            voxel.InSpace = true;
            s.Indices.Add(vector);
            s.Voxels.Add(voxel);
        }

        s.CreateArrow();
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

    public void CreateArrow()
    {
        _infoArrow = GameObject.Instantiate(Resources.Load<GameObject>("GameObjects/InfoArrow"));
        _infoArrow.transform.position = _center;
        Debug.Log(_center);
    }

    public void InfoArrowVisibility(bool visible)
    {
        _infoArrow.SetActive(visible);
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
