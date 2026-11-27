using UnityEngine;

namespace EpicLoot.Adventure
{
    public class SE_BountyTracker : StatusEffect
    {
        private float m_updateTargetTimer = 1.0f;
        private BountyTether m_tether;

        public override void UpdateStatusEffect(float dt)
        {
            base.UpdateStatusEffect(dt);
            if (m_character != Player.m_localPlayer)
            {
                return;
            }

            m_updateTargetTimer += dt;
            if (m_updateTargetTimer <= 1.0f)
            {
                return;
            }

            m_updateTargetTimer = 0.0f;
            var target = BountyTarget.FindClosestInRange(m_character.transform.position, EpicLoot.GetBountyTrackerRange());
            if (target == null)
            {
                RemoveTether();
                return;
            }

            if (m_tether == null)
            {
                m_tether = BountyTether.Create(m_character);
            }

            if (m_tether != null)
            {
                m_tether.SetTarget(target);
            }
        }

        private void RemoveTether()
        {
            if (m_tether != null)
            {
                Object.Destroy(m_tether.gameObject);
                m_tether = null;
            }
        }

        public override void Stop()
        {
            base.Stop();
            RemoveTether();
        }

        public override void OnDestroy()
        {
            base.OnDestroy();
            RemoveTether();
        }
    }

    public class BountyTether : MonoBehaviour
    {
        private const string ConnectionSourcePrefab = "piece_workbench_ext1";

        private Character m_from;
        private Character m_to;

        public static BountyTether Create(Character from)
        {
            var source = ZNetScene.instance.GetPrefab(ConnectionSourcePrefab);
            var extension = source != null ? source.GetComponent<StationExtension>() : null;
            if (extension == null || extension.m_connectionPrefab == null)
            {
                return null;
            }

            var connection = Instantiate(extension.m_connectionPrefab, from.GetCenterPoint(), Quaternion.identity);
            var tether = connection.AddComponent<BountyTether>();
            tether.m_from = from;
            return tether;
        }

        public void SetTarget(Character to)
        {
            if (m_to == to)
            {
                return;
            }

            m_to = to;
            UpdateConnection();
        }

        public void LateUpdate()
        {
            if (m_from == null || m_to == null)
            {
                Destroy(gameObject);
                return;
            }

            UpdateConnection();
        }

        private void UpdateConnection()
        {
            var connectionPoint = m_from.GetCenterPoint();
            var vector = m_to.GetCenterPoint() - connectionPoint;
            var distance = vector.magnitude;
            if (distance < 0.1f)
            {
                return;
            }

            transform.SetPositionAndRotation(connectionPoint, Quaternion.LookRotation(vector / distance));
            transform.localScale = new Vector3(1f, 1f, distance);
        }
    }
}
