using UnityEngine;
using System.Collections.Generic;

public class GridMotion : MonoBehaviour
{
    public enum MotionType { None, Rotate, Patrol, IndividualWave }

    [Header("Hareket Tipi Seç")]
    public MotionType motionType;

    [Header("Rotate")]
    public Vector3 rotationAxis = new Vector3(0, 1, 0); 
    public float rotateSpeed = 50f;

    [Header("Patrol Ayarlarý")]
    public Vector3 patrolDirection = new Vector3(1, 0, 0); 
    public float patrolDistance = 3f;
    public float patrolSpeed = 2f;

    [Header("Wave")]
    public float waveSpeed = 3f;
    public float waveHeight = 1f;
    public float waveOffset = 0.5f; 


    private Vector3 _startPos;
    private List<Transform> _children = new List<Transform>();
    private List<Vector3> _childrenStartPos = new List<Vector3>();

    void Start()
    {
        _startPos = transform.position;


        if (motionType == MotionType.IndividualWave)
        {
            foreach (Transform child in transform)
            {
               
                _children.Add(child);
                _childrenStartPos.Add(child.localPosition);
            }
        }
    }

    void Update()
    {
        switch (motionType)
        {
            case MotionType.Rotate:
                HandleRotation();
                break;
            case MotionType.Patrol:
                HandlePatrol();
                break;
            case MotionType.IndividualWave:
                HandleWave();
                break;
        }
    }

  
    void HandleRotation()
    {
        transform.Rotate(rotationAxis * rotateSpeed * Time.deltaTime);
    }


    void HandlePatrol()
    {
      
        float move = Mathf.PingPong(Time.time * patrolSpeed, patrolDistance);

      
        float smoothMove = Mathf.Sin(Time.time * patrolSpeed) * patrolDistance;

        transform.position = _startPos + (patrolDirection * smoothMove);
    }

    
    void HandleWave()
    {
        for (int i = 0; i < _children.Count; i++)
        {
            Transform child = _children[i];
            Vector3 originalPos = _childrenStartPos[i];
            float wave = Mathf.Sin(Time.time * waveSpeed + (i * waveOffset)) * waveHeight;
            child.localPosition = originalPos + (Vector3.right * wave);
        }
    }
}