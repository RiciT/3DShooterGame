using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Gun : MonoBehaviour
{
    [Header("General")]
    public float damage = 10f;
    public float range = 100f;
    public float fireRate = 15f;
    public float impactForce = 30f;
    public bool autoFire = true;

    [Header("Scope")]
    public Vector3 aimDownSight;
    public Vector3 hipFire;
    public bool hasScope = false;
    public float aimSpeed = 5f;
    public float scopeAnimMinDist = .05f;
    public float scopedFOV = 15f;
    private float normalFOV = 60f;

    [Header("Ammo")]
    public int maxAmmo = 10;
    private int currentAmmo;
    public int allAmmo = 20;
    public float reloadTime = 1f;
    private bool isReloading = false;

    [Header("GameObjects")]
    public Camera FPSCam;
    public GameObject WeaponCamera;
    public GameObject scopeOvarlay;
    public ParticleSystem muzzleFlash;
    public GameObject impactEffect;
    public GameObject firstPersonController;

    private float nextTimeToFire = 0f;
    private WeaponSwitching weaponSwitching;
    private int inScope = 1;

    void Start()
    {
        currentAmmo = maxAmmo;
        weaponSwitching = firstPersonController.GetComponentInChildren<WeaponSwitching>();
        WeaponCamera.SetActive(false);
        WeaponCamera.SetActive(true);
    }

    void Update()
    {
        if (isReloading)
            return;
        if (Input.GetMouseButton(1) && transform.localPosition != aimDownSight)
        {
            OnScoped();
        }
        if (Input.GetMouseButtonUp(1))
        {
            UnScope();
        }
        if (allAmmo <= 0)
            return;
        if (currentAmmo <= 0)
        {
            StartCoroutine(Reload());
            return;
        }
        if (autoFire)
        {
            if (Input.GetButton("Fire1") && Time.time >= nextTimeToFire)
            {
                nextTimeToFire = Time.time + 1f / fireRate;
                Shoot();
            }
        }
        else
        {
            if (Input.GetButtonDown("Fire1") && Time.time >= nextTimeToFire)
            {
                nextTimeToFire = Time.time + 1f / fireRate;
                Shoot();
            }
        }
    }

    IEnumerator Reload()
    {
        isReloading = true;
        Debug.Log("Reloading...");
        UnScope();
        yield return new WaitForSeconds(reloadTime);
        if (maxAmmo <= allAmmo)
            currentAmmo = maxAmmo;
        else
            currentAmmo = allAmmo;
        isReloading = false;
        if (Input.GetMouseButton(1))
        {
            OnScoped();
        }
    }

    void OnScoped()
    {
        transform.localPosition = Vector3.Slerp(transform.localPosition, aimDownSight, aimSpeed * Time.deltaTime);
        if (inScope == 1)
        {
            firstPersonController.GetComponent<UnityStandardAssets.Characters.FirstPerson.FirstPersonController>().Scope();
            weaponSwitching.SetScopedValue(true);
        }
        if (hasScope && Math.Abs(transform.localPosition.x - aimDownSight.x) < scopeAnimMinDist
            && Math.Abs(transform.localPosition.y - aimDownSight.y) < scopeAnimMinDist
            && Math.Abs(transform.localPosition.z - aimDownSight.z) < scopeAnimMinDist)
        {
            //normalFOV = FPSCam.fieldOfView;
            FPSCam.fieldOfView = scopedFOV;
            WeaponCamera.SetActive(false);
            scopeOvarlay.SetActive(true);
        }
        inScope--;
    }

    void UnScope()
    {
        if (inScope != 1)
        {
            inScope = 1;
            firstPersonController.GetComponent<UnityStandardAssets.Characters.FirstPerson.FirstPersonController>().UnScope();
            weaponSwitching.SetScopedValue(false);
        }
        if (hasScope)
        {
            FPSCam.fieldOfView = normalFOV;
            WeaponCamera.SetActive(true);
            scopeOvarlay.SetActive(false);
        }
        transform.localPosition = hipFire;
    }

    private void Shoot()
    {
        muzzleFlash.Play();

        currentAmmo--;
        allAmmo--;

        RaycastHit hit;
        if (Physics.Raycast(FPSCam.transform.position, FPSCam.transform.forward, out hit, range))
        {
            var target = hit.transform.GetComponent<Enemy>() == null ? hit.transform.GetComponent<Target>() : hit.transform.GetComponent<Enemy>();
            if (target != null)
            {
                target.TakeDamage(damage);
            }
            if (hit.rigidbody != null)
            {
                hit.rigidbody.AddForce(-hit.normal * impactForce);
            }

            GameObject impactGO = Instantiate(impactEffect, hit.point, Quaternion.LookRotation(hit.normal));
            Destroy(impactGO, 2f);
        }
    }
}
