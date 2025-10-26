using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.Collections;


public class PotentialFieldsManager : MonoBehaviour
{
    public static PotentialFieldsManager Instance;

    private List<GameObject> currentItems = new List<GameObject>();
    private Coroutine pfCoroutine;

    [Header("Potential Field Settings")]
    public float attractionStrength = 5f; 
    public float repulsionStrength = 3f;  
    public float repulsionRange = 2f;
    private float stopDistance = 0.0001f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
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


    public IEnumerator StartPFMovement(List<Vector3> waypoints, GameObject moveObject)
    {
        if (waypoints == null || waypoints.Count == 0 || moveObject == null)
            yield break;

        foreach (Vector3 targetPoint in waypoints)
        {
            bool reached = false;

            while (!reached)
            {
                if (moveObject == null)
                    yield break;

                Vector3 pos = moveObject.transform.position;
                Vector3 dir = targetPoint - pos;
                float dist = dir.magnitude;
                if (dist > stopDistance) 
                {
                    Vector3 attractionForce = dir.normalized * attractionStrength;
                    Vector3 velocity = attractionForce.normalized *
                                       GameUIControl.Instance.GetMoveSpeed() * Time.deltaTime;
                    moveObject.transform.position += velocity;
                }
                else
                {
                    moveObject.transform.position = targetPoint;
                    reached = true;
                }

                yield return null; 
            }
        }
    }


    //public void StartPFMovement(List<Vector3> waypoints)
    //{
    //    if (waypoints == null || waypoints.Count == 0)
    //    {
    //        Debug.LogWarning("No waypoints provided for PF movement!");
    //        return;
    //    }

    //    if (pfCoroutine != null)
    //        StopCoroutine(pfCoroutine);

    //    pfCoroutine = StartCoroutine(PFMovementCoroutine(waypoints));
    //}

    public void StopPFMovement()
    {
        if (pfCoroutine != null)
        {
            StopCoroutine(pfCoroutine);
            pfCoroutine = null;
        }
    }

    private IEnumerator PFMovementCoroutine(List<Vector3> waypoints)
    {
        int currentWaypointIndex = 0;

        while (true)
        {
            if (currentItems.Count == 0)
                yield break;

            Vector3 target = waypoints[currentWaypointIndex];

            bool allReached = true;

            foreach (GameObject item in currentItems)
            {
                if (item == null) continue;

                Vector3 pos = item.transform.position;
                Vector3 totalForce = Vector3.zero;
                Vector3 dirToTarget = target - pos;
                float distToTarget = dirToTarget.magnitude;

                //if (distToTarget > stopDistance)
                //{
                //    Vector3 attractionForce = dirToTarget.normalized * attractionStrength;
                //    totalForce += attractionForce;
                //    allReached = false; 
                //}

                foreach (GameObject other in currentItems)
                {
                    if (other == null || other == item) continue;

                    Vector3 dir = pos - other.transform.position;
                    float dist = dir.magnitude;

                    if (dist < repulsionRange && dist > 0.01f)
                    {
                        Vector3 repulsionForce = dir.normalized * (repulsionStrength / dist);
                        totalForce += repulsionForce;
                    }
                }

                Vector3 velocity = totalForce.normalized * GameUIControl.Instance.GetMoveSpeed() * Time.deltaTime;
                item.transform.position += velocity;
            }

            if (allReached)
            {
                currentWaypointIndex++;
                if (currentWaypointIndex >= waypoints.Count)
                {
                    Debug.Log("All waypoints reached! PF movement finished.");
                    StopPFMovement();
                    yield break;
                }
            }

            yield return null;
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        foreach (var obj in currentItems)
        {
            if (obj != null)
                Gizmos.DrawWireSphere(obj.transform.position, repulsionRange);
        }
    }
}
