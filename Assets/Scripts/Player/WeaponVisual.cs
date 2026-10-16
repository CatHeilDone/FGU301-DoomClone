using UnityEngine;

public class WeaponVisual : MonoBehaviour
{
    public CharacterController controller;   // kéo Player vào
    public float bobSpeed = 12f;
    public float bobX = 0.02f;
    public float bobY = 0.025f;
    public float smooth = 10f;
    public float recoilDistance = 0.08f;
    public float recoilRecover = 12f;

    Vector3 basePos, bobOffset;
    float timer, recoil;

    void Start() => basePos = transform.localPosition;

    void Update()
    {
        Vector3 v = controller.velocity; v.y = 0;
        Vector3 targetBob = Vector3.zero;

        if (v.magnitude > 0.1f)
        {
            timer += Time.deltaTime * bobSpeed;
            targetBob = new Vector3(Mathf.Cos(timer) * bobX, Mathf.Sin(timer * 2f) * bobY, 0);
        }
        else timer = 0;

        bobOffset = Vector3.Lerp(bobOffset, targetBob, Time.deltaTime * smooth);
        recoil = Mathf.Lerp(recoil, 0, Time.deltaTime * recoilRecover);

        transform.localPosition = basePos + bobOffset + new Vector3(0, 0, -recoil);
    }

    public void PlayRecoil() => recoil = recoilDistance;
}