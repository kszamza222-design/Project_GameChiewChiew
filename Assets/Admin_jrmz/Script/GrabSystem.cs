using UnityEngine;
using UnityEngine.InputSystem;

public class GrabSystem : MonoBehaviour
{
    [Header("Player Setup")]
    public int playerIndex = 0;

    [Header("Grab Settings")]
    public Transform handPoint;
    public float grabRadius = 0.6f;
    public LayerMask grabbableLayer;

    private PlayerInputActions inputActions;
    private FixedJoint grabJoint;
    private Rigidbody grabbedObject;

    void Awake()
    {
        inputActions = new PlayerInputActions();
    }

    void OnEnable()
    {
        if (playerIndex == 0)
        {
            inputActions.Player1.Enable();
            inputActions.Player1.Grab.performed += ctx => GrabObject();
            inputActions.Player1.Grab.canceled  += ctx => ReleaseObject();
        }
        else
        {
            inputActions.Player2.Enable();
            inputActions.Player2.Grab.performed += ctx => GrabObject();
            inputActions.Player2.Grab.canceled  += ctx => ReleaseObject();
        }
    }

    void OnDisable()
    {
        inputActions.Player1.Disable();
        inputActions.Player2.Disable();
    }

    void GrabObject()
    {
        if (grabJoint != null) return;

        Collider[] hits = Physics.OverlapSphere(
            handPoint.position, grabRadius, grabbableLayer
        );
        if (hits.Length == 0) return;

        Rigidbody targetRb = null;
        float minDist = Mathf.Infinity;

        foreach (Collider col in hits)
        {
            Rigidbody rb = col.attachedRigidbody;
            if (rb == null) continue;
            float dist = Vector3.Distance(handPoint.position, col.transform.position);
            if (dist < minDist) { minDist = dist; targetRb = rb; }
        }

        if (targetRb == null) return;

        grabJoint = handPoint.gameObject.AddComponent<FixedJoint>();
        grabJoint.connectedBody  = targetRb;
        grabJoint.breakForce     = 1500f;
        grabJoint.breakTorque    = 1500f;
        grabbedObject = targetRb;
        grabbedObject.linearDamping = 1f;
    }

    void ReleaseObject()
    {
        if (grabJoint == null) return;
        if (grabbedObject != null) grabbedObject.linearDamping = 0f;
        Destroy(grabJoint);
        grabJoint     = null;
        grabbedObject = null;
    }

    void OnDrawGizmosSelected()
    {
        if (!handPoint) return;
        Gizmos.color = playerIndex == 0 ? Color.cyan : Color.magenta;
        Gizmos.DrawWireSphere(handPoint.position, grabRadius);
    }
}