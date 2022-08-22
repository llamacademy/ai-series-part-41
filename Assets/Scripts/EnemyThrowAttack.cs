using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class EnemyThrowAttack : MonoBehaviour
{
    public Transform Target;
    [SerializeField]
    private Enemy Enemy;
    [SerializeField]
    private Rigidbody AttackProjectile;
    [Range(0, 1)]
    [Tooltip("Using a values closer to 0 will make the agent throw with the lower force"
        + "down to the least possible force (highest angle) to reach the target.\n"
        + "Using a value of 1 the agent will always throw with the MaxThrowForce below.")]
    public float ForceRatio = 0;
    [SerializeField]
    [Tooltip("If the required force to throw the attack is greater than this value, "
        + "the agent will move closer until they come within range.")]
    private float MaxThrowForce = 25;

    [SerializeField]
    private LayerMask SightLayers;
    [SerializeField]
    private float AttackDelay = 5f;

    [SerializeField]
    private bool UseMovementPrediction;
    public PredictionMode MovementPredictionMode;
    [Range(0.01f, 5f)]
    public float HistoricalTime = 1f;
    [Range(1, 100)]
    public int HistoricalResolution = 10;
    private Queue<Vector3> HistoricalPositions;

    private float HistoricalPositionInterval;
    private float LastHistoryRecordedTime;

    private CharacterController PlayerCharacterController;
    private float SpherecastRadius = 0.5f;
    private float LastAttackTime;

    private void Start()
    {
        AttackProjectile.useGravity = false;
        AttackProjectile.isKinematic = true;
        SpherecastRadius = AttackProjectile.GetComponent<SphereCollider>().radius;
        LastAttackTime = Random.Range(0, 5);

        int capacity = Mathf.CeilToInt(HistoricalResolution * HistoricalTime);
        HistoricalPositions = new Queue<Vector3>(capacity);
        for (int i = 0; i < capacity; i++)
        {
            HistoricalPositions.Enqueue(Target.position);
        }
        HistoricalPositionInterval = HistoricalTime / HistoricalResolution;
    }

    private void Update()
    {
        if (Time.time > LastAttackTime + AttackDelay
            && Physics.SphereCast(
                transform.position,
                SpherecastRadius,
                (Target.transform.position + Vector3.up - transform.position).normalized,
                out RaycastHit hit,
                float.MaxValue,
                SightLayers)
            && hit.transform == Target)
        {
            LastAttackTime = Time.time;
            AttackProjectile.transform.SetParent(transform, true);
            AttackProjectile.transform.localPosition = new Vector3(0, 0, 1f);
            AttackProjectile.useGravity = false;
            AttackProjectile.velocity = Vector3.zero;
            StartCoroutine(Attack());
        }

        if (LastHistoryRecordedTime + HistoricalPositionInterval < Time.time)
        {
            LastHistoryRecordedTime = Time.time;
            HistoricalPositions.Dequeue();
            HistoricalPositions.Enqueue(Target.position);
        }
    }

    public IEnumerator Attack()
    {
        Enemy.Movement.StopMoving();
        transform.LookAt(Target);
        AttackProjectile.gameObject.SetActive(true);
        AttackProjectile.transform.SetParent(null, true);
        yield return null;

        if (PlayerCharacterController == null)
        {
            PlayerCharacterController = Target.GetComponent<CharacterController>();
        }

        ThrowData throwData = CalculateThrowData(
            Target.position + PlayerCharacterController.center,
            AttackProjectile.position
        );

        if (UseMovementPrediction)
        {
            throwData = GetPredictedPositionThrowData(throwData);
        }

        DoThrow(throwData);

        yield return null;
        // resume movement the frame after the attack is launched
        Enemy.Movement.GoToRandomPoint();
    }

    private ThrowData GetPredictedPositionThrowData(ThrowData DirectThrowData)
    {
        Vector3 throwVelocity = DirectThrowData.ThrowVelocity;
        throwVelocity.y = 0;
        float time = DirectThrowData.DeltaXZ / throwVelocity.magnitude;
        Vector3 playerMovement;

        if (MovementPredictionMode == PredictionMode.CurrentVelocity)
        {
            playerMovement = PlayerCharacterController.velocity * time;
        }
        else
        {
            Vector3[] positions = HistoricalPositions.ToArray();
            Vector3 averageVelocity = Vector3.zero;
            for (int i = 1; i < positions.Length; i++)
            {
                averageVelocity += (positions[i] - positions[i - 1]) / HistoricalPositionInterval;
            }
            averageVelocity /= HistoricalTime * HistoricalResolution;
            playerMovement = averageVelocity;

        }

        Vector3 newTargetPosition = new Vector3(
            Target.position.x + PlayerCharacterController.center.x + playerMovement.x,
            Target.position.y + PlayerCharacterController.center.y + playerMovement.y,
            Target.position.z + PlayerCharacterController.center.x + playerMovement.z
        );

        // Option Calculate again the trajectory based on target position
        ThrowData predictiveThrowData = CalculateThrowData(
            newTargetPosition, 
            AttackProjectile.position
        );

        predictiveThrowData.ThrowVelocity = Vector3.ClampMagnitude(
            predictiveThrowData.ThrowVelocity, 
            MaxThrowForce
        );

        return predictiveThrowData;
    }

    private void DoThrow(ThrowData ThrowData)
    {
        AttackProjectile.useGravity = true;
        AttackProjectile.isKinematic = false;
        AttackProjectile.velocity = ThrowData.ThrowVelocity;
    }

    private ThrowData CalculateThrowData(Vector3 TargetPosition, Vector3 StartPosition)
    {
        // v = initial velocity, assume max speed for now
        // x = distance to travel on X/Z plane only
        // y = difference in altitudes from thrown point to target hit point
        // g = gravity

        Vector3 displacement = new Vector3(
            TargetPosition.x,
            StartPosition.y,
            TargetPosition.z
        ) - StartPosition;
        float deltaY = TargetPosition.y - StartPosition.y;
        float deltaXZ = displacement.magnitude;

        // find lowest initial launch velocity with other magic formula from https://en.wikipedia.org/wiki/Projectile_motion
        // v^2 / g = y + sqrt(y^2 + x^2)
        // meaning.... v = sqrt(g * (y+ sqrt(y^2 + x^2)))
        float gravity = Mathf.Abs(Physics.gravity.y);
        float throwStrength = Mathf.Clamp(
            Mathf.Sqrt(
                gravity
                * (deltaY + Mathf.Sqrt(Mathf.Pow(deltaY, 2)
                + Mathf.Pow(deltaXZ, 2)))),
            0.01f,
            MaxThrowForce
        );
        throwStrength = Mathf.Lerp(throwStrength, MaxThrowForce, ForceRatio);

        float angle;
        if (ForceRatio == 0)
        {
            // optimal angle is chosen with a relatively simple formula
            angle = Mathf.PI / 2f - (0.5f * (Mathf.PI / 2 - (deltaY / deltaXZ)));
        }
        else
        {
            // when we know the initial velocity, we have to calculate it with this formula
            // Angle to throw = arctan((v^2 +- sqrt(v^4 - g * (g * x^2 + 2 * y * v^2)) / g*x)
            angle = Mathf.Atan(
                (Mathf.Pow(throwStrength, 2) - Mathf.Sqrt(
                    Mathf.Pow(throwStrength, 4) - gravity
                    * (gravity * Mathf.Pow(deltaXZ, 2)
                    + 2 * deltaY * Mathf.Pow(throwStrength, 2)))
                ) / (gravity * deltaXZ)
            );
        }

        if (float.IsNaN(angle))
        {
            // you will need to handle this case when there
            // is no feasible angle to throw the object and reach the target.
            return new ThrowData(); 
        }

        Vector3 initialVelocity =
            Mathf.Cos(angle) * throwStrength * displacement.normalized
            + Mathf.Sin(angle) * throwStrength * Vector3.up;

        return new ThrowData
        {
            ThrowVelocity = initialVelocity,
            Angle = angle,
            DeltaXZ = deltaXZ,
            DeltaY = deltaY
        };
    }

    private struct ThrowData
    {
        public Vector3 ThrowVelocity;
        public float Angle;
        public float DeltaXZ;
        public float DeltaY;
    }

    public enum PredictionMode
    {
        CurrentVelocity,
        AverageVelocity
    }
}
