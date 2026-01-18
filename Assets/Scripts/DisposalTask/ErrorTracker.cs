using UnityEngine;
using System.Collections.Generic;

public class ErrorTracker : MonoBehaviour
{
    public static ErrorTracker Instance { get; private set; }

    public int disposalErrors = 0;
    public int maintenanceErrors = 0;
    public int storageErrors = 0;
    public int fireNPCErrors = 0;
    public int fireLeverErrors = 0;
    public int fireSmokeDoorErrors = 0;
    public int fireExtinguisherErrors = 0;
    public int fireWrongExtinguisherErrors = 0;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void RecordDisposalError()
    {
        disposalErrors++;
        Debug.Log($"[ErrorTracker] Disposal errors: {disposalErrors}");
    }

    public void RecordMaintenanceError()
    {
        maintenanceErrors++;
        Debug.Log($"[ErrorTracker] Maintenance errors: {maintenanceErrors}");
    }

    public void RecordStorageError()
    {
        storageErrors++;
        Debug.Log($"[ErrorTracker] Storage errors: {storageErrors}");
    }

    public void RecordFireNPCError()
    {
        fireNPCErrors++;
        Debug.Log($"[ErrorTracker] Fire NPC errors: {fireNPCErrors}");
    }

    public void RecordFireLeverError()
    {
        fireLeverErrors++;
        Debug.Log($"[ErrorTracker] Fire lever errors: {fireLeverErrors}");
    }

    public void RecordFireSmokeDoorError()
    {
        fireSmokeDoorErrors++;
        Debug.Log($"[ErrorTracker] Fire smoke door errors: {fireSmokeDoorErrors}");
    }

    public void RecordFireExtinguisherError()
    {
        fireExtinguisherErrors++;
        Debug.Log($"[ErrorTracker] Fire extinguisher errors: {fireExtinguisherErrors}");
    }

    public void RecordFireWrongExtinguisherError()
    {
        fireWrongExtinguisherErrors++;
        Debug.Log($"[ErrorTracker] Fire wrong extinguisher errors: {fireWrongExtinguisherErrors}");
    }

    public void ResetAllErrors()
    {
        disposalErrors = 0;
        maintenanceErrors = 0;
        storageErrors = 0;
        fireNPCErrors = 0;
        fireLeverErrors = 0;
        fireSmokeDoorErrors = 0;
        fireExtinguisherErrors = 0;
        fireWrongExtinguisherErrors = 0;
    }
}