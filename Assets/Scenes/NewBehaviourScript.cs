using System.Collections.Generic;
using UnityEngine;

public class MirrorObjects : MonoBehaviour
{
    [System.Serializable]
    public class MirrorObject
    {
        public GameObject original;
        [HideInInspector]
        public GameObject mirroredClone;
    }

    [Header("Mirror Settings")]
    public Transform mirrorPlane; // The green cube that acts as the mirror
    public List<GameObject> objectsToMirror = new List<GameObject>();
    
    [Header("Mirror Plane Settings")]
    public Vector3 mirrorNormal = Vector3.forward; // In Unity, this would be the Z-axis (equivalent to Blender's Y-axis)
    
    [Header("Auto Update")]
    public bool updateInRealTime = true;
    
    [Header("Clone Settings")]
    public Material mirroredMaterial; // Optional: assign a different material for mirrored objects
    public bool autoChangeColor = true; // Automatically change red to blue, etc.

    private List<MirrorObject> mirrorObjects = new List<MirrorObject>();

    void Start()
    {
        CreateMirroredClones();
        UpdateMirroredObjects();
    }

    void Update()
    {
        if (updateInRealTime)
        {
            UpdateMirroredObjects();
        }
    }

    public void CreateMirroredClones()
    {
        // Clear existing clones
        foreach (var mirrorObj in mirrorObjects)
        {
            if (mirrorObj.mirroredClone != null)
            {
                DestroyImmediate(mirrorObj.mirroredClone);
            }
        }
        mirrorObjects.Clear();

        // Create new clones
        foreach (var original in objectsToMirror)
        {
            if (original != null)
            {
                GameObject clone = Instantiate(original);
                clone.name = original.name + "_Mirrored";
                
                // Change material/color if specified
                ChangeMaterialColor(clone);
                
                mirrorObjects.Add(new MirrorObject 
                { 
                    original = original, 
                    mirroredClone = clone 
                });
            }
        }
    }

    void ChangeMaterialColor(GameObject clone)
    {
        if (!autoChangeColor && mirroredMaterial == null) return;

        Renderer renderer = clone.GetComponent<Renderer>();
        if (renderer != null)
        {
            if (mirroredMaterial != null)
            {
                renderer.material = mirroredMaterial;
            }
            else if (autoChangeColor)
            {
                Material mat = new Material(renderer.material);
                
                // Auto color conversion
                if (IsColorSimilar(mat.color, Color.red))
                    mat.color = Color.blue;
                else if (IsColorSimilar(mat.color, Color.blue))
                    mat.color = Color.red;
                else if (IsColorSimilar(mat.color, Color.green))
                    mat.color = Color.magenta;
                else if (IsColorSimilar(mat.color, Color.yellow))
                    mat.color = Color.cyan;
                else
                {
                    // For other colors, just make them a bit different
                    mat.color = new Color(1f - mat.color.r, 1f - mat.color.g, 1f - mat.color.b, mat.color.a);
                }
                
                renderer.material = mat;
            }
        }
    }

    bool IsColorSimilar(Color a, Color b, float threshold = 0.3f)
    {
        return Vector3.Distance(new Vector3(a.r, a.g, a.b), new Vector3(b.r, b.g, b.b)) < threshold;
    }

    public void UpdateMirroredObjects()
    {
        if (mirrorPlane == null) return;

        Vector3 mirrorPosition = mirrorPlane.position;
        Vector3 worldMirrorNormal = mirrorPlane.TransformDirection(mirrorNormal).normalized;

        foreach (var mirrorObj in mirrorObjects)
        {
            if (mirrorObj.original != null && mirrorObj.mirroredClone != null)
            {
                MirrorTransform(mirrorObj.original.transform, mirrorObj.mirroredClone.transform, mirrorPosition, worldMirrorNormal);
            }
        }
    }

    void MirrorTransform(Transform original, Transform mirrored, Vector3 mirrorPos, Vector3 mirrorNormal)
    {
        // Mirror position
        Vector3 originalPos = original.position;
        Vector3 toOriginal = originalPos - mirrorPos;
        float distanceToPlane = Vector3.Dot(toOriginal, mirrorNormal);
        Vector3 mirroredPos = originalPos - 2 * distanceToPlane * mirrorNormal;
        mirrored.position = mirroredPos;

        // Mirror rotation
        Vector3 mirroredForward = Vector3.Reflect(original.forward, mirrorNormal);
        Vector3 mirroredUp = Vector3.Reflect(original.up, mirrorNormal);
        mirrored.rotation = Quaternion.LookRotation(mirroredForward, mirroredUp);

        // Mirror scale (flip one axis to maintain proper mirroring)
        Vector3 originalScale = original.localScale;
        Vector3 mirroredScale = originalScale;
        
        // Determine which axis to flip based on the mirror normal
        if (Mathf.Abs(mirrorNormal.x) > 0.5f)
            mirroredScale.x *= -1;
        else if (Mathf.Abs(mirrorNormal.y) > 0.5f)
            mirroredScale.y *= -1;
        else if (Mathf.Abs(mirrorNormal.z) > 0.5f)
            mirroredScale.z *= -1;
            
        mirrored.localScale = mirroredScale;
    }

    // Public methods for runtime usage
    public void AddObjectToMirror(GameObject obj)
    {
        if (!objectsToMirror.Contains(obj))
        {
            objectsToMirror.Add(obj);
            
            // Create clone immediately if in play mode
            if (Application.isPlaying)
            {
                GameObject clone = Instantiate(obj);
                clone.name = obj.name + "_Mirrored";
                ChangeMaterialColor(clone);
                
                mirrorObjects.Add(new MirrorObject 
                { 
                    original = obj, 
                    mirroredClone = clone 
                });
            }
        }
    }

    public void RemoveObjectFromMirror(GameObject obj)
    {
        objectsToMirror.Remove(obj);
        
        var mirrorObj = mirrorObjects.Find(m => m.original == obj);
        if (mirrorObj != null)
        {
            if (mirrorObj.mirroredClone != null)
            {
                DestroyImmediate(mirrorObj.mirroredClone);
            }
            mirrorObjects.Remove(mirrorObj);
        }
    }

    public void RefreshMirrors()
    {
        CreateMirroredClones();
        UpdateMirroredObjects();
    }

    void OnDrawGizmos()
    {
        if (mirrorPlane != null)
        {
            // Draw the mirror plane
            Gizmos.color = Color.green;
            Vector3 worldNormal = mirrorPlane.TransformDirection(mirrorNormal);
            
            // Draw plane representation
            Gizmos.matrix = Matrix4x4.TRS(mirrorPlane.position, Quaternion.LookRotation(worldNormal), Vector3.one);
            Gizmos.DrawWireCube(Vector3.zero, new Vector3(2, 2, 0.1f));
            
            // Draw normal
            Gizmos.color = Color.yellow;
            Gizmos.DrawRay(mirrorPlane.position, worldNormal * 2);
        }
    }

    void OnValidate()
    {
        // Refresh mirrors when values change in inspector
        if (Application.isPlaying)
        {
            RefreshMirrors();
        }
    }
}