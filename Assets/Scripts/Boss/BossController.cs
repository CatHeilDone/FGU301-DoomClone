using System.Collections;
using UnityEngine;

public class BossController : MonoBehaviour
{
    [Header("Kích hoạt")]
    public Transform player;
    [Tooltip("Người chơi vào trong khoảng này (mét) thì boss bắt đầu trận đấu")]
    public float activationRange = 25f;
    public float startDelay = 2f;

    [Header("Xoay mặt")]
    [Tooltip("Độ/giây. Càng thấp thì càng dễ chạy vòng ra lưng boss")]
    public float turnSpeed = 30f;
    public bool canTurn = true;

    [Header("Bay lơ lửng")]
    [Tooltip("Độ cao tâm quả cầu so với chân boss (mét). Trần phòng thấp thì giảm xuống")]
    public float hoverHeight = 5.5f;
    public float bobAmplitude = 0.3f;
    public float bobSpeed = 1.2f;

    [Header("Nhịp đánh")]
    [Tooltip("Khoảng thời gian giữa hai đòn (tính từ lúc bắt đầu đòn trước)")]
    public float attackInterval = 5f;
    public float attackIntervalPhase2 = 4f;

    [Header("Laze (dùng chung cho laze ngang và laze dọc)")]
    public Transform eyeL;
    public Transform eyeR;
    public Material warningMaterial;
    public Material beamMaterial;
    public Color eyeGlowColor = new Color(1f, 0.2f, 0.2f);
    [Tooltip("Tổng thời gian gồng trước khi bắn (giây)")]
    public float laserChargeTime = 1.5f;
    [Tooltip("Đến giây này thì khóa hướng. Phần còn lại là thời gian để người chơi né")]
    public float laserLockTime = 0.8f;
    [Tooltip("Những layer chặn tia và nhận sát thương: tường, cột băng, người chơi")]
    public LayerMask laserMask = 1;
    public float laserMaxLength = 60f;
    [Tooltip("Bắn trúng mắt bấy nhiêu lần lúc đang gồng thì đòn bị ngắt")]
    public int eyeHitsToInterrupt = 1;
    public float interruptStunTime = 1.5f;

    [Header("Laze ngang")]
    public float laserFireTime = 0.7f;
    public int laserDamage = 20;
    [Tooltip("Độ dày của tia khi tính trúng (mét)")]
    public float laserRadius = 0.4f;

    [Header("Laze dọc")]
    [Tooltip("Tự tìm object con tên Model")]
    public Transform model;
    [Tooltip("Góc ngửa mặt lên khi gồng (độ)")]
    public float verticalTiltAngle = 25f;
    public float verticalSweepTime = 0.6f;
    [Tooltip("Góc bắt đầu của tia (âm = hướng lên trời)")]
    public float verticalSweepFrom = -20f;
    [Tooltip("Góc kết thúc của tia (dương = chúc xuống sàn)")]
    public float verticalSweepTo = 70f;
    [Tooltip("Một nửa độ rộng của dải nguy hiểm (mét)")]
    public float verticalBeamRadius = 0.6f;
    public int verticalDamage = 25;
    public float verticalWarnLength = 40f;

    [Header("Dậm đất")]
    [Tooltip("Bán kính vùng nguy hiểm quanh boss (mét)")]
    public float stompRadius = 12f;
    [Tooltip("Boss bay vọt lên cao thêm bao nhiêu mét trước khi rơi")]
    public float stompRiseHeight = 3f;
    public float stompRiseTime = 1.2f;
    public float stompFallTime = 0.3f;
    [Tooltip("Độ cao tâm quả cầu khi chạm sàn (bán kính 3 + 0,1)")]
    public float landedHeight = 3.1f;
    public float shockwaveTime = 0.5f;
    public int stompDamage = 10;
    public float playerStunTime = 1.5f;
    [Tooltip("Boss nằm choáng trên sàn bao lâu (giây)")]
    public float stompRecoverTime = 2.5f;
    public float stompReturnTime = 1f;

    [Header("Pha 2 (dưới 50% máu)")]
    public Color phase2BodyColor = new Color(1f, 0.55f, 0.5f);
    [Tooltip("Phát laze ngang thứ hai: thời gian gồng ngắn hơn")]
    public float phase2SecondCharge = 1.1f;
    public float phase2SecondLock = 0.45f;
    public float phase2ShotGap = 0.3f;

    [Header("Khi bị hạ")]
    public float deathFallTime = 1.5f;
    [Tooltip("Chờ bao lâu sau khi boss nằm xuống thì hiện màn hình YOU WIN")]
    public float victoryDelay = 1.5f;
    public Color deadColor = new Color(0.25f, 0.25f, 0.28f);

    [Header("Trạng thái (chỉ để xem)")]
    public string currentAction = "Chờ";

    public bool IsActivated => activated;

    BossHealth health;
    PlayerHealth playerHealth;
    PlayerMove playerMove;
    bool activated;

    Transform[] eyes;
    Renderer[] eyeRenderers;
    WeakPoint[] eyeWeakPoints;
    Color[] eyeBaseColors;
    Renderer bodyRenderer;
    LineRenderer[] warningLines;
    LineRenderer[] beamLines;
    LineRenderer floorWarningLine;
    LineRenderer verticalBeamLine;
    LineRenderer stompWarnLine;
    LineRenderer shockwaveLine;

    bool charging;
    int chargeHits;
    bool laserInterrupted;

    // Bay lơ lửng
    float heightOffset;          // độ lệch thêm do đòn dậm đất / lúc chết
    bool bobbing = true;
    float bobScale = 1f;
    float modelBaseX, modelBaseZ;

    void Awake()
    {
        health = GetComponent<BossHealth>();
        if (!eyeL) eyeL = transform.Find("Model/EyeL");
        if (!eyeR) eyeR = transform.Find("Model/EyeR");
        if (!model) model = transform.Find("Model");
        if (model)
        {
            modelBaseX = model.localPosition.x;
            modelBaseZ = model.localPosition.z;
        }
    }

    void Start()
    {
        if (!player)
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p) player = p.transform;
        }
        if (player)
        {
            playerHealth = player.GetComponentInParent<PlayerHealth>();
            playerMove = player.GetComponentInParent<PlayerMove>();
        }
        if (!playerHealth) playerHealth = FindFirstObjectByType<PlayerHealth>();
        if (!playerMove) playerMove = FindFirstObjectByType<PlayerMove>();

        SetupEyes();

        health.OnDied += HandleDied;
        health.OnPhase2 += HandlePhase2;
        health.OnWeakPointHit += HandleWeakPointHit;
    }

    void OnDisable()
    {
        if (playerMove) playerMove.enabled = true;   // phòng khi người chơi đang bị choáng
    }

    void OnDestroy()
    {
        if (!health) return;
        health.OnDied -= HandleDied;
        health.OnPhase2 -= HandlePhase2;
        health.OnWeakPointHit -= HandleWeakPointHit;
    }

    void SetupEyes()
    {
        eyes = new[] { eyeL, eyeR };
        eyeRenderers = new Renderer[2];
        eyeWeakPoints = new WeakPoint[2];
        eyeBaseColors = new Color[2];
        warningLines = new LineRenderer[2];
        beamLines = new LineRenderer[2];

        if (!warningMaterial || !beamMaterial)
            Debug.LogWarning("[Boss] Chưa gán Warning Material / Beam Material cho BossController");

        var body = transform.Find("Model/Body");
        if (body) bodyRenderer = body.GetComponent<Renderer>();

        for (int i = 0; i < 2; i++)
        {
            if (!eyes[i]) { Debug.LogError("[Boss] Không tìm thấy mắt " + i); continue; }

            eyeRenderers[i] = eyes[i].GetComponent<Renderer>();
            eyeWeakPoints[i] = eyes[i].GetComponent<WeakPoint>();
            if (eyeRenderers[i]) eyeBaseColors[i] = eyeRenderers[i].material.color;

            warningLines[i] = MakeLine("LaserWarning", eyes[i], warningMaterial, 0.06f);
            beamLines[i] = MakeLine("LaserBeam", eyes[i], beamMaterial, laserRadius * 2f);
        }

        // Laze dọc: dải đỏ trên sàn và tia chém
        floorWarningLine = MakeLine("FloorWarning", transform, warningMaterial, verticalBeamRadius * 2f);
        floorWarningLine.alignment = LineAlignment.TransformZ;
        verticalBeamLine = MakeLine("VerticalBeam", transform, beamMaterial, verticalBeamRadius * 2f);

        // Dậm đất: vòng báo hiệu và vòng sóng xung kích
        stompWarnLine = MakeCircle("StompWarning", warningMaterial, 0.2f);
        shockwaveLine = MakeCircle("Shockwave", beamMaterial, 0.6f);
    }

    LineRenderer MakeLine(string objName, Transform parent, Material mat, float width)
    {
        var go = new GameObject(objName);
        go.transform.SetParent(parent, false);
        var lr = go.AddComponent<LineRenderer>();
        lr.useWorldSpace = true;
        lr.positionCount = 2;
        lr.startWidth = width;
        lr.endWidth = width;
        lr.sharedMaterial = mat;
        lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        lr.receiveShadows = false;
        lr.enabled = false;
        return lr;
    }

    LineRenderer MakeCircle(string objName, Material mat, float width)
    {
        var lr = MakeLine(objName, transform, mat, width);
        lr.positionCount = 64;
        lr.loop = true;
        lr.alignment = LineAlignment.TransformZ;
        return lr;
    }

    void Update()
    {
        ApplyHover();

        if (health.IsDead || !player) return;

        Vector3 toPlayer = player.position - transform.position;
        toPlayer.y = 0f;   // chỉ xoay quanh trục dọc

        if (!activated)
        {
            if (toPlayer.magnitude <= activationRange)
            {
                activated = true;
                StartCoroutine(AttackLoop());
            }
            return;
        }

        if (canTurn && toPlayer.sqrMagnitude > 0.01f)
        {
            Quaternion target = Quaternion.LookRotation(toPlayer);
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation, target, turnSpeed * Time.deltaTime);
        }
    }

    // Giữ cho đầu boss lơ lửng ở độ cao hoverHeight, nhấp nhô nhẹ
    void ApplyHover()
    {
        if (!model) return;
        bobScale = Mathf.MoveTowards(bobScale, bobbing ? 1f : 0f, Time.deltaTime * 2f);
        float bob = Mathf.Sin(Time.time * bobSpeed) * bobAmplitude * bobScale;
        model.localPosition = new Vector3(modelBaseX, hoverHeight + heightOffset + bob, modelBaseZ);
    }

    IEnumerator AttackLoop()
    {
        currentAction = "Chuẩn bị";
        yield return new WaitForSeconds(startDelay);

        int index = 0;
        while (!health.IsDead)
        {
            // Đang vỡ băng thì boss đứng yên chịu đòn, không tấn công
            while (health.IsBroken)
            {
                currentAction = "Vỡ băng";
                yield return null;
            }

            float startTime = Time.time;

            switch (index % 3)
            {
                case 0: yield return StartCoroutine(LaserHorizontal()); break;
                case 1: yield return StartCoroutine(LaserVertical()); break;
                default: yield return StartCoroutine(Stomp()); break;
            }
            index++;

            currentAction = "Nghỉ";
            float interval = health.IsPhase2 ? attackIntervalPhase2 : attackInterval;
            float rest = interval - (Time.time - startTime);
            yield return new WaitForSeconds(Mathf.Max(0.5f, rest));
        }
    }

    // ================= LAZE NGANG =================

    // Pha 1: một phát. Pha 2: hai phát liên tiếp (phát thứ hai gồng ngắn hơn)
    IEnumerator LaserHorizontal()
    {
        int shots = health.IsPhase2 ? 2 : 1;
        for (int i = 0; i < shots; i++)
        {
            laserInterrupted = false;
            float charge = (i == 0) ? laserChargeTime : phase2SecondCharge;
            float lockT = (i == 0) ? laserLockTime : phase2SecondLock;

            yield return StartCoroutine(LaserShot(charge, lockT));

            if (laserInterrupted) yield break;   // bị ngắt thì hủy luôn phát còn lại
            if (i < shots - 1) yield return new WaitForSeconds(phase2ShotGap);
        }
    }

    IEnumerator LaserShot(float chargeTime, float lockTime)
    {
        currentAction = "Laze ngang: gồng";
        charging = true;
        chargeHits = 0;

        Vector3 aimPoint = player.position;
        bool locked = false;
        float t = 0f;

        while (t < chargeTime)
        {
            t += Time.deltaTime;
            SetEyeGlow(t / chargeTime);

            if (!locked)
            {
                aimPoint = player.position;
                if (t >= lockTime)
                {
                    locked = true;
                    canTurn = false;
                    currentAction = "Laze ngang: đã khóa hướng";
                }
            }
            DrawLines(warningLines, aimPoint);

            if (chargeHits >= eyeHitsToInterrupt)
            {
                laserInterrupted = true;
                yield return StartCoroutine(InterruptLaser());
                yield break;
            }
            yield return null;
        }

        charging = false;
        HideLines(warningLines);
        currentAction = "Laze ngang: bắn";

        bool alreadyHit = false;
        float fireT = 0f;
        while (fireT < laserFireTime)
        {
            fireT += Time.deltaTime;
            bool touchedPlayer = DrawLines(beamLines, aimPoint);
            if (touchedPlayer && !alreadyHit)
            {
                alreadyHit = true;   // mỗi phát chỉ gây sát thương một lần
                if (playerHealth) playerHealth.DamagePlayer(laserDamage);
            }
            yield return null;
        }

        HideLines(beamLines);
        ResetEyes();
        canTurn = true;
    }

    // ================= LAZE DỌC =================

    IEnumerator LaserVertical()
    {
        currentAction = "Laze dọc: gồng";
        charging = true;
        chargeHits = 0;

        float floorY = FloorY();
        Vector3 lockedDir = FlatDirToPlayer();
        bool locked = false;
        float t = 0f;

        // Gồng: ngửa mặt lên, dải đỏ trên sàn bám theo người chơi rồi khóa
        while (t < laserChargeTime)
        {
            t += Time.deltaTime;
            float k = t / laserChargeTime;
            SetEyeGlow(k);
            SetHeadTilt(Mathf.Lerp(0f, verticalTiltAngle, k));

            if (!locked)
            {
                lockedDir = FlatDirToPlayer();
                if (t >= laserLockTime)
                {
                    locked = true;
                    canTurn = false;
                    currentAction = "Laze dọc: đã khóa hướng";
                }
            }
            DrawFloorLine(lockedDir, floorY);

            if (chargeHits >= eyeHitsToInterrupt)
            {
                yield return StartCoroutine(InterruptLaser());
                yield break;
            }
            yield return null;
        }

        charging = false;
        currentAction = "Laze dọc: chém";

        // Điểm xuất phát của tia: ngay trước mặt boss, cao bằng hai mắt
        Vector3 eyeMid = FaceOrigin();
        Vector3 horiz = eyeMid - transform.position;
        horiz.y = 0f;
        Vector3 origin = new Vector3(transform.position.x, eyeMid.y, transform.position.z)
                         + lockedDir * horiz.magnitude;
        Vector3 right = Vector3.Cross(Vector3.up, lockedDir);

        // Chém: tia quét từ trên trời xuống sàn trong mặt phẳng dải đỏ
        bool alreadyHit = false;
        float s = 0f;
        while (s < 1f)
        {
            s += Time.deltaTime / verticalSweepTime;
            float angle = Mathf.Lerp(verticalSweepFrom, verticalSweepTo, Mathf.Clamp01(s));
            Vector3 dir = Quaternion.AngleAxis(angle, right) * lockedDir;

            float len = laserMaxLength;
            if (Physics.SphereCast(origin, verticalBeamRadius, dir, out RaycastHit hit,
                    laserMaxLength, laserMask, QueryTriggerInteraction.Ignore))
            {
                len = hit.distance;
                if (!alreadyHit && hit.collider.GetComponentInParent<PlayerHealth>())
                {
                    alreadyHit = true;
                    if (playerHealth) playerHealth.DamagePlayer(verticalDamage);
                }
            }

            if (verticalBeamLine)
            {
                verticalBeamLine.enabled = true;
                verticalBeamLine.SetPosition(0, origin);
                verticalBeamLine.SetPosition(1, origin + dir * len);
            }
            yield return null;
        }

        yield return new WaitForSeconds(0.1f);
        HideVerticalVisuals();
        ResetEyes();
        yield return StartCoroutine(LowerHead(0.3f));
        canTurn = true;
    }

    float FloorY()
    {
        if (Physics.Raycast(transform.position + Vector3.up, Vector3.down, out RaycastHit h,
                20f, laserMask, QueryTriggerInteraction.Ignore))
            return h.point.y;
        return transform.position.y;
    }

    Vector3 FlatDirToPlayer()
    {
        Vector3 d = player.position - transform.position;
        d.y = 0f;
        return d.sqrMagnitude > 0.01f ? d.normalized : transform.forward;
    }

    Vector3 FaceOrigin()
    {
        if (eyeL && eyeR) return (eyeL.position + eyeR.position) * 0.5f;
        return transform.position + Vector3.up * hoverHeight;
    }

    void DrawFloorLine(Vector3 dir, float floorY)
    {
        if (!floorWarningLine) return;
        Vector3 start = new Vector3(transform.position.x, floorY + 0.05f, transform.position.z);
        floorWarningLine.enabled = true;
        floorWarningLine.transform.rotation = Quaternion.Euler(-90f, 0f, 0f);   // nằm phẳng trên sàn
        floorWarningLine.SetPosition(0, start);
        floorWarningLine.SetPosition(1, start + dir * verticalWarnLength);
    }

    void HideVerticalVisuals()
    {
        if (floorWarningLine) floorWarningLine.enabled = false;
        if (verticalBeamLine) verticalBeamLine.enabled = false;
    }

    void SetHeadTilt(float angle)
    {
        if (model) model.localRotation = Quaternion.Euler(-angle, 0f, 0f);
    }

    IEnumerator LowerHead(float duration)
    {
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            SetHeadTilt(Mathf.Lerp(verticalTiltAngle, 0f, t / duration));
            yield return null;
        }
        SetHeadTilt(0f);
    }

    // ================= DẬM ĐẤT =================

    IEnumerator Stomp()
    {
        currentAction = "Dậm đất: bay lên";
        canTurn = false;
        bobbing = false;

        float floorY = FloorY();
        Vector3 center = new Vector3(transform.position.x, floorY + 0.05f, transform.position.z);
        // độ lệch cần có để tâm quả cầu hạ xuống đúng độ cao landedHeight so với sàn
        float landOffset = landedHeight + (floorY - transform.position.y) - hoverHeight;

        // 1) Bay vọt lên cao, vòng đỏ báo vùng nguy hiểm
        float t = 0f;
        while (t < stompRiseTime)
        {
            t += Time.deltaTime;
            heightOffset = Mathf.Lerp(0f, stompRiseHeight, Mathf.SmoothStep(0f, 1f, t / stompRiseTime));
            ShowCircle(stompWarnLine, center, stompRadius);
            yield return null;
        }

        // 2) Lao xuống sàn
        currentAction = "Dậm đất: lao xuống";
        t = 0f;
        while (t < stompFallTime)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / stompFallTime);
            heightOffset = Mathf.Lerp(stompRiseHeight, landOffset, k * k);
            yield return null;
        }
        heightOffset = landOffset;
        HideLine(stompWarnLine);

        // 3) Sóng xung kích lan ra
        currentAction = "Dậm đất: sóng xung kích";
        bool alreadyHit = false;
        t = 0f;
        while (t < shockwaveTime)
        {
            t += Time.deltaTime;
            float r = Mathf.Lerp(0.5f, stompRadius, t / shockwaveTime);
            ShowCircle(shockwaveLine, center, r);

            if (!alreadyHit && player)
            {
                Vector3 d = player.position - center;
                d.y = 0f;
                if (d.magnitude <= r)
                {
                    alreadyHit = true;
                    if (playerHealth) playerHealth.DamagePlayer(stompDamage);
                    StunPlayer(playerStunTime);
                }
            }
            yield return null;
        }
        HideLine(shockwaveLine);

        // 4) Boss nằm choáng trên sàn, lộ lưng cho người chơi bắn
        currentAction = "Choáng (lộ lưng)";
        yield return new WaitForSeconds(stompRecoverTime);

        // 5) Bay trở lại vị trí lơ lửng
        currentAction = "Bay lên lại";
        t = 0f;
        while (t < stompReturnTime)
        {
            t += Time.deltaTime;
            heightOffset = Mathf.Lerp(landOffset, 0f, Mathf.SmoothStep(0f, 1f, t / stompReturnTime));
            yield return null;
        }
        heightOffset = 0f;
        bobbing = true;
        canTurn = true;
    }

    void ShowCircle(LineRenderer lr, Vector3 center, float radius)
    {
        if (!lr) return;
        lr.enabled = true;
        lr.transform.rotation = Quaternion.Euler(-90f, 0f, 0f);   // nằm phẳng trên sàn
        int n = lr.positionCount;
        for (int i = 0; i < n; i++)
        {
            float a = (i / (float)n) * Mathf.PI * 2f;
            lr.SetPosition(i, center + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * radius);
        }
    }

    void HideLine(LineRenderer lr)
    {
        if (lr) lr.enabled = false;
    }

    // Làm người chơi không đi được trong một lúc (vẫn xoay được tầm nhìn)
    void StunPlayer(float duration)
    {
        if (playerMove) StartCoroutine(StunPlayerRoutine(duration));
    }

    IEnumerator StunPlayerRoutine(float duration)
    {
        playerMove.enabled = false;
        if (playerMove.camAnim) playerMove.camAnim.SetBool("isWalking", false);
        yield return new WaitForSeconds(duration);
        playerMove.enabled = true;
    }

    // ================= DÙNG CHUNG =================

    IEnumerator InterruptLaser()
    {
        charging = false;
        currentAction = "Bị ngắt (choáng)";
        HideLines(warningLines);
        HideVerticalVisuals();
        SetHeadTilt(0f);
        ResetEyes();
        canTurn = true;
        yield return new WaitForSeconds(interruptStunTime);
    }

    // Vẽ tia từ mỗi mắt tới điểm ngắm. Trả về true nếu tia chạm người chơi
    bool DrawLines(LineRenderer[] lines, Vector3 aimPoint)
    {
        bool hitPlayer = false;

        for (int i = 0; i < eyes.Length; i++)
        {
            if (!eyes[i] || lines[i] == null) continue;

            Vector3 origin = eyes[i].position;
            Vector3 dir = (aimPoint - origin).normalized;
            float len = laserMaxLength;

            if (Physics.SphereCast(origin, laserRadius, dir, out RaycastHit hit,
                    laserMaxLength, laserMask, QueryTriggerInteraction.Ignore))
            {
                len = hit.distance;
                if (hit.collider.GetComponentInParent<PlayerHealth>()) hitPlayer = true;
            }

            lines[i].enabled = true;
            lines[i].SetPosition(0, origin);
            lines[i].SetPosition(1, origin + dir * len);
        }
        return hitPlayer;
    }

    void HideLines(LineRenderer[] lines)
    {
        if (lines == null) return;
        foreach (var lr in lines)
            if (lr) lr.enabled = false;
    }

    void SetEyeGlow(float k)
    {
        if (eyeRenderers == null) return;
        for (int i = 0; i < eyeRenderers.Length; i++)
        {
            if (!eyeRenderers[i]) continue;
            if (eyeWeakPoints[i] && eyeWeakPoints[i].IsBroken) continue;   // mắt đã vỡ thì giữ màu xám
            eyeRenderers[i].material.color = Color.Lerp(eyeBaseColors[i], eyeGlowColor, k);
        }
    }

    void ResetEyes()
    {
        SetEyeGlow(0f);
    }

    // ================= SỰ KIỆN TỪ BossHealth =================

    void HandleWeakPointHit(WeakPoint wp)
    {
        // Bắn trúng mắt lúc đang gồng laze thì tích lũy để ngắt đòn
        if (charging && wp.kind == WeakPoint.Kind.Eye)
            chargeHits++;
    }

    void HandlePhase2()
    {
        Debug.Log("[Boss] Vào pha 2");
        StartCoroutine(TintBody(phase2BodyColor, 1f));
    }

    IEnumerator TintBody(Color target, float duration)
    {
        if (!bodyRenderer) yield break;
        Color from = bodyRenderer.material.color;
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            bodyRenderer.material.color = Color.Lerp(from, target, t / duration);
            yield return null;
        }
        bodyRenderer.material.color = target;
    }

    void HandleDied()
    {
        StopAllCoroutines();
        HideLines(warningLines);
        HideLines(beamLines);
        HideVerticalVisuals();
        HideLine(stompWarnLine);
        HideLine(shockwaveLine);
        SetHeadTilt(0f);
        if (playerMove) playerMove.enabled = true;   // dừng choáng nếu boss chết giữa chừng
        canTurn = false;
        currentAction = "Đã bị hạ";
        StartCoroutine(DeathSequence());
    }

    // Boss rơi xuống sàn, tối màu, rồi hiện màn hình chiến thắng
    IEnumerator DeathSequence()
    {
        bobbing = false;

        // tắt mắt và tối màu thân
        if (eyeRenderers != null)
            foreach (var r in eyeRenderers)
                if (r) r.material.color = deadColor;
        if (bodyRenderer) bodyRenderer.material.color = deadColor;

        float floorY = FloorY();
        float landOffset = landedHeight + (floorY - transform.position.y) - hoverHeight;
        float startOffset = heightOffset;
        float t = 0f;
        while (t < deathFallTime)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / deathFallTime);
            heightOffset = Mathf.Lerp(startOffset, landOffset, k * k);
            yield return null;
        }
        heightOffset = landOffset;

        yield return new WaitForSeconds(victoryDelay);

        if (ScoreManager.Instance) ScoreManager.Instance.TriggerVictory();
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, activationRange);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, stompRadius);
    }
}