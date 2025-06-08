using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PassthroughManager : MonoBehaviour
{
    [Header("Passthrough Settings")]
    public Camera vrCamera;
    public GameObject[] sceneEnvironmentObjects; 
    public Skybox sceneSkybox; 
    [SerializeField] private bool defaultPassthrough = false;
    
    [Header("UI References")]
    public Toggle passthroughToggle;
    
    
    private const string PASSTHROUGH_PREF_KEY = "PassthroughEnabled";
    
    
    private bool isPassthroughSupported = false;
    private bool isPassthroughEnabled = false;
    
    
    private CameraClearFlags originalClearFlags;
    private Color originalBackgroundColor;
    private Material originalSkyboxMaterial;
    
    
    public static PassthroughManager Instance { get; private set; }
    
    private void Awake()
    {
        
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
        
        CheckPassthroughSupport();
        StoreOriginalCameraSettings();
    }
    
    void Start()
    {
        InitializePassthrough();
    }
    
    private void CheckPassthroughSupport()
    {
        
        #if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            
            var ovrManager = FindObjectOfType<OVRManager>();
            if (ovrManager != null)
            {
                isPassthroughSupported = true;
                Debug.Log("Passthrough supported: Meta Quest detected");
                return;
            }
        }
        catch (System.Exception)
        {
            
        }
        #endif
        
        
        #if UNITY_XR_MANAGEMENT
        try
        {
            var xrGeneralSettings = UnityEngine.XR.Management.XRGeneralSettings.Instance;
            if (xrGeneralSettings != null && xrGeneralSettings.Manager.activeLoader != null)
            {
                isPassthroughSupported = true;
                Debug.Log("Passthrough supported: OpenXR detected");
                return;
            }
        }
        catch (System.Exception)
        {
            
        }
        #endif
        
        Debug.Log("Passthrough not supported on this platform/device");
    }
    
    private void StoreOriginalCameraSettings()
    {
        if (vrCamera != null)
        {
            originalClearFlags = vrCamera.clearFlags;
            originalBackgroundColor = vrCamera.backgroundColor;
            if (RenderSettings.skybox != null)
            {
                originalSkyboxMaterial = RenderSettings.skybox;
            }
        }
    }
    
    private void InitializePassthrough()
    {
        
        if (passthroughToggle != null)
        {
            bool savedPassthrough = PlayerPrefs.GetInt(PASSTHROUGH_PREF_KEY, defaultPassthrough ? 1 : 0) == 1;
            passthroughToggle.isOn = savedPassthrough;
            passthroughToggle.onValueChanged.AddListener(OnPassthroughToggleChanged);
            passthroughToggle.interactable = isPassthroughSupported;
            
            
            if (isPassthroughSupported)
            {
                SetPassthroughMode(savedPassthrough);
            }
            else
            {
                
                passthroughToggle.transform.parent.gameObject.SetActive(false);
            }
        }
    }
    
    public void OnPassthroughToggleChanged(bool enablePassthrough)
    {
        if (!isPassthroughSupported)
        {
            Debug.LogWarning("Passthrough not supported on this device/platform");
            return;
        }
        
        PlayerPrefs.SetInt(PASSTHROUGH_PREF_KEY, enablePassthrough ? 1 : 0);
        PlayerPrefs.Save();
        
        SetPassthroughMode(enablePassthrough);
        
        Debug.Log($"Passthrough {(enablePassthrough ? "enabled" : "disabled")}");
    }
    
    public void SetPassthroughMode(bool enablePassthrough)
    {
        if (!isPassthroughSupported || vrCamera == null) return;
        
        isPassthroughEnabled = enablePassthrough;
        
        if (enablePassthrough)
        {
            EnablePassthrough();
        }
        else
        {
            DisablePassthrough();
        }
    }
    
    private void EnablePassthrough()
    {
        
        #if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            var ovrManager = FindObjectOfType<OVRManager>();
            if (ovrManager != null)
            {
                ovrManager.isInsightPassthroughEnabled = true;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"Failed to enable OVR passthrough: {e.Message}");
        }
        #endif
        
        
        vrCamera.clearFlags = CameraClearFlags.SolidColor;
        vrCamera.backgroundColor = Color.clear;
        
        
        if (sceneEnvironmentObjects != null)
        {
            foreach (GameObject obj in sceneEnvironmentObjects)
            {
                if (obj != null)
                {
                    obj.SetActive(false);
                }
            }
        }
        
        
        if (sceneSkybox != null)
        {
            sceneSkybox.enabled = false;
        }
        RenderSettings.skybox = null;
        
        Debug.Log("Passthrough mode enabled");
    }
    
    private void DisablePassthrough()
    {
        
        #if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            var ovrManager = FindObjectOfType<OVRManager>();
            if (ovrManager != null)
            {
                ovrManager.isInsightPassthroughEnabled = false;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"Failed to disable OVR passthrough: {e.Message}");
        }
        #endif
        
        vrCamera.clearFlags = originalClearFlags;
        vrCamera.backgroundColor = originalBackgroundColor;
        
        
        if (sceneEnvironmentObjects != null)
        {
            foreach (GameObject obj in sceneEnvironmentObjects)
            {
                if (obj != null)
                {
                    obj.SetActive(true);
                }
            }
        }
        
        if (sceneSkybox != null)
        {
            sceneSkybox.enabled = true;
        }
        if (originalSkyboxMaterial != null)
        {
            RenderSettings.skybox = originalSkyboxMaterial;
        }
        
        Debug.Log("Passthrough mode disabled");
    }
    
    public bool IsPassthroughSupported()
    {
        return isPassthroughSupported;
    }
    
    public bool IsPassthroughEnabled()
    {
        return isPassthroughEnabled;
    }
    
    public void TogglePassthrough()
    {
        if (passthroughToggle != null)
        {
            passthroughToggle.isOn = !passthroughToggle.isOn;
        }
        else
        {
            SetPassthroughMode(!isPassthroughEnabled);
        }
    }
    
    public void ResetPassthrough()
    {
        if (passthroughToggle != null && isPassthroughSupported)
        {
            passthroughToggle.isOn = defaultPassthrough;
            PlayerPrefs.SetInt(PASSTHROUGH_PREF_KEY, defaultPassthrough ? 1 : 0);
            PlayerPrefs.Save();
            SetPassthroughMode(defaultPassthrough);
        }
    }
    
    public bool GetPassthroughEnabled()
    {
        return PlayerPrefs.GetInt(PASSTHROUGH_PREF_KEY, defaultPassthrough ? 1 : 0) == 1;
    }
    
    public void UpdateReferences(Camera newVrCamera, GameObject[] newSceneObjects, Skybox newSkybox = null)
    {
        vrCamera = newVrCamera;
        sceneEnvironmentObjects = newSceneObjects;
        sceneSkybox = newSkybox;
        
        StoreOriginalCameraSettings();
        
        if (isPassthroughSupported)
        {
            SetPassthroughMode(isPassthroughEnabled);
        }
    }
}