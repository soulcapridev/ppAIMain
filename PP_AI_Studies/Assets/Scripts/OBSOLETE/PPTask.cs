using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

[System.Serializable]
public class PPTask : IEquatable<PPTask>
{
    public Tenant Tenant;
    public string TenantName;
    public string TaskTitle;
    public int TaskDay;
    public int TaskTime;
    public int Cost;
    public float TaskProbability;
    public bool AiSuggested;
    public bool AiAccepted;

    public void NewTenant()
    {
        Tenant = new Tenant();
        Tenant.Name = TenantName;
    }

    public PPTask CopyTask()
    {
        PPTask newTask = new PPTask();

        newTask.Tenant = Tenant;
        newTask.TenantName = TenantName;
        newTask.TaskTitle = TaskTitle;
        newTask.TaskDay = TaskDay;
        newTask.TaskTime = TaskTime;
        newTask.TaskProbability = TaskProbability;
        newTask.AiSuggested = AiSuggested;
        newTask.AiAccepted = AiAccepted;

        return newTask;
    }

    public bool Equals(PPTask other)
    {
        //Task other = value as Task;

        return (other != null) 
            && (TenantName == other.TenantName) 
            && (TaskTitle == other.TaskTitle)
            && (TaskDay == other.TaskDay)
            && (TaskTime == other.TaskTime);
    }

    public override int GetHashCode()
    {
        return TenantName.GetHashCode() + TaskTitle.GetHashCode() + TaskDay.GetHashCode() + TaskTime.GetHashCode();
    }
}

[System.Serializable]
public class TaskCollection
{
    public PPTask[] Tasks;
}
