using System.Collections.Generic;
using UnityEngine;

public class Butelka : MonoBehaviour
{
    [SerializeField] GameObject Object;

    public List<Rigidbody> allParts = new List<Rigidbody>();

    public void Explosion()
    {
        foreach (Rigidbody part in allParts)
        {
            part.gameObject.SetActive(true);
            part.isKinematic = false;
        }
        Destroy(Object);
        Destroy(GetComponent<BoxCollider>());
    }
}
