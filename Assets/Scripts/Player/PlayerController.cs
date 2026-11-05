using UnityEngine;

namespace DesertRunner
{
    /// <summary>Forward running, 3-lane movement, jump, slide, collision handling.</summary>
    public class PlayerController : MonoBehaviour
    {
        public const float Gravity = 32f;
        public const float JumpVelocity = 10.8f;
        public const float FastFallVelocity = -22f;
        public const float LaneSwitchSpeed = 16f;
        public const float SlideDuration = 0.8f;

        public static PlayerController Instance { get; private set; }

        public bool IsGrounded { get { return grounded; } }
        public bool IsSliding { get { return sliding; } }
        public int Lane { get { return lane; } }

        PlayerVisual visual;
        InputHandler input;
        BoxCollider box;
        int lane = 1;
        float x, y, vy;
        bool grounded = true, sliding, slideQueued, dead;
        float slideTimer, jumpBufferUntil, invulnerableUntil;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            box = GetComponent<BoxCollider>();
            visual = GetComponent<PlayerVisual>();
            input = GetComponent<InputHandler>();
            SetColliderSliding(false);
        }

        void Start()
        {
            if (input != null) input.OnCommand += HandleCommand;
        }

        void OnDestroy()
        {
            if (input != null) input.OnCommand -= HandleCommand;
            if (Instance == this) Instance = null;
        }

        public bool IsInvulnerable
        {
            get
            {
                PowerUpManager pm = PowerUpManager.Instance;
                return Time.time < invulnerableUntil || (pm != null && pm.BoostActive);
            }
        }

        public void GrantInvulnerability(float seconds)
        {
            invulnerableUntil = Mathf.Max(invulnerableUntil, Time.time + seconds);
        }

        void SetColliderSliding(bool s)
        {
            if (box == null) return;
            box.size = s ? new Vector3(0.6f, 0.7f, 0.5f) : new Vector3(0.6f, 1.6f, 0.5f);
            box.center = s ? new Vector3(0f, 0.35f, 0f) : new Vector3(0f, 0.8f, 0f);
        }

        void HandleCommand(Cmd c)
        {
            GameManager gm = GameManager.Instance;
            if (gm == null || gm.State != GameState.Playing || dead) return;
            switch (c)
            {
                case Cmd.Left: lane = Mathf.Max(0, lane - 1); break;
                case Cmd.Right: lane = Mathf.Min(2, lane + 1); break;
                case Cmd.Jump:
                    if (grounded) Jump(); else jumpBufferUntil = Time.time + 0.12f;
                    break;
                case Cmd.Slide:
                    if (grounded) StartSlide();
                    else { vy = FastFallVelocity; slideQueued = true; }
                    break;
            }
        }

        void Jump()
        {
            if (sliding) EndSlide();
            vy = JumpVelocity;
            grounded = false;
            slideQueued = false;
            AudioManager.Play(Sfx.Jump, 0.7f);
        }

        void StartSlide()
        {
            sliding = true;
            slideTimer = SlideDuration;
            SetColliderSliding(true);
            AudioManager.Play(Sfx.Slide, 0.6f);
        }

        void EndSlide()
        {
            sliding = false;
            SetColliderSliding(false);
        }

        void Update()
        {
            GameManager gm = GameManager.Instance;
            if (gm == null) return;
            float targetX = (lane - 1) * PropFactory.LaneWidth;
            if (visual != null)
            {
                visual.SetSpeed(gm.CurrentSpeed);
                visual.SetSteer(targetX - x);
                visual.SetBlink(!dead && Time.time < invulnerableUntil && gm.State == GameState.Playing && (PowerUpManager.Instance == null || !PowerUpManager.Instance.BoostActive));
                visual.SetState(dead ? AnimState.Dead : (!grounded ? AnimState.Jump : (sliding ? AnimState.Slide : AnimState.Run)));
            }
            if (gm.State != GameState.Playing || dead) return;

            float dt = Time.deltaTime;
            Vector3 p = transform.position;
            p.z += gm.CurrentSpeed * dt;
            x = Mathf.MoveTowards(x, targetX, LaneSwitchSpeed * dt);
            p.x = x;
            if (!grounded)
            {
                vy -= Gravity * dt;
                y += vy * dt;
                if (y <= 0f)
                {
                    y = 0f; vy = 0f; grounded = true;
                    AudioManager.Play(Sfx.Land, 0.5f);
                    if (slideQueued) { slideQueued = false; StartSlide(); }
                    else if (Time.time < jumpBufferUntil) Jump();
                }
            }
            p.y = y;
            if (sliding)
            {
                slideTimer -= dt;
                if (slideTimer <= 0f) EndSlide();
            }
            transform.position = p;
        }

        void OnTriggerEnter(Collider other)
        {
            GameManager gm = GameManager.Instance;
            if (gm == null || gm.State != GameState.Playing || dead) return;
            Obstacle ob = other.GetComponent<Obstacle>();
            if (ob != null) { HitObstacle(ob); return; }
            Collectible c = other.GetComponent<Collectible>();
            if (c != null) c.Collect();
        }

        void HitObstacle(Obstacle ob)
        {
            if (IsInvulnerable) { ob.Smash(); return; }
            PowerUpManager pm = PowerUpManager.Instance;
            if (pm != null && pm.ConsumeShield())
            {
                GrantInvulnerability(1.2f);
                AudioManager.Play(Sfx.ShieldBreak);
                PropFactory.Fx("fx_powerup", transform.position + Vector3.up, PowerUpManager.ColorOf(PowerUpType.Shield));
                ob.Smash();
                return;
            }
            Die();
        }

        void Die()
        {
            dead = true;
            AudioManager.Play(Sfx.Hit);
            PropFactory.Fx("fx_hit", transform.position + Vector3.up, new Color(1f, 0.7f, 0.3f));
            FollowCamera fc = Camera.main != null ? Camera.main.GetComponent<FollowCamera>() : null;
            if (fc != null) fc.Shake(0.4f, 0.35f);
            if (sliding) EndSlide();
            if (GameManager.Instance != null) GameManager.Instance.PlayerDied();
        }

        public void Revive()
        {
            dead = false;
            sliding = false; slideQueued = false;
            SetColliderSliding(false);
            y = 0f; vy = 0f; grounded = true;
            Vector3 p = transform.position; p.y = 0f; transform.position = p;
            GrantInvulnerability(3f);
        }
    }
}
