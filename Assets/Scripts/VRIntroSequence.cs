using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class VRIntroSequence : MonoBehaviour
{
    [Header("Camera Intro")]
    [Tooltip("Optional center point for the intro view. If empty, camera start position is used.")]
    public Transform introCenter;
    [Tooltip("Degrees per second for the 360 pan.")]
    public float rotationSpeed = 30f;
    [Tooltip("How high above the center point the intro camera starts.")]
    public float introHeight = 8f;
    [Tooltip("Downward look angle during intro. Positive values look down.")]
    public float lookDownAngle = 25f;
    [Tooltip("Explicit tracking behaviours to disable during intro (e.g., TrackedPoseDriver).")]
    public Behaviour[] cameraTrackingBehavioursToDisable;
    [Tooltip("Force camera transform before render to hard-lock head movement during intro.")]
    public bool hardLockHeadTracking = true;

    [Header("Gameplay")]
    [Tooltip("Optional root object for player systems. Do not disable if it contains the XR camera.")]
    public GameObject playerController;
    [Tooltip("Locomotion/turning behaviours to disable during intro (recommended for VR).")]
    public Behaviour[] gameplayBehavioursToDisable;
    [Tooltip("If true, attempts to disable playerController GameObject during intro.")]
    public bool disablePlayerObjectDuringIntro = false;

    private bool playerObjectWasDisabled;
    private readonly Dictionary<Behaviour, bool> cameraBehavioursState = new Dictionary<Behaviour, bool>();
    private bool introIsRunning;
    private Transform introCameraTransform;
    private Vector3 forcedCameraPosition;
    private Quaternion forcedCameraRotation;

    [Header("Dialogue Trigger")]
    [Tooltip("Disabled GameObject that has your dialogue script on it. It will be enabled after intro.")]
    public GameObject dialogueObjectToEnable;

    void Start()
    {
        DisableGameplayDuringIntro();

        StartCoroutine(ExecuteIntro());
    }

    IEnumerator ExecuteIntro()
    {
        if (Camera.main == null)
        {
            Debug.LogError("VRIntroSequence could not find a Main Camera.");
            RestoreGameplayAfterIntro();
            yield break;
        }

        Transform activeCamera = Camera.main.transform;

        Vector3 originalPosition = activeCamera.position;
        Quaternion originalRotation = activeCamera.rotation;
        float startYaw = originalRotation.eulerAngles.y;
        Vector3 centerPoint = introCenter != null ? introCenter.position : originalPosition;

        DisableCameraTrackingDuringIntro(activeCamera);
        BeginHardCameraLock(activeCamera);

        // Move only the camera to an elevated overview point and look down during the sweep.
        forcedCameraPosition = centerPoint + (Vector3.up * introHeight);
        activeCamera.position = forcedCameraPosition;

        float totalRotation = 0f;
        while (totalRotation < 360f)
        {
            float rotationStep = rotationSpeed * Time.deltaTime;

            if (totalRotation + rotationStep > 360f)
            {
                rotationStep = 360f - totalRotation;
            }

            totalRotation += rotationStep;

            float currentYaw = startYaw + totalRotation;
            forcedCameraRotation = Quaternion.Euler(lookDownAngle, currentYaw, 0f);
            activeCamera.SetPositionAndRotation(forcedCameraPosition, forcedCameraRotation);
            yield return null; 
        }

        // Return camera to its exact original transform before gameplay starts.
        EndHardCameraLock();
        activeCamera.position = originalPosition;
        activeCamera.rotation = originalRotation;

        RestoreCameraTrackingAfterIntro();

        EnableDialogueObject();

        RestoreGameplayAfterIntro();
    }

    private void DisableGameplayDuringIntro()
    {
        if (gameplayBehavioursToDisable != null)
        {
            for (int i = 0; i < gameplayBehavioursToDisable.Length; i++)
            {
                if (gameplayBehavioursToDisable[i] != null)
                {
                    gameplayBehavioursToDisable[i].enabled = false;
                }
            }
        }

        if (!disablePlayerObjectDuringIntro || playerController == null)
        {
            return;
        }

        Camera cameraInPlayer = playerController.GetComponentInChildren<Camera>(true);
        if (cameraInPlayer != null)
        {
            Debug.LogWarning("VRIntroSequence did not disable playerController because it contains a Camera. Disable locomotion behaviours instead.");
            return;
        }

        playerController.SetActive(false);
        playerObjectWasDisabled = true;
    }

    private void RestoreGameplayAfterIntro()
    {
        if (gameplayBehavioursToDisable != null)
        {
            for (int i = 0; i < gameplayBehavioursToDisable.Length; i++)
            {
                if (gameplayBehavioursToDisable[i] != null)
                {
                    gameplayBehavioursToDisable[i].enabled = true;
                }
            }
        }

        if (playerObjectWasDisabled && playerController != null)
        {
            playerController.SetActive(true);
            playerObjectWasDisabled = false;
        }
    }

    private void EnableDialogueObject()
    {
        if (dialogueObjectToEnable == null)
        {
            return;
        }

        dialogueObjectToEnable.SetActive(true);
    }

    private void DisableCameraTrackingDuringIntro(Transform activeCamera)
    {
        cameraBehavioursState.Clear();

        if (cameraTrackingBehavioursToDisable != null)
        {
            for (int i = 0; i < cameraTrackingBehavioursToDisable.Length; i++)
            {
                CacheAndDisableBehaviour(cameraTrackingBehavioursToDisable[i]);
            }
        }

        // Auto-detect common pose drivers so head movement does not override the scripted pan.
        Transform current = activeCamera;
        while (current != null)
        {
            Behaviour[] behaviours = current.GetComponents<Behaviour>();
            for (int i = 0; i < behaviours.Length; i++)
            {
                Behaviour behaviour = behaviours[i];
                if (behaviour == null)
                {
                    continue;
                }

                string typeName = behaviour.GetType().Name;
                if (typeName.Contains("TrackedPoseDriver") || typeName.Contains("PoseDriver"))
                {
                    CacheAndDisableBehaviour(behaviour);
                }
            }

            current = current.parent;
        }
    }

    private void CacheAndDisableBehaviour(Behaviour behaviour)
    {
        if (behaviour == null)
        {
            return;
        }

        if (!cameraBehavioursState.ContainsKey(behaviour))
        {
            cameraBehavioursState.Add(behaviour, behaviour.enabled);
        }

        behaviour.enabled = false;
    }

    private void RestoreCameraTrackingAfterIntro()
    {
        foreach (KeyValuePair<Behaviour, bool> entry in cameraBehavioursState)
        {
            if (entry.Key != null)
            {
                entry.Key.enabled = entry.Value;
            }
        }

        cameraBehavioursState.Clear();
    }

    private void BeginHardCameraLock(Transform activeCamera)
    {
        if (!hardLockHeadTracking || activeCamera == null)
        {
            return;
        }

        introCameraTransform = activeCamera;
        forcedCameraPosition = activeCamera.position;
        forcedCameraRotation = activeCamera.rotation;
        introIsRunning = true;
        Application.onBeforeRender += ForceCameraPose;
    }

    private void EndHardCameraLock()
    {
        if (!hardLockHeadTracking)
        {
            return;
        }

        Application.onBeforeRender -= ForceCameraPose;
        introIsRunning = false;
        introCameraTransform = null;
    }

    private void ForceCameraPose()
    {
        if (!introIsRunning || introCameraTransform == null)
        {
            return;
        }

        introCameraTransform.SetPositionAndRotation(forcedCameraPosition, forcedCameraRotation);
    }

    void OnDisable()
    {
        EndHardCameraLock();
    }
}