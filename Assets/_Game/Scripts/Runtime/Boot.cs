using UnityEngine;
using UnityEngine.SceneManagement;

namespace ShiftShift
{
    /// <summary>First scene in the build: loads the next one. Points at Run until the Menu exists (days 21-22).</summary>
    public sealed class Boot : MonoBehaviour
    {
        [SerializeField] private string firstScene = "Run";

        private void Start() => SceneManager.LoadScene(firstScene);
    }
}
