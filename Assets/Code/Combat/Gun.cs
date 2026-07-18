using UnityEngine;

public class Gun : MonoBehaviour
{
    [SerializeField] private bool startsEquipped;

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

    public bool IsEquipped { get; private set; }

    private void Awake()
    {
        IsEquipped = startsEquipped;
    }

    void Start()
    {
        enemyAi = FindFirstObjectByType<EnemyAi>();
    }

    public void Equip(InventoryItemData weaponItem)
    {
        if (weaponItem == null || weaponItem.ItemType != InventoryItemType.Weapon)
        {
            Unequip();
            return;
        }

        damage = weaponItem.WeaponDamage;
        range = weaponItem.WeaponRange;
        fireRate = weaponItem.WeaponFireRate;
        fireforce = weaponItem.WeaponFireForce;
        IsEquipped = true;
    }

    public void Unequip()
    {
        IsEquipped = false;
    }

    public void Shoot()
    {
        if (!IsEquipped || fpsCam == null)
        {
            return;
        }

        if (muzzleFlash != null)
        {
            muzzleFlash.Play();
        }

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

            if (impactEffect != null)
            {
                GameObject impactGO = Instantiate(impactEffect, hit.point, Quaternion.LookRotation(hit.normal));
                Destroy(impactGO, 2f);
            }

            hit.rigidbody?.AddForce(-hit.normal * fireforce);
        }

    }

    public void FireRate()
    {
        if (!IsEquipped || fireRate <= 0f)
        {
            return;
        }

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
