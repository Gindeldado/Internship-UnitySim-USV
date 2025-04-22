using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.ProBuilder;
using UnityEngine.ProBuilder.MeshOperations;

public class CreatePolygon : MonoBehaviour
{
    ProBuilderMesh m_mesh;
    public float m_height = 1f; 
    public bool m_flipNormals = false;
    public Material meshMaterial;
    public Camera cam;
    public Vector3 worldPos;

    //Holds Prefab of red sphere which is used as clicked position
    public GameObject p;
    //hold all coordinates for creating a shape
    public List<Vector3> points; 
    //holds all instaniated objects where the user has clicked for the shape
    public GameObject PointsHolder; 

    //Hold created land objects
    public List<GameObject>landObjects;

    public UIManager uimScript;

    //ray for mouse positin
    bool useRay = false;

    UIManager.HelpMessage warningWrongPerspective = new UIManager.HelpMessage();
    UIManager.HelpMessage warningPoint1 = new UIManager.HelpMessage();
    UIManager.HelpMessage warningPoint2 = new UIManager.HelpMessage();
    
    // Start is called before the first frame update
    void Start()  
    {
        PointsHolder = new GameObject("PointsHolder");
        uimScript = GameObject.Find("Canvas").GetComponent<UIManager>();

        warningWrongPerspective.title = "Warning";
        warningWrongPerspective.message = "You need to be in Top-Down perspective, to make a obstacle.\nGo to Settings > Camera > Top-down";
        
        warningPoint1.title = "Warning";
        warningPoint1.message = "No points have been placed!\nUse Left mouse button to create points.";
        
        warningPoint2.title = "Warning";
        warningPoint2.message = "You need atleast 3 points!"; 
    }

    // Update is called once per frame
    void Update()
    {   
        //Turn mouse ray on 
        if(uimScript.currentState == UIManager.State.PLACING_POINTS ||
        uimScript.currentState == UIManager.State.DELETE )
        {
            useRay = true;
        }  
        else
        {
            useRay = false;
            warningWrongPerspective.seen = false;
            warningPoint1.seen = false;
            warningPoint2.seen = false;
        }     

        if(useRay){
            if(uimScript.CameraScript.currentCam > 0)
            {
                if(!warningWrongPerspective.seen)
                {
                    uimScript.PopUpMsg(warningWrongPerspective.title, warningWrongPerspective.message);
                    warningWrongPerspective.seen = true;
                }
                return;
            }
            //Create ray which will detetct collisions
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            Debug.DrawRay(ray.origin, ray.direction * 100f);       

            //Left mousebutton clicked     
            if(Input.GetMouseButtonUp(0))
            {
                //To prevent regeistering a clik when it was on UI
                if(uimScript.clicked)   
                {
                    uimScript.clicked = false;
                    return;
                }
                if(uimScript.currentState == UIManager.State.PLACING_POINTS){
                    PlacePoint(ray);
                }
                if(uimScript.currentState == UIManager.State.DELETE){
                    RemoveObject(SelectObject(ray));
                }
            }
            //Right mousebutton clicked     
            if (Input.GetMouseButtonUp(1))
            {   
                if(uimScript.currentState == UIManager.State.PLACING_POINTS){
                    if(points.Count == 0)
                    {
                        Debug.Log("No points have been created");
                        if(!warningPoint1.seen)
                        {
                            uimScript.PopUpMsg(warningPoint1.title, warningPoint1.message);
                            warningPoint1.seen = true;
                        }
                        return;
                    }
                    Destroy(PointsHolder.transform.GetChild(PointsHolder.transform.childCount - 1).gameObject);
                    points.RemoveAt(points.Count - 1);
                    Debug.Log("Last added point has been deleted");            
                }
            }
            if (Input.GetKeyUp(KeyCode.Return))
            {
               if(uimScript.currentState == UIManager.State.PLACING_POINTS){
                    if(points.Count < 3)
                    {
                        Debug.Log("You need atleast 3 points!");
                        if(!warningPoint2.seen)
                        {
                            uimScript.PopUpMsg(warningPoint2.title, warningPoint2.message);
                            warningPoint2.seen = true;
                        }
                        return;
                    }
                    Build(points);
               }
            }
        }
        else{
            //clear points when out of edit mode 
            if(PointsHolder.transform.childCount > 0){
                points.Clear();
                ResetClickedPoints();
            }
        }
    }

/// <summary>
/// Destroy al spheres objects used for making polygon shape
/// </summary>
    void ResetClickedPoints(){
        points.Clear();
        foreach (Transform item in PointsHolder.transform)
        {
            Destroy(item.gameObject);
        }
    }

/// <summary>
/// Create a spheres objects used for making polygon shape
/// </summary>
/// <param name="ray">ray from mouse to camera view</param>
    void PlacePoint(Ray ray){
        RaycastHit hitData;
        if(Physics.Raycast(ray, out hitData, 100) && hitData.collider.gameObject.name == "water"){
            worldPos = hitData.point;
            worldPos.y += 0.1f;
            points.Add(worldPos);
            var obj = Instantiate(p, worldPos, p.transform.rotation);
            obj.transform.parent = PointsHolder.transform;
        }
    }
/// <summary>
/// Make the polygon shape of the shpere points
/// </summary>
/// <param name="points">points for the polygon shape</param>
    void Build(List<Vector3> points)
    {
        //object for storing polygon mesh
        var go = new GameObject
        {name = "land_shape"};
        //creating porobuilder mesh
        m_mesh = go.gameObject.AddComponent<ProBuilderMesh>();

        //creating shapes of points
        m_mesh.CreateShapeFromPolygon(points, m_height, m_flipNormals);
        //check of het een valid polygon shape is, dus niet een die door een edge heen gaat!
        if(go.GetComponent<MeshFilter>().mesh.subMeshCount == 0)
        {
            Debug.Log("Invalid shape!");
            Destroy(go);
            ResetClickedPoints();
            return;
        }
        //giving the shape a mesh
        go.GetComponent<MeshRenderer>().material = meshMaterial;
        go.AddComponent<MeshCollider>();
        Debug.Log("Created shape!");
        Debug.Log("world pos= " + go.transform.position);
        ResetClickedPoints();

        //add object to list
        landObjects.Append(go);
    }

/// <summary>
/// Get the gameobject where the user is clicking. 
/// </summary>
/// <param name="ray">ray from mouse to camera view</param>
/// <returns>clicked gameobject</returns>
    GameObject SelectObject(Ray ray){
        RaycastHit hitData;
        if(Physics.Raycast(ray, out hitData, 100) && hitData.collider.gameObject.name == "land_shape"){
            return hitData.collider.gameObject;
        }
        else return null;
    }

/// <summary>
/// Delete gameobject
/// </summary>
/// <param name="obj">gameobject which will be deleted</param>
    void RemoveObject(GameObject obj){
        if(uimScript.currentState == UIManager.State.DELETE)
        {
            landObjects.Remove(obj);
            Destroy(obj);
        }
    }
}
