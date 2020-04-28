using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

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
            t.TenantName = task.TenantName;
            t.NewTenant();
            t.TaskDay = task.TaskDay;
            t.TaskProbability = task.TaskProbability;
            t.TaskTime = task.TaskTime;
            t.TaskTitle = task.TaskTitle;
            outList.Add(t);
        }
        return outList;
    }

    public static List<PPSpaceRequest> ReadSpaceRequests(string file, List<Tenant> tenantList)
    {
        List<PPSpaceRequest> requests = new List<PPSpaceRequest>();
        string jsonString = Resources.Load<TextAsset>(file).text;
        PPRequestCollection requestList = JsonUtility.FromJson<PPRequestCollection>(jsonString);
        var existingTenantNames = tenantList.Select(t => t.Name).ToList();
        List<Tenant> newTenants = new List<Tenant>();
        List<string> newTenantsNames = new List<string>();
        foreach (var request in requestList.Requests)
        {
            if (existingTenantNames.Contains(request.TenantName))
            {
                request.Tenant = tenantList[existingTenantNames.IndexOf(request.TenantName)];
            }
            else
            {
                var nt = request.NewTenant();
                newTenants.Add(nt);
                newTenantsNames.Add(request.TenantName);
            }
            request.Function = (SpaceFunction)System.Enum.Parse(typeof(SpaceFunction), request.FunctionName, false);
            var probabilities = request.Probabilities_S.Split('_');
            request.RequestProbability = new Dictionary<int, float>();
            for (int i = 0; i < probabilities.Length; i++)
            {
                float probability = int.Parse(probabilities[i]) / 100.00f;
                request.RequestProbability.Add(i, probability);
            }
            requests.Add(request);
            
        }
        return requests;
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
        int count = 0;
        foreach (var space in spaceList.Spaces)
        {
            PPSpace s = new PPSpace();
            s.OCIndexes = space.OCIndexes;
            outList.Add(s.NewSpace(grid, $"Space_{count++}"));
        }

        return outList;
    }
}
