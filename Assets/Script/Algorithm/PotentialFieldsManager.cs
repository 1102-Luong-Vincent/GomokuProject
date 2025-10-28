using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.Collections;


public class PotentialFieldsManager : MonoBehaviour
{
    public static PotentialFieldsManager Instance;

    private bool isMovementTimedOut = false;

    private List<GameObject> currentItems = new List<GameObject>();
    [SerializeField] Transform Decorate;
    private List<GameObject> DecorateObjects = new List<GameObject>();

    [Header("Potential Field Settings")]
    public float attractionStrength = 10f;
    public float repulsionStrength = 9f;
    private float stopDistance = 0.0001f;

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
        for (int i = 0; i < Decorate.childCount; i++)
        {
            GameObject childObj = Decorate.GetChild(i).gameObject;
            DecorateObjects.Add(childObj);
        }
    }


    public void Init()
    {
        ClearCurrentItems();
    }

    public void ClearCurrentItems()
    {
        foreach (GameObject item in currentItems)
        {
            if (item != null)
                Destroy(item);
        }
        currentItems.Clear();
    }

    public void AddCurrentItems(GameObject item)
    {
        if (item != null && !currentItems.Contains(item))
            currentItems.Add(item);
    }

    public void RemoveCurrentItem(GameObject item)
    {
        if (item != null && !currentItems.Contains(item))
            currentItems.Remove(item);
    }


    public IEnumerator StartIndependentPFMovement(List<GameObject> moveObjects, List<List<Vector3>> waypointsList)
    {
        if (moveObjects == null || waypointsList == null ||
            moveObjects.Count == 0 || waypointsList.Count == 0 ||
            moveObjects.Count != waypointsList.Count)
            yield break;

        isMovementTimedOut = false;

        Vector3[] finalDestinations = new Vector3[moveObjects.Count];
        for (int i = 0; i < moveObjects.Count; i++)
        {
            if (waypointsList[i] != null && waypointsList[i].Count > 0)
            {
                finalDestinations[i] = waypointsList[i][waypointsList[i].Count - 1];
            }
        }

        List<Coroutine> movementCoroutines = new List<Coroutine>();

        for (int i = 0; i < moveObjects.Count; i++)
        {
            if (moveObjects[i] != null && waypointsList[i] != null && waypointsList[i].Count > 0)
            {
                Coroutine moveCoroutine = StartCoroutine(
                    IndependentPFMovement(moveObjects[i], waypointsList[i])
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
                if (moveObjects[i] != null && waypointsList[i] != null && waypointsList[i].Count > 0)
                {
                    moveObjects[i].transform.position = finalDestinations[i];
                }
            }
        }
    }

    private IEnumerator IndependentPFMovement(GameObject moveObject, List<Vector3> waypoints)
    {
        if (moveObject == null || waypoints == null || waypoints.Count == 0)
            yield break;

        float moveRadius = GetObjectRadius(moveObject);
        Vector3 finalDestination = waypoints[waypoints.Count - 1];

        float totalElapsedTime = 0f;

        foreach (Vector3 targetPoint in waypoints)
        {
            if (moveObject == null)
                yield break;

            float waypointElapsedTime = 0f;
            bool reached = false;

            while (!reached && totalElapsedTime < waypointTimeout)
            {
                if (moveObject == null)
                    yield break;

                Vector3 pos = moveObject.transform.position;
                float distToTarget = Vector3.Distance(pos, targetPoint);

                if (distToTarget > stopDistance)
                {
                    Vector3 velocity = CalculatePotentialFieldVelocity(
                        moveObject, pos, targetPoint, moveRadius
                    );
                    moveObject.transform.position += velocity;

                    waypointElapsedTime += Time.deltaTime;
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
                {
                    moveObject.transform.position = finalDestination;
                }

                yield break;
            }

            if (moveObject != null)
            {
                moveObject.transform.position = targetPoint;
            }
        }
    }
    public IEnumerator StartPFMovement(List<Vector3> waypoints, GameObject moveObject)
    {
        if (waypoints == null || waypoints.Count == 0 || moveObject == null)
            yield break;

        foreach (Vector3 targetPoint in waypoints)
        {
            yield return MoveToWaypoint(moveObject, targetPoint);
        }
    }

    private IEnumerator MoveToWaypoint(GameObject moveObject, Vector3 targetPoint)
    {
        if (moveObject == null)
            yield break;

        float elapsedTime = 0f;
        bool reached = false;

        float moveRadius = GetObjectRadius(moveObject);

        while (!reached && elapsedTime < waypointTimeout)
        {
            if (moveObject == null)
                yield break;

            Vector3 pos = moveObject.transform.position;
            float distToTarget = Vector3.Distance(pos, targetPoint);

            if (distToTarget > stopDistance)
            {
                Vector3 velocity = CalculatePotentialFieldVelocity(moveObject, pos, targetPoint, moveRadius);
                moveObject.transform.position += velocity;
                elapsedTime += Time.deltaTime;
            }
            else
            {
                reached = true;
            }

            yield return null;
        }

        if (moveObject != null)
        {
            moveObject.transform.position = targetPoint;

        }
    }





    private Vector3 CalculatePotentialFieldVelocity(GameObject moveObject, Vector3 currentPos,
                                                     Vector3 targetPos, float moveRadius)
    {
        Vector3 attractionForce = CalculateAttractionForce(currentPos, targetPos);
        Vector3 totalRepulsion = CalculateTotalRepulsionForce(moveObject, currentPos, moveRadius);
        Vector3 totalForce = attractionForce + totalRepulsion;
        float speed = GameUIControl.Instance.GetMoveSpeed();
        Vector3 velocity = totalForce.normalized * speed * Time.deltaTime;

        return velocity;
    }

    private Vector3 CalculateAttractionForce(Vector3 currentPos, Vector3 targetPos)
    {
        Vector3 direction = targetPos - currentPos;
        return direction.normalized * attractionStrength;
    }

    private Vector3 CalculateTotalRepulsionForce(GameObject moveObject, Vector3 currentPos, float moveRadius)
    {
        Vector3 totalRepulsion = Vector3.zero;

        foreach (GameObject obstacle in DecorateObjects)
        {
            if (obstacle == null || obstacle == moveObject)
                continue;

            Vector3 repulsionForce = CalculateSingleObstacleRepulsion(
                currentPos,
                obstacle,
                moveRadius
            );

            totalRepulsion += repulsionForce;
        }

        return totalRepulsion;
    }
    private Vector3 CalculateSingleObstacleRepulsion(Vector3 currentPos, GameObject obstacle, float moveRadius)
    {
        float obstacleRadius = GetObjectRadius(obstacle);

        Vector3 repulsionDir = currentPos - obstacle.transform.position;
        float distance = repulsionDir.magnitude;

        float safeDistance = moveRadius + obstacleRadius + 0.1f;

        float influenceRadius = safeDistance * 2.5f; 
        if (distance < influenceRadius && distance > 0.001f)
        {
            float influence = 1f - (distance / influenceRadius);
            influence = Mathf.Pow(influence, 2); 

            if (distance < safeDistance)
            {
                influence = Mathf.Lerp(influence, 1f, (safeDistance - distance) / safeDistance);
                influence = Mathf.Pow(influence, 1.5f); 
            }

            Vector3 repulsionForce = repulsionDir.normalized * (repulsionStrength * influence);
            Debug.DrawRay(currentPos, repulsionForce, Color.red);

            return repulsionForce;
        }

        return Vector3.zero;
    }

    private float GetObjectRadius(GameObject obj)
    {
        Collider collider = obj.GetComponent<Collider>();

        if (collider != null)
        {
            return collider.bounds.extents.magnitude;
        }

        return 0.5f; 
    }
    private void OnDrawGizmos()
    {
        if (!Application.isPlaying || DecorateObjects == null)
            return;

        Gizmos.color = Color.yellow;
        foreach (GameObject obstacle in DecorateObjects)
        {
            if (obstacle == null) continue;

            float radius = GetObjectRadius(obstacle);
            float influenceRadius = radius * 2f;

            Gizmos.DrawWireSphere(obstacle.transform.position, influenceRadius);
        }
    }
}