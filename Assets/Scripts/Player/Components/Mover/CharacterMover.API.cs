using UnityEngine;

namespace Player {
    public partial class CharacterMover
    {
        public void Move(Vector3 velocity)
        {
            _inputVelocity = velocity;
        }
    }
}