using System.Collections.Generic;
using UnityEngine;

namespace ShiftShift
{
    /// <summary>
    /// Builds a straight track along +Z from modular piece prefabs: the pieces in <see cref="sequence"/> in order,
    /// then <see cref="fillPiece"/> repeated until <see cref="lengthMetres"/> is reached.
    /// Built in the editor (context menu "Build Track", or the greybox setup) so obstacles can be placed against it;
    /// the built pieces are saved in the scene as children.
    /// </summary>
    public sealed class TrackBuilder : MonoBehaviour
    {
        [Min(10f)] public float lengthMetres = 200f;
        public List<TrackPiece> sequence = new List<TrackPiece>();
        public TrackPiece fillPiece;

        public float LengthMetres => lengthMetres;

        [ContextMenu("Build Track")]
        public void Build()
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                GameObject child = transform.GetChild(i).gameObject;
                if (Application.isPlaying) Destroy(child); else DestroyImmediate(child);
            }

            if (fillPiece == null)
            {
                Debug.LogError("TrackBuilder: no fill piece assigned.", this);
                return;
            }

            float z = 0f;
            int index = 0;
            int safety = 0;
            while (z < lengthMetres && safety++ < 1000)
            {
                TrackPiece prefab = index < sequence.Count && sequence[index] != null ? sequence[index] : fillPiece;
                index++;
                TrackPiece piece = Instantiate(prefab, transform);
                piece.name = $"{prefab.name} @{z:0}m";
                piece.transform.localPosition = new Vector3(0f, 0f, z);
                z += piece.length;
            }
        }
    }
}
