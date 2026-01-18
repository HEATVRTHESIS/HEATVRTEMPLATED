using UnityEngine;
using UnityEngine.Events;

public class PullDownTrigger : CustomTaskController
{
    private HingeJoint myHingeJoint; 
    public float successAngle = 92f; 
    [SerializeField] private UnityEvent onPullSuccess;
    private bool _isPulled = false;

    [Header("Fire Alarm Settings")]
    public HighlightableObject targetObject;
    
    [Header("Error Dialogue")]
    [Tooltip("The dialogue lines to display if task incomplete when timer expires.")]
    public string[] taskErrorDialogue;

    [Header("Audio")]
    public AudioClip errorSound;
    
    [Header("UI")]
    public PopupManager popupManager;

    private void Start()
    {
        myHingeJoint = GetComponent<HingeJoint>();
        if (myHingeJoint == null)
        {
            Debug.LogError("Hinge Joint not found on this GameObject.");
        }

        if (targetObject == null)
        {
            targetObject = GetComponent<HighlightableObject>();
        }

        totalItems = 1;
    }

    public override void InitializeTask()
    {
        base.InitializeTask();
        Debug.Log($"Fire alarm task '{taskName}' initialized. Required angle: {successAngle}°");
    }

    public void UpdateInitialProgress()
    {
        OnProgressUpdated.Invoke(0, totalItems);
    }

    private void Update()
    {
        if (!_isPulled && !IsTaskCompleted())
        {
            if (myHingeJoint.angle >= successAngle - 5.0f)
            {
                onPullSuccess?.Invoke();
                _isPulled = true;
                
                CompleteTask();

                if (FireScoreTracker.Instance != null)
                {
                    FireScoreTracker.Instance.OnFireAlarmPulled();
                }

                FireScoreTracker.Instance.OnSafetyCompliance("Fire alarm activated");
            }
        }
    }

    public void OnTimerExpiredIncomplete()
    {
        if (IsTaskCompleted()) return;

        if (ErrorTracker.Instance != null)
            ErrorTracker.Instance.RecordFireLeverError();

        if (audioSource != null && errorSound != null)
            audioSource.PlayOneShot(errorSound);

        VRDialogueSystem dialogueSystem = FindObjectOfType<VRDialogueSystem>();
        if (dialogueSystem != null && taskErrorDialogue != null && taskErrorDialogue.Length > 0)
        {
            dialogueSystem.StartDialog(taskErrorDialogue);
        }
    }
    
    public override void StartTask()
    {
        if (IsTaskCompleted()) return;

        if (targetObject != null)
        {
            targetObject.SetHighlight(true);
        }
    }

    public override void EndTask()
    {
        if (targetObject != null)
        {
            targetObject.SetHighlight(false);
        }
    }

    public float GetPullProgress()
    {
        if (myHingeJoint == null) return 0f;
        return Mathf.Clamp01(myHingeJoint.angle / successAngle);
    }

    public float GetCurrentAngle()
    {
        return myHingeJoint != null ? myHingeJoint.angle : 0f;
    }

    public bool IsLeverPulled()
    {
        return _isPulled;
    }

    [ContextMenu("Force Complete Task")]
    public void ForceCompleteTask()
    {
        _isPulled = true;
        CompleteTask();
    }

    void OnDrawGizmosSelected()
    {
        if (myHingeJoint != null)
        {
            Gizmos.color = IsTaskCompleted() ? Color.green : (_isPulled ? Color.red : Color.yellow);
            Gizmos.DrawWireSphere(transform.position, 0.5f);
            
            float progress = GetPullProgress();
            Gizmos.color = Color.Lerp(Color.red, Color.green, progress);
            Gizmos.DrawWireCube(transform.position + Vector3.up * 1f, Vector3.one * 0.5f * progress);
        }
    }
}