using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[System.Serializable]
public class AnimationInput
{
    public string animationPropertyName;
    public InputActionProperty action;
}

public class AnimateOnInput : MonoBehaviour
{
    public List<AnimationInput> animationInputs;
    public Animator animator;
    public bool debugMode = true; // Toggle this to see debug info

    private void OnEnable()
    {
        // Enable all input actions when the component is enabled
        foreach (var item in animationInputs)
        {
            if (item.action.action != null)
            {
                item.action.action.Enable();
                if (debugMode)
                {
                    Debug.Log($"Enabled action for {item.animationPropertyName}");
                }
            }
            else
            {
                Debug.LogWarning($"Action for {item.animationPropertyName} is null!");
            }
        }
    }

    private void OnDisable()
    {
        // Disable all input actions when the component is disabled
        foreach (var item in animationInputs)
        {
            if (item.action.action != null)
            {
                item.action.action.Disable();
            }
        }
    }

    void Update()
    {
        if (animator == null)
        {
            Debug.LogError("Animator is not assigned!");
            return;
        }

        foreach (var item in animationInputs)
        {
            if (item.action.action != null)
            {
                float actionValue = item.action.action.ReadValue<float>();
                animator.SetFloat(item.animationPropertyName, actionValue);

                // Debug logging (disable debugMode in inspector when working)
                if (debugMode && actionValue > 0.01f)
                {
                    Debug.Log($"{item.animationPropertyName}: {actionValue}");
                }
            }
        }
    }

    // Optional: Validate in editor
    private void OnValidate()
    {
        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }
    }
}