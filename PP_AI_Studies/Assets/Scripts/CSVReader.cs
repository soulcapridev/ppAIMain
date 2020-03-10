using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;

public static class CSVReader
{
   public static int[] ReadDWP(string fileName)
    {
        string file = Resources.Load<TextAsset>(fileName).text;
        var lines = file.Split('\n').ToArray();

        return lines.Select(l => Int32.Parse(l)).ToArray();
    }

    public static string[] ReadNames(string fileName)
    {
        string file = Resources.Load<TextAsset>(fileName).text;

        return file.Split('\n').ToArray();
    }
    public static void SetGridState(VoxelGrid grid, string filename)
    {
        string file = Resources.Load<TextAsset>(filename).text;
        var lines = file.Split('\n').ToArray();
        foreach (var line in lines)
        {
            var comps = line.Split('_').ToArray();
            var x = int.Parse(comps[0]);
            var y = int.Parse(comps[1]);
            var z = int.Parse(comps[2]);
            var b = bool.Parse(comps[3]);
            grid.Voxels[x, y, z].IsActive = b;
        }
    }
}
