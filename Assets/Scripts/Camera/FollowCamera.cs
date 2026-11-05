using UnityEngine;

namespace DesertRunner
{
    /// <summary>Smooth third-person camera behind the player (or slow cinematic sway in the menu).</summary>
    public class FollowCamera : MonoBehaviour
    {
        public Transform Target;
        public bool MenuMode;

        Camera cam;
        float x, shakeT, shakeDur = 1f, shakeMag;

        void Awake() { cam = GetComponent<Camera>(); }

        public void Shake(float duration, float magnitude)
        {
            shakeT = duration; shakeDur = Mathf.Max(0.01f, duration); shakeMag = magnitude;
        }

        void LateUpdate()
        {
            if (Target == null) return;
            if (MenuMode)
            {
                float t = Time.time * 0.25f;
                transform.position = new Vector3(3.2f + Mathf.Sin(t) * 1.2f, 1.7f + Mathf.Sin(t * 0.7f) * 0.15f, -5.2f);
                transform.rotation = Quaternion.LookRotation(Target.position + new Vector3(0f, 1.1f, 3f) - transform.position);
                return;
            }
            float dt = Time.deltaTime;
            Vector3 tp = Target.position;
            x = Mathf.Lerp(x, tp.x * 0.6f, 1f - Mathf.Exp(-6f * dt));
            Vector3 pos = new Vector3(x, 4.3f + tp.y * 0.35f, tp.z - 8f);
            if (shakeT > 0f)
            {
                shakeT -= dt;
                pos += Random.insideUnitSphere * shakeMag * Mathf.Clamp01(shakeT / shakeDur);
            }
            transform.position = pos;
            Vector3 look = new Vector3(tp.x * 0.35f, 1.4f + tp.y * 0.2f, tp.z + 8f);
            transform.rotation = Quaternion.LookRotation(look - pos);
            if (cam != null)
            {
                GameManager gm = GameManager.Instance;
                float speed = gm != null ? gm.CurrentSpeed : 14f;
                bool boost = PowerUpManager.Instance != null && PowerUpManager.Instance.BoostActive;
                float targetFov = 58f + speed * 0.25f + (boost ? 8f : 0f);
                cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, targetFov, 1f - Mathf.Exp(-3f * dt));
            }
        }
    }
}
