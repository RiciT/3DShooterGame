using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CrouchCheck : MonoBehaviour
{
    UnityStandardAssets.Characters.FirstPerson.FirstPersonController firstPersonController;
    public LayerMask layerMask;

    private void Start()
    {
        firstPersonController = this.gameObject.GetComponentInParent<UnityStandardAssets.Characters.FirstPerson.FirstPersonController>();
        //Debug.Log(this.gameObject.GetComponentInParent<UnityStandardAssets.Characters.FirstPerson.FirstPersonController>().gameObject.name);
    }

    private void OnTriggerStay(Collider other)
    {
        if (Mathf.Pow(2, other.gameObject.layer) == layerMask.value)
        {
            firstPersonController.SetCrouch(true);
        }
    }
    private void OnTriggerExit(Collider other)
    {
        if (Mathf.Pow(2, other.gameObject.layer) == layerMask.value)
        {
            firstPersonController.SetCrouch(false);
        }
    }
}
