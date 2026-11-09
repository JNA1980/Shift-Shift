using UnityEngine;

namespace ShiftShift
{
    /// <summary>One modular track piece. Its origin is the start of the piece; it extends <see cref="length"/> metres along +Z.</summary>
    public sealed class TrackPiece : MonoBehaviour
    {
        [Min(0.5f)] public float length = 10f;
    }
}
