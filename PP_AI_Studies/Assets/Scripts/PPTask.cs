using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

[System.Serializable]
public class PPTask : IEquatable<PPTask>
{
    public Tenant tenant;
    public string tenantName;
    public string taskTitle;
    public int taskDay;
    public int taskTime;
    public int Cost;
    public float taskProbability;
    public bool aiSuggested;
    public bool aiAccepted;

    public void NewTenant()
    {
        tenant = new Tenant();
        tenant.name = tenantName;
    }

    public PPTask CopyTask()
    {
        PPTask newTask = new PPTask();

        newTask.tenant = tenant;
        newTask.tenantName = tenantName;
        newTask.taskTitle = taskTitle;
        newTask.taskDay = taskDay;
        newTask.taskTime = taskTime;
        newTask.taskProbability = taskProbability;
        newTask.aiSuggested = aiSuggested;
        newTask.aiAccepted = aiAccepted;

        return newTask;
    }

    public bool Equals(PPTask other)
    {
        //Task other = value as Task;

        return (other != null) 
            && (tenantName == other.tenantName) 
            && (taskTitle == other.taskTitle)
            && (taskDay == other.taskDay)
            && (taskTime == other.taskTime);
    }

    public override int GetHashCode()
    {
        return tenantName.GetHashCode() + taskTitle.GetHashCode() + taskDay.GetHashCode() + taskTime.GetHashCode();
    }
}

[System.Serializable]
public class TaskCollection
{
    public PPTask[] Tasks;
}
