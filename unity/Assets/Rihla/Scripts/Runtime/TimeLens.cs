using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Rendering;

namespace Rihla
{
    /// <summary>
    /// The Time Lens: the same street seen as it was ("then") and as it is today ("now"). "Then" is shown with an
    /// old-photograph look (a post-processing volume) and with the things that were different, such as the Dome of
    /// the Rock's dark lead dome of about 1910. Which eras these are comes from content/walk.json and history.json.
    /// The first reveal turns "then" into "now" slowly; afterwards T switches at any time.
    /// </summary>
    public class TimeLens : MonoBehaviour
    {
        /// <summary>The old-photograph look, at weight 1 for "then" and 0 for "now".</summary>
        public Volume thenLook;
        /// <summary>Renderers whose colour differs between the eras (the Dome of the Rock's dome), blended smoothly.</summary>
        public Renderer[] changing;
        public Color[] thenColors, nowColors;
        public float[] thenMetal, nowMetal;
        MaterialPropertyBlock block;
        /// <summary>Things that exist only in one era.</summary>
        public GameObject[] onlyThen, onlyNow;
        public float revealSeconds = 5f;

        public bool Then { get; private set; } = true;
        public bool Revealed { get; private set; }
        float weight = 1f, target = 1f, speed;

        void Start() => Apply(true);

        /// <summary>The slow first reveal from "then" to "now".</summary>
        public void Reveal()
        {
            Revealed = true;
            Soundscape.Reveal();
            Set(false, revealSeconds);
        }

        public void Toggle() { if (Revealed) Set(!Then, 1.2f); }

        void Set(bool then, float seconds)
        {
            Then = then; target = then ? 1f : 0f; speed = 1f / Mathf.Max(0.05f, seconds);
            var era = Era(then);
            if (RihlaUI.Instance && era != null)
                RihlaUI.Instance.Banner((then ? Lang.T("w.then") : Lang.T("w.now")) + Lang.Sep + Lang.Pick(era["when"]), Lang.Pick(era["text"]), seconds + 4f);
        }

        /// <summary>The "then" or "now" era of the lens's place, from history.json.</summary>
        public static JToken Era(bool then)
        {
            var c = ContentLibrary.Instance; var lens = c?.Walk?["timeLens"];
            if (lens == null) return null;
            int i = (int)(then ? lens["thenEra"] : lens["nowEra"]);
            return c.History?["sites"]?[(string)lens["site"]]?["eras"]?[i];
        }

        public string Label => Lang.T(Then ? "w.then" : "w.now") + (Era(Then) != null ? Lang.Sep + Lang.Pick(Era(Then)["when"]) : "");

        void Update()
        {
            if (Mathf.Approximately(weight, target)) return;
            weight = Mathf.MoveTowards(weight, target, speed * Time.deltaTime);
            Apply(false);
        }

        void Apply(bool force)
        {
            if (thenLook) thenLook.weight = weight;
            bool showThen = weight > 0.5f;
            block ??= new MaterialPropertyBlock();
            float w = Mathf.SmoothStep(0f, 1f, weight);
            for (int i = 0; i < changing.Length; i++)
            {
                if (!changing[i]) continue;
                changing[i].GetPropertyBlock(block);
                block.SetColor("_BaseColor", Color.Lerp(nowColors[i], thenColors[i], w));
                block.SetFloat("_Metallic", Mathf.Lerp(nowMetal[i], thenMetal[i], w));
                changing[i].SetPropertyBlock(block);
            }
            foreach (var g in onlyThen) if (g) g.SetActive(showThen);
            foreach (var g in onlyNow) if (g) g.SetActive(!showThen);
        }
    }
}
