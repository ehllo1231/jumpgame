using UnityEngine;

[DisallowMultipleComponent]
public sealed class CameraFollow : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private Vector3 offset = new Vector3(0f, 1.5f, -10f);
    [SerializeField] private float smoothTime = 0.15f;
    [SerializeField] private bool followX = true;
    [SerializeField] private bool followDownward = true;

    private Vector3 velocity;
    private float highestDesiredY;

    private void Awake()
    {
        if (target == null)
        {
            try
            {
                var player = GameObject.FindGameObjectWithTag("Player");
                if (player != null)
                {
                    target = player.transform;
                }
            }
            catch (UnityException)
            {
                target = null;
            }
        }

        highestDesiredY = transform.position.y;
    }

    private void LateUpdate()
    {
        if (target == null)
        {
            return;
        }

        var desiredPosition = target.position + offset;

        if (!followX)
        {
            desiredPosition.x = transform.position.x;
        }

        if (!followDownward)
        {
            highestDesiredY = Mathf.Max(highestDesiredY, desiredPosition.y);
            desiredPosition.y = highestDesiredY;
        }

        transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref velocity, smoothTime);
    }

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;

        if (target != null)
        {
            highestDesiredY = Mathf.Max(highestDesiredY, target.position.y + offset.y);
        }
    }

    public void SetFollowDownward(bool shouldFollowDownward)
    {
        followDownward = shouldFollowDownward;
    }
}
