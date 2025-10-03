using UnityEngine;
using TMPro;
using System.Collections;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.AI;

public class FollowerNPC : MonoBehaviour
{
    [Header("UI Elements")]
    public GameObject dialoguePanel;
    public TextMeshProUGUI dialogueText;
    public Button followButton;
    public Button cancelButton;

    [Header("Navigation")]
    public NavMeshAgent navAgent;
    
    [Header("VR Interaction")]
    public GameObject interactionIndicator;
    public Transform rightControllerTransform;
    public InputActionProperty talkAction;
    
    [Header("Dialogue")]
    [TextArea(2, 4)]
    public string initialDialogue = "I don't know where to go! Can you help me?";
    [TextArea(2, 4)]
    public string followingDialogue = "Okay, I'll follow you!";
    
    [Header("NPC Settings")]
    public Animator npcAnimator;
    public Transform playerTransform;
    
    private bool isPlayerPointingAtNPC = false;
    private bool isDialogueActive = false;
    private bool isFollowing = false;

    void Start()
    {
        dialoguePanel.SetActive(false);
        interactionIndicator.SetActive(false);
        
        followButton.onClick.AddListener(OnFollowButtonClicked);
        cancelButton.onClick.AddListener(OnCancelButtonClicked);
        
        talkAction.action.Enable();
    }

    void Update()
    {
        if (isFollowing)
        {
            FollowPlayer();
        }
        else
        {
            CheckForRaycastHit();
            
            if (isPlayerPointingAtNPC && talkAction.action.WasPressedThisFrame() && !isDialogueActive)
            {
                ShowInitialDialogue();
            }
        }
    }

    void CheckForRaycastHit()
    {
        RaycastHit hit;
        if (Physics.Raycast(rightControllerTransform.position, rightControllerTransform.forward, out hit, 10f))
        {
            if (hit.collider.gameObject == this.gameObject || hit.collider.transform.IsChildOf(transform))
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
        
        followButton.gameObject.SetActive(true);
        cancelButton.gameObject.SetActive(true);
    }

    void OnFollowButtonClicked()
    {
        dialogueText.text = followingDialogue;
        followButton.gameObject.SetActive(false);
        cancelButton.gameObject.SetActive(false);
        
        StartCoroutine(StartFollowingAfterDelay(1.5f));
    }

    void OnCancelButtonClicked()
    {
        dialoguePanel.SetActive(false);
        isDialogueActive = false;
    }

    IEnumerator StartFollowingAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        
        dialoguePanel.SetActive(false);
        isDialogueActive = false;
        isFollowing = true;
        
        Debug.Log($"{gameObject.name} is now following the player");
    }

    void FollowPlayer()
    {
        if (playerTransform == null)
        {
            Debug.LogWarning("Player transform not assigned!");
            return;
        }

        if (navAgent != null)
        {
            // Set destination to player
            navAgent.SetDestination(playerTransform.position);
            
            // Check if agent has reached the destination (within stopping distance)
            if (!navAgent.pathPending && navAgent.remainingDistance <= navAgent.stoppingDistance)
            {
                // Reached destination - idle
                if (npcAnimator != null)
                {
                    npcAnimator.SetTrigger("Idle");
                }
            }
            else
            {
                // Still moving - walk
                if (npcAnimator != null)
                {
                    npcAnimator.SetTrigger("Walk");
                }
            }
        }
    }

    /// <summary>
    /// Call this to make the NPC stop following (e.g., when they reach the exit)
    /// </summary>
    public void StopFollowing()
    {
        isFollowing = false;
        
        if (navAgent != null)
        {
            navAgent.isStopped = true;
        }
        
        if (npcAnimator != null)
        {
            npcAnimator.SetTrigger("Idle");
        }
        
        Debug.Log($"{gameObject.name} stopped following");
    }

    /// <summary>
    /// Optional: Auto-detect player if not assigned
    /// </summary>
    void OnValidate()
    {
        if (playerTransform == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                playerTransform = player.transform;
            }
        }
    }
}