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

    public static List<Part_BAK> ReadPartsAsList(VoxelGrid grid, string file)
    {
        List<Part_BAK> outList = new List<Part_BAK>();
        
        string jsonString = Resources.Load<TextAsset>(file).text;

        PartCollection_BAK partList = JsonUtility.FromJson<PartCollection_BAK>(jsonString);
        foreach (var part in partList.Parts)
        {
            Part_BAK p = new Part_BAK();

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

    public static List<StructuralPart> ReadStructureAsList(VoxelGrid grid, string file)
    {
        List<StructuralPart> outList = new List<StructuralPart>();
        string jsonString = Resources.Load<TextAsset>(file).text;
        SPartCollection partList = JsonUtility.FromJson<SPartCollection>(jsonString);

        foreach (var part in partList.Parts)
        {
            StructuralPart p = new StructuralPart();
            p.OCIndexes = part.OCIndexes;
            p.OrientationName = part.OrientationName;
            p.OccupiedIndexes = part.OccupiedIndexes;
            p.Height = part.Height;

            outList.Add(p.NewPart(grid));
        }
        return outList;
    }

    public static List<ConfigurablePart> ReadConfigurablesAsList(VoxelGrid grid, string file)
    {
        List<ConfigurablePart> outList = new List<ConfigurablePart>();
        string jsonString = Resources.Load<TextAsset>(file).text;
        CPartCollection partList = JsonUtility.FromJson<CPartCollection>(jsonString);

        foreach (var part in partList.Parts)
        {
            ConfigurablePart p = new ConfigurablePart();
            p.OCIndexes = part.OCIndexes;
            p.OrientationName = part.OrientationName;
            p.OccupiedIndexes = part.OccupiedIndexes;
            p.Height = part.Height;

            outList.Add(p.NewPart(grid));
        }

        return outList;
    }

    public static List<PPSpace> ReadSpacesAsList(VoxelGrid grid, string file)
    {
        List<PPSpace> outList = new List<PPSpace>();
        string jsonString = Resources.Load<TextAsset>(file).text;
        PPSpaceCollection spaceList = JsonUtility.FromJson<PPSpaceCollection>(jsonString);
        Debug.Log(spaceList);

        foreach (var space in spaceList.Spaces)
        {
            PPSpace s = new PPSpace();
            s.OCIndexes = space.OCIndexes;
            //s.OrientationName = part.OrientationName;
            //s.OccupiedIndexes = part.OccupiedIndexes;
            //s.Height = part.Height;

            outList.Add(s.NewSpace(grid));
        }

        return outList;
    }
}
