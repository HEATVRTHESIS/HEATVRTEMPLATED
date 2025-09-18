using UnityEngine;
using TMPro;
using System.Collections;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class NPCInteraction : MonoBehaviour
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

    // VR Controller Input
    public InputActionProperty talkAction;

    private bool isPlayerPointingAtNPC = false;
    private bool isDialogueActive = false;

    void Start()
    {
        dialoguePanel.SetActive(false);
        interactionIndicator.SetActive(false);
        replyOption1.onClick.AddListener(OnReplyOption1);
        replyOption2.onClick.AddListener(OnReplyOption2);
        talkAction.action.Enable();
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

        Destroy(gameObject);
        dialoguePanel.SetActive(false);
    }
}