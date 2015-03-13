//Copyright 2015 by Nicholas Harshfield. All Rights Reserved.
//View associated license file in root directory for details.

using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;
using System.Collections.Generic;

public class GridManager : MonoBehaviour {

    private const float PAD_X = 3.1f;
    private const float PAD_Y = 1.8f;
    private const float SIZE  = 2.5f;
    private const float LEVEL = 0.0f;

    public int height = 13;
    public int width = 23;
    public Material openMaterial, obstacleMaterial, originMaterial, objectiveMaterial, lineMaterial, pathMaterial;
    public Text info, gridSizeText, countPanel, distancePanel, gridWidth, gridHeight;
    
    GameObject[,] hexGrid;
    ArrayList lines;
    ArrayList counters;
    ArrayList log;
    ArrayList distanceLog;
    ArrayList directionLog;
    ArrayList next;
    bool origin, objective, cameraDrag;
    int originX, originY, objectiveX, objectiveY, count, distance;
    Vector3 cameraOrigin, cameraDiff;
    
	void Start()
    {
        //instantiate the hex grid and data structures
        lines = new ArrayList();
        counters = new ArrayList();
        log = new ArrayList();
        distanceLog = new ArrayList();
        directionLog = new ArrayList();
        next = new ArrayList();
        
        //initialize variables (negative == N/A)
        count = 0;
        distance = 0;
        
        //set some default text
        gridWidth.text = width.ToString();
        gridHeight.text = height.ToString();
        
        //initialize the hexagon grid
        InitializeGrid();
	}
    
    void InitializeGrid()
    {
        //initialize hex grid array
        hexGrid = new GameObject[width, height];
        
        //limit lower width and height bounds
        if (width < 5) width = 5;
        if (height < 5) height = 5;
        
        //limit upper width and height bounds
        if (width > 99) width = 99;
        if (height > 99) height = 99;
    
        //initialize hexagon game objects
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                //skip hexagon creation for last in every other row
                if (y % 2 == 0 && x == width - 1) continue;
                
                //create and name
                GameObject newHex = new GameObject("Hex[" + x + "," + y + "]");
                
                //attach and configure hexagon script
                Hexagon hexComp = (Hexagon)newHex.AddComponent<Hexagon>();
                hexComp.Create(x, y, this);
                
                //place and scale the new hex
                newHex.transform.localScale = new Vector3(SIZE, 1.0f, SIZE);
                if (y % 2 == 0) 
                {
                    newHex.transform.position = new Vector3(x * (SIZE + PAD_X) + (SIZE + PAD_X) / 2, LEVEL, y * (SIZE + PAD_Y));
                }
                else
                {
                    newHex.transform.position = new Vector3(x * (SIZE + PAD_X), LEVEL, y * (SIZE + PAD_Y));
                }
                
                //store pointer for our grid
                hexGrid[x, y] = newHex;
            }
        }
        
        //focus camera on the center of the middle-most hex
        Vector3 center = hexGrid[width / 2, height / 2].GetComponent<Renderer>().bounds.center;
        Camera.main.transform.position = new Vector3(center.x - SIZE / 2, 60f, center.z);
    }
    
    void DestroyGrid()
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (hexGrid[x, y] != null)
                {
                    Destroy(hexGrid[x, y]);
                }
            }
        }
        hexGrid = null;
    }
    
    void Update()
    {
        //keep time cost panel updated
        if (count == 0) countPanel.text = "N/A";
        else countPanel.text = count + "";
        
        //keep distance panel updated
        if (distance == 0) distancePanel.text = "N/A";
        else distancePanel.text = distance + "";
        
        //parse for user key inputs
        if (Input.GetKey(KeyCode.B))
        {
            if (origin && objective)
            {
                ClearSearch();
                info.text = "Running a breadth-first search...";
                log.Add(new Tuple<int, int>(originX, originY));
                directionLog.Add(null);
                ArrayList path = BFS(originX, originY, objectiveX, objectiveY);
                if (path != null && path.Count > 0)
                {
                    for (int i = 0; i < path.Count - 1; i++)
                    {
                        Tuple<int, int> n1 = (Tuple<int, int>)path[i];
                        Tuple<int, int> n2 = (Tuple<int, int>)path[i + 1];
                        DrawPath(n1.Item1, n1.Item2, n2.Item1, n2.Item2);
                    }
                    info.text = "A valid path from Origin to Objective was found!";
                }
                else
                {
                    info.text = "No path between Origin and Objective could be found!";
                }
            }
            else
            {
                info.text = "Please specify Origin and Objective cells before running search!";
            }
        }
        else if (Input.GetKey(KeyCode.D))
        {
            if (origin && objective)
            {
                ClearSearch();
                info.text = "Running a depth-first search...";
                log.Add(new Tuple<int, int>(originX, originY));
                ArrayList path = DFS(originX, originY, objectiveX, objectiveY);
                if (path != null && path.Count > 0)
                {
                    for (int i = 0; i < path.Count - 1; i++)
                    {
                        Tuple<int, int> n1 = (Tuple<int, int>)path[i];
                        Tuple<int, int> n2 = (Tuple<int, int>)path[i + 1];
                        DrawPath(n1.Item1, n1.Item2, n2.Item1, n2.Item2);
                    }
                    info.text = "A valid path from Origin to Objective was found!";
                }
                else
                {
                    info.text = "No path between Origin and Objective could be found!";
                }
            }
            else
            {
                info.text = "Please specify Origin and Objective cells before running search!";
            }
        }
        else if (Input.GetKey(KeyCode.A))
        {
            if (origin && objective)
            {
                ClearSearch();
                info.text = "Running an A* search...";
                log.Add(new Tuple<int, int>(originX, originY));
                directionLog.Add(null);
                distanceLog.Add(GetDistanceBetweenCells(originX, originY, objectiveX, objectiveY));
                ArrayList path = AStar(originX, originY, objectiveX, objectiveY);
                if (path != null && path.Count > 0)
                {
                    for (int i = 0; i < path.Count - 1; i++)
                    {
                        Tuple<int, int> n1 = (Tuple<int, int>)path[i];
                        Tuple<int, int> n2 = (Tuple<int, int>)path[i + 1];
                        DrawPath(n1.Item1, n1.Item2, n2.Item1, n2.Item2);
                    }
                    info.text = "A valid path from Origin to Objective was found!";
                }
                else
                {
                    info.text = "No path between Origin and Objective could be found!";
                }
            }
            else
            {
                info.text = "Please specify Origin and Objective cells before running search!";
            }
        }
        else if (Input.GetKey(KeyCode.R))
        {
            ResetBoard();
            info.text = "The board has been reset.";
        }
    }
    
    void LateUpdate()
    {
        //control camera dragging with mouse right click
        if (Input.GetMouseButton(1)) 
        {
            cameraDiff = Camera.main.ScreenToWorldPoint(Input.mousePosition) - Camera.main.transform.position;
            if (!cameraDrag) 
            {
                cameraDrag = true;
                cameraOrigin = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            }
        }
        else cameraDrag = false;
        if (cameraDrag) Camera.main.transform.position = cameraOrigin - cameraDiff;
        
        //control zoom by modifying orthographic camera size on mouse wheel
        float mouseScroll = Input.GetAxis("Mouse ScrollWheel");
        Camera.main.orthographicSize += mouseScroll * -12f;
    }
    
    public void ResetBoard()
    {
        //forget everything and destroy grid
        ClearSearch();
        ClearOrigin();
        ClearObjective();
        DestroyGrid();
        
        //fetch parameters from UI text input
        if (!Int32.TryParse(gridWidth.text, out width)) width = 5;
        if (!Int32.TryParse(gridHeight.text, out height)) height = 5;
        
        //initialize grid with the new width and height
        InitializeGrid();
    }
    
    public bool HasOrigin()
    {
        return origin;
    }
    
    public bool HasObjective()
    {
        return objective;
    }
    
    public void ClearOrigin()
    {
        origin = false;
    }
    
    public void ClearObjective()
    {
        objective = false;
    }
    
    public void SetOrigin(int x, int y)
    {
        originX = x;
        originY = y;
        origin = true;
    }
    
    public void SetObjective(int x, int y)
    {
        objectiveX = x;
        objectiveY = y;
        objective = true;
    }
    
    public Material GetMaterial(HexState state)
    {
        switch (state) {
            case HexState.Open:
                return openMaterial;
            case HexState.Obstacle:
                return obstacleMaterial;    
            case HexState.Origin:
                return originMaterial;
            case HexState.Objective:
                return objectiveMaterial;
            default:
                return null;
        }
    }
    
    enum HexDirection { LU, L, LD, RU, R, RD };
    HexDirection[] GetDirections()
    {
        return new HexDirection[] 
        {
            HexDirection.LU,
            HexDirection.L,
            HexDirection.LD,
            HexDirection.RD,
            HexDirection.R,
            HexDirection.RU
        };
    }
    
    HexDirection RotateDirection(HexDirection direction)
    {
        switch (direction)
        {
            case HexDirection.L:
                return HexDirection.LU;
            case HexDirection.LD:
                return HexDirection.L;
            case HexDirection.RD:
                return HexDirection.LD;
            case HexDirection.R:
                return HexDirection.RD;
            case HexDirection.RU:
                return HexDirection.R;
            case HexDirection.LU:
                return HexDirection.RU;
            default:
                string error = "Invalid HexDirection in RotateDirection()!";
                throw new Exception(error);
        }
    }
    
    HexDirection FlipDirection(HexDirection direction)
    {
        switch (direction)
        {
            case HexDirection.L:
                return HexDirection.R;
            case HexDirection.LD:
                return HexDirection.RU;
            case HexDirection.RD:
                return HexDirection.LU;
            case HexDirection.R:
                return HexDirection.L;
            case HexDirection.RU:
                return HexDirection.LD;
            case HexDirection.LU:
                return HexDirection.RD;
            default:
                string error = "Invalid HexDirection in FlipDirection()!";
                throw new Exception(error);
        }
    }
    
    float GetDistanceBetweenCells(int x1, int y1, int x2, int y2)
    {
        Vector3 p1 = hexGrid[x1, y1].transform.position;
        Vector3 p2 = hexGrid[x2, y2].transform.position;
        float d = Vector3.Distance(p1, p2);
        return d;
    }
    
    Tuple<int, int> GetAdjacentHex(HexDirection direction, int x, int y)
    {
        int destX = x, destY = y;
        switch (direction)
        {
            case HexDirection.LU:
                destY = y + 1;
                if (y % 2 != 0) 
                {
                    destX = x - 1;
                }
                break;
                
            case HexDirection.L:
                destX = x - 1;
                break;
                
            case HexDirection.LD:
                destY = y - 1;
                if (y % 2 != 0)
                {
                    destX = x - 1;
                }
                break;
                
            case HexDirection.RD:
                destY = y - 1;
                if (y % 2 == 0)
                {
                    destX = x + 1;
                }
                break;
                
            case HexDirection.R:
                destX = x + 1;
                break;
                
            case HexDirection.RU:
                destY = y + 1;
                if (y % 2 == 0)
                {
                    destX = x + 1;
                }
                break;
        }
        return new Tuple<int, int>(destX, destY);
    }
    
    bool IsValidHex(int x, int y)
    {
        //enforce boundary constraints
        if (y % 2 == 0)
        {
            if (x < 0 || y < 0 || x > width - 2 || y > height - 1)
            {
                return false;
            }
        }
        else
        {
            if (x < 0 || y < 0 || x > width - 1 || y > height - 1)
            {
                return false;
            }
        }
        
        //given a valid cell, check if state is openMaterial
        return hexGrid[x, y].GetComponent<Hexagon>().state == HexState.Open;
    }
    
    bool IsOpenHex(HexDirection direction, int x, int y)
    {
        //determine the openness of the target cell
        //draw a path if valid and return openness
        if (IsValidHex(x, y))
        {
            return true;
        }
        
        //any other state is not open
        return false;
    }
    
    ArrayList DFS(int x, int y, int objectiveX, int objectiveY)
    {
        //check all the surrounding hexagons for paths
        foreach (HexDirection direction in GetDirections())
        {
            count++;
            
            //get the coordinates of the cell in the given direction
            Tuple<int, int> target = GetAdjacentHex(direction, x, y);
            int targetX = target.Item1, targetY = target.Item2;
            
            //skip this cell if we've already been here:
            if (log.Contains(target))
            {
                continue;
            }
            
            //did we make it?!?
            if (targetX == objectiveX && targetY == objectiveY)
            {
                //cool! let's visualize the search path and return true!
                DrawLine(x, y, targetX, targetY);
                ArrayList path = new ArrayList();
                path.Add(target);
                path.Add(new Tuple<int, int>(x, y));
                distance = 1;
                return path;
            }
            
            //else mark this adjacent hex so that we never return to it
            log.Add(new Tuple<int, int>(targetX, targetY));
            
            //check if this adjacent hex is both valid & open
            if (IsOpenHex(direction, targetX, targetY))
            {
                //cool! let's visualize the search path
                DrawLine(x, y, targetX, targetY);
                
                //keep going deeper in this direction...
                return DFS(targetX, targetY, objectiveX, objectiveY);
            }
        }
        
        //when all directional paths exhausted...
        return null;
    }
    
    ArrayList BFS(int x, int y, int objectiveX, int objectiveY)
    {
        //check all the surrounding hexagons for paths
        foreach (HexDirection direction in GetDirections())
        {
            //increment counter for each direction searched
            count++;
            
            //get the coordinates of the cell in each direction
            Tuple<int, int> targetHex = GetAdjacentHex(direction, x, y);
            
            //did we make it? if so, let's visualize the last jump
            //also, we return a new list with only the objective node
            if (targetHex.Item1 == objectiveX && targetHex.Item2 == objectiveY)
            {
                DrawLine(x, y, targetHex.Item1, targetHex.Item2);
                ArrayList path = new ArrayList();
                path.Add(targetHex);
                path.Add(new Tuple<int, int>(x, y));
                distance = 1;
                return path;
            }
            
            //check if this adjacent hex is both valid & open
            //if so, visualize and log it so we return to it later
            else if (IsOpenHex(direction, targetHex.Item1, targetHex.Item2))
            {
                DrawLine(x, y, targetHex.Item1, targetHex.Item2);
                log.Add(new Tuple<int, int>(targetHex.Item1, targetHex.Item2));
                directionLog.Add(direction);
                next.Add(new Tuple<int, int>(targetHex.Item1, targetHex.Item2));
            }
        }
        
        //invoke recursive helper function
        ArrayList bfs = null;
        while (bfs == null && next.Count > 0)
        {
            bfs = BFSHelper(objectiveX, objectiveY);
        }
        return bfs;
    }
    
    ArrayList BFSHelper(int objectiveX, int objectiveY)
    {
        ArrayList temp = new ArrayList();
        for (int i=0; i<next.Count; i++)
        {
            Tuple<int, int> edgeHex = (Tuple<int, int>)next[i];
            int x = edgeHex.Item1;
            int y = edgeHex.Item2;
            foreach (HexDirection direction in GetDirections())
            {
                count++;
                Tuple<int, int> targetHex = GetAdjacentHex(direction, x, y);
                int tX = targetHex.Item1;
                int tY = targetHex.Item2;
                if (log.Contains(targetHex))
                {
                    continue;
                }
                else if (tX == objectiveX && tY == objectiveY)
                {
                    DrawLine(x, y, tX, tY);
                    ArrayList path = new ArrayList();
                    path.Add(targetHex);
                    Tuple<int, int> o = (   Tuple<int, int>)log[0];
                    Tuple<int, int> k = new Tuple<int, int>(x, y);
                    while (!k.Equals(o))
                    {
                        path.Add(k);
                        HexDirection d = (HexDirection)directionLog[log.IndexOf(k)];
                        k = GetAdjacentHex(FlipDirection(d), k.Item1, k.Item2);
                    }
                    path.Add(o);
                    distance = path.Count - 1;
                    return path;
                }
                else if (IsOpenHex(direction, tX, tY))
                {
                    DrawLine(x, y, tX, tY);
                    log.Add(new Tuple<int, int>(tX, tY));
                    directionLog.Add(direction);
                    temp.Add(new Tuple<int, int>(tX, tY));
                }
            }
        }
        next = temp;
        return null;
    }
    
    ArrayList AStar(int x, int y, int objectiveX, int objectiveY)
    {
        foreach (HexDirection direction in GetDirections())
        {
            count++;
            Tuple<int, int> targetHex = GetAdjacentHex(direction, x, y);
            int tX = targetHex.Item1;
            int tY = targetHex.Item2;
            if (targetHex.Item1 == objectiveX && targetHex.Item2 == objectiveY)
            {
                DrawLine(x, y, tX, tY);
                ArrayList path = new ArrayList();
                path.Add(targetHex);
                path.Add(new Tuple<int, int>(x, y));
                distance = 1;
                return path;
            }
            else if (IsOpenHex(direction, tX, tY))
            {
                float d = GetDistanceBetweenCells(tX, tY, objectiveX, objectiveY);
                DrawLine(x, y, tX, tY);
                log.Add(new Tuple<int, int>(tX, tY));
                distanceLog.Add(d);
                directionLog.Add(direction);
            }
        }
        
        ArrayList astar = null;
        while (astar == null)
        {
            int indexOfShortest = -1;
            float shortestDistance = -1f;
            for (int i=0; i<log.Count; i++)
            {
                if (next.Contains(i))
                {
                    continue;
                }
                else if (shortestDistance < 0 || (float)distanceLog[i] < shortestDistance)
                {
                    shortestDistance = (float)distanceLog[i];
                    indexOfShortest = i;
                }
            }
            if (indexOfShortest == -1)
            {
                return null;
            }
            astar = AStarHelper(objectiveX, objectiveY, indexOfShortest);
        }
        return astar;
    }
    
    ArrayList AStarHelper(int objectiveX, int objectiveY, int i)
    {
        Tuple<int, int> edgeHex = (Tuple<int, int>)log[i];
        int x = edgeHex.Item1;
        int y = edgeHex.Item2;
        int initialLogSize = log.Count;
        foreach (HexDirection direction in GetDirections())
        {
            count++;
            Tuple<int, int> targetHex = GetAdjacentHex(direction, x, y);
            int tX = targetHex.Item1;
            int tY = targetHex.Item2;
            if (log.Contains(targetHex))
            {
                continue;
            }
            else if (tX == objectiveX && tY == objectiveY)
            {
                DrawLine(x, y, tX, tY);
                ArrayList path = new ArrayList();
                path.Add(targetHex);
                Tuple<int, int> o = (Tuple<int, int>)log[0];
                Tuple<int, int> k = new Tuple<int, int>(x, y);
                while (!k.Equals(o))
                {
                    path.Add(k);
                    HexDirection d = (HexDirection)directionLog[log.IndexOf(k)];
                    k = GetAdjacentHex(FlipDirection(d), k.Item1, k.Item2);
                }
                path.Add(o);
                distance = path.Count - 1;
                return path;
            }
            else if (IsOpenHex(direction, tX, tY))
            {
                float d = GetDistanceBetweenCells(tX, tY, objectiveX, objectiveY);
                DrawLine(x, y, tX, tY);
                log.Add(new Tuple<int, int>(tX, tY));
                distanceLog.Add(d);
                directionLog.Add(direction);
            }
        }
        if (initialLogSize == log.Count)
        {
            next.Add(i);
        }
        return null;
    }
    
    void DrawPath(int x1, int y1, int x2, int y2)
    {
        GameObject path = new GameObject("Path");
        LineRenderer lr = path.AddComponent<LineRenderer>();
        Vector3 offset = new Vector3(0f, 0.1f, 0f);
        lr.material = pathMaterial;
        lr.sortingOrder = 100;
        lr.SetWidth(1f, 1f);
        lr.SetVertexCount(2);
        lr.SetPosition(0, hexGrid[x1, y1].transform.position + offset);
        lr.SetPosition(1, hexGrid[x2, y2].transform.position + offset);
        lines.Add(path);
    }
    
    void DrawLine(int x1, int y1, int x2, int y2)
    {
        GameObject path = new GameObject("Path");
        LineRenderer lr = path.AddComponent<LineRenderer>();
        Vector3 offset = new Vector3(0f, 0.1f, 0f);
        lr.material = lineMaterial;
        lr.sortingOrder = 100;
        lr.SetWidth(1f, 1f);
        lr.SetVertexCount(2);
        lr.SetPosition(0, hexGrid[x1, y1].transform.position + offset);
        lr.SetPosition(1, hexGrid[x2, y2].transform.position + offset);
        lines.Add(path);
    }
    
    public void ClearSearch()
    {
        distance = 0;
        count = 0;
        foreach (GameObject line in lines)
        {
            GameObject.Destroy(line);
        }
        foreach (GameObject counter in counters)
        {
            GameObject.Destroy(counter);
        }
        log.Clear();
        distanceLog.Clear();
        directionLog.Clear();
        next.Clear();
        info.text = "";
    }
}
