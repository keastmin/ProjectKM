using System.Collections.Generic;
using UnityEngine;

namespace Player
{
    public class HitController : MonoBehaviour
    {
        [SerializeField] private LayerMask _hitLayer;
        [SerializeField] private HitboxID[] _hitboxIDs;

        public Dictionary<string, BoxCollider[]> Hitboxes;
        public LayerMask HitLayer => _hitLayer;

        private void Awake()
        {
            Hitboxes = new Dictionary<string, BoxCollider[]>();
            foreach(var h in _hitboxIDs)
            {
                Hitboxes.Add(h.HitboxName, h.HitboxColliders);
            }
        }

        public bool TryGetHitboxes(string hitboxId, out BoxCollider[] hitboxes)
        {
            if (string.IsNullOrWhiteSpace(hitboxId))
            {
                hitboxes = null;
                return false;
            }

            return Hitboxes.TryGetValue(hitboxId, out hitboxes);
        }
    }
}
