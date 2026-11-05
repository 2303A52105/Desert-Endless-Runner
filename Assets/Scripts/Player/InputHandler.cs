using UnityEngine;
using UnityEngine.EventSystems;

namespace DesertRunner
{
    public enum Cmd { Left, Right, Jump, Slide }

    /// <summary>Keyboard (Windows) + swipe (Android touch, mouse drag in editor) input.</summary>
    public class InputHandler : MonoBehaviour
    {
        public event System.Action<Cmd> OnCommand;

        bool tracking;
        Vector2 startPos;
        int trackedFinger = -1;

        void Fire(Cmd c) { if (OnCommand != null) OnCommand(c); }

        static bool OverUI(int fingerId)
        {
            EventSystem es = EventSystem.current;
            if (es == null) return false;
            return fingerId >= 0 ? es.IsPointerOverGameObject(fingerId) : es.IsPointerOverGameObject();
        }

        void Update()
        {
            if (GameManager.Instance == null || GameManager.Instance.State != GameState.Playing)
            {
                tracking = false;
                return;
            }
            ReadKeyboard();
            ReadTouch();
            if (!Application.isMobilePlatform) ReadMouse();
        }

        void ReadKeyboard()
        {
            if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A)) Fire(Cmd.Left);
            if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D)) Fire(Cmd.Right);
            if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.Space)) Fire(Cmd.Jump);
            if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S)) Fire(Cmd.Slide);
        }

        float Threshold { get { return Mathf.Max(40f, Screen.height * 0.06f); } }

        void ReadTouch()
        {
            for (int i = 0; i < Input.touchCount; i++)
            {
                Touch t = Input.GetTouch(i);
                if (t.phase == TouchPhase.Began)
                {
                    if (OverUI(t.fingerId)) continue;
                    if (trackedFinger < 0) { trackedFinger = t.fingerId; startPos = t.position; tracking = true; }
                }
                else if (t.fingerId == trackedFinger)
                {
                    if (t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled)
                    {
                        trackedFinger = -1; tracking = false;
                    }
                    else if (tracking)
                    {
                        Evaluate(t.position);
                    }
                }
            }
        }

        void ReadMouse()
        {
            if (Input.GetMouseButtonDown(0))
            {
                if (OverUI(-1)) return;
                startPos = Input.mousePosition; tracking = true;
            }
            else if (Input.GetMouseButton(0) && tracking) Evaluate(Input.mousePosition);
            else if (Input.GetMouseButtonUp(0)) tracking = false;
        }

        void Evaluate(Vector2 pos)
        {
            Vector2 d = pos - startPos;
            if (d.magnitude < Threshold) return;
            if (Mathf.Abs(d.x) > Mathf.Abs(d.y)) Fire(d.x > 0f ? Cmd.Right : Cmd.Left);
            else Fire(d.y > 0f ? Cmd.Jump : Cmd.Slide);
            tracking = false;   // one swipe per touch
            if (trackedFinger >= 0) { /* keep finger id until it lifts */ }
        }
    }
}
