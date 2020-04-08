using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VoxelGrid
{
    public Vector3Int Size;
    public Voxel[,,] Voxels;
    public Face[][,,] Faces = new Face[3][,,];
    public float VoxelSize;
    public Vector3 Origin;

    public VoxelGrid(Vector3Int size, float voxelSize, Vector3 origin)
    {
        Size = size;
        VoxelSize = voxelSize;
        Origin = origin;

        Voxels = new Voxel[Size.x, Size.y, Size.z];

        for (int x = 0; x < Size.x; x++)
        {
            for (int y = 0; y < Size.y; y++)
            {
                for (int z = 0; z < Size.z; z++)
                {
                    Voxels[x, y, z] = new Voxel(new Vector3Int(x, y, z), this);
                }
            }
        }

        // make faces (from https://github.com/ADRC4/Voxel)
        Faces[0] = new Face[Size.x + 1, Size.y, Size.z];

        for (int x = 0; x < Size.x + 1; x++)
            for (int y = 0; y < Size.y; y++)
                for (int z = 0; z < Size.z; z++)
                {
                    Faces[0][x, y, z] = new Face(x, y, z, Axis.X, this);
                }

        Faces[1] = new Face[Size.x, Size.y + 1, Size.z];

        for (int x = 0; x < Size.x; x++)
            for (int y = 0; y < Size.y + 1; y++)
                for (int z = 0; z < Size.z; z++)
                {
                    Faces[1][x, y, z] = new Face(x, y, z, Axis.Y, this);
                }

        Faces[2] = new Face[Size.x, Size.y, Size.z + 1];

        for (int x = 0; x < Size.x; x++)
            for (int y = 0; y < Size.y; y++)
                for (int z = 0; z < Size.z + 1; z++)
                {
                    Faces[2][x, y, z] = new Face(x, y, z, Axis.Z, this);
                }

    }

    public List<Voxel> ActiveVoxelsAsList()
    {
        List<Voxel> outList = new List<Voxel>();
        for (int x = 0; x < Size.x; x++)
        {
            for (int y = 0; y < Size.y; y++)
            {
                for (int z = 0; z < Size.z; z++)
                {
                    if (Voxels[x, y, z].IsActive) outList.Add(Voxels[x, y, z]);
                }
            }
        }

        return outList;
    }

    public void ClearGrid()
    {
        for (int x = 0; x < Size.x; x++)
        {
            for (int y = 0; y < Size.y; y++)
            {
                for (int z = 0; z < Size.z; z++)
                {
                    var v = Voxels[x, y, z];
                    if (v.IsActive && v.IsOccupied && v.Part.Type != PartType.Structure)
                    {
                        v.IsOccupied = false;
                        v.Part = null;
                    }
                }
            }
        }
    }

    // Get faces (from https://github.com/ADRC4/Voxel)
    public IEnumerable<Face> GetFaces()
    {
        for (int n = 0; n < 3; n++)
        {
            int xSize = Faces[n].GetLength(0);
            int ySize = Faces[n].GetLength(1);
            int zSize = Faces[n].GetLength(2);

            for (int x = 0; x < xSize; x++)
                for (int y = 0; y < ySize; y++)
                    for (int z = 0; z < zSize; z++)
                    {
                        yield return Faces[n][x, y, z];
                    }
        }
    }
}