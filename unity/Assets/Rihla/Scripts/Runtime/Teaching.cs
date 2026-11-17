using System.Linq;
using UnityEngine;

namespace Rihla
{
    /// <summary>
    /// Chapter 1's teaching on the Jericho road, as in the browser prototype (src/main.ts): the HUD, a banner on
    /// reaching each place and on crossing sea level, the first page of the Rihla waiting at Khan al-Hatruri,
    /// the story of each place through time nearby, the opening card, and saving where the car is.
    /// </summary>
    public class Teaching : MonoBehaviour
    {
        public CarController car;
        public PlayerSwitch player;
        public RoadPath road;
        public Hud hud;
        /// <summary>The golden scroll at Khan al-Hatruri, made by the scene builder.</summary>
        public Transform pageMarker;
        /// <summary>Game heights are compressed; multiply to get true metres above sea level (src/world/warp.ts).</summary>
        public float verticalScale = 2.5f;

        RihlaUI ui; string pageId, pagePlace, lastPlace = "", historySite = "";
        float lastElev = float.NaN, saveTimer; int roadHint = -1;

        void Start()
        {
            ui = RihlaUI.Instance;
            var page = TeachingPages.FirstPage;
            pageId = (string)page?["id"]; pagePlace = (string)page?["place"];
            ui.OnJournal = () => { if (Found) TeachingPages.ShowPage(page); };
            ui.OnHistory = () => { if (historySite.Length > 0) TeachingPages.ShowHistory(historySite); };
            RestorePosition();
            if (!SaveData.Current.introSeen && !car.autopilot)
            {
                void Intro() => TeachingPages.ShowIntro(() => { SaveData.Current.introSeen = true; SaveData.Save(); });
                if (SaveData.Current.heroChosen) Intro(); else TeachingPages.ChooseHero(Intro);
            }
        }

        bool Found => pageId != null && SaveData.Current.HasPage(pageId);

        void Update()
        {
            if (ui == null) return;
            bool onFoot = player && player.OnFoot;
            Transform who = onFoot ? player.walker.transform : car.transform;
            Vector3 p = who.position;

            float kmh = onFoot ? player.walker.Speed * 3.6f : car.SpeedKmh;
            float elev = p.y * verticalScale;
            string elevText = Lang.Num(Mathf.Abs(elev)) + " " + Lang.T(elev >= 0f ? "above" : "below");

            // Where along the road we are, searched near the last answer, or over the whole road after a jump.
            int idx = road.Nearest(p, roadHint, 120);
            if (roadHint < 0 || Flat(road.points[idx], p) > 60f) idx = road.Nearest(p);
            roadHint = idx;
            float along = road.distances[idx];
            var pagePl = road.places.FirstOrDefault(x => x.id == pagePlace);
            float toPage = pageMarker ? Flat(pageMarker.position, p) : float.MaxValue;
            string trip;
            if (!Found) trip = toPage < 14f ? Lang.T("pageNear") : Lang.T("objective") + Lang.Sep + Lang.Num(Mathf.Abs(pagePl.roadDist - along) / 1000f, 1) + " " + Lang.T("u.km");
            else
            {
                float left = road.Length - along;
                trip = left > 30f ? Lang.Num(left / 1000f, 1) + " " + Lang.T("u.km") + " " + Lang.T("toAriha") : "";
            }

            // The page is picked up by stopping beside it, or by walking up to it.
            if (!Found && pageMarker && !car.autopilot && (onFoot ? toPage < 2.5f : toPage < 11f && kmh < 9f))
            {
                SaveData.Current.AddPage(pageId);
                ui.SetJournalVisible(true);
                TeachingPages.ShowPage(TeachingPages.FirstPage, () => ui.Banner(Lang.T("pageFound"), Lang.Pick(TeachingPages.FirstPage["title"])));
            }
            if (pageMarker) pageMarker.gameObject.SetActive(!Found);
            ui.SetJournalVisible(Found);

            var near = road.NearestPlace(p, out float metres);
            string placeMain = "", placeOther = "";
            if (near.HasValue)
            {
                placeMain = Lang.Arabic ? near.Value.nameAr : near.Value.nameEn;
                placeOther = Lang.Arabic ? near.Value.nameEn : near.Value.nameAr;
                if (metres < 90f && near.Value.id != lastPlace && near.Value.kind != "nature")
                {
                    lastPlace = near.Value.id;
                    ui.Banner(placeMain, placeOther);
                }
            }

            // Crossing sea level on the way down.
            if (!float.IsNaN(lastElev) && lastElev >= 0f && elev < 0f && elev > -5f)
                ui.Banner(Lang.T("seaLevel") + Lang.Sep + Lang.Num(0) + " " + Lang.T("u.m"), Lang.T("seaLevelSub"), 8f);
            lastElev = elev;

            // The History button: the nearest place within 600 m whose story through time has been written.
            historySite = ""; float best = 600f;
            foreach (var pl in road.places)
            {
                if (!TeachingPages.HasHistory(pl.id)) continue;
                float d = Flat(pl.position, p);
                if (d < best) { best = d; historySite = pl.id; }
            }
            ui.SetHistoryVisible(historySite.Length > 0);

            string hint = onFoot ? Lang.T("u.onFoot") : car.autopilot ? Lang.T("u.jiddoDrives") : Lang.T("u.driving");
            // Near Bab al-Asbat, stopped or on foot: G walks into the Old City (the E2 walk, at true scale).
            var gate = road.places.FirstOrDefault(x => x.id == "bab-al-asbat");
            bool canWalkIn = gate.id != null && Flat(gate.position, p) < 120f && (onFoot || kmh < 3f) && Application.CanStreamedLevelBeLoaded("E2_AlQuds_Walk");
            if (canWalkIn)
            {
                hint = Lang.T("w.toWalk") + "\n" + hint;
                var kb = UnityEngine.InputSystem.Keyboard.current;
                if (kb != null && kb.gKey.wasPressedThisFrame && Time.timeSinceLevelLoad > 1f && !ui.ModalOpen)
                { SavePosition(); UnityEngine.SceneManagement.SceneManager.LoadScene("E2_AlQuds_Walk"); return; }
            }
            ui.SetHud(Lang.Num(kmh) + " " + Lang.T("kmh"), elevText, trip, placeMain, placeOther, hint + "\n" + Lang.T("u.toFly"));
            if (hud) ui.SetFps(hud.Fps);

            if ((saveTimer += Time.unscaledDeltaTime) > 10f) { saveTimer = 0f; SavePosition(); }
        }

        static float Flat(Vector3 a, Vector3 b) => Vector2.Distance(new Vector2(a.x, a.z), new Vector2(b.x, b.z));

        void SavePosition()
        {
            if (player && player.OnFoot) return;
            var s = SaveData.Current; var t = car.transform;
            s.hasPosition = true; s.x = t.position.x; s.y = t.position.y; s.z = t.position.z; s.yaw = t.eulerAngles.y;
            SaveData.Save();
        }

        void RestorePosition()
        {
            var s = SaveData.Current;
            if (!s.hasPosition || car.autopilot) return;
            var pos = new Vector3(s.x, s.y + 0.6f, s.z);
            car.transform.SetPositionAndRotation(pos, Quaternion.Euler(0f, s.yaw, 0f));
            var rb = car.GetComponent<Rigidbody>(); rb.linearVelocity = Vector3.zero; rb.angularVelocity = Vector3.zero;
            Physics.SyncTransforms();
            var cam = player ? player.cam : null; if (cam) cam.Snap();
        }

        void OnApplicationQuit() => SavePosition();
    }
}
