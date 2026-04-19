using System;
using System.Collections.Generic;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using ECommons.EzIpcManager;

public class VnavmeshIPC
{
    // ---------------- Nav ----------------

    [EzIPC("vnavmesh.Nav.IsReady", applyPrefix: false)]
    public readonly Func<bool> IsReady;

    [EzIPC("vnavmesh.Nav.BuildProgress", applyPrefix: false)]
    public readonly Func<float> BuildProgress;

    [EzIPC("vnavmesh.Nav.Reload", applyPrefix: false)]
    public readonly Func<bool> Reload;

    [EzIPC("vnavmesh.Nav.Rebuild", applyPrefix: false)]
    public readonly Func<bool> Rebuild;

    /// <summary>
    /// Vector3 from, Vector3 to, bool fly
    /// </summary>
    [EzIPC("vnavmesh.Nav.Pathfind", applyPrefix: false)]
    public readonly Func<Vector3, Vector3, bool, Task<List<Vector3>>> Pathfind;

    /// <summary>
    /// Vector3 from, Vector3 to, bool fly, CancellationToken token
    /// </summary>
    [EzIPC("vnavmesh.Nav.PathfindCancelable", applyPrefix: false)]
    public readonly Func<Vector3, Vector3, bool, CancellationToken, Task<List<Vector3>>> PathfindCancelable;

    [EzIPC("vnavmesh.Nav.PathfindCancelAll", applyPrefix: false)]
    public readonly Action PathfindCancelAll;

    [EzIPC("vnavmesh.Nav.PathfindInProgress", applyPrefix: false)]
    public readonly Func<bool> PathfindInProgressNav;

    [EzIPC("vnavmesh.Nav.PathfindNumQueued", applyPrefix: false)]
    public readonly Func<int> PathfindNumQueued;

    [EzIPC("vnavmesh.Nav.IsAutoLoad", applyPrefix: false)]
    public readonly Func<bool> IsAutoLoad;

    [EzIPC("vnavmesh.Nav.SetAutoLoad", applyPrefix: false)]
    public readonly Action<bool> SetAutoLoad;


    // ---------------- SimpleMove ----------------

    [EzIPC("vnavmesh.SimpleMove.PathfindAndMoveTo", applyPrefix: false)]
    public readonly Func<Vector3, bool, bool> PathfindAndMoveTo;

    /// <summary>
    /// Vector3 target, bool fly, float distance
    /// </summary>
    [EzIPC("vnavmesh.SimpleMove.PathfindAndMoveCloseTo", applyPrefix: false)]
    public readonly Func<Vector3, bool, float, bool> PathfindAndMoveCloseTo;

    [EzIPC("vnavmesh.SimpleMove.PathfindInProgress", applyPrefix: false)]
    public readonly Func<bool> PathfindInProgress;


    // ---------------- Path ----------------

    [EzIPC("vnavmesh.Path.Stop", applyPrefix: false)]
    public readonly Action Stop;

    [EzIPC("vnavmesh.Path.IsRunning", applyPrefix: false)]
    public readonly Func<bool> IsRunning;

    [EzIPC("vnavmesh.Path.NumWaypoints", applyPrefix: false)]
    public readonly Func<int> NumWaypoints;

    [EzIPC("vnavmesh.Path.ListWaypoints", applyPrefix: false)]
    public readonly Func<List<Vector3>> ListWaypoints;

    [EzIPC("vnavmesh.Path.GetMovementAllowed", applyPrefix: false)]
    public readonly Func<bool> GetMovementAllowed;

    [EzIPC("vnavmesh.Path.SetMovementAllowed", applyPrefix: false)]
    public readonly Action<bool> SetMovementAllowed;

    [EzIPC("vnavmesh.Path.GetAlignCamera", applyPrefix: false)]
    public readonly Func<bool> GetAlignCamera;

    [EzIPC("vnavmesh.Path.SetAlignCamera", applyPrefix: false)]
    public readonly Action<bool> SetAlignCamera;

    [EzIPC("vnavmesh.Path.GetTolerance", applyPrefix: false)]
    public readonly Func<float> GetTolerance;

    [EzIPC("vnavmesh.Path.SetTolerance", applyPrefix: false)]
    public readonly Action<float> SetTolerance;


    // ---------------- Query.Mesh ----------------

    /// <summary>
    /// Vector3 p, float halfExtentXZ, float halfExtentY
    /// </summary>
    [EzIPC("vnavmesh.Query.Mesh.NearestPoint", applyPrefix: false)]
    public readonly Func<Vector3, float, float, Vector3?> NearestPoint;

    /// <summary>
    /// Vector3 p, bool allowUnlandable, float halfExtentXZ
    /// </summary>
    [EzIPC("vnavmesh.Query.Mesh.PointOnFloor", applyPrefix: false)]
    public readonly Func<Vector3, bool, float, Vector3?> PointOnFloor;

    /// <summary>
    /// Vector3 from, Vector3 to
    /// true = blocked, false = clear
    /// </summary>
    [EzIPC("vnavmesh.Query.Mesh.Raycast", applyPrefix: false)]
    public readonly Func<Vector3, Vector3, bool> Raycast;
    

    // ---------------- Window ----------------

    [EzIPC("vnavmesh.Window.IsOpen", applyPrefix: false)]
    public readonly Func<bool> IsOpen;

    [EzIPC("vnavmesh.Window.SetOpen", applyPrefix: false)]
    public readonly Action<bool> SetOpen;


    // ---------------- DTR ----------------

    [EzIPC("vnavmesh.DTR.IsShown", applyPrefix: false)]
    public readonly Func<bool> IsShown;

    [EzIPC("vnavmesh.DTR.SetShown", applyPrefix: false)]
    public readonly Action<bool> SetShown;


    public VnavmeshIPC()
    {
        EzIPC.Init(this, "vnavmesh", SafeWrapper.AnyException);
    }
}
