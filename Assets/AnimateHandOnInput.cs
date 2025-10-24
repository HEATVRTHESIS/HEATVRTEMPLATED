using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class AnimateHandOnInput : MonoBehaviour
{
    public InputActionProperty triggerValue;
    public InputActionProperty gripValue;

    public Animator handAnimator;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        float trigger = triggerValue.action.ReadValue<float>();
        float grip = gripValue.action.ReadValue<float>();

        handAnimator.SetFloat("Trigger", trigger);
        handAnimator.SetFloat("Grip", grip);
    }
}
