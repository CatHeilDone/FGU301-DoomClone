using UnityEngine;

public class PlayerInventory : MonoBehaviour
{
    public bool hasYellow, hasOrange,hasBlue,hasPurple;
    private void Update()
    {
        CanvasManager.Instance.clearKey();
    }
}
