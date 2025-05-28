using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BatonGuide : MonoBehaviour
{
    [System.Serializable]
    public struct BatonKeyframe
    {
        public Vector3 position;
        public Quaternion rotation;
        public float timestamp; // Actual time when this pose should occur
        public float speed; // Speed multiplier for this segment
    }
    
    public BatonKeyframe[] batonPath;
    public bool loop = true;
    public bool autoPlay = true;
    public float globalSpeedMultiplier = 1f;
    
    [Header("Baton Specific")]
    public bool showBaton = true;
    public LineRenderer batonTrail; // Optional trail effect
    
    private float currentTime = 0f;
    private int currentIndex = 0;
    private bool isPlaying = false;
    
    void Start()
    {
        if (autoPlay) StartGuide();
    }
    
    void Update()
    {
        if (!isPlaying || batonPath.Length < 2) return;
        
        currentTime += Time.deltaTime * globalSpeedMultiplier;
        UpdateBatonPosition();
    }
    
    void UpdateBatonPosition()
    {
        // Find current segment
        while (currentIndex < batonPath.Length - 1 && 
               currentTime >= batonPath[currentIndex + 1].timestamp)
        {
            currentIndex++;
        }
        
        // Handle loop or end
        if (currentIndex >= batonPath.Length - 1)
        {
            if (loop)
            {
                currentTime = 0f;
                currentIndex = 0;
                return;
            }
            else
            {
                // Stay at final position
                transform.position = batonPath[batonPath.Length - 1].position;
                transform.rotation = batonPath[batonPath.Length - 1].rotation;
                return;
            }
        }
        
        // Interpolate between current and next keyframe
        BatonKeyframe current = batonPath[currentIndex];
        BatonKeyframe next = batonPath[currentIndex + 1];
        
        float segmentDuration = next.timestamp - current.timestamp;
        float segmentProgress = (currentTime - current.timestamp) / segmentDuration;
        
        // Use different interpolation based on baton movement type
        Vector3 newPosition = Vector3.Lerp(current.position, next.position, segmentProgress);
        Quaternion newRotation = Quaternion.Slerp(current.rotation, next.rotation, segmentProgress);
        
        transform.position = newPosition;
        transform.rotation = newRotation;
        
        // Update trail if available
        if (batonTrail != null && showBaton)
        {
            UpdateBatonTrail();
        }
    }
    
    void UpdateBatonTrail()
    {
        // Add current tip position to trail
        Vector3 batonTip = transform.position + transform.forward * 1f; // Adjust baton length
        // LineRenderer trail implementation here
    }
    
    public void StartGuide()
    {
        isPlaying = true;
        currentTime = 0f;
        currentIndex = 0;
    }
    
    public void StopGuide()
    {
        isPlaying = false;
    }
    
    public void RecordCurrentPose(float timestamp)
    {
        // Helper method to record poses during development
        BatonKeyframe newKeyframe = new BatonKeyframe
        {
            position = transform.position,
            rotation = transform.rotation,
            timestamp = timestamp,
            speed = 1f
        };
        
        // Add to array (you'd need to resize the array)
    }
}
