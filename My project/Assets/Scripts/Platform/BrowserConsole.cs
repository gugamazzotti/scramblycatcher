using System.Runtime.InteropServices;
using UnityEngine;

namespace Scrambly.Catch
{
    /// <summary>
    /// Writes to the Unity log and, in a WebGL player, the hosting page's JavaScript console.
    /// </summary>
    /// <remarks>
    /// The DllImport targets <c>PlayableLog</c> in <c>Assets/Plugins/WebGL/BrowserConsole.jslib</c>.
    /// Editor and standalone builds only use Debug.Log so reviewers still see CTA clicks in the Console window.
    /// Keep messages short; playable-ad QA greps for "CTA clicked".
    /// </remarks>
    public static class BrowserConsole
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")]
        private static extern void PlayableLog(string message);
#endif

        /// <summary>
        /// Logs a message locally and, on WebGL, in the hosting page console.
        /// </summary>
        /// <param name="message">Text to print. Null/empty still goes to Debug.Log.</param>
        public static void Log(string message)
        {
            Debug.Log(message);
#if UNITY_WEBGL && !UNITY_EDITOR
            PlayableLog(message);
#endif
        }
    }
}
