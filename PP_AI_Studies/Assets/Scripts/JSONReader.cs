using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class JSONReader
{ 
    public static List<PPTask> ReadTasksAsList()
    {
        List<PPTask> outList = new List<PPTask>();
        string jsonString = Resources.Load<TextAsset>("Input Data/U_Tasks").text;

        TaskCollection taskList = JsonUtility.FromJson<TaskCollection>(jsonString);
        foreach (var task in taskList.Tasks)
        {
            PPTask t = new PPTask();

            t.tenantName = task.tenantName;
            t.NewTenant();

            t.taskDay = task.taskDay;
            t.taskProbability = task.taskProbability;
            t.taskTime = task.taskTime;
            t.taskTitle = task.taskTitle;
            outList.Add(t);
        }


        return outList;
    }

    public static List<Part> ReadPartsAsList(VoxelGrid grid, string file)
    {
        List<Part> outList = new List<Part>();
        
        string jsonString = Resources.Load<TextAsset>(file).text;

        PartCollection partList = JsonUtility.FromJson<PartCollection>(jsonString);
        foreach (var part in partList.Parts)
        {
            Part p = new Part();

            p.TypeName = part.TypeName;
            p.OrientationName = part.OrientationName;
            p.ReferenceX = part.ReferenceX;
            p.ReferenceY = part.ReferenceY;
            p.ReferenceZ = part.ReferenceZ;
            p.Height = part.Height;

            outList.Add(p.NewPart(grid));
        }
        return outList;
    }
}
