using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IKFootSolver : MonoBehaviour
{
    public bool isMovingForward;

    [SerializeField] LayerMask terrainLayer = default;
    [SerializeField] Transform body = default;
    [SerializeField] IKFootSolver otherFoot = default;
    [SerializeField] float speed = 4;
    [SerializeField] float stepDistance = .28f;
    [SerializeField] float stepLength = .30f;
    [SerializeField] float sideStepLength = .22f;
    [SerializeField] float maxDistanceBeforeForcedStep = .32f;
    [SerializeField] float stepSpeedFromBodyVelocity = 0.45f;
    [SerializeField] float maxAdaptiveStepSpeed = 6f;
    [SerializeField] float strafeStepSpeedMultiplier = 1.12f;
    [SerializeField] float strafeForwardBias = 0.20f;
    [SerializeField] float strafeTriggerDistanceMultiplier = 0.72f;
    [SerializeField] float strafeForcedCatchUpDistance = 0.24f;
    [SerializeField] float bodySpeedSmoothing = 10f;

    [SerializeField] float stepHeight = .3f;
    [SerializeField] float stepHeightVariation = .08f;
    [SerializeField] float stepLengthVariation = .06f;
    [SerializeField] float footRotationSmoothSpeed = 14f;
    [SerializeField] Vector3 footOffset = default;

    public Vector3 footRotOffset;
    public float footYPosOffset = 0.1f;

    public float rayStartYOffset = 0;
    public float rayLength = 1.5f;
    
    float footSpacing;
    Vector3 oldPosition, currentPosition, newPosition;
    Vector3 oldNormal, currentNormal, newNormal;
    float lerp;
    Vector3 previousBodyPosition;
    float bodyHorizontalSpeed;
    float smoothedBodyHorizontalSpeed;
    float activeStepHeight;
    Quaternion currentFootRotation;

    private void Start()
    {
        footSpacing = transform.localPosition.x;
        currentPosition = newPosition = oldPosition = transform.position;
        currentNormal = newNormal = oldNormal = transform.up;
        lerp = 1;
        previousBodyPosition = body.position;
        smoothedBodyHorizontalSpeed = 0f;
        activeStepHeight = stepHeight;
        currentFootRotation = transform.rotation;
    }

    // Update is called once per frame

    void Update()
    {
        transform.position = currentPosition + Vector3.up * footYPosOffset;

        // Keep foot aligned to ground while preserving body facing direction.
        Vector3 projectedForward = Vector3.ProjectOnPlane(body.forward, currentNormal);
        if (projectedForward.sqrMagnitude < 0.0001f)
        {
            projectedForward = Vector3.ProjectOnPlane(transform.forward, currentNormal);
        }

        Quaternion terrainAlignedRotation = Quaternion.LookRotation(projectedForward.normalized, currentNormal) * Quaternion.Euler(footRotOffset);
        currentFootRotation = Quaternion.Slerp(currentFootRotation, terrainAlignedRotation, Time.deltaTime * footRotationSmoothSpeed);
        transform.rotation = currentFootRotation;

        Vector3 bodyDelta = body.position - previousBodyPosition;
        bodyDelta.y = 0f;
        bodyHorizontalSpeed = bodyDelta.magnitude / Mathf.Max(0.0001f, Time.deltaTime);
        smoothedBodyHorizontalSpeed = Mathf.Lerp(smoothedBodyHorizontalSpeed, bodyHorizontalSpeed, Time.deltaTime * bodySpeedSmoothing);
        previousBodyPosition = body.position;

        Ray ray = new Ray(body.position + (body.right * footSpacing) + Vector3.up * rayStartYOffset, Vector3.down);

        Debug.DrawRay(body.position + (body.right * footSpacing) + Vector3.up * rayStartYOffset, Vector3.down);
            
        if (Physics.Raycast(ray, out RaycastHit info, rayLength, terrainLayer.value))
        {
            Vector3 planarDelta = Vector3.ProjectOnPlane(info.point - currentPosition, Vector3.up);
            float targetDistance = planarDelta.magnitude;

            Vector3 movementDirection = planarDelta.sqrMagnitude > 0.0001f
                ? planarDelta.normalized
                : Vector3.ProjectOnPlane(body.forward, Vector3.up).normalized;

            float forwardDotPre = Mathf.Abs(Vector3.Dot(movementDirection, body.forward));
            float sideDotPre = Mathf.Abs(Vector3.Dot(movementDirection, body.right));
            bool predictedForwardStep = forwardDotPre >= sideDotPre;

            float adaptiveStepDistance = Mathf.Max(0.08f, stepDistance - (smoothedBodyHorizontalSpeed * 0.02f));
            if (!predictedForwardStep)
            {
                adaptiveStepDistance *= Mathf.Clamp(strafeTriggerDistanceMultiplier, 0.2f, 1f);
            }

            bool requiresCatchUp = targetDistance > maxDistanceBeforeForcedStep;
            if (!predictedForwardStep && targetDistance > strafeForcedCatchUpDistance)
            {
                requiresCatchUp = true;
            }

            bool canStartStep = lerp >= 1f && (!otherFoot.IsMoving() || requiresCatchUp);

            if (targetDistance > adaptiveStepDistance && canStartStep)
            {
                lerp = 0;

                oldPosition = currentPosition;
                oldNormal = currentNormal;

                Vector3 direction = planarDelta.sqrMagnitude > 0.0001f
                    ? planarDelta.normalized
                    : Vector3.ProjectOnPlane(body.forward, Vector3.up).normalized;

                float forwardDot = Mathf.Abs(Vector3.Dot(direction, body.forward));
                float sideDot = Mathf.Abs(Vector3.Dot(direction, body.right));
                isMovingForward = forwardDot >= sideDot;

                if (isMovingForward)
                {
                    float strideMul = Random.Range(1f - stepLengthVariation, 1f + stepLengthVariation);
                    newPosition = info.point + direction * (stepLength * strideMul) + footOffset;
                    newNormal = info.normal;
                }
                else
                {
                    // Blend some forward into side steps to avoid sideways knee collapse.
                    Vector3 bodyForwardOnPlane = Vector3.ProjectOnPlane(body.forward, Vector3.up).normalized;
                    direction = Vector3.Slerp(direction, bodyForwardOnPlane, Mathf.Clamp01(strafeForwardBias)).normalized;

                    float sideMul = Random.Range(1f - stepLengthVariation, 1f + stepLengthVariation);
                    newPosition = info.point + direction * (sideStepLength * sideMul) + footOffset;
                    newNormal = info.normal;
                }

                activeStepHeight = stepHeight * Random.Range(1f - stepHeightVariation, 1f + stepHeightVariation);
                activeStepHeight = Mathf.Max(0.02f, activeStepHeight);

            }
        }

        if (lerp < 1)
        {
            // Ease in/out removes robotic constant-speed stepping.
            float easedLerp = Mathf.SmoothStep(0f, 1f, lerp);

            Vector3 tempPosition = Vector3.Lerp(oldPosition, newPosition, easedLerp);
            tempPosition.y += Mathf.Sin(easedLerp * Mathf.PI) * activeStepHeight;

            currentPosition = tempPosition;
            currentNormal = Vector3.Slerp(oldNormal, newNormal, easedLerp);
            float adaptiveLerpSpeed = speed + (smoothedBodyHorizontalSpeed * stepSpeedFromBodyVelocity);
            if (!isMovingForward)
            {
                adaptiveLerpSpeed *= Mathf.Clamp(strafeStepSpeedMultiplier, 0.1f, 1f);
            }

            adaptiveLerpSpeed = Mathf.Min(adaptiveLerpSpeed, Mathf.Max(0.1f, maxAdaptiveStepSpeed));
            lerp += Time.deltaTime * Mathf.Max(0.1f, adaptiveLerpSpeed);
        }
        else
        {
            oldPosition = newPosition;
            oldNormal = newNormal;
        }
    }

    private void OnDrawGizmos()
    {

        Gizmos.color = Color.red;
        Gizmos.DrawSphere(newPosition, 0.1f);
    }



    public bool IsMoving()
    {
        return lerp < 1;
    }



}
