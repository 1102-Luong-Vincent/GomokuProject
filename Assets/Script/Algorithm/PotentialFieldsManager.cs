using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PotentialFieldsManager : MonoBehaviour
{
    public static PotentialFieldsManager Instance;

    private bool isMovementTimedOut = false;

    private List<GameObject> currentItems = new List<GameObject>();
    [SerializeField] Transform Decorate;
    private List<GameObject> DecorateObjects = new List<GameObject>();

    [Header("Potential Field Settings")]
    [Tooltip("How strongly pieces are attracted to the next waypoint.")]
    public float attractionStrength = 10f;

    [Tooltip("How strong the repulsion from obstacles/other objects is.")]
    public float repulsionStrength = 6f;

    [Tooltip("Multiplier applied to object sizes to compute an influence radius.")]
    public float repulsionRangeMultiplier = 2.0f;

    [Tooltip("Stop threshold for considering a waypoint reached (meters).")]
    public float stopDistance = 0.01f;

    [Header("Timeout Settings")]
    public float waypointTimeout = 5f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        DecorateObjects.Clear();
        if (Decorate != null)
        {
            for (int i = 0; i < Decorate.childCount; i++)
            {
                GameObject childObj = Decorate.GetChild(i).gameObject;
                if (childObj != null)
                    DecorateObjects.Add(childObj);
            }
        }
    }

    public void Init()
    {
        ClearCurrentItems();
    }

    public void ClearCurrentItems()
    {
        // don't destroy passed-in prefabs from DeskControl; this is only for runtime temp items
        currentItems.Clear();
    }

    public void AddCurrentItems(GameObject item)
    {
        if (item != null && !currentItems.Contains(item))
            currentItems.Add(item);
    }

    public void RemoveCurrentItem(GameObject item)
    {
        if (item != null && currentItems.Contains(item))
            currentItems.Remove(item);
    }

    public IEnumerator StartIndependentPFMovement(List<GameObject> moveObjects, List<List<Vector3>> waypointsList)
    {
        if (moveObjects == null || waypointsList == null ||
            moveObjects.Count == 0 || waypointsList.Count == 0 ||
            moveObjects.Count != waypointsList.Count)
            yield break;

        isMovementTimedOut = false;

        int n = moveObjects.Count;
        float[] pathLengths = new float[n];
        float maxLength = 0f;
        for (int i = 0; i < n; i++)
        {
            float len = 0f;
            var w = waypointsList[i];
            if (w != null && w.Count > 0)
            {
                Vector3 prev = moveObjects[i] != null ? moveObjects[i].transform.position : w[0];
                for (int j = 0; j < w.Count; j++)
                {
                    len += Vector3.Distance(prev, w[j]);
                    prev = w[j];
                }
            }
            pathLengths[i] = len;
            if (len > maxLength) maxLength = len;
        }

        if (maxLength <= 0f)
            yield break;

        float baseSpeed = Mathf.Max(0.0001f, GameUIControl.Instance.GetMoveSpeed());
        float syncDuration = Mathf.Max(0.01f, maxLength / baseSpeed); // seconds to synchronize

        Vector3[] finalDestinations = new Vector3[n];
        for (int i = 0; i < n; i++)
        {
            if (waypointsList[i] != null && waypointsList[i].Count > 0)
                finalDestinations[i] = waypointsList[i][waypointsList[i].Count - 1];
            else
                finalDestinations[i] = moveObjects[i] != null ? moveObjects[i].transform.position : Vector3.zero;
        }

        List<Coroutine> movementCoroutines = new List<Coroutine>();
        for (int i = 0; i < n; i++)
        {
            if (moveObjects[i] != null && waypointsList[i] != null && waypointsList[i].Count > 0)
            {
                float pathLen = pathLengths[i];
                Coroutine moveCoroutine = StartCoroutine(
                    IndependentPFMovement(moveObjects[i], waypointsList[i], pathLen, syncDuration, finalDestinations[i])
                );
                movementCoroutines.Add(moveCoroutine);
            }
        }

        foreach (var coroutine in movementCoroutines)
        {
            yield return coroutine;
        }

        if (isMovementTimedOut)
        {
            for (int i = 0; i < moveObjects.Count; i++)
            {
                if (moveObjects[i] != null)
                    moveObjects[i].transform.position = finalDestinations[i];
            }
        }

        yield break;
    }

    private IEnumerator IndependentPFMovement(GameObject moveObject, List<Vector3> waypoints, float pathLength, float syncDuration, Vector3 finalDestination)
    {
        if (moveObject == null || waypoints == null || waypoints.Count == 0)
            yield break;

        float totalElapsedTime = 0f;
        float baseSpeedRatio = Mathf.Max(0.0001f, pathLength / Mathf.Max(0.0001f, syncDuration));

        foreach (Vector3 targetPoint in waypoints)
        {
            if (moveObject == null)
                yield break;

            bool reached = false;

            while (!reached && totalElapsedTime < waypointTimeout)
            {
                if (moveObject == null)
                    yield break;

                Vector3 pos = moveObject.transform.position;
                float distToTarget = Vector3.Distance(pos, targetPoint);

                if (distToTarget > stopDistance)
                {
                    float currentGlobalSpeed = Mathf.Max(0.0001f, GameUIControl.Instance.GetMoveSpeed());
                    float dynamicSpeed = baseSpeedRatio * currentGlobalSpeed;

                    // compute combined force direction (no delta-time applied here)
                    Vector3 totalForce = CalculatePotentialFieldForce(moveObject, pos, targetPoint);

                    // if total force is near zero, fallback to direct direction
                    Vector3 moveDir = (totalForce.sqrMagnitude > 0.00001f) ? totalForce.normalized : (targetPoint - pos).normalized;

                    // move with dynamic speed (units/sec) * deltaTime
                    Vector3 delta = moveDir * dynamicSpeed * Time.deltaTime;

                    // clamp overshoot
                    if (delta.magnitude > distToTarget)
                        delta = (targetPoint - pos);

                    moveObject.transform.position += delta;
                    totalElapsedTime += Time.deltaTime;
                }
                else
                {
                    reached = true;
                }

                yield return null;
            }

            if (totalElapsedTime >= waypointTimeout)
            {
                isMovementTimedOut = true;
                if (moveObject != null)
                    moveObject.transform.position = finalDestination;
                yield break;
            }

            if (moveObject != null)
                moveObject.transform.position = targetPoint;
        }
    }

    /// <summary>
    /// Computes the vector force (not time-scaled) from attraction + repulsion.
    /// </summary>
    private Vector3 CalculatePotentialFieldForce(GameObject moveObject, Vector3 currentPos, Vector3 targetPos)
    {
        Vector3 attractionForce = CalculateAttractionForce(currentPos, targetPos);
        Vector3 totalRepulsion = CalculateTotalRepulsionForce(moveObject, currentPos);

        // Cap repulsion so it doesn't completely overpower attraction in direction calculation
        float maxRepulsionMagnitude = attractionForce.magnitude * 1.5f;
        if (totalRepulsion.magnitude > maxRepulsionMagnitude)
            totalRepulsion = totalRepulsion.normalized * maxRepulsionMagnitude;

        Vector3 totalForce = attractionForce + totalRepulsion;
        if (totalForce.sqrMagnitude <= 0.00001f)
            return Vector3.zero;

        return totalForce;
    }

    private Vector3 CalculateAttractionForce(Vector3 currentPos, Vector3 targetPos)
    {
        Vector3 dir = (targetPos - currentPos);
        if (dir.sqrMagnitude <= 0.0000001f) return Vector3.zero;
        return dir.normalized * attractionStrength;
    }

    private Vector3 CalculateTotalRepulsionForce(GameObject moveObject, Vector3 currentPos)
    {
        Vector3 totalRepulsion = Vector3.zero;

        // combine repulsion from DecorateObjects and other currentItems
        foreach (GameObject obstacle in DecorateObjects)
        {
            if (obstacle == null || obstacle == moveObject)
                continue;

            Vector3 repulsionForce = CalculateBoundsBasedRepulsion(moveObject, currentPos, obstacle);
            totalRepulsion += repulsionForce;
        }

        // also repulse other moving items to prevent stacking
        foreach (GameObject other in currentItems)
        {
            if (other == null || other == moveObject) continue;
            Vector3 repulsionForce = CalculateBoundsBasedRepulsion(moveObject, currentPos, other);
            totalRepulsion += repulsionForce;
        }

        return totalRepulsion;
    }

    private Vector3 CalculateBoundsBasedRepulsion(GameObject moveObject, Vector3 currentPos, GameObject obstacle)
    {
        Collider moveCollider = moveObject.GetComponent<Collider>();
        Collider obstacleCollider = obstacle.GetComponent<Collider>();

        if (moveCollider == null || obstacleCollider == null)
            return Vector3.zero;

        Bounds moveBounds = moveCollider.bounds;
        Bounds obstacleBounds = obstacleCollider.bounds;

        Vector3 closestPointOnObstacle = obstacleBounds.ClosestPoint(currentPos);
        Vector3 repulsionDir = (currentPos - closestPointOnObstacle);
        float distance = repulsionDir.magnitude;

        if (distance < 0.0001f)
        {
            // fallback direction away from obstacle center
            repulsionDir = (currentPos - obstacleBounds.center);
            distance = repulsionDir.magnitude;
            if (distance < 0.0001f)
                return Vector3.up * repulsionStrength;
        }

        float moveHalfExtent = moveBounds.extents.magnitude;
        float obstacleHalfExtent = obstacleBounds.extents.magnitude;
        float safeDistance = moveHalfExtent + obstacleHalfExtent;

        float influenceRadius = safeDistance * Mathf.Max(1f, repulsionRangeMultiplier);

        if (distance < influenceRadius)
        {
            float influence = 1f - (distance / influenceRadius); // 1 close, 0 far
            influence = Mathf.Pow(influence, 2f); // smooth falloff

            if (distance < safeDistance)
            {
                // inside collision area -> strong repulsion
                influence = Mathf.Lerp(influence, 1f, (safeDistance - distance) / Mathf.Max(0.0001f, safeDistance));
                influence = Mathf.Pow(influence, 1.5f);
            }

            Vector3 repulsionForce = repulsionDir.normalized * (repulsionStrength * influence);
            // optional debug draw:
            Debug.DrawRay(currentPos, repulsionForce * 0.01f, Color.red, 0.02f);
            return repulsionForce;
        }

        return Vector3.zero;
    }

    private void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;

        if (DecorateObjects != null)
        {
            Gizmos.color = Color.yellow;
            foreach (GameObject obstacle in DecorateObjects)
            {
                if (obstacle == null) continue;

                Collider collider = obstacle.GetComponent<Collider>();
                if (collider != null)
                {
                    Bounds bounds = collider.bounds;
                    Gizmos.DrawWireCube(bounds.center, bounds.size);

                    float halfExtent = bounds.extents.magnitude;
                    float influenceRadius = halfExtent * repulsionRangeMultiplier;
                    Gizmos.color = Color.cyan;
                    Gizmos.DrawWireSphere(bounds.center, influenceRadius);
                    Gizmos.color = Color.yellow;
                }
            }
        }
    }
}
