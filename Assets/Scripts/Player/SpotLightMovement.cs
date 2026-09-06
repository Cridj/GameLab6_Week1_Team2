using UnityEngine;

[DisallowMultipleComponent]
public sealed class SpotLightMovement : MonoBehaviour
{
    [SerializeField] private Transform leftSpotLight;
    [SerializeField] private Transform rightSpotLight;
    [SerializeField] private Transform centerSpotLight;
    [SerializeField] private AnimationCurve approachCurve =
        AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    private Vector3 leftStartPosition;
    private Vector3 rightStartPosition;
    private bool hasCapturedStartPositions;

    public bool IsConfigured => leftSpotLight != null &&
                                rightSpotLight != null &&
                                centerSpotLight != null;

    private void Awake()
    {
        CaptureStartPositions();
    }

    public void UpdatePositions(
        double songTime,
        double leftHitTime,
        double rightHitTime,
        double approachDuration)
    {
        if (!IsConfigured || approachDuration <= 0d)
        {
            return;
        }

        if (!hasCapturedStartPositions)
        {
            CaptureStartPositions();
        }

        SetPosition(leftSpotLight, leftStartPosition, songTime, leftHitTime, approachDuration);
        SetPosition(rightSpotLight, rightStartPosition, songTime, rightHitTime, approachDuration);
    }

    public void ResetToStart()
    {
        if (!IsConfigured)
        {
            return;
        }

        if (!hasCapturedStartPositions)
        {
            CaptureStartPositions();
        }

        leftSpotLight.position = leftStartPosition;
        rightSpotLight.position = rightStartPosition;
    }

    private void SetPosition(
        Transform movingSpotLight,
        Vector3 startPosition,
        double songTime,
        double hitTime,
        double approachDuration)
    {
        double startTime = hitTime - approachDuration;
        float progress = Mathf.Clamp01((float)((songTime - startTime) / approachDuration));
        float curvedProgress = approachCurve.Evaluate(progress);

        movingSpotLight.position = Vector3.LerpUnclamped(
            startPosition,
            centerSpotLight.position,
            curvedProgress);
    }

    private void CaptureStartPositions()
    {
        if (!IsConfigured)
        {
            return;
        }

        leftStartPosition = leftSpotLight.position;
        rightStartPosition = rightSpotLight.position;
        hasCapturedStartPositions = true;
    }

    private void OnValidate()
    {
        if (!Application.isPlaying)
        {
            hasCapturedStartPositions = false;
        }
    }
}
