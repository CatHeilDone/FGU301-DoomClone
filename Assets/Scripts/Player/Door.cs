using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Door : MonoBehaviour
{
    public Animator doorAnim;
    public GameObject areaToSpawn;
    public bool requiresKey;
    public bool reqYellow, reqPurple, reqBlue, reqOrange;

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (requiresKey)
            {
                if (reqYellow && other.GetComponent<PlayerInventory>().hasYellow)
                {
                    doorAnim.SetTrigger("OpenDoor");
                    areaToSpawn.SetActive(true);
                }
                if (reqPurple && other.GetComponent<PlayerInventory>().hasPurple)
                {
                    doorAnim.SetTrigger("OpenDoor");
                    areaToSpawn.SetActive(true);
                }
                if (reqBlue && other.GetComponent<PlayerInventory>().hasBlue)
                {
                    doorAnim.SetTrigger("OpenDoor");
                    areaToSpawn.SetActive(true);
                }
                if (reqOrange && other.GetComponent<PlayerInventory>().hasOrange)
                {
                    doorAnim.SetTrigger("OpenDoor");
                    areaToSpawn.SetActive(true);
                }
            }
            else
            {
                doorAnim.SetTrigger("OpenDoor");
                areaToSpawn.SetActive(true);
            }

        }
    }
}
