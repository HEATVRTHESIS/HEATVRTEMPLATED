using UnityEngine;
using TMPro;

public class FireEvacuationScoreTracker : MonoBehaviour
{
    public static FireEvacuationScoreTracker Instance { get; private set; }
    
    [Header("Score Settings")]
    [Tooltip("Points for using wet cloth (awarded once)")]
    public int wetClothBonus = 15;
    
    [Tooltip("Points for evacuating with NPC")]
    public int npcRescueBonus = 15;
    
    [Tooltip("Points for completing evacuation on time")]
    public int timeCompletionBonus = 15;
    
    [Tooltip("Penalty for each fire damage hit")]
    public int fireDamagePenalty = -10;
    
    [Tooltip("Penalty for oxygen depletion")]
    public int oxygenDepletionPenalty = -10;
    
    [Header("Current Score Data")]
    [SerializeField] private int currentScore = 0;
    [SerializeField] private int safetyViolations = 0;
    [SerializeField] private int errorCount = 0;
    
    private bool hasUsedWetCloth = false;
    private bool hasRescuedNPC = false;
    private bool completedOnTime = false;
    private bool hasRecordedTimeExpired = false;
    private bool hasRecordedOxygenDepletion = false;
    
    [Header("UI References")]
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI safetyViolationsText;
    public TextMeshProUGUI errorsText;
    
    [Header("References")]
    public OxygenManager oxygenManager;
    public PlayerFireHealth playerFireHealth;
    public FireEvacuationTimer evacuationTimer;
    
    private int lastPlayerHealth;
    
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }
    
    void Start()
    {
        // Auto-find references if not assigned
        if (oxygenManager == null)
            oxygenManager = FindObjectOfType<OxygenManager>();
        
        if (playerFireHealth == null)
            playerFireHealth = FindObjectOfType<PlayerFireHealth>();
        
        if (evacuationTimer == null)
            evacuationTimer = FindObjectOfType<FireEvacuationTimer>();
        
        if (playerFireHealth != null)
        {
            lastPlayerHealth = playerFireHealth.GetCurrentHealth();
        }
        
        UpdateUI();
        
        Debug.Log("FireEvacuationScoreTracker initialized");
    }
    
    void Update()
    {
        // Monitor wet cloth usage (only count once)
        if (!hasUsedWetCloth && oxygenManager != null)
        {
            if (oxygenManager.GetProtectingClothCount() > 0)
            {
                RecordWetClothUsage();
            }
        }
        
        // Monitor fire damage
        if (playerFireHealth != null)
        {
            int currentHealth = playerFireHealth.GetCurrentHealth();
            
            if (currentHealth < lastPlayerHealth)
            {
                int damageAmount = lastPlayerHealth - currentHealth;
                for (int i = 0; i < damageAmount; i++)
                {
                    RecordFireDamage();
                }
            }
            
            lastPlayerHealth = currentHealth;
            
            // Check if player died
            if (!playerFireHealth.IsAlive())
            {
                RecordPlayerDeath();
            }
        }
        
        // Monitor oxygen depletion
        if (!hasRecordedOxygenDepletion && oxygenManager != null)
        {
            if (oxygenManager.HasSuffocated())
            {
                RecordOxygenDepletion();
            }
        }
        
        UpdateUI();
    }
    
    public void RecordWetClothUsage()
    {
        if (hasUsedWetCloth) return;
        
        hasUsedWetCloth = true;
        currentScore += wetClothBonus;
        
        Debug.Log($"<color=green>+{wetClothBonus} points: Used wet cloth for protection</color>");
    }
    
    public void RecordNPCRescue()
    {
        if (hasRescuedNPC) return;
        
        // Check if any FollowerNPC is currently following
        FollowerNPC[] npcs = FindObjectsOfType<FollowerNPC>();
        bool hasFollowingNPC = false;
        
        foreach (var npc in npcs)
        {
            // Check if NPC has IsFollowing method (add this to your FollowerNPC.cs)
            if (npc.gameObject.activeInHierarchy)
            {
                // Try to get IsFollowing status if the method exists
                // If you added the IsFollowing() method to FollowerNPC, uncomment this:
                // if (npc.IsFollowing())
                // {
                //     hasFollowingNPC = true;
                //     break;
                // }
                
                // Temporary: Check if NPC is close to player (fallback method)
                if (npc.playerTransform != null)
                {
                    float distance = Vector3.Distance(npc.transform.position, npc.playerTransform.position);
                    if (distance < 5f) // NPC is close to player
                    {
                        hasFollowingNPC = true;
                        break;
                    }
                }
            }
        }
        
        if (hasFollowingNPC)
        {
            hasRescuedNPC = true;
            currentScore += npcRescueBonus;
            
            Debug.Log($"<color=green>+{npcRescueBonus} points: Evacuated with NPC</color>");
        }
    }
    
    public void RecordFireDamage()
    {
        currentScore += fireDamagePenalty;
        safetyViolations++;
        errorCount++;
        
        Debug.Log($"<color=red>{fireDamagePenalty} points: Fire damage taken</color>");
    }
    
    public void RecordOxygenDepletion()
    {
        if (hasRecordedOxygenDepletion) return;
        
        hasRecordedOxygenDepletion = true;
        currentScore += oxygenDepletionPenalty;
        safetyViolations++;
        errorCount++;
        
        Debug.Log($"<color=red>{oxygenDepletionPenalty} points: Oxygen depleted</color>");
        
        // Trigger failure state
        HandleFailure();
    }
    
    public void RecordTimeExpired()
    {
        if (hasRecordedTimeExpired) return;
        
        hasRecordedTimeExpired = true;
        completedOnTime = false;
        
        Debug.Log("<color=red>Evacuation failed: Time expired</color>");
        
        // No score change, just record the failure
        SaveScoreData();
    }
    
    public void RecordSuccessfulExit()
    {
        // Check if NPC is following at exit
        RecordNPCRescue();
        
        // Check time completion
        if (evacuationTimer != null && !evacuationTimer.HasTimeExpired())
        {
            completedOnTime = true;
            currentScore += timeCompletionBonus;
            
            Debug.Log($"<color=green>+{timeCompletionBonus} points: Completed evacuation on time</color>");
            
            // Add time bonus
            float timeBonus = evacuationTimer.GetTimeBonus();
            if (timeBonus > 0)
            {
                int timeBonusPoints = Mathf.RoundToInt(timeBonus);
                currentScore += timeBonusPoints;
                Debug.Log($"<color=green>+{timeBonusPoints} points: Time bonus</color>");
            }
            else if (timeBonus < 0)
            {
                int timePenalty = Mathf.RoundToInt(timeBonus);
                currentScore += timePenalty;
                Debug.Log($"<color=yellow>{timePenalty} points: Time penalty</color>");
            }
        }
        
        // Save score and proceed to next scene
        SaveScoreData();
    }
    
    void RecordPlayerDeath()
    {
        Debug.Log("<color=red>Player died from fire damage</color>");
        HandleFailure();
    }
    
    void HandleFailure()
    {
        // Stop the timer
        if (evacuationTimer != null)
        {
            evacuationTimer.StopTimer();
        }
        
        // Open failure canvas and stop time
        var timer = FindObjectOfType<FireEvacuationTimer>();
        if (timer != null && timer.failureCanvas != null)
        {
            Time.timeScale = 0f;
            timer.failureCanvas.SetActive(true);
        }
        
        // Save score data even on failure
        SaveScoreData();
    }
    
    void SaveScoreData()
{
    if (ScoreDataManager.Instance == null)
    {
        Debug.LogWarning("ScoreDataManager not found! Cannot save score data.");
        return;
    }
    
    Debug.Log($"===== EVACUATION COMPLETE =====");
    Debug.Log($"Final Score: {currentScore}");
    Debug.Log($"Safety Violations: {safetyViolations}");
    Debug.Log($"Errors: {errorCount}");
    Debug.Log($"Completed On Time: {completedOnTime}");
    Debug.Log($"Used Wet Cloth: {hasUsedWetCloth}");
    Debug.Log($"Rescued NPC: {hasRescuedNPC}");
    Debug.Log($"==============================");
    
    // The actual saving is done by ScoreDataManager.SaveFireEvacuationData()
    // which is called from the FireEvacuationExitPoint
}
    
    void UpdateUI()
    {
        if (scoreText != null)
        {
            scoreText.text = $"Score: {currentScore}";
        }
        
        if (safetyViolationsText != null)
        {
            safetyViolationsText.text = $"Safety Violations: {safetyViolations}";
        }
        
        if (errorsText != null)
        {
            errorsText.text = $"Errors: {errorCount}";
        }
    }
    
    // Public getters
    public int GetCurrentScore() => currentScore;
    public int GetSafetyViolations() => safetyViolations;
    public int GetErrorCount() => errorCount;
    public bool HasUsedWetCloth() => hasUsedWetCloth;
    public bool HasRescuedNPC() => hasRescuedNPC;
    public bool CompletedOnTime() => completedOnTime;
}