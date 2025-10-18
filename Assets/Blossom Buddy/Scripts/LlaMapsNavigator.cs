using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using Meta.XR;
using Random = UnityEngine.Random;

using Meta.XR.BuildingBlocks.AIBlocks;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using UnityEngine;
using Meta.XR;
using System;

using UnityEngine.Networking;
using System.Threading.Tasks;


public class LlaMapsNavigator : MonoBehaviour
{
    [Header("Backend")]
    public string baseUrl;

    public string destination;
    public string mode;

    [Header("Arrow")]
    public Arrow arrow;

    [Header("Polling")]
    public float locationPollSeconds = 0.5f;

    [Serializable] public class LatLng { public float lat; public float lng; }
    [Serializable] public class RouteRequest { public string destination; public string travelMode; }
    [Serializable]
    public class RouteResponse
    {
        public bool success;
        public List<LatLng> coords;

        public string error;
    }

    public class LocationResponse
    {
        public bool success;
        public LatLng position;

        public string error;
    }

    private List<Vector2> _route;

    // polling state sans coroutines
    private float _pollTimer = 0f;

    private bool _draw = true;

    private async Task Start()
    {   
        if (destination == null || mode == null)
        {
            return;
        }
        // Debug.LogWarning("Navigator Started");
        _route = await RequestRoute(destination, mode);
        // Debug.LogWarning(_route);
        var pos = await RequestLocation();
        var dir2D = ComputeNextDirection2D(pos);
        // Debug.LogWarning("start flowering drawing");
        arrow?.Draw(dir2D);
        // Debug.LogWarning(pos.ToString());
        // Debug.LogWarning(dir2D.ToString());
        // Debug.LogWarning("flower drawn");
        // Debug.LogWarning("Navigator start ended");
    }
    
    private async Task Update()
    {
        if (OVRInput.GetDown(OVRInput.Button.One))
        {
            _draw = !_draw;
        }
        if (_route == null) return;
        if (!_draw) return;

        Debug.LogWarning("DRAWING...");
        Debug.LogWarning(_draw);

        _pollTimer = 0f;
        var pos = await RequestLocation();
        var dir2D = ComputeNextDirection2D(pos);
        arrow?.Draw(dir2D);
    }

    // ===== Networking (async) =====

    public async Task<Vector2> RequestLocation()
    {
        // Debug.LogWarning("Location requested");
        using var req = new UnityWebRequest($"{baseUrl}/api/location", "GET");
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");
        await req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
            throw new Exception(req.error);

        var resp = JsonUtility.FromJson<LocationResponse>(req.downloadHandler.text);
        if (resp == null || !resp.success)
            throw new Exception($"Route fetch failed: {req.downloadHandler.text}");

        Vector2 pos = new Vector2(resp.position.lat, resp.position.lng);

        // Debug.LogWarning(pos.ToString());
        return pos;
    }

    public async Task<List<Vector2>> RequestRoute(string dest, string mode)
    {

        // Debug.LogWarning(dest);
        // Debug.LogWarning(mode);
        // Debug.LogWarning("Route requested");

        var reqObj = new RouteRequest { destination = dest, travelMode = mode };
        string json = JsonUtility.ToJson(reqObj);

        using var req = new UnityWebRequest($"{baseUrl}/api/route", "POST");
        req.uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(json));
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");

        await req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
            throw new Exception(req.error);

        var resp = JsonUtility.FromJson<RouteResponse>(req.downloadHandler.text);
        if (resp == null || !resp.success || resp.coords == null || resp.coords.Count == 0)
            throw new Exception($"Route fetch failed: {req.downloadHandler.text}");

        var route = resp.coords
                    .Select(ll => new Vector2((float)ll.lng, (float)ll.lat))
                    .ToList();

        return route;
    }

    // ===== Geometry & Navigation =====

    private Vector2 ComputeNextDirection2D(Vector2 pos)
    {
        var target = GetClosestPathPos(pos);
        if (target < _route.Count - 1)
        {
            target++;
        }

        Vector2 direction = (_route[target] - pos).normalized;
        Debug.LogWarning(direction.ToString());
        return direction;
    }

    private int GetClosestPathPos(Vector2 p)
    {
        float minDist = float.MaxValue;
        int closestIndex = 0;

        for (int i = 0; i < _route.Count; i++)
        {
            float sq = (_route[i] - p).sqrMagnitude;
            if (sq < minDist)
            {
                minDist = sq; closestIndex = i;
            }
        }

        return closestIndex;
    }
}
