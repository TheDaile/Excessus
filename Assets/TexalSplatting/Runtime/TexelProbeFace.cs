using UnityEngine;

namespace Excessus.TexelSplatting
{
    [DisallowMultipleComponent]
    [AddComponentMenu("")]
    public sealed class TexelProbeFace : MonoBehaviour
    {
        public TexelSplattingController Owner { get; private set; }
        public int FaceIndex { get; private set; }

        internal void Initialize(TexelSplattingController owner, int faceIndex)
        {
            Owner = owner;
            FaceIndex = faceIndex;
        }
    }
}
