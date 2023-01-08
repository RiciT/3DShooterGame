using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using UnityStandardAssets.Characters.FirstPerson;

public class Telekinesis : MonoBehaviour
{
    public float speed = 20f;
    public float range = 75f;
    public FirstPersonController fpsController;

    Vector3 m_Input;

    // Update is called once per frame
    void Update()
    {
        if (Input.GetButton("Fire1"))
        {
            fpsController.SkipMove();
            Control();
        }
    }

    private void Control()
    {
        RaycastHit hit;
        Debug.DrawRay(transform.GetComponentInParent<Transform>().position, transform.GetComponentInParent<Transform>().forward * 100f, Color.green);
        if (Physics.Raycast(transform.GetComponentInParent<Transform>().position, transform.GetComponentInParent<Transform>().forward, out hit, range))
        {
            Debug.Log(hit.transform.name);
            var target = hit.transform.GetComponent<MoveableObject>();
            if (target != null)
            {
                float horizontal = Input.GetAxis("Horizontal");
                float vertical = Input.GetAxis("Vertical");
                float updown = Input.GetAxis("Up/Down");
                m_Input = new Vector3(horizontal, updown, vertical);

                target.transform.position = new Vector3(target.transform.localPosition.x + m_Input.x * Time.deltaTime * speed / target.weight,
                    target.transform.localPosition.y + m_Input.y * Time.deltaTime * speed / target.weight, 
                    target.transform.localPosition.z + m_Input.z * Time.deltaTime * speed / target.weight);
            }
        }
    }
}
