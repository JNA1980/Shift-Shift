using UnityEngine;
using UnityEngine.SceneManagement;

namespace ShiftShift
{
    /// <summary>First scene in the build: loads the menu.</summary>
    public sealed class Boot : MonoBehaviour
    {
        [SerializeField] private string firstScene = "Menu";

        private void Start() => SceneManager.LoadScene(firstScene);
    }
}
