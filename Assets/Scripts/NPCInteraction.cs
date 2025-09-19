using UnityEngine;
using TMPro;
using System.Collections;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class NPCInteraction : CustomTaskController
{
    // UI Elements
    public GameObject dialoguePanel;
    public TextMeshProUGUI dialogueText;
    public Button replyOption1;
    public Button replyOption2;

    // VR Interaction UI
    public GameObject interactionIndicator;

    // Dialogue Content
    private string initialDialogue = "Hello, traveler. Can you help me?";
    private string correctDialogue = "Thank you! I will evacuate immediately.";
    private string wrongDialogue = "That's not what I'm looking for. Try again.";

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
    public override void InitializeTask()
    {
        base.InitializeTask();

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

        // COMPLETE THE TASK - This was missing!
        CompleteTask();

        // Hide dialogue first
        dialoguePanel.SetActive(false);

        // Wait for events to finish, then destroy
        yield return new WaitForSeconds(0.5f);

        Debug.Log("Destroying NPC now");
        Destroy(gameObject);
    }

    /// <summary>
    /// Override the StartTask method from TaskController
    /// </summary>
    public override void StartTask()
{
    Debug.Log($"NPCInteraction.StartTask() called. IsCompleted: {IsTaskCompleted()}");
    Debug.Log($"NPC targetObject is: {(targetObject != null ? targetObject.name : "null")}");
    
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
    public override void EndTask()
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

}