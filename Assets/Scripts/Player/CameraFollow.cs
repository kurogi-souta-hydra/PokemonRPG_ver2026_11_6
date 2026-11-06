using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("カメラ追従")]
    [SerializeField] Transform target;


    // ==========================================
    // カメラ追従
    // ==========================================

    private void LateUpdate()
	{
	    if (target == null)
	        return;

	    Vector3 pos = transform.position;

	    pos.x = target.position.x;
	    pos.y = target.position.y;
	    pos.z = -10f;

	    transform.position = pos;
	}
}