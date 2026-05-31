using UnityEngine;
using UnityEngine.UI;

namespace Fitzmark.BDRSim.World
{
    /// <summary>Cycles a UI Image through sprite frames at a fixed rate — used for the overworld's
    /// animated LPC water tiles. A small shared phase offset keeps tiles from shimmering in lockstep.</summary>
    public class WaterAnimator : MonoBehaviour
    {
        public Sprite[] frames;
        public float fps = 4f;
        public float phase;

        private Image _img;

        private void Awake() => _img = GetComponent<Image>();

        private void Update()
        {
            if (_img == null || frames == null || frames.Length == 0) return;
            int i = (int)((Time.unscaledTime + phase) * fps) % frames.Length;
            _img.sprite = frames[i];
        }
    }
}
