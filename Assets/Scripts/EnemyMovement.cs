using System.Collections;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class EnemyMovement : MonoBehaviour
{
    public NavMeshTriangulation Triangulation;
    private NavMeshAgent Agent;
    [SerializeField]
    [Range(0f, 3f)]
    private float WaitDelay = 1f;

    private void Awake()
    {
        Agent = GetComponent<NavMeshAgent>();
    }

    private void Start()
    {
        GoToRandomPoint();
    }

    public void GoToRandomPoint()
    {
        StartCoroutine(DoMoveToRandomPoint());
    }

    public void GoTowardsPlayer(Transform Player)
    {
        StartCoroutine(DoMoveToPlayer(Player));
    }

    public void StopMoving()
    {
        Agent.isStopped = true;
        StopAllCoroutines();
    }

    private IEnumerator DoMoveToRandomPoint()
    {
        Agent.enabled = true;
        Agent.isStopped = false;
        WaitForSeconds Wait = new WaitForSeconds(WaitDelay);
        while (true)
        {
            int index = Random.Range(1, Triangulation.vertices.Length - 1);
            Agent.SetDestination(Vector3.Lerp(
                Triangulation.vertices[index],
                Triangulation.vertices[index + (Random.value > 0.5f ? -1 : 1)],
                Random.value)
            );

            yield return null;
            yield return new WaitUntil(() => Agent.remainingDistance <=  Agent.stoppingDistance);
            yield return Wait;
        }
    }

    private IEnumerator DoMoveToPlayer(Transform Player)
    {
        Agent.enabled = true;
        Agent.isStopped = false;
        while (true)
        {
            Agent.SetDestination(Player.position);

            yield return new WaitUntil(() => Mathf.Approximately((Agent.destination - Player.position).sqrMagnitude, 0));
        }
    }
}
