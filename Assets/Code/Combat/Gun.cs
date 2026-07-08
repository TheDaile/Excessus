using UnityEngine;

public class Gun : MonoBehaviour
{
    public float damage = 10f;
    public float range = 100f;
    public Camera fpsCam;
    public ParticleSystem muzzleFlash;
    public GameObject impactEffect;
    public float fireforce = 15f;
    public float fireRate = 15f;
    public float nextTimeToFire = 0f;

    //[SerializeField] public GameObject bulletEffect;
    EnemyAi enemyAi;
    void Start()
    {
        enemyAi = FindFirstObjectByType<EnemyAi>();
    }

    public void Shoot()
    {
        muzzleFlash.Play();

        RaycastHit hit;
        if (Physics.Raycast(fpsCam.transform.position, fpsCam.transform.forward, out hit, range))
        {
            Debug.Log(hit.transform.name);

            if (hit.collider.TryGetComponent<IDamageable>(out var damageable))
            {
                damageable.TakeDamage(damage);
            }

            if (hit.collider.GetComponent("Butelka") != null)
            {
                hit.collider.gameObject.SendMessage("Explosion", SendMessageOptions.DontRequireReceiver);
            }

            GameObject impactGO = Instantiate(impactEffect, hit.point, Quaternion.LookRotation(hit.normal));
            //BulletEffect(hit);

            hit.rigidbody?.AddForce(-hit.normal * fireforce);

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

    // public void BulletEffect(RaycastHit hit)
    // {
    //     GameObject impact = Instantiate(
    //         bulletEffect,
    //         hit.point,
    //         Quaternion.LookRotation(hit.normal)
    //     );

    //     impact.transform.SetParent(hit.transform);

    //     impact.transform.localPosition =
    //         hit.transform.InverseTransformPoint(hit.point + hit.normal * 0.001f);

    //     impact.transform.localRotation =
    //         Quaternion.Inverse(hit.transform.rotation) *
    //         Quaternion.LookRotation(hit.normal);

    //     // Destroy(impact, 10f);
    // }
}
