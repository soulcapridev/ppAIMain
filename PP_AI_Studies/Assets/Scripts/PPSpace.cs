using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;

[System.Serializable]
public class PPSpace : IEquatable<PPSpace>
{
    //
    //FIELDS AND PROPERTIES
    //
    private VoxelGrid _grid;
    public HashSet<Voxel> Voxels = new HashSet<Voxel>();
    public HashSet<Vector3Int> Indices = new HashSet<Vector3Int>();
    public string OCIndexes; // Used to read Space data from Json file
    public string Name;
    public Tenant Tenant;
    public bool Occupied;

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
    public float ConnectionRatio => (float)Math.Round((float)NumberOfConnections / BoundaryVoxels.Count(), 2);
    
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
            return tempSpaces.Distinct().Where(s => s != null);
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
                tempDictionary.Add(space, t);
            }
            return tempDictionary;
        }
    }

    //The average center of this space, used to create the data exposer
    Vector3 _center => new Vector3(
        Indices.Average(i => (float)i.x), 
        0, 
        Indices.Average(i => (float)i.z)) * _grid.VoxelSize;

    //Game object used to visualize space data
    GameObject _infoArrow;
    
    //
    //CONSTRUCTORS
    //
    public PPSpace(VoxelGrid grid)
    {
        _grid = grid;
    }

    public PPSpace()
    {
        //This is a generic constructor. 
    }
    //
    //METHODS AND FUNCTIONS
    //
    public PPSpace NewSpace(VoxelGrid grid, string name)
    {
        //Method to create new spaces, read from a JSON file
        //Still not sure if this is unecessarily creating extra spaces
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
        s.Name = name;
        s.CreateArrow();
        return s;
    }

    public List<Voxel> DestroySpace()
    {
        //Destroys a space by removing and cleaning its voxels beforehand
        List<Voxel> orphans = new List<Voxel>();
        foreach (var voxel in Voxels)
        {
            voxel.InSpace = false;
            voxel.ParentSpace = null;
            orphans.Add(voxel);
        }
        Voxels = new HashSet<Voxel>();
        _infoArrow.GetComponent<InfoArrow>().SelfDestroy();
        return orphans;
    }

    public void CreateArrow()
    {
        //Instantiates the InfoArrow GameObject on the average center of the space
        //and sets this space to be referenced by the arrow
        _infoArrow = GameObject.Instantiate(Resources.Load<GameObject>("GameObjects/InfoArrow"));
        _infoArrow.transform.position = _center + new Vector3(0,1.5f,0);
        _infoArrow.GetComponent<InfoArrow>().SetSpace(this);
    }

    public void InfoArrowVisibility(bool visible)
    {
        //Sets the visibility / state of the space's InfoArrow
        _infoArrow.SetActive(visible);
    }

    public string GetSpaceInfo()
    {
        string output = "";
        string tab = "  ";
        string breakLine = "\n";
        
        string nameHeader = $"[{Name}]";
        
        string sizeHeader = $"[Size Parameters]";
        string area = $"Area: {nVoxels} voxels";
        string averageX = $"Average X Width: {AverageXWidth} voxels";
        string averageZ = $"Average Z Width: {AverageZWidth} voxels";

        string connectivityHeader = $"[Connectivity Parameters]";
        string connections = $"Connections: {NumberOfConnections} voxels";
        string boundary = $"Boundary Length: {BoundaryVoxels.Count()} voxels";
        string connectivityRatio = $"Connectivity Ratio: {ConnectionRatio}";

        string neighboursHeader = "[Neighbours]";
        string neighbours = "";
        foreach (var neighbour in NeighbourSpaces)
        {
            string name = neighbour.Name;
            string length = ConnectionLenghts[neighbour].ToString();

            neighbours += tab + tab + name + ": " + length + "voxels" + breakLine;

        }
        output = nameHeader + breakLine +
            sizeHeader + breakLine +
            tab + area + breakLine +
            tab + averageX + breakLine +
            tab + averageZ + breakLine +
            breakLine +
            connectivityHeader + breakLine +
            tab + connections + breakLine +
            tab + boundary + breakLine +
            tab + connectivityRatio + breakLine +
            tab + neighboursHeader + breakLine +
            neighbours;

        return output;
    }

    public Vector3 GetCenter()
    {
        return _center;
    }

    //Equality checking
    public bool Equals(PPSpace other)
    {
        return (other != null && Voxels.Count == other.Voxels.Count && Voxels.All(other.Voxels.Contains));
    }
    public override int GetHashCode()
    {
        //return Voxels.Sum(v => v.GetHashCode());
        return Voxels.GetHashCode();
    }
}

public class PPSpaceCollection
{
    //Class to hold the data read from the JSON file
    public PPSpace[] Spaces;
}