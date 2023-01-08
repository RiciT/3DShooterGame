using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class Enemy : Target
{
    public GameObject player;

    public float speed = 4f;
    public LayerMask layerMask;
    public float lookRadius = 50f;
    public float shootRadius = 25f;
    [Range(0.0f, 1.0f)]
    public float AttackPropability = 0.7f;
    [Range(0.0f, 1.0f)]
    public float HitAccuracy = 0.7f;
    public float damage = 10f;
    public float AOV = 90f;
    public float rotateSpeed = 10f;
    public float timeBetweenChecks = 1f;
    public float timeBetweenShoots = 1f;

    NavMeshAgent agent;
    Vector3 lastPlayerPos;
    private bool isShooting = false;
    private bool doingSomething = false;
    private bool isFacingTarget = false;

    void Start()
    {
        agent = gameObject.GetComponent<NavMeshAgent>();
    }

    void Update()
    {
        if (!doingSomething)
            StartCoroutine(TakeAction());

        FaceTarget();
    }

    IEnumerator TakeAction()
    {
        RaycastHit hit;
        Vector3 raycastDir = player.transform.position - gameObject.transform.position;
        Physics.Raycast(transform.position, raycastDir, out hit, lookRadius);
        float angleToTarget = Vector2.Angle(transform.forward, raycastDir);

        if (Mathf.Pow(2, hit.collider.gameObject.layer) == layerMask.value && angleToTarget < AOV / 2) 
        {
            doingSomething = true;
            float distance = Vector3.Distance(player.transform.position, transform.position);
            GetComponent<NavMeshAgent>().speed = speed;
            bool skipped = false;

            if (distance <= shootRadius)
            {
                if (UnityEngine.Random.Range(0.0f, 1.0f) < AttackPropability && !isShooting)
                {
                    StartCoroutine(Shoot());
                    agent.SetDestination(transform.position);
                    yield return new WaitForSeconds(timeBetweenChecks / 4);
                    doingSomething = false;
                    yield break;
                }
                else
                    skipped = true;
            }
            else
                skipped = true;
            if (distance <= lookRadius && skipped)
            {
                isFacingTarget = true;
                agent.SetDestination(player.transform.position);
            }
            else if (distance > lookRadius)
            {
                isFacingTarget = false;
            }
            yield return new WaitForSeconds(timeBetweenChecks);
            doingSomething = false;
        }
    }

    IEnumerator Shoot()
    {
        isShooting = true;
        player.GetComponent<UnityStandardAssets.Characters.FirstPerson.FirstPersonController>().TakeDamage(Mathf.Round(UnityEngine.Random.Range(HitAccuracy, 1.0f) * damage));
        yield return new WaitForSeconds(timeBetweenShoots);
        isShooting = false;
    }

    private void FaceTarget()
    {
        if (isFacingTarget)
        {
            Vector3 direction = (player.transform.position - transform.position).normalized;
            Quaternion lookRotation = Quaternion.LookRotation(new Vector3(direction.x, 0, direction.z));
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * rotateSpeed);
        }
    }
}
 