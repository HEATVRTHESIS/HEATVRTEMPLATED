using UnityEngine;
using TMPro;
using System.Collections;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class FollowerNPC : MonoBehaviour
{
    [Header("UI Elements")]
    public GameObject dialoguePanel;
    public TextMeshProUGUI dialogueText;
    public Button followButton;
    public Button cancelButton;
    
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
    public float followSpeed = 3f;
    public float followDistance = 2f; // How close to stay to player
    public float stoppingDistance = 1.5f; // Stop moving when this close
    public float rotationSpeed = 5f;
    public bool lockYPosition = true; // Keep NPC at ground level
    
    private bool isPlayerPointingAtNPC = false;
    private bool isDialogueActive = false;
    private bool isFollowing = false;
    private float initialYPosition;

    void Start()
    {
        dialoguePanel.SetActive(false);
        interactionIndicator.SetActive(false);
        
        followButton.onClick.AddListener(OnFollowButtonClicked);
        cancelButton.onClick.AddListener(OnCancelButtonClicked);
        
        talkAction.action.Enable();
        
        // Store initial Y position to keep NPC on ground
        initialYPosition = transform.position.y;
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
        
        // Calculate distance on XZ plane only (ignore Y)
        Vector3 npcPosFlat = new Vector3(transform.position.x, 0, transform.position.z);
        Vector3 playerPosFlat = new Vector3(playerTransform.position.x, 0, playerTransform.position.z);
        float distanceToPlayer = Vector3.Distance(npcPosFlat, playerPosFlat);
        
        // Only move if beyond stopping distance
        if (distanceToPlayer > stoppingDistance)
        {
            // Move directly towards player (on XZ plane)
            Vector3 directionToPlayer = (playerPosFlat - npcPosFlat).normalized;
            
            // Calculate new position
            Vector3 newPosition = transform.position + directionToPlayer * followSpeed * Time.deltaTime;
            
            // Lock Y position to stay on ground
            if (lockYPosition)
            {
                newPosition.y = initialYPosition;
            }
            
            transform.position = newPosition;
            
            // Trigger walk animation
            if (npcAnimator != null)
            {
                npcAnimator.SetTrigger("Walk");
            }
        }
        else
        {
            // Close enough - trigger idle
            if (npcAnimator != null)
            {
                npcAnimator.SetTrigger("Idle");
            }
        }
        
        // Rotate to face player (only on Y axis)
        Vector3 lookDirection = playerPosFlat - npcPosFlat;
        
        if (lookDirection.magnitude > 0.1f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(lookDirection);
            transform.rotation = Quaternion.Slerp(
                transform.rotation, 
                targetRotation, 
                rotationSpeed * Time.deltaTime
            );
        }
    }

    /// <summary>
    /// Call this to make the NPC stop following (e.g., when they reach the exit)
    /// </summary>
    public void StopFollowing()
    {
        isFollowing = false;
        
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

    void OnDrawGizmosSelected()
    {
        if (playerTransform != null && isFollowing)
        {
            // Draw stopping distance
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, stoppingDistance);
            
            // Draw follow distance (for reference)
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, followDistance);
            
            // Draw line to player (flat on ground)
            Gizmos.color = Color.cyan;
            Vector3 npcFlat = new Vector3(transform.position.x, 0, transform.position.z);
            Vector3 playerFlat = new Vector3(playerTransform.position.x, 0, playerTransform.position.z);
            Gizmos.DrawLine(npcFlat, playerFlat);
        }
    }
}