using UnityEngine;

public class Gun: MonoBehaviour
{
    public float damage = 10f;
    public float range = 100f;
    public Camera fpsCam;
    public ParticleSystem muzzleFlash;
    public GameObject impactEffect;
    public float fireforce = 15f;
    public float fireRate = 15f;
    public float nextTimeToFire = 0f;

    [SerializeField] public GameObject bulletEffect;
    void Start()
    {
        
    }

    public void Shoot()
    {
        muzzleFlash.Play();

        RaycastHit hit;
        if (Physics.Raycast(fpsCam.transform.position, fpsCam.transform.forward, out hit, range))
        {
            Debug.Log(hit.transform.name);


            Target  target = hit.transform.GetComponent<Target>();
            if (target != null)
            {
                target.TakeDamage(damage);
            }

            hit.rigidbody?.AddForce(-hit.normal * fireforce);

            GameObject impactGO = Instantiate(impactEffect, hit.point, Quaternion.LookRotation(hit.normal));
            BulletEffect(hit);
            Destroy(impactGO, 2f);
        }

    }

    public void FireRate()
    {
        if (Time.time >= nextTimeToFire)
        {
            Shoot();
            nextTimeToFire = Time.time + 1f / fireRate;
        }
    }

    public void BulletEffect(RaycastHit hit)
    {
        GameObject impact = Instantiate(bulletEffect, hit.point, Quaternion.LookRotation(hit.normal));
        Vector3 forwardVector = impact.transform.forward;
        impact.transform.Translate(forwardVector * 0.01f, Space.World);
        Destroy(impact, 10f);
    }
}
