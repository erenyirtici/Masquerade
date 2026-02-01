using UnityEngine;
using System.Collections;

public class MathCamera : MonoBehaviour
{
    [Header("Bileþenler")]
    public Light spotLight;
    public Transform eyePosition;

    [Header("En Ýyi Ayarlar")]
    public float rotationSpeed = 1.5f; 
    public float rotationAngle = 60f; 
    public float catchDelay = 0.5f;    

    [Header("Renkler")]
    public Color safeColor = Color.green;
    public Color alertColor = Color.red;

    [Header("Oyun Mantýðý")]
    public Transform respawnPoint;

    private Transform _playerTransform;
    private Quaternion _startRotation;
    private bool _isAlarmActive = false;

    void Start()
    {
  
        _startRotation = transform.localRotation;

        if (spotLight) spotLight.color = safeColor;


        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null) _playerTransform = playerObj.transform;
    }

    void Update()
    {
        if (_isAlarmActive) return;


        float angle = Mathf.Sin(Time.time * rotationSpeed) * rotationAngle;
        transform.localRotation = _startRotation * Quaternion.Euler(0, angle, 0);

     
        if (_playerTransform != null)
        {
            DetectPlayerMath();
        }
    }

    void DetectPlayerMath()
    {
    
        float distanceToPlayer = Vector3.Distance(eyePosition.position, _playerTransform.position);
        if (distanceToPlayer > spotLight.range) return;

     
        Vector3 dirToPlayer = (_playerTransform.position - eyePosition.position).normalized;
        float angleToPlayer = Vector3.Angle(eyePosition.forward, dirToPlayer);

      
        if (angleToPlayer < spotLight.spotAngle / 2f)
        {
           
            RaycastHit hit;
           
            if (Physics.Raycast(eyePosition.position, dirToPlayer, out hit, spotLight.range))
            {
                if (hit.collider.CompareTag("Player"))
                {
                    CheckMaskAndReact(hit.collider.gameObject);
                }
            }
        }
    }

    void CheckMaskAndReact(GameObject player)
    {
        var maskScript = player.GetComponent<PlayerMaskController>();
        if (maskScript == null) return;

        bool caught = false;
        switch (maskScript.currentMask)
        {
            case PlayerMaskController.MaskType.Thief: caught = true; break;
            case PlayerMaskController.MaskType.Guard: caught = false; break;
            case PlayerMaskController.MaskType.Statue:
                if (player.GetComponent<CharacterController>().velocity.magnitude > 0.1f) caught = true;
                break;
        }

        if (caught) StartCoroutine(CatchSequence(player));
    }

    IEnumerator CatchSequence(GameObject player)
    {
        _isAlarmActive = true;
        if (spotLight) spotLight.color = alertColor;
        Debug.Log("?? CATCHED!");

        yield return new WaitForSeconds(catchDelay);

        TeleportPlayer(player);

        if (spotLight) spotLight.color = safeColor;
        _isAlarmActive = false;
    }

    void TeleportPlayer(GameObject player)
    {
        var controller = player.GetComponent<CharacterController>();
        if (controller) controller.enabled = false;
        if (respawnPoint != null)
        {
            player.transform.position = respawnPoint.position;
            player.transform.rotation = respawnPoint.rotation;
        }
        if (controller) controller.enabled = true;
    }


    private void OnDrawGizmos()
    {
        if (eyePosition == null || spotLight == null) return;

        Gizmos.color = _isAlarmActive ? Color.red : Color.yellow;

    
        Gizmos.DrawRay(eyePosition.position, eyePosition.forward * spotLight.range);

 
        float halfAngle = spotLight.spotAngle / 2f;
        Quaternion leftRayRotation = Quaternion.AngleAxis(-halfAngle, eyePosition.up);
        Quaternion rightRayRotation = Quaternion.AngleAxis(halfAngle, eyePosition.up);
        Quaternion upRayRotation = Quaternion.AngleAxis(-halfAngle, eyePosition.right);
        Quaternion downRayRotation = Quaternion.AngleAxis(halfAngle, eyePosition.right);

        Gizmos.DrawRay(eyePosition.position, (leftRayRotation * eyePosition.forward) * spotLight.range);
        Gizmos.DrawRay(eyePosition.position, (rightRayRotation * eyePosition.forward) * spotLight.range);
        Gizmos.DrawRay(eyePosition.position, (upRayRotation * eyePosition.forward) * spotLight.range);
        Gizmos.DrawRay(eyePosition.position, (downRayRotation * eyePosition.forward) * spotLight.range);
    }
}