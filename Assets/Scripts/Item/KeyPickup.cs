using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class KeyPickup : MonoBehaviour
{
    public bool isYellowKey,isPurpleKey,isBlueKey,isOrangeKey;
    void OnTriggerEnter(Collider other) {
        if(other.CompareTag("Player"))
        {
            if(isYellowKey)
            {
                other.GetComponent<PlayerInventory>().hasYellow = true;
            }
            if(isPurpleKey)
            {
                other.GetComponent<PlayerInventory>().hasPurple = true;
                CanvasManager.Instance.UpdateKey(keyColor:"purple");
            }
            if(isBlueKey)
            {
                other.GetComponent<PlayerInventory>().hasBlue = true;
                CanvasManager.Instance.UpdateKey(keyColor:"blue");
            }
            if(isOrangeKey)
            {
                other.GetComponent<PlayerInventory>().hasOrange = true;
                CanvasManager.Instance.UpdateKey(keyColor:"orange");
            }
            Destroy(gameObject);
        }
    }
}
