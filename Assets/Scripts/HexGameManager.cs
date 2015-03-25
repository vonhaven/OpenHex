//Copyright 2015 by Nicholas Harshfield. All Rights Reserved.
//View associated license file in root directory for details.

using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;
using System.Collections.Generic;

public class HexGameManager : MonoBehaviour
{
    //public variables, to be edited in the Unity inspector
    public int height = 13;
    public int width = 23;
    public Material openMaterial, obstacleMaterial, originMaterial, objectiveMaterial, lineMaterial, pathMaterial, voyagerMaterial;
    public Text info, gridSizeText, countPanel, distancePanel, gridWidth, gridHeight;
    
    //private constants
    private const float PAD_X = 3.1f;
    private const float PAD_Y = 1.8f;
    private const float SIZE  = 2.5f;
    private const float LEVEL = 0.0f;
    private const int MIN_WIDTH = 11;
    private const int MIN_HEIGHT = 9;
    private const int MAX_WIDTH = 99;
    private const int MAX_HEIGHT = 99;
    
    //private variables
    bool origin, objective, cameraDrag;
    int originX, originY, objectiveX, objectiveY, count, distance;
    GameObject[,] hexGrid;
    Vector3 cameraOrigin, cameraDiff;
    List<GameObject> lines;
    List<Tuple<int, int>> log, next;
    List<float> distanceLog;
    List<int> nextIndex;
    List<HexDirection> directionLog;
    
    /** Initializes variables and the starting grid */
    void Start()
    {
        //initialize variables (negative == N/A)
        count = 0;
        distance = 0;
        
        //instantiate the hex grid and data structures
        lines = new List<GameObject>();
        log = new List<Tuple<int, int>>();
        next = new List<Tuple<int, int>>();
        distanceLog = new List<float>();
        nextIndex = new List<int>();
        directionLog = new List<HexDirection>();
        
        //initialize the hexagon grid
        InitializeGrid();
    }
    
    /** Initializes the board with the current width/height as a 2-D grid */
    void InitializeGrid()
    {
        //limit lower width and height bounds
        if (width < MIN_WIDTH) width = MIN_WIDTH;
        if (height < MIN_HEIGHT) height = MIN_HEIGHT;
        
        //limit upper width and height bounds
        if (width > MAX_WIDTH) width = MAX_WIDTH;
        if (height > MAX_HEIGHT) height = MAX_HEIGHT;
        
        //initialize hex grid array
        hexGrid = new GameObject[width, height];
    
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
                    newHex.transform.position = new Vector3(x * (SIZE + PAD_X) 
                        + (SIZE + PAD_X) / 2, LEVEL, y * (SIZE + PAD_Y));
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
    
    /** Returns the x, y, z position of the given hexagon on the board */
    public Vector3 GetPositionOfHexagon(int x, int y)
    {
        return hexGrid[x, y].transform.position;
    }
    
    /** Update method, mainly for controlling user inputs & actions */
    void Update()
    {
        //keep time cost panel updated
        if (count == 0) countPanel.text = "N/A";
        else countPanel.text = count.ToString();
        
        //keep distance panel updated
        if (distance == 0) distancePanel.text = "N/A";
        else distancePanel.text = distance.ToString();
        
        //parse for user key inputs
        if (Input.GetKey(KeyCode.B))
        {
            if (origin && objective)
            {
                //inform user of pending operations
                info.text = "[BFS] Running a breadth-first search...";
                
                //clear the search history
                ClearSearch();
                
                //determine the optimal path that the BFS produces
                List<Tuple<int, int>> path = null;
                while ((path == null && next.Count > 0) || count == 0) 
                {
                    path = BFS(originX, originY, objectiveX, objectiveY);
                }
                
                //draw the path if it could be found
                if (path != null && path.Count > 0)
                {
                    for (int i = 0; i < path.Count - 1; i++)
                    {
                        Tuple<int, int> n1 = path[i];
                        Tuple<int, int> n2 = path[i + 1];
                        DrawLine(n1.Item1, n1.Item2, n2.Item1, n2.Item2, pathMaterial);
                    }
                    
                    //inform the user of the success
                    info.text = "[BFS] A valid path from Origin to Objective was found!";
                    
                    //create and position a voyager unit
                    GameObject voyager = CreateHexObject("Voyager", originX, originY);
                    HexObject voyagerUnit = voyager.GetComponent<HexObject>();
                    voyagerUnit.Create(this, voyagerMaterial);
                    Vector3 startLocation = hexGrid[originX, originY].transform.position;
                    Vector3 floatHeight = new Vector3(0f, 1f, 0f);
                    voyager.transform.position = startLocation + floatHeight;
                    
                    //animate the voyage from the start to the finish
                    path.Reverse();
                    voyagerUnit.SetPath(path);
                    lines.Add(voyager);
                }
                else
                {
                    //inform the user of the failure
                    info.text = "[BFS] No path between Origin and Objective could be found!";
                }
            }
            else
            {
                //inform the user of the search requirements
                info.text = "[BFS] Please specify Origin and Objective cells before running search!";
            }
        }
        else if (Input.GetKey(KeyCode.A))
        {
            if (origin && objective)
            {
                //inform user of pending operations
                info.text = "[A*] Running an A* search...";
                
                //clear the search history
                ClearSearch();
                
                //determine the path that the A* search produces
                log.Add(new Tuple<int, int>(originX, originY));
                distanceLog.Add(GetDistanceBetweenCells(originX, originY, objectiveX, objectiveY));
                List<Tuple<int, int>> path = AStar(originX, originY, objectiveX, objectiveY);
                
                //draw the path if it could be found
                if (path != null && path.Count > 0)
                {
                    for (int i = 0; i < path.Count - 1; i++)
                    {
                        Tuple<int, int> n1 = (Tuple<int, int>)path[i];
                        Tuple<int, int> n2 = (Tuple<int, int>)path[i + 1];
                        DrawLine(n1.Item1, n1.Item2, n2.Item1, n2.Item2, pathMaterial);
                    }
                    
                    //inform the user of the success
                    info.text = "[A*] A valid path from Origin to Objective was found!";
                    
                    //create and position a voyager unit
                    GameObject voyager = CreateHexObject("Voyager", originX, originY);
                    HexObject voyagerUnit = voyager.GetComponent<HexObject>();
                    voyagerUnit.Create(this, voyagerMaterial);
                    Vector3 startLocation = hexGrid[originX, originY].transform.position;
                    Vector3 floatHeight = new Vector3(0f, 1f, 0f);
                    voyager.transform.position = startLocation + floatHeight;
                    
                    //animate the voyage from the start to the finish
                    path.Reverse();
                    voyagerUnit.SetPath(path);
                    lines.Add(voyager);
                }
                else
                {
                    //inform the user of the failure
                    info.text = "[A*] No path between Origin and Objective could be found!";
                }
            }
            else
            {
                //inform the user of the search requirements
                info.text = "[A*] Please specify Origin and Objective cells before running search!";
            }
        }
        else if (Input.GetKey(KeyCode.R))
        {
            //inform the user of the pending operations
            info.text = "The board has been reset.";
            
            //wipe and remake the board
            ResetBoard();
        }
    }
    
    /** Creates a visible HexObject unit to animate paths */
    GameObject CreateHexObject(string name, int x, int y)
    {
        //create the new object and component
        GameObject newGameObject = new GameObject(name);
        HexObject newHexObject = newGameObject.AddComponent<HexObject>();
        
        //initialize the new component to the given coordinates
        newHexObject.properties = new List<string>();
        newHexObject.values = new Dictionary<string, string>();
        //newHexObject.x = x;
        //newHexObject.y = y;
        
        //return a pointer to this GameObject
        return newGameObject;
    }
    
    /** Update function for camera movement features */
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
    
    /** Destroys the entire board */
    void DestroyBoard()
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
    
    /** Clears the board and resizes it if necessary */
    void ResetBoard()
    {
        //forget everything and destroy grid
        ClearSearch();
        ClearOrigin();
        ClearObjective();
        DestroyBoard();
        
        //fetch parameters from UI text input
        if (!Int32.TryParse(gridWidth.text, out width)) width = 5;
        if (!Int32.TryParse(gridHeight.text, out height)) height = 5;
        
        //initialize grid with the new width and height
        InitializeGrid();
    }
    
    /** Determines if an origin is set */
    public bool HasOrigin()
    {
        return origin;
    }
    
    /** Determines if an objective is set */
    public bool HasObjective()
    {
        return objective;
    }
    
    /** Forget about the last origin that was set */
    public void ClearOrigin()
    {
        origin = false;
    }
    
    /** Forget about the last objective that was set */
    public void ClearObjective()
    {
        objective = false;
    }
    
    /** Sets the origin hexagon to the hexagon at the given coordinates */
    public void SetOrigin(int x, int y)
    {
        originX = x;
        originY = y;
        origin = true;
    }
    
    /** Sets the objective hexagon to the hexagon at the given coordinates */
    public void SetObjective(int x, int y)
    {
        objectiveX = x;
        objectiveY = y;
        objective = true;
    }
    
    /** Gets the material for the given hexagon state */
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
    
    /** An enumeration defining possible Hexagon directions */
    enum HexDirection { LU, L, LD, RU, R, RD };
    
    /** Returns all hexagon directions */
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
    
    /** Static method which rotates the given direction once counter-clockwise */
    static HexDirection RotateDirection(HexDirection direction)
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
    
    /** Static method which provides the opposite direction of that given */
    static HexDirection FlipDirection(HexDirection direction)
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
    
    /** Returns the flost distance between two hexagon cells */
    float GetDistanceBetweenCells(int x1, int y1, int x2, int y2)
    {
        Vector3 p1 = hexGrid[x1, y1].transform.position;
        Vector3 p2 = hexGrid[x2, y2].transform.position;
        float d = Vector3.Distance(p1, p2);
        return d;
    }
    
    /** Returns the adjacent hexagon in the given direction of the given location */
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
    
    /** Determines if the hexagon at the given position is a valid location */
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
    
    /** Determines if the hexagon in the given direction is valid */
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
    
    /** Performs a depth-first search of the hexagon grid graph
        NOTE: this method is terrible and should be obliterated */
    List<Tuple<int, int>> DFS(int x, int y, int objectiveX, int objectiveY)
    {
        //check all the surrounding hexagons for paths
        foreach (HexDirection direction in GetDirections())
        {
            count++;
            
            //get the coordinates of the cell in the given direction
            Tuple<int, int> target = GetAdjacentHex(direction, x, y);
            int targetX = target.Item1, targetY = target.Item2;
            
            //skip this cell if we've already been here:
            if (log.Contains(target)) continue;
            
            //did we make it?!?
            if (targetX == objectiveX && targetY == objectiveY)
            {
                //cool! let's visualize the search path and return true!
                DrawLine(x, y, targetX, targetY);
                List<Tuple<int, int>> path = new List<Tuple<int, int>>();
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
    
    /** Performs a breadth-first search from the origin to the objective
        Returns an array of nodes representing the absolutely optimal path */
    List<Tuple<int, int>> BFS(int originX, int originY, int objectiveX, int objectiveY)
    {
        //perform initialization tasks for BFS search
        if (count == 0)
        {
            Tuple<int, int> o = new Tuple<int, int>(originX, originY);
            log.Add(o);
            next.Add(o);
        }
    
        //core of the recursive algorithm
        List<Tuple<int, int>> temp = new List<Tuple<int, int>>();
        for (int i=0; i<next.Count; i++)
        {
            Tuple<int, int> edgeHex = next[i];
            int x = edgeHex.Item1, y = edgeHex.Item2;
            foreach (HexDirection direction in GetDirections())
            {
                Tuple<int, int> targetHex = GetAdjacentHex(direction, x, y);
                int tX = targetHex.Item1, tY = targetHex.Item2;
                if (log.Contains(targetHex)) continue;
                else if (tX == objectiveX && tY == objectiveY)
                {
                    count++;
                    DrawLine(x, y, tX, tY);
                    List<Tuple<int, int>> path = new List<Tuple<int, int>>();
                    path.Add(targetHex);
                    Tuple<int, int> o = (Tuple<int, int>)log[0];
                    Tuple<int, int> k = new Tuple<int, int>(x, y);
                    while (!k.Equals(o))
                    {
                        path.Add(k);
                        HexDirection d = (HexDirection)directionLog[log.IndexOf(k) - 1];
                        k = GetAdjacentHex(FlipDirection(d), k.Item1, k.Item2);
                    }
                    path.Add(o);
                    distance = path.Count - 1;
                    return path;
                }
                else if (IsOpenHex(direction, tX, tY))
                {
                    count++;
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
    
    /** The first iteration of the A* search function */
    List<Tuple<int, int>> AStar(int x, int y, int objectiveX, int objectiveY)
    {
        foreach (HexDirection direction in GetDirections())
        {
            count++;
            Tuple<int, int> targetHex = GetAdjacentHex(direction, x, y);
            int tX = targetHex.Item1;
            int tY = targetHex.Item2;
            if (targetHex.Item1 == objectiveX && targetHex.Item2 == objectiveY)
            {
                count++;
                DrawLine(x, y, tX, tY);
                List<Tuple<int, int>> path = new List<Tuple<int, int>>();
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
        
        List<Tuple<int, int>> astar = null;
        while (astar == null)
        {
            int indexOfShortest = -1;
            float shortestDistance = -1f;
            for (int i=0; i<log.Count; i++)
            {
                if (nextIndex.Contains(i)) continue;
                else if (shortestDistance < 0 || distanceLog[i] < shortestDistance)
                {
                    shortestDistance = (float)distanceLog[i];
                    indexOfShortest = i;
                }
            }
            if (indexOfShortest == -1) return null;
            astar = AStarHelper(objectiveX, objectiveY, indexOfShortest);
        }
        return astar;
    }
    
    /** A helper function which checks arrays of unchecked nodes for distance */
    List<Tuple<int, int>> AStarHelper(int objectiveX, int objectiveY, int i)
    {
        Tuple<int, int> edgeHex = (Tuple<int, int>)log[i];
        int x = edgeHex.Item1;
        int y = edgeHex.Item2;
        int initialLogSize = log.Count;
        foreach (HexDirection direction in GetDirections())
        {
            Tuple<int, int> targetHex = GetAdjacentHex(direction, x, y);
            int tX = targetHex.Item1;
            int tY = targetHex.Item2;
            if (log.Contains(targetHex)) continue;
            else if (tX == objectiveX && tY == objectiveY)
            {
                count++;
                DrawLine(x, y, tX, tY);
                List<Tuple<int, int>> path = new List<Tuple<int, int>>();
                path.Add(targetHex);
                Tuple<int, int> o = (Tuple<int, int>)log[0];
                Tuple<int, int> k = new Tuple<int, int>(x, y);
                while (!k.Equals(o))
                {
                    path.Add(k);
                    HexDirection d = (HexDirection)directionLog[log.IndexOf(k) - 1];
                    k = GetAdjacentHex(FlipDirection(d), k.Item1, k.Item2);
                }
                path.Add(o);
                distance = path.Count - 1;
                return path;
            }
            else if (IsOpenHex(direction, tX, tY))
            {
                count++;
                float d = GetDistanceBetweenCells(tX, tY, objectiveX, objectiveY);
                DrawLine(x, y, tX, tY);
                log.Add(new Tuple<int, int>(tX, tY));
                distanceLog.Add(d);
                directionLog.Add(direction);
            }
        }
        if (initialLogSize == log.Count) nextIndex.Add(i);
        return null;
    }
    
    /** Draws a line between two given Hexagons and stores it in "lines" */
    void DrawLine(int x1, int y1, int x2, int y2, Material mat = null)
    {
        if (mat == null) mat = lineMaterial;
        GameObject path = new GameObject("Path");
        LineRenderer lr = path.AddComponent<LineRenderer>();
        Vector3 offset = new Vector3(0f, 0.1f, 0f);
        lr.material = mat;
        lr.sortingOrder = 100;
        lr.SetWidth(1f, 1f);
        lr.SetVertexCount(2);
        lr.SetPosition(0, hexGrid[x1, y1].transform.position + offset);
        lr.SetPosition(1, hexGrid[x2, y2].transform.position + offset);
        lines.Add(path);
    }
    
    /** Clears memory associated with a search function and prepares the next */
    public void ClearSearch()
    {
        distance = 0;
        count = 0;
        foreach (GameObject line in lines) GameObject.Destroy(line);
        log.Clear();
        distanceLog.Clear();
        directionLog.Clear();
        next.Clear();
        nextIndex.Clear();
        info.text = "";
    }
}
