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
    
    // Fire evacuation errors
    public int evacuationTimeExpiredErrors = 0;
    public int evacuationFireObstacleErrors = 0;
    public int evacuationOxygenErrors = 0;
    public int evacuationNPCLeftBehindErrors = 0;
    public int evacuationNPCNotRescuedErrors = 0;
    public int evacuationNoWetClothErrors = 0;

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

    public void RecordEvacuationTimeExpiredError()
    {
        evacuationTimeExpiredErrors++;
        Debug.Log($"[ErrorTracker] Evacuation time expired errors: {evacuationTimeExpiredErrors}");
    }

    public void RecordEvacuationFireObstacleError()
    {
        evacuationFireObstacleErrors++;
        Debug.Log($"[ErrorTracker] Evacuation fire obstacle errors: {evacuationFireObstacleErrors}");
    }

    public void RecordEvacuationOxygenError()
    {
        evacuationOxygenErrors++;
        Debug.Log($"[ErrorTracker] Evacuation oxygen errors: {evacuationOxygenErrors}");
    }

    public void RecordEvacuationNPCLeftBehindError()
    {
        evacuationNPCLeftBehindErrors++;
        Debug.Log($"[ErrorTracker] Evacuation NPC left behind errors: {evacuationNPCLeftBehindErrors}");
    }

    public void RecordEvacuationNPCNotRescuedError()
    {
        evacuationNPCNotRescuedErrors++;
        Debug.Log($"[ErrorTracker] Evacuation NPC not rescued errors: {evacuationNPCNotRescuedErrors}");
    }

    public void RecordEvacuationNoWetClothError()
    {
        evacuationNoWetClothErrors++;
        Debug.Log($"[ErrorTracker] Evacuation no wet cloth errors: {evacuationNoWetClothErrors}");
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
        evacuationTimeExpiredErrors = 0;
        evacuationFireObstacleErrors = 0;
        evacuationOxygenErrors = 0;
        evacuationNPCLeftBehindErrors = 0;
        evacuationNPCNotRescuedErrors = 0;
        evacuationNoWetClothErrors = 0;
    }
}