using System.Collections.Generic;
using UnityEngine;

namespace DesertRunner
{
    public enum ObstacleKind { Low, Tall, Overhead }   // Low = jump, Tall = change lane, Overhead = slide

    public class Obstacle : MonoBehaviour
    {
        public static readonly List<Obstacle> Active = new List<Obstacle>();
        public ObstacleKind Kind;

        void OnEnable() { Active.Add(this); }
        void OnDisable() { Active.Remove(this); }

        /// <summary>Destroy with a debris burst (shield / boost / revive).</summary>
        public void Smash()
        {
            PropFactory.Fx("fx_debris", transform.position + Vector3.up * 0.8f, new Color(0.75f, 0.55f, 0.32f));
            Remove();
        }

        public void Remove()
        {
            PooledObject po = GetComponent<PooledObject>();
            if (po != null) po.Release(); else gameObject.SetActive(false);
        }

        public static void ClearRange(float zMin, float zMax)
        {
            for (int i = Active.Count - 1; i >= 0; i--)
            {
                Obstacle o = Active[i];
                if (o == null) continue;
                float z = o.transform.position.z;
                if (z >= zMin && z <= zMax) o.Smash();
            }
        }
    }
}
