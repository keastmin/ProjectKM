using UnityEngine;

namespace Player
{
    public partial class CharacterMover
    {
        private void SetVelocity(Vector3 velocity)
        {
            _rigidbody.linearVelocity = velocity;
        }

        private void ClearUpdate()
        {
            _inputVelocity = Vector3.zero;
        }
    }
}