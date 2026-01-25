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
    
    // Task tracking
    private int completedTasks = 0;
    private int totalTasks = 3; // wet cloth, NPC rescue, on-time completion
    
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
        if (!hasUsedWetCloth && oxygenManager != null)
        {
            if (oxygenManager.GetProtectingClothCount() > 0)
            {
                RecordWetClothUsage();
            }
        }
        
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
            
            if (!playerFireHealth.IsAlive())
            {
                RecordPlayerDeath();
            }
        }
        
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
        completedTasks++;
        
        Debug.Log($"<color=green>+{wetClothBonus} points: Used wet cloth for protection</color>");
    }
    
    public void RecordNPCRescue()
    {
        if (hasRescuedNPC) return;
        
        FollowerNPC[] npcs = FindObjectsOfType<FollowerNPC>();
        IVPoleFollowerNPC[] ivNpcs = FindObjectsOfType<IVPoleFollowerNPC>();
        bool hasFollowingNPC = false;
        
        // Check regular follower NPCs
        foreach (var npc in npcs)
        {
            if (npc.gameObject.activeInHierarchy)
            {
                if (npc.IsFollowing())
                {
                    hasFollowingNPC = true;
                    break;
                }
            }
        }
        
        // Check IV pole follower NPCs
        if (!hasFollowingNPC)
        {
            foreach (var npc in ivNpcs)
            {
                if (npc.gameObject.activeInHierarchy)
                {
                    if (npc.IsFollowing())
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
            completedTasks++;
            
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
        
        HandleFailure();
    }
    
    public void RecordTimeExpired()
    {
        if (hasRecordedTimeExpired) return;
        
        hasRecordedTimeExpired = true;
        completedOnTime = false;
        
        Debug.Log("<color=red>Evacuation failed: Time expired</color>");
        
        SaveScoreData();
    }
    
    public void RecordSuccessfulExit()
    {
        RecordNPCRescue();
        
        if (evacuationTimer != null && !evacuationTimer.HasTimeExpired())
        {
            completedOnTime = true;
            currentScore += timeCompletionBonus;
            completedTasks++;
            
            Debug.Log($"<color=green>+{timeCompletionBonus} points: Completed evacuation on time</color>");
            
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
        
        SaveScoreData();
    }
    
    void RecordPlayerDeath()
    {
        Debug.Log("<color=red>Player died from fire damage</color>");
        HandleFailure();
    }
    
    void HandleFailure()
    {
        if (evacuationTimer != null)
        {
            evacuationTimer.StopTimer();
        }
        
        var timer = FindObjectOfType<FireEvacuationTimer>();
        if (timer != null && timer.failureCanvas != null)
        {
            Time.timeScale = 0f;
            timer.failureCanvas.SetActive(true);
        }
        
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
        Debug.Log($"Completed Tasks: {completedTasks}/{totalTasks}");
        Debug.Log($"Safety Violations: {safetyViolations}");
        Debug.Log($"Errors: {errorCount}");
        Debug.Log($"Completed On Time: {completedOnTime}");
        Debug.Log($"Used Wet Cloth: {hasUsedWetCloth}");
        Debug.Log($"Rescued NPC: {hasRescuedNPC}");
        Debug.Log($"==============================");
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
    
    // Task tracking getters
    public int GetCompletedTasks() => completedTasks;
    public int GetTotalTasks() => totalTasks;
    public float GetCompletionPercentage() => totalTasks > 0 ? (float)completedTasks / totalTasks * 100f : 0f;
}