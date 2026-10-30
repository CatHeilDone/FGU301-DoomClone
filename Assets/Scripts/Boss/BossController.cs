using System.Collections;
using UnityEngine;

public class BossController : MonoBehaviour
{
    [Header("Activation")]
    public Transform player;

    [Tooltip("When the player gets this close (meters) the fight starts")]
    public float activationRange = 25f;
    public float startDelay = 2f;

    [Header("Turning")]
    [Tooltip("Degrees per second. Lower = easier to run around to the boss's back")]
    public float turnSpeed = 30f;
    public bool canTurn = true;

    [Header("Hovering")]
    [Tooltip(
        "Height of the sphere's center above the boss's feet (meters). Lower it if the room's ceiling is low"
    )]
    public float hoverHeight = 5.5f;
    public float bobAmplitude = 0.3f;
    public float bobSpeed = 1.2f;

    [Header("Chase")]
    public bool chasePlayer = true;
    public float chaseSpeed = 3.5f;
    public float chaseSpeedPhase2 = 5f;

    [Tooltip("How quickly the boss speeds up / slows down (m/s per second)")]
    public float chaseAcceleration = 4f;

    [Tooltip("The boss stops this close to the player (meters)")]
    public float chaseStopDistance = 9f;

    [Tooltip("If the player is farther than this, the boss stays put")]
    public float chaseMaxPlayerDistance = 60f;

    [Tooltip("The boss never goes farther than this from where it started")]
    public float leashRadius = 30f;

    [Tooltip("Size used to check for walls while flying (the body is about 3)")]
    public float bodyRadius = 2.8f;

    [Tooltip("Speed multiplier while charging a laser, before the aim locks")]
    public float chargeMoveMultiplier = 0.5f;

    [Header("Attack rhythm")]
    [Tooltip("Time between the starts of two attacks")]
    public float attackInterval = 5f;
    public float attackIntervalPhase2 = 4f;

    [Header("Lasers (shared by horizontal and vertical laser)")]
    public Transform eyeL;
    public Transform eyeR;
    public Material warningMaterial;
    public Material beamMaterial;
    public Color eyeGlowColor = new Color(1f, 0.2f, 0.2f);

    [Tooltip("Total charge time before firing (seconds)")]
    public float laserChargeTime = 1.5f;

    [Tooltip("The aim locks at this time. The rest is the player's time to dodge")]
    public float laserLockTime = 0.8f;

    [Tooltip("Layers that block the beams and can be hit: walls, pillars, the player")]
    public LayerMask laserMask = 1;
    public float laserMaxLength = 60f;

    [Tooltip("Hits on an eye while charging that interrupt the attack")]
    public int eyeHitsToInterrupt = 1;
    public float interruptStunTime = 1.5f;

    [Header("Horizontal laser")]
    public float laserFireTime = 0.7f;
    public int laserDamage = 20;

    [Tooltip("Beam thickness used for hit detection (meters)")]
    public float laserRadius = 0.4f;

    [Header("Vertical laser")]
    [Tooltip("Found automatically (the child named Model)")]
    public Transform model;

    [Tooltip("How far the head tilts back while charging (degrees)")]
    public float verticalTiltAngle = 25f;
    public float verticalSweepTime = 0.6f;

    [Tooltip("Start angle of the beam (negative = pointing up)")]
    public float verticalSweepFrom = -20f;

    [Tooltip("End angle of the beam (positive = pointing down at the floor)")]
    public float verticalSweepTo = 70f;

    [Tooltip("Half the width of the danger strip (meters)")]
    public float verticalBeamRadius = 0.6f;
    public int verticalDamage = 25;
    public float verticalWarnLength = 40f;

    [Header("Stomp")]
    [Tooltip("Radius of the danger zone around the boss (meters)")]
    public float stompRadius = 12f;

    [Tooltip("How much higher the boss rises before slamming down (meters)")]
    public float stompRiseHeight = 3f;
    public float stompRiseTime = 1.2f;
    public float stompFallTime = 0.3f;

    [Tooltip("Height of the sphere's center when it lands (radius 3 + 0.1)")]
    public float landedHeight = 3.1f;
    public float shockwaveTime = 0.5f;
    public int stompDamage = 10;
    public float playerStunTime = 1.5f;

    [Tooltip("How long the boss lies stunned on the floor (seconds)")]
    public float stompRecoverTime = 2.5f;
    public float stompReturnTime = 1f;

    [Header("Phase 2 (below 50% health)")]
    public Color phase2BodyColor = new Color(1f, 0.55f, 0.5f);

    [Tooltip("Second horizontal laser shot: shorter charge time")]
    public float phase2SecondCharge = 1.1f;
    public float phase2SecondLock = 0.45f;
    public float phase2ShotGap = 0.3f;

    [Header("When defeated")]
    public float deathFallTime = 1.5f;

    [Tooltip("How long to wait after the boss lies down before showing YOU WIN")]
    public float victoryDelay = 1.5f;
    public Color deadColor = new Color(0.25f, 0.25f, 0.28f);

    [Header("Body renderer (optional)")]
    [Tooltip("Renderer tinted in phase 2 and when dying. Leave empty to find it automatically")]
    public Renderer bodyRenderer;

    [Header("State (read-only, for debugging)")]
    public string currentAction = "Idle";

    public bool IsActivated => activated;

    BossHealth health;
    PlayerHealth playerHealth;
    PlayerMove playerMove;
    bool activated;

    Transform[] eyes;
    Renderer[] eyeRenderers;
    WeakPoint[] eyeWeakPoints;
    Color[] eyeBaseColors;
    LineRenderer[] warningLines;
    LineRenderer[] beamLines;
    LineRenderer floorWarningLine;
    LineRenderer verticalBeamLine;
    LineRenderer stompWarnLine;
    LineRenderer shockwaveLine;

    bool charging;
    int chargeHits;
    bool laserInterrupted;

    // Chase
    bool attacking;
    float chaseVelocity;
    Vector3 homePosition;
    static readonly RaycastHit[] castBuffer = new RaycastHit[8];

    // Hovering
    float heightOffset; // extra offset from the stomp / death
    bool bobbing = true;
    float bobScale = 1f;
    float modelBaseX,
        modelBaseZ;

    static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    static readonly int ColorId = Shader.PropertyToID("_Color");

    [Header("Boss Sound Effects")]
    [SerializeField]
    private AudioSource chargeAudioSource;

    [SerializeField]
    private AudioSource effectsAudioSource;

    [SerializeField]
    private AudioClip laserChargeSFX;

    [SerializeField]
    private AudioClip laserFireSFX;

    [SerializeField]
    private AudioClip stompRiseSFX;

    [SerializeField]
    private AudioClip stompImpactSFX;

    void PlayLaserCharge()
    {
        if (!chargeAudioSource || !laserChargeSFX)
            return;

        chargeAudioSource.Stop();
        chargeAudioSource.clip = laserChargeSFX;
        chargeAudioSource.Play();
    }

    void StopLaserCharge()
    {
        if (chargeAudioSource)
            chargeAudioSource.Stop();
    }

    void PlayEffect(AudioClip clip, float volume = 1f)
    {
        if (effectsAudioSource && clip)
            effectsAudioSource.PlayOneShot(clip, volume);
    }

    void Awake()
    {
        health = GetComponent<BossHealth>();
        if (!eyeL)
            eyeL = transform.Find("Model/EyeL");
        if (!eyeR)
            eyeR = transform.Find("Model/EyeR");
        if (!model)
            model = transform.Find("Model");
        if (model)
        {
            modelBaseX = model.localPosition.x;
            modelBaseZ = model.localPosition.z;
        }
        homePosition = transform.position;
    }

    void Start()
    {
        if (!player)
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p)
                player = p.transform;
        }
        if (player)
        {
            playerHealth = player.GetComponentInParent<PlayerHealth>();
            playerMove = player.GetComponentInParent<PlayerMove>();
        }
        if (!playerHealth)
            playerHealth = FindFirstObjectByType<PlayerHealth>();
        if (!playerMove)
            playerMove = FindFirstObjectByType<PlayerMove>();

        SetupEyes();

        health.OnDied += HandleDied;
        health.OnPhase2 += HandlePhase2;
        health.OnWeakPointHit += HandleWeakPointHit;
    }

    void OnDisable()
    {
        if (playerMove)
            playerMove.enabled = true; // in case the player is stunned
    }

    void OnDestroy()
    {
        if (!health)
            return;
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
            Debug.LogWarning(
                "[Boss] Warning Material / Beam Material are not assigned on BossController"
            );

        if (!bodyRenderer)
            bodyRenderer = FindBodyRenderer();

        for (int i = 0; i < 2; i++)
        {
            if (!eyes[i])
            {
                Debug.LogError("[Boss] Could not find eye " + i);
                continue;
            }

            eyeRenderers[i] = eyes[i].GetComponent<Renderer>();
            eyeWeakPoints[i] = eyes[i].GetComponent<WeakPoint>();
            if (eyeRenderers[i])
                eyeBaseColors[i] = eyeRenderers[i].material.color;

            warningLines[i] = MakeLine("LaserWarning", eyes[i], warningMaterial, 0.06f);
            beamLines[i] = MakeLine("LaserBeam", eyes[i], beamMaterial, laserRadius * 2f);
        }

        // Vertical laser: floor strip and the slashing beam
        floorWarningLine = MakeLine(
            "FloorWarning",
            transform,
            warningMaterial,
            verticalBeamRadius * 2f
        );
        floorWarningLine.alignment = LineAlignment.TransformZ;
        verticalBeamLine = MakeLine(
            "VerticalBeam",
            transform,
            beamMaterial,
            verticalBeamRadius * 2f
        );

        // Stomp: warning circle and shockwave ring
        stompWarnLine = MakeCircle("StompWarning", warningMaterial, 0.2f);
        shockwaveLine = MakeCircle("Shockwave", beamMaterial, 0.6f);
    }

    // The body is the first active renderer under Model that is not an eye / weak point
    Renderer FindBodyRenderer()
    {
        var body = transform.Find("Model/Body");
        if (body && body.gameObject.activeInHierarchy)
        {
            var r = body.GetComponent<Renderer>();
            if (r)
                return r;
        }
        if (!model)
            return null;

        foreach (var r in model.GetComponentsInChildren<Renderer>()) // active objects only
        {
            if (r is LineRenderer)
                continue;
            if (r.GetComponent<WeakPoint>())
                continue;
            return r;
        }
        return null;
    }

    // Color helpers that don't spam errors when a shader has no color property
    static bool TryGetColor(Renderer r, out Color c)
    {
        c = Color.white;
        if (!r)
            return false;
        var m = r.material;
        if (m.HasProperty(BaseColorId))
        {
            c = m.GetColor(BaseColorId);
            return true;
        }
        if (m.HasProperty(ColorId))
        {
            c = m.GetColor(ColorId);
            return true;
        }
        return false;
    }

    static void SetColor(Renderer r, Color c)
    {
        if (!r)
            return;
        var m = r.material;
        if (m.HasProperty(BaseColorId))
            m.SetColor(BaseColorId, c);
        if (m.HasProperty(ColorId))
            m.SetColor(ColorId, c);
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

        if (health.IsDead || !player)
            return;

        Vector3 toPlayer = player.position - transform.position;
        toPlayer.y = 0f; // only rotate around the vertical axis

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
                transform.rotation,
                target,
                turnSpeed * Time.deltaTime
            );
        }

        ChasePlayer(toPlayer);
    }

    // Keeps the head floating at hoverHeight with a gentle bob
    void ApplyHover()
    {
        if (!model)
            return;
        bobScale = Mathf.MoveTowards(bobScale, bobbing ? 1f : 0f, Time.deltaTime * 2f);
        float bob = Mathf.Sin(Time.time * bobSpeed) * bobAmplitude * bobScale;
        model.localPosition = new Vector3(modelBaseX, hoverHeight + heightOffset + bob, modelBaseZ);
    }

    // ================= CHASE =================

    // Fly toward the player (horizontal only; the head still hovers on its own)
    void ChasePlayer(Vector3 toPlayerFlat)
    {
        float dist = toPlayerFlat.magnitude;
        float targetSpeed = 0f;

        if (
            chasePlayer
            && !health.IsBroken
            && dist > chaseStopDistance
            && dist <= chaseMaxPlayerDistance
        )
        {
            float mult;
            if (!attacking)
                mult = 1f; // between attacks: full speed
            else if (charging && canTurn)
                mult = chargeMoveMultiplier; // charging, aim not locked yet
            else
                mult = 0f; // locked / stomping / stunned: stand still

            float speed = health.IsPhase2 ? chaseSpeedPhase2 : chaseSpeed;
            targetSpeed = speed * mult;
        }

        chaseVelocity = Mathf.MoveTowards(
            chaseVelocity,
            targetSpeed,
            chaseAcceleration * Time.deltaTime
        );
        if (chaseVelocity < 0.01f)
            return;

        Vector3 step = toPlayerFlat.normalized * chaseVelocity * Time.deltaTime;
        step = ClipMovement(step);

        Vector3 newPos = transform.position + step;

        // Stay inside the leash around the starting position
        Vector3 fromHome = newPos - homePosition;
        fromHome.y = 0f;
        if (fromHome.magnitude > leashRadius)
        {
            fromHome = fromHome.normalized * leashRadius;
            newPos = new Vector3(
                homePosition.x + fromHome.x,
                transform.position.y,
                homePosition.z + fromHome.z
            );
        }
        transform.position = newPos;
    }

    // Stops the boss at walls and slides it along them
    Vector3 ClipMovement(Vector3 step)
    {
        float len = step.magnitude;
        if (len < 1e-5f)
            return step;

        Vector3 origin = model ? model.position : transform.position + Vector3.up * hoverHeight;

        if (BlockedAlong(origin, step / len, len + 0.05f, out RaycastHit hit))
        {
            Vector3 slide = Vector3.ProjectOnPlane(step, hit.normal);
            slide.y = 0f;
            float slen = slide.magnitude;
            if (slen > 1e-5f && !BlockedAlong(origin, slide / slen, slen + 0.05f, out _))
                return slide;
            return Vector3.zero;
        }
        return step;
    }

    // Nearest obstacle ahead, ignoring the boss's own colliders
    bool BlockedAlong(Vector3 origin, Vector3 dir, float dist, out RaycastHit nearest)
    {
        nearest = default;
        int n = Physics.SphereCastNonAlloc(
            origin,
            bodyRadius,
            dir,
            castBuffer,
            dist,
            laserMask,
            QueryTriggerInteraction.Ignore
        );

        float best = float.MaxValue;
        bool found = false;
        for (int i = 0; i < n; i++)
        {
            var h = castBuffer[i];
            if (h.collider.transform.IsChildOf(transform))
                continue; // ignore ourselves
            if (h.distance < best)
            {
                best = h.distance;
                nearest = h;
                found = true;
            }
        }
        return found;
    }

    // ================= ATTACK LOOP =================

    IEnumerator AttackLoop()
    {
        currentAction = "Getting ready";
        yield return new WaitForSeconds(startDelay);

        int index = 0;
        while (!health.IsDead)
        {
            // While "broken" the boss stands still and takes extra damage; no attacks
            while (health.IsBroken)
            {
                currentAction = "Broken";
                yield return null;
            }

            float startTime = Time.time;

            attacking = true;
            switch (index % 3)
            {
                case 0:
                    yield return StartCoroutine(LaserHorizontal());
                    break;
                case 1:
                    yield return StartCoroutine(LaserVertical());
                    break;
                default:
                    yield return StartCoroutine(Stomp());
                    break;
            }
            index++;
            attacking = false;

            currentAction = "Resting";
            float interval = health.IsPhase2 ? attackIntervalPhase2 : attackInterval;
            float rest = interval - (Time.time - startTime);
            yield return new WaitForSeconds(Mathf.Max(0.5f, rest));
        }
    }

    // ================= HORIZONTAL LASER =================

    // Phase 1: one shot. Phase 2: two shots in a row (the second charges faster)
    IEnumerator LaserHorizontal()
    {
        int shots = health.IsPhase2 ? 2 : 1;
        for (int i = 0; i < shots; i++)
        {
            laserInterrupted = false;
            float charge = (i == 0) ? laserChargeTime : phase2SecondCharge;
            float lockT = (i == 0) ? laserLockTime : phase2SecondLock;

            yield return StartCoroutine(LaserShot(charge, lockT));

            if (laserInterrupted)
                yield break; // interrupted: cancel the remaining shot
            if (i < shots - 1)
                yield return new WaitForSeconds(phase2ShotGap);
        }
    }

    IEnumerator LaserShot(float chargeTime, float lockTime)
    {
        currentAction = "Horizontal laser: charging";
        PlayLaserCharge();
        charging = true;
        chargeHits = 0;

        Vector3 aimPoint = player.position;
        bool locked = false;
        float t = 0f;

        // Charge: warning lines follow the player, then lock
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
                    currentAction = "Horizontal laser: aim locked";
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
        StopLaserCharge();

        HideLines(warningLines);
        currentAction = "Horizontal laser: firing";

        PlayEffect(laserFireSFX);

        // Fire
        bool alreadyHit = false;
        float fireT = 0f;
        while (fireT < laserFireTime)
        {
            fireT += Time.deltaTime;
            bool touchedPlayer = DrawLines(beamLines, aimPoint);
            if (touchedPlayer && !alreadyHit)
            {
                alreadyHit = true; // damage only once per shot
                if (playerHealth)
                    playerHealth.DamagePlayer(laserDamage);
            }
            yield return null;
        }

        HideLines(beamLines);
        ResetEyes();
        canTurn = true;
    }

    // ================= VERTICAL LASER =================

    IEnumerator LaserVertical()
    {
        currentAction = "Vertical laser: charging";
        PlayLaserCharge();
        charging = true;
        chargeHits = 0;

        float floorY = FloorY();
        Vector3 lockedDir = FlatDirToPlayer();
        bool locked = false;
        float t = 0f;

        // Charge: head tilts back, the red strip on the floor follows the player, then locks
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
                    currentAction = "Vertical laser: aim locked";
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
        StopLaserCharge();

        currentAction = "Vertical laser: slash";
        PlayEffect(laserFireSFX);

        // Beam origin: right in front of the boss, at eye height
        Vector3 eyeMid = FaceOrigin();
        Vector3 horiz = eyeMid - transform.position;
        horiz.y = 0f;
        Vector3 origin =
            new Vector3(transform.position.x, eyeMid.y, transform.position.z)
            + lockedDir * horiz.magnitude;
        Vector3 right = Vector3.Cross(Vector3.up, lockedDir);

        // Slash: the beam sweeps from the sky down to the floor inside the strip's plane
        bool alreadyHit = false;
        float s = 0f;
        while (s < 1f)
        {
            s += Time.deltaTime / verticalSweepTime;
            float angle = Mathf.Lerp(verticalSweepFrom, verticalSweepTo, Mathf.Clamp01(s));
            Vector3 dir = Quaternion.AngleAxis(angle, right) * lockedDir;

            float len = laserMaxLength;
            if (
                Physics.SphereCast(
                    origin,
                    verticalBeamRadius,
                    dir,
                    out RaycastHit hit,
                    laserMaxLength,
                    laserMask,
                    QueryTriggerInteraction.Ignore
                )
            )
            {
                len = hit.distance;
                if (!alreadyHit && hit.collider.GetComponentInParent<PlayerHealth>())
                {
                    alreadyHit = true;
                    if (playerHealth)
                        playerHealth.DamagePlayer(verticalDamage);
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
        if (
            Physics.Raycast(
                transform.position + Vector3.up,
                Vector3.down,
                out RaycastHit h,
                20f,
                laserMask,
                QueryTriggerInteraction.Ignore
            )
        )
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
        if (eyeL && eyeR)
            return (eyeL.position + eyeR.position) * 0.5f;
        return transform.position + Vector3.up * hoverHeight;
    }

    void DrawFloorLine(Vector3 dir, float floorY)
    {
        if (!floorWarningLine)
            return;
        Vector3 start = new Vector3(transform.position.x, floorY + 0.05f, transform.position.z);
        floorWarningLine.enabled = true;
        floorWarningLine.transform.rotation = Quaternion.Euler(-90f, 0f, 0f); // lies flat on the floor
        floorWarningLine.SetPosition(0, start);
        floorWarningLine.SetPosition(1, start + dir * verticalWarnLength);
    }

    void HideVerticalVisuals()
    {
        if (floorWarningLine)
            floorWarningLine.enabled = false;
        if (verticalBeamLine)
            verticalBeamLine.enabled = false;
    }

    void SetHeadTilt(float angle)
    {
        if (model)
            model.localRotation = Quaternion.Euler(-angle, 0f, 0f);
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

    // ================= STOMP =================

    IEnumerator Stomp()
    {
        currentAction = "Stomp: rising";
        PlayEffect(stompRiseSFX);
        canTurn = false;
        bobbing = false;

        float floorY = FloorY();
        Vector3 center = new Vector3(transform.position.x, floorY + 0.05f, transform.position.z);
        // offset needed so the sphere's center ends up landedHeight above the floor
        float landOffset = landedHeight + (floorY - transform.position.y) - hoverHeight;

        // 1) Rise high, red circle shows the danger zone
        float t = 0f;
        while (t < stompRiseTime)
        {
            t += Time.deltaTime;
            heightOffset = Mathf.Lerp(
                0f,
                stompRiseHeight,
                Mathf.SmoothStep(0f, 1f, t / stompRiseTime)
            );
            ShowCircle(stompWarnLine, center, stompRadius);
            yield return null;
        }

        // 2) Slam down
        currentAction = "Stomp: slamming";
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

        // Âm thanh va chạm phát đúng lúc Boss chạm đất
        PlayEffect(stompImpactSFX, 1.5f);

        // 3) Shockwave spreads out
        currentAction = "Stomp: shockwave";

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
                    if (playerHealth)
                        playerHealth.DamagePlayer(stompDamage);
                    StunPlayer(playerStunTime);
                }
            }
            yield return null;
        }
        HideLine(shockwaveLine);

        // 4) Stunned on the floor: its back is exposed
        currentAction = "Stunned (back exposed)";
        yield return new WaitForSeconds(stompRecoverTime);

        // 5) Float back up
        currentAction = "Rising again";
        t = 0f;
        while (t < stompReturnTime)
        {
            t += Time.deltaTime;
            heightOffset = Mathf.Lerp(
                landOffset,
                0f,
                Mathf.SmoothStep(0f, 1f, t / stompReturnTime)
            );
            yield return null;
        }
        heightOffset = 0f;
        bobbing = true;
        canTurn = true;
    }

    void ShowCircle(LineRenderer lr, Vector3 center, float radius)
    {
        if (!lr)
            return;
        lr.enabled = true;
        lr.transform.rotation = Quaternion.Euler(-90f, 0f, 0f); // lies flat on the floor
        int n = lr.positionCount;
        for (int i = 0; i < n; i++)
        {
            float a = (i / (float)n) * Mathf.PI * 2f;
            lr.SetPosition(i, center + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * radius);
        }
    }

    void HideLine(LineRenderer lr)
    {
        if (lr)
            lr.enabled = false;
    }

    // Freezes the player's movement for a while (they can still look around)
    void StunPlayer(float duration)
    {
        if (playerMove)
            StartCoroutine(StunPlayerRoutine(duration));
    }

    IEnumerator StunPlayerRoutine(float duration)
    {
        playerMove.enabled = false;
        if (playerMove.camAnim)
            playerMove.camAnim.SetBool("isWalking", false);
        yield return new WaitForSeconds(duration);
        playerMove.enabled = true;
    }

    // ================= SHARED =================

    IEnumerator InterruptLaser()
    {
        charging = false;
        currentAction = "Interrupted (stunned)";
        HideLines(warningLines);
        HideVerticalVisuals();
        SetHeadTilt(0f);
        ResetEyes();
        canTurn = true;
        yield return new WaitForSeconds(interruptStunTime);
        StopLaserCharge();
    }

    // Draws a beam from each eye toward the aim point. Returns true if a beam touches the player
    bool DrawLines(LineRenderer[] lines, Vector3 aimPoint)
    {
        bool hitPlayer = false;

        for (int i = 0; i < eyes.Length; i++)
        {
            if (!eyes[i] || lines[i] == null)
                continue;

            Vector3 origin = eyes[i].position;
            Vector3 dir = (aimPoint - origin).normalized;
            float len = laserMaxLength;

            if (
                Physics.SphereCast(
                    origin,
                    laserRadius,
                    dir,
                    out RaycastHit hit,
                    laserMaxLength,
                    laserMask,
                    QueryTriggerInteraction.Ignore
                )
            )
            {
                len = hit.distance;
                if (hit.collider.GetComponentInParent<PlayerHealth>())
                    hitPlayer = true;
            }

            lines[i].enabled = true;
            lines[i].SetPosition(0, origin);
            lines[i].SetPosition(1, origin + dir * len);
        }
        return hitPlayer;
    }

    void HideLines(LineRenderer[] lines)
    {
        if (lines == null)
            return;
        foreach (var lr in lines)
            if (lr)
                lr.enabled = false;
    }

    void SetEyeGlow(float k)
    {
        if (eyeRenderers == null)
            return;
        for (int i = 0; i < eyeRenderers.Length; i++)
        {
            if (!eyeRenderers[i])
                continue;
            if (eyeWeakPoints[i] && eyeWeakPoints[i].IsBroken)
                continue; // a broken eye keeps its gray color
            eyeRenderers[i].material.color = Color.Lerp(eyeBaseColors[i], eyeGlowColor, k);
        }
    }

    void ResetEyes()
    {
        SetEyeGlow(0f);
    }

    // ================= EVENTS FROM BossHealth =================

    void HandleWeakPointHit(WeakPoint wp)
    {
        // Hitting an eye while a laser is charging builds up toward interrupting it
        if (charging && wp.kind == WeakPoint.Kind.Eye)
            chargeHits++;
    }

    void HandlePhase2()
    {
        Debug.Log("[Boss] Phase 2");
        StartCoroutine(TintBody(phase2BodyColor, 1f));
    }

    IEnumerator TintBody(Color target, float duration)
    {
        if (!bodyRenderer || !TryGetColor(bodyRenderer, out Color from))
            yield break;

        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            SetColor(bodyRenderer, Color.Lerp(from, target, t / duration));
            yield return null;
        }
        SetColor(bodyRenderer, target);
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
        if (playerMove)
            playerMove.enabled = true; // release the player if the boss dies mid-stun
        attacking = false;
        chaseVelocity = 0f;
        canTurn = false;
        currentAction = "Defeated";
        StartCoroutine(DeathSequence());
    }

    // The boss drops to the floor, darkens, then the victory screen is shown
    IEnumerator DeathSequence()
    {
        bobbing = false;

        // eyes go dark, body darkens
        if (eyeRenderers != null)
            foreach (var r in eyeRenderers)
                if (r)
                    r.material.color = deadColor;
        SetColor(bodyRenderer, deadColor);

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

        if (ScoreManager.Instance)
            ScoreManager.Instance.TriggerVictory();
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, activationRange);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, stompRadius);
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, chaseStopDistance);
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(
            Application.isPlaying ? homePosition : transform.position,
            leashRadius
        );
    }
}
