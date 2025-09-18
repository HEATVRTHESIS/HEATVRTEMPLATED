using UnityEngine;
using TMPro;
using System.Collections;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using System.Reflection;

public class NPCInteraction : TaskController
{
    // UI Elements
    public GameObject dialoguePanel;
    public TextMeshProUGUI dialogueText;
    public Button replyOption1;
    public Button replyOption2;

    // VR Interaction UI
    public GameObject interactionIndicator;

    private bool isCompleting = false;
    
    // Dialogue Content
    private string initialDialogue = "Ah there is a fire what should we do?";
    private string correctDialogue = "Ok! I will evacuate and alert the others immediately.";
    private string wrongDialogue = "That doesn't sound right... Try again.";
    
    // Player and NPC
    public Transform rightControllerTransform;
    public Animator npcAnimator;
    public float evacuationSpeed = 2f;
    public Transform evacuationPoint;

    // Task-specific fields
    [Header("Task Settings")]
    [Tooltip("The NPC object to highlight (usually this same GameObject)")]
    public HighlightableObject targetObject;
    
    [Header("UI")]
    public PopupManager popupManager;

    // VR Controller Input
    public InputActionProperty talkAction;

    private bool isPlayerPointingAtNPC = false;
    private bool isDialogueActive = false;


   new void Awake()
{
    Debug.Log($"NPCInteraction Awake() called on {gameObject.name}");
    // Don't call base.Awake() since it expects ObjectSpawner and Bin components
}

    void Start()
    {

        Debug.Log($"NPCInteraction Start() called on {gameObject.name}");
        Debug.Log($"taskName: '{taskName}', taskDescription: '{taskDescription}'");
        dialoguePanel.SetActive(false);
        interactionIndicator.SetActive(false);
        replyOption1.onClick.AddListener(OnReplyOption1);
        replyOption2.onClick.AddListener(OnReplyOption2);
        talkAction.action.Enable();
        
        // Task controller setup
        if (targetObject == null)
        {
            // Try to find HighlightableObject on this GameObject if not assigned
            targetObject = GetComponent<HighlightableObject>();
        }
        
        // Set totalItems to 1 for NPC evacuation task
        totalItems = 1;
    }

    /// <summary>
    /// Override the InitializeTask method from TaskController
    /// </summary>
    public new void InitializeTask()
    {
        // Don't call base.InitializeTask() since it tries to spawn objects
        // Set totalItems and don't update progress yet (will be done by UI)
        totalItems = 1;
        
        Debug.Log($"NPC evacuation task '{taskName}' initialized.");
    }

    /// <summary>
    /// Called after UI is set up to update initial progress
    /// </summary>
    public void UpdateInitialProgress()
    {
        OnProgressUpdated.Invoke(0, totalItems);
    }

    void Update()
    {
        CheckForRaycastHit();

        if (isPlayerPointingAtNPC && talkAction.action.WasPressedThisFrame() && !isDialogueActive)
        {
            ShowInitialDialogue();
        }
    }

    void CheckForRaycastHit()
    {
        RaycastHit hit;
        if (Physics.Raycast(rightControllerTransform.position, rightControllerTransform.forward, out hit, 10f))
        {
            if (hit.collider.gameObject == this.gameObject)
            {
                isPlayerPointingAtNPC = true;
                interactionIndicator.SetActive(true);
            }
            else
            {
                isPlayerPointingAtNPC = false;
                interactionIndicator.SetActive(false);
            }
        }
        else
        {
            isPlayerPointingAtNPC = false;
            interactionIndicator.SetActive(false);
        }
    }

    void ShowInitialDialogue()
    {
        isDialogueActive = true;
        dialoguePanel.SetActive(true);
        dialogueText.text = initialDialogue;
        interactionIndicator.SetActive(false);
    }

    void OnReplyOption1()
    {
        dialogueText.text = correctDialogue;
        replyOption1.gameObject.SetActive(false);
        replyOption2.gameObject.SetActive(false);
        StartCoroutine(EvacuateNPC());
        isDialogueActive = false;
    }

    void OnReplyOption2()
    {
        dialogueText.text = wrongDialogue;
    }

    IEnumerator EvacuateNPC()
{
    // Immediately start the walk animation
    npcAnimator.SetTrigger("Walk");
    
    while (Vector3.Distance(transform.position, evacuationPoint.position) > 0.1f)
    {
        transform.position = Vector3.MoveTowards(transform.position, evacuationPoint.position, evacuationSpeed * Time.deltaTime);
        transform.LookAt(evacuationPoint);
        yield return null;
    }

    Debug.Log("NPC reached evacuation point");
    
    // Hide dialogue first
    dialoguePanel.SetActive(false);
    
    // Complete the task
    CompleteNPCTask();
    
    // Wait for events to finish, then destroy
    yield return new WaitForSeconds(0.5f);
    
    Debug.Log("Destroying NPC now");
    Destroy(gameObject);
}

    /// <summary>
    /// Completes the NPC evacuation task
    /// </summary>
    private void CompleteNPCTask()
{
    if (isCompleting || IsTaskCompleted())
    {
        Debug.Log("Task already completing or completed, skipping");
        return;
    }
    
    isCompleting = true;
    Debug.Log("CompleteNPCTask() started");
    
    try
    {
        Debug.Log($"NPC evacuation task '{taskName}' completed! NPC reached evacuation point.");
        
        Debug.Log("About to SetTaskCompleted...");
        SetTaskCompleted();
        
        Debug.Log("About to invoke progress update...");
        OnProgressUpdated.Invoke(1, totalItems);
        
        Debug.Log("About to invoke task completed...");
        OnTaskCompleted.Invoke();
        
        Debug.Log("About to call EndTask...");
        EndTask();

        Debug.Log("About to handle audio...");
        if (audioSource != null && successSound != null)
        {
            audioSource.PlayOneShot(successSound);
        }
        else if (successSound != null)
        {
            PlaySuccessSoundFallback();
        }

        Debug.Log("About to show completion dialogue...");
        ShowCompletionDialogue();
        
        Debug.Log("About to show popup...");
        if (popupManager != null)
        {
            popupManager.ShowMessage("NPC successfully evacuated!");
        }
        
        Debug.Log("CompleteNPCTask() finished");
    }
    catch (System.Exception e)
    {
        Debug.LogError($"Exception in CompleteNPCTask: {e.Message}");
        Debug.LogError($"Stack trace: {e.StackTrace}");
    }
}

    /// <summary>
    /// Sets the task as completed using reflection since isTaskCompleted is private
    /// </summary>
    private void SetTaskCompleted()
    {
        var field = typeof(TaskController).GetField("isTaskCompleted", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (field != null)
        {
            field.SetValue(this, true);
        }
    }

    /// <summary>
    /// Check if task is completed using reflection since isTaskCompleted is private
    /// </summary>
    private bool IsTaskCompleted()
    {
        var field = typeof(TaskController).GetField("isTaskCompleted", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (field != null)
        {
            return (bool)field.GetValue(this);
        }
        return false;
    }

    /// <summary>
    /// Fallback method to play success sound without AudioSource component
    /// </summary>
    private void PlaySuccessSoundFallback()
    {
        AudioSource foundAudioSource = GetComponentInChildren<AudioSource>();
        if (foundAudioSource != null)
        {
            foundAudioSource.PlayOneShot(successSound);
        }
        else
        {
            GameObject tempAudio = new GameObject("TempAudioSource");
            AudioSource tempSource = tempAudio.AddComponent<AudioSource>();
            tempSource.PlayOneShot(successSound);
            Destroy(tempAudio, successSound.length);
        }
    }

    /// <summary>
    /// Shows the completion dialogue using VRDialogueSystem
    /// </summary>
    private void ShowCompletionDialogue()
    {
        VRDialogueSystem dialogueSystem = FindObjectOfType<VRDialogueSystem>();
        if (dialogueSystem != null && taskCompletionDialogue != null && taskCompletionDialogue.Length > 0)
        {
            dialogueSystem.StartDialog(taskCompletionDialogue);
        }
    }

    /// <summary>
    /// Override the StartTask method from TaskController
    /// </summary>
    public new void StartTask()
    {
        if (IsTaskCompleted()) return;

        if (targetObject != null)
        {
            targetObject.SetHighlight(true);
        }
        
        Debug.Log($"Started NPC evacuation task '{taskName}'.");
    }

    /// <summary>
    /// Override the EndTask method from TaskController
    /// </summary>
    public new void EndTask()
    {
        Debug.Log($"Ending NPC evacuation task '{taskName}' and turning off highlights.");
        
        if (targetObject != null)
        {
            targetObject.SetHighlight(false);
        }
        
        if (interactionIndicator != null)
        {
            interactionIndicator.SetActive(false);
        }
    }

    /// <summary>
    /// Force complete the task (for testing)
    /// </summary>
    [ContextMenu("Force Complete Task")]
    public void ForceCompleteTask()
    {
        CompleteNPCTask();
    }
}