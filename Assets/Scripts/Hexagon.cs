using UnityEngine;
using System.Collections;

public class Hexagon : MonoBehaviour {

    public HexState state = HexState.Open;
    
    GridManager gm;
    int x, y;
    
    void OnMouseDown()
    {
        //clear fragments
        gm.ClearSearch();
        
        //determine the next state
        switch (state)
        {
            case HexState.Open:
                state = HexState.Obstacle;
                break;
                
            case HexState.Obstacle:
                if (!gm.HasOrigin())
                {
                    state = HexState.Origin;
                    gm.SetOrigin(x, y);
                }
                else if (!gm.HasObjective())
                {
                    state = HexState.Objective;
                    gm.SetObjective(x, y);
                }
                else
                {
                    state = HexState.Open;
                }
                break;
                
            case HexState.Origin:
                if (!gm.HasObjective())
                {
                    state = HexState.Objective;
                    gm.SetObjective(x, y);
                }
                else
                {
                    state = HexState.Open;
                }
                gm.ClearOrigin();
                break;
                
            case HexState.Objective:
                state = HexState.Open;
                gm.ClearObjective();
                break;
        }
        
        //draw the new hexagon state
        Draw(gm.GetMaterial(state));
    }
    
    public void Reset()
    {
        state = HexState.Open;
        Draw(gm.GetMaterial(state));
    }
    
    public void DrawState()
    {
        //get the material from the gm and draw our state
        Draw(gm.GetMaterial(state));
    }
    
    public void Create(int x, int y, GridManager gm)
    {
        //remember its position for future functions
        this.x = x;
        this.y = y;
        this.gm = gm;
    
        //specify all hexagon vertices
        Vector3[] vertices = new Vector3[] {
            new Vector3(-1f , 0.0f, -.5f),
            new Vector3(-1f, 0.0f, .5f),
            new Vector3(0f, 0.0f, 1f),
            new Vector3(1f, 0.0f, .5f),
            new Vector3(1f, 0.0f, -.5f),
            new Vector3(0f, 0.0f, -1f)
        };
        
        //specify all triangle connections
        int[] triangles = new int[] {
            1,5,0,
            1,4,5,
            1,2,4,
            2,3,4
        };
        
        //specify line draws
        Vector2[] uv = new Vector2[] {
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
        Draw(gm.GetMaterial(state));
    }
    
    public void Draw(Material material)
    {
        GetComponent<Renderer>().material = material;
    }
}
