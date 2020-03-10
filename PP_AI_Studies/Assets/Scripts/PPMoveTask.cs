using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PPMoveTask : PPTask
{
    public Part Part;
    public Voxel OriginVoxel => Part.OriginVoxel;
    public Voxel TargetVoxel;
    new public string taskTitle => $"Move {Part.Type.ToString()} " +
                    $"from {OriginVoxel.Index.x}_{OriginVoxel.Index.y}_{OriginVoxel.Index.z} " +
                    $"to {TargetVoxel.Index.x}_{TargetVoxel.Index.y}_{TargetVoxel.Index.z}";

}
