//Copyright 2015 by Nicholas Harshfield. All Rights Reserved.
//View associated license file in root directory for details.

using UnityEngine;
using System.Collections.Generic;

public class HexObject : MonoBehaviour
{
    public List<string> properties;
    public Dictionary<string, string> values;
    public float speed = 25f;
    
    const float distThreshold = 0.01f;

    HexGameManager gm;
    bool isTraveling;
    Vector3 target;
    List<Tuple<int, int>> path;
    int i;
    Vector3 floatHeight;
    
    void Start()
    {
        floatHeight = new Vector3(0f, 0.5f, 0f);
        transform.localScale = new Vector3(1.5f, 1.5f, 1.5f);
    }
    
    void Update()
    {
        //enter conditional if unit should be moving
        if (isTraveling)
        {
            //enter conditional if the unit has reached its next target in path
            if (Vector3.Distance(transform.position, target) < distThreshold)
            {
                i++;
                if (i < path.Count)
                {
                    Tuple<int, int> n2 = path[i];
                    target = gm.GetPositionOfHexagon(n2.Item1, n2.Item2) + floatHeight;
                }
                else
                {
                    path = null;
                    isTraveling = false;
                }
            }
            else
            {
                float step = speed * Time.deltaTime;
                transform.position = Vector3.MoveTowards(transform.position, target, step);
            }
        }
    }
    
    public void SetPath(List<Tuple<int, int>> path)
    {
        //reset search function
        this.path = path;
        isTraveling = true;
        i = 0;
        
        //set the first target to the first non-origin node in the path
        Tuple<int, int> n = path[1];
        target = gm.GetPositionOfHexagon(n.Item1, n.Item2) + floatHeight;
    }
    
    public void Create(HexGameManager gm, Material material)
    {    
        //keep a local pointer
        this.gm = gm;
    
        //specify all hexagon vertices
        Vector3[] vertices = new Vector3[] 
        {
            new Vector3(-1f , 0.0f, -.5f),
            new Vector3(-1f, 0.0f, .5f),
            new Vector3(0f, 0.0f, 1f),
            new Vector3(1f, 0.0f, .5f),
            new Vector3(1f, 0.0f, -.5f),
            new Vector3(0f, 0.0f, -1f)
        };
        
        //specify all triangle connections
        int[] triangles = new int[]
        {
            1,5,0,
            1,4,5,
            1,2,4,
            2,3,4
        };
        
        //specify line draws
        Vector2[] uv = new Vector2[]
        {
            new Vector2(0, 0.25f),
            new Vector2(0, 0.75f),
            new Vector2(0.5f, 1),
            new Vector2(1, 0.75f),
            new Vector2(1, 0.25f),
            new Vector2(0.5f, 0)
        };
        
        //add required components
        MeshFilter meshFilter = gameObject.AddComponent<MeshFilter>();
        gameObject.AddComponent<MeshRenderer>();
        MeshCollider collider = gameObject.AddComponent<MeshCollider>();
        
        //set up the mesh and apply the material
        Mesh mesh = new Mesh();
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uv;
        mesh.RecalculateNormals();
        meshFilter.mesh = mesh;
        collider.sharedMesh = mesh;
        
        //draw it!
        GetComponent<Renderer>().material = material;
    }
}