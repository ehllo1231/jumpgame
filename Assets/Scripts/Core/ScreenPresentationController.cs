using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Camera))]
public sealed class ScreenPresentationController : MonoBehaviour
{
    private const int ReferencePortraitWidth = 1080;
    private const int ReferencePortraitHeight = 1920;
    private const int ReferenceLandscapeWidth = 1920;
    private const int ReferenceLandscapeHeight = 1080;

    [SerializeField] private Vector2 targetAspect = new Vector2(16f, 9f);
    [SerializeField] private bool enforceTargetOrientation = true;
    [SerializeField] private bool letterboxToTargetAspect = true;

    private Camera targetCamera;
    private int lastScreenWidth = -1;
    private int lastScreenHeight = -1;
    private Vector2 lastTargetAspect;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void ApplyDefaultRuntimeSettings()
    {
        ApplyRuntimeOrientation(new Vector2(16f, 9f));
    }

    public void Configure(Vector2 aspect, bool shouldEnforceTargetOrientation = true, bool shouldLetterboxToTargetAspect = true)
    {
        targetAspect = SanitizeAspect(aspect);
        enforceTargetOrientation = shouldEnforceTargetOrientation;
        letterboxToTargetAspect = shouldLetterboxToTargetAspect;
        CacheCamera();
        ApplyPresentation(true);
    }

    private void Awake()
    {
        CacheCamera();
        ApplyPresentation(true);
    }

    private void OnEnable()
    {
        CacheCamera();
        ApplyPresentation(true);
    }

    private void Update()
    {
        ApplyPresentation(false);
    }

    private void CacheCamera()
    {
        if (targetCamera == null)
        {
            targetCamera = GetComponent<Camera>();
        }
    }

    private void ApplyPresentation(bool force)
    {
        var currentAspect = SanitizeAspect(targetAspect);
        if (enforceTargetOrientation && Application.isPlaying)
        {
            ApplyRuntimeOrientation(currentAspect);
        }

        if (targetCamera == null)
        {
            return;
        }

        if (!force && Screen.width == lastScreenWidth && Screen.height == lastScreenHeight && currentAspect == lastTargetAspect)
        {
            return;
        }

        lastScreenWidth = Screen.width;
        lastScreenHeight = Screen.height;
        lastTargetAspect = currentAspect;
        ApplyCameraAspect(currentAspect);
    }

    private void ApplyCameraAspect(Vector2 aspect)
    {
        var target = aspect.x / aspect.y;
        targetCamera.aspect = target;

        if (!letterboxToTargetAspect || Screen.width <= 0 || Screen.height <= 0)
        {
            targetCamera.rect = new Rect(0f, 0f, 1f, 1f);
            return;
        }

        var screenAspect = (float)Screen.width / Screen.height;
        if (screenAspect > target)
        {
            var normalizedWidth = target / screenAspect;
            targetCamera.rect = new Rect((1f - normalizedWidth) * 0.5f, 0f, normalizedWidth, 1f);
            return;
        }

        var normalizedHeight = screenAspect / target;
        targetCamera.rect = new Rect(0f, (1f - normalizedHeight) * 0.5f, 1f, normalizedHeight);
    }

    private static Vector2 SanitizeAspect(Vector2 aspect)
    {
        return new Vector2(Mathf.Max(1f, aspect.x), Mathf.Max(1f, aspect.y));
    }

    private static void ApplyRuntimeOrientation(Vector2 aspect)
    {
        var isLandscape = aspect.x >= aspect.y;
        Screen.autorotateToPortrait = !isLandscape;
        Screen.autorotateToPortraitUpsideDown = false;
        Screen.autorotateToLandscapeLeft = isLandscape;
        Screen.autorotateToLandscapeRight = isLandscape;
        Screen.orientation = isLandscape ? ScreenOrientation.LandscapeLeft : ScreenOrientation.Portrait;

#if UNITY_STANDALONE && !UNITY_EDITOR
        if (isLandscape && Screen.height > Screen.width)
        {
            Screen.SetResolution(ReferenceLandscapeWidth, ReferenceLandscapeHeight, false);
            return;
        }

        if (!isLandscape && Screen.width > Screen.height)
        {
            Screen.SetResolution(ReferencePortraitWidth, ReferencePortraitHeight, false);
        }
#endif
    }
}
