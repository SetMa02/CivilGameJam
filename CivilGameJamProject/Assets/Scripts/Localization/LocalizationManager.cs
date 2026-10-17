using System;
using System.Collections.Generic;
using UnityEngine;
using CivilGame.Threats;
using CivilGame.DayCycle;

namespace CivilGame.Localization
{
    public enum GameLanguage
    {
        English = 0,
        Turkish = 1
    }

    public static class LocalizationManager
    {
        private const string LanguagePrefKey = "SelectedLanguage";
        private static GameLanguage _currentLanguage = GameLanguage.English;
        private static bool _isLoaded = false;

        public static event Action<GameLanguage> OnLanguageChanged;

        public static GameLanguage CurrentLanguage
        {
            get
            {
                if (!_isLoaded) LoadLanguage();
                return _currentLanguage;
            }
            set
            {
                if (_currentLanguage != value || !_isLoaded)
                {
                    _currentLanguage = value;
                    _isLoaded = true;
                    PlayerPrefs.SetString(LanguagePrefKey, _currentLanguage.ToString());
                    PlayerPrefs.Save();
                    OnLanguageChanged?.Invoke(_currentLanguage);
                }
            }
        }

        private static void LoadLanguage()
        {
            string saved = PlayerPrefs.GetString(LanguagePrefKey, GameLanguage.English.ToString());
            if (Enum.TryParse(saved, out GameLanguage lang))
            {
                _currentLanguage = lang;
            }
            else
            {
                _currentLanguage = GameLanguage.English;
            }
            _isLoaded = true;
        }

        private static readonly Dictionary<string, (string en, string tr)> Translations = new Dictionary<string, (string en, string tr)>
        {
            // Main Menu
            { "menu.title", ("CIVIL GAME", "CIVIL GAME") },
            { "menu.subtitle", ("MAIN MENU", "ANA MENÜ") },
            { "menu.new_game", ("New Game", "Yeni Oyun") },
            { "menu.settings", ("Settings", "Ayarlar") },
            { "menu.exit", ("Exit", "Çıkış") },
            { "menu.version", ("v0.1.0 • Dark Theme", "v0.1.0 • Koyu Tema") },

            // Settings Modal
            { "settings.title", ("SETTINGS", "AYARLAR") },
            { "settings.volume", ("Master Volume", "Ana Ses") },
            { "settings.fullscreen", ("Fullscreen Mode", "Tam Ekran Modu") },
            { "settings.language", ("Language", "Dil") },
            { "settings.back", ("Back", "Geri") },
            { "settings.lang_en", ("English", "English") },
            { "settings.lang_tr", ("Türkçe", "Türkçe") },

            // HUD & General Game
            { "hud.day", ("DAY {0}", "GÜN {0}") },
            { "gameover.title", ("ALL ZONES DESTROYED\n<size=22><color=#f87171>The island has fallen. You failed to protect the lighthouse.</color></size>",
                                 "TÜM BÖLGELER YOK EDİLDİ\n<size=22><color=#f87171>Ada yok oldu. Deniz fenerini koruyamadınız.</color></size>") },
            { "gameover.restart", ("Restart", "Yeniden Başlat") },

            // Squad Names
            { "squad.irrigators", ("Irrigators", "Sulama Ekipleri") },
            { "squad.lumberjacks", ("Lumberjacks", "Ormancılar") },
            { "squad.firefighters", ("Firefighters", "İtfaiyeciler") },
            { "squad.rescue_heli", ("Rescue Helicopter", "Kurtarma Helikopteri") },
            { "squad.climbers", ("Mountain Climbers", "Dağcılar") },
            { "squad.divers", ("Divers", "Dalgıçlar") },

            // Zone Names
            { "zone.sea", ("Sea", "Deniz") },
            { "zone.mountains", ("Mountains", "Dağlar") },
            { "zone.forest", ("Forest", "Orman") },
            { "zone.field", ("Field", "Ova") },

            // Phone UI
            { "phone.title", ("HEADQUARTERS COMM\n<size=15><color=#94a3b8>WARDEN PHONE</color></size>",
                              "MERKEZ İLETİŞİM\n<size=15><color=#94a3b8>FENER BEKÇİSİ TELEFONU</color></size>") },
            { "phone.close_hint", ("[[ Click outside to hang up ]]", "[[ Kapatmak için dışarı tıklayın ]]") },
            { "phone.report_btn", ("REPORT", "RAPOR") },
            { "phone.shop_btn", ("SHOP", "MAĞAZA") },
            { "phone.back", ("< Back", "< Geri") },
            { "phone.back_to_shop", ("< Back to Shop", "< Mağazaya Dön") },
            { "phone.change_squad", ("< Change Squad", "< Ekibi Değiştir") },
            { "phone.squad_prompt", ("Select squad to dispatch:", "Gönderilecek ekibi seçin:") },
            { "phone.zone_prompt", ("Select destination zone:", "Hedef bölgeyi seçin:") },
            { "phone.limit_reached", ("<color=#94a3b8>[Dispatch limit ({0}/{0}) reached. Wait for tomorrow or open SHOP.]</color>",
                                      "<color=#94a3b8>[Çağrı limiti ({0}/{0}) tükendi. Yarını bekleyin veya MAĞAZAYI açın.]</color>") },
            { "phone.limit_reached_alert", ("<color=#f87171>Headquarters: Daily dispatch limit reached! Wait for tomorrow or use SHOP.</color>",
                                            "<color=#f87171>Merkez bildiriyor: Günlük çağrı limiti doldu! Yarını bekleyin veya MAĞAZAYI kullanın.</color>") },

            // Phone Shop Items & Prompts
            { "phone.shop_second_call", ("Second report call — $200", "İkinci ekip çağrısı — $200") },
            { "phone.shop_restore_area", ("Restore a destroyed area — $250", "Yok edilen bölgeyi onar — $250") },
            { "phone.shop_pause_growth", ("Pause threat growth (1 day) — $350", "Tehdit artışını durdur (1 gün) — $350") },
            { "phone.shop_rollback", ("Roll back threat level by 1 — $500", "Tehdit seviyesini 1 kademe düşür — $500") },
            { "phone.shop_prompt_restore", ("Select DESTROYED zone to restore ($250):", "Onarılacak YOK EDİLMİŞ bölgeyi seçin ($250):") },
            { "phone.shop_prompt_pause", ("Select THREATENED zone to pause growth for 1 day ($350):", "Gelişimi 1 gün durdurulacak bölgeyi seçin ($350):") },
            { "phone.shop_prompt_rollback", ("Select THREATENED zone to roll back by 1 ($500):", "Tehdidi 1 kademe düşürülecek bölgeyi seçin ($500):") },

            // Phone Shop Feedback
            { "phone.shop_success_call", ("<color=#4ade80>Success! Extra dispatch unlocked for today.</color>",
                                          "<color=#4ade80>Başarılı! Bugün için ek ekip çağrısı açıldı.</color>") },
            { "phone.shop_insufficient_funds", ("<color=#f87171>Insufficient funds (${0})!</color>",
                                                "<color=#f87171>Yetersiz bakiye (${0})!</color>") },
            { "phone.shop_no_destroyed", ("<color=#94a3b8>No destroyed zones to restore.</color>",
                                          "<color=#94a3b8>Onarılacak yıkılmış bölge yok.</color>") },
            { "phone.shop_no_threatened", ("<color=#94a3b8>No zones currently under threat.</color>",
                                           "<color=#94a3b8>Şu anda tehdit altında bölge yok.</color>") },
            { "phone.shop_success_restore", ("<color=#4ade80>Success! Zone {0} fully restored!</color>",
                                             "<color=#4ade80>Başarılı! {0} bölgesi tamamen onarıldı!</color>") },
            { "phone.shop_success_pause", ("<color=#4ade80>Success! Threat growth in {0} paused for 1 day!</color>",
                                           "<color=#4ade80>Başarılı! {0} bölgesindeki tehdit 1 gün durduruldu!</color>") },
            { "phone.shop_success_rollback", ("<color=#4ade80>Success! Threat level in {0} reduced by 1!</color>",
                                              "<color=#4ade80>Başarılı! {0} bölgesindeki tehdit 1 kademe düşürüldü!</color>") },

            // Book UI
            { "book.close_hint", ("[[ Click outside to close ]]", "[[ Kapatmak için dışarı tıklayın ]]") },

            // Zone & Status Labels
            { "status.destroyed", ("<color=#ef4444>[DESTROYED]</color>", "<color=#ef4444>[YOK EDİLDİ]</color>") },
            { "status.calm", ("<color=#94a3b8>Calm</color>", "<color=#94a3b8>Sakin</color>") },
            { "status.tag_destroyed", ("[DESTROYED]", "[YOK EDİLDİ]") },
            { "status.stage_prefix", ("Stage {0}", "{0}. Aşama") },

            // Night Reports
            { "dispatch.accepted", ("Request accepted by Headquarters!\nSquad <b>{0}</b> dispatched to sector <b>{1}</b> (Dispatched today: {2}/{3}).\nOperation results will be reported tomorrow morning.",
                                    "Talep Merkez tarafından kabul edildi!\n<b>{0}</b> ekibi <b>{1}</b> sektörüne yönlendirildi (Bugün gönderilen: {2}/{3}).\nOperasyon sonuçları yarın sabah bildirilecektir.") },
            { "report.lost_coords", ("• REPORT: Zone coordinates were lost.",
                                     "• RAPOR: Bölge koordinatları kayboldu.") },
            { "report.already_destroyed", ("• REPORT: Squad {0} arrived at {1}, but the sector was already destroyed.",
                                          "• RAPOR: {0} ekibi {1} bölgesine ulaştı, ancak sektör zaten yıkılmıştı.") },
            { "report.no_threats", ("• REPORT: Squad {0} inspected sector {1}. No active threats found.",
                                    "• RAPOR: {0} ekibi {1} sektörünü denetledi. Aktif tehdit bulunamadı.") },
            { "report.success", ("• <color=#86efac>SUCCESS!</color> Squad {0} resolved '{1}' in sector {2}! (+${3})",
                                 "• <color=#86efac>BAŞARILI!</color> {0} ekibi {2} sektöründeki '{1}' tehdidini bertaraf etti! (+${3})") },
            { "report.failure", ("• <color=#f87171>FAILURE!</color> Squad {0} failed to handle threat in sector {1}!",
                                 "• <color=#f87171>BAŞARISIZ!</color> {0} ekibi {1} sektöründeki tehdidi durduramadı!") },
        };

        public static string Get(string key)
        {
            if (Translations.TryGetValue(key, out var val))
            {
                return CurrentLanguage == GameLanguage.Turkish ? val.tr : val.en;
            }
            return key;
        }

        public static string Get(string key, params object[] args)
        {
            string raw = Get(key);
            try
            {
                return string.Format(raw, args);
            }
            catch
            {
                return raw;
            }
        }

        public static string GetZoneName(ZoneType type)
        {
            bool isTr = CurrentLanguage == GameLanguage.Turkish;
            switch (type)
            {
                case ZoneType.Sea: return isTr ? "Deniz" : "Sea";
                case ZoneType.Mountains: return isTr ? "Dağlar" : "Mountains";
                case ZoneType.Forest: return isTr ? "Orman" : "Forest";
                case ZoneType.Field: return isTr ? "Ova" : "Field";
                default: return type.ToString();
            }
        }

        public static string GetSquadName(SquadType squad)
        {
            bool isTr = CurrentLanguage == GameLanguage.Turkish;
            switch (squad)
            {
                case SquadType.Irrigators: return isTr ? "Sulama Ekipleri" : "Irrigators";
                case SquadType.Lumberjacks: return isTr ? "Ormancılar" : "Lumberjacks";
                case SquadType.Firefighters: return isTr ? "İtfaiyeciler" : "Firefighters";
                case SquadType.RescueHelicopter: return isTr ? "Kurtarma Helikopteri" : "Rescue Helicopter";
                case SquadType.MountainClimbers: return isTr ? "Dağcılar" : "Mountain Climbers";
                case SquadType.Divers: return isTr ? "Dalgıçlar" : "Divers";
                default: return squad.ToString();
            }
        }

        public static string GetThreatName(ThreatType type)
        {
            bool isTr = CurrentLanguage == GameLanguage.Turkish;
            switch (type)
            {
                case ThreatType.Drought: return isTr ? "Kuraklık" : "Drought";
                case ThreatType.Wildfire: return isTr ? "Orman Yangını" : "Wildfire";
                case ThreatType.Landslide: return isTr ? "Heyelan" : "Landslide";
                case ThreatType.Tsunami: return isTr ? "Tsunami" : "Tsunami";
                default: return type.ToString();
            }
        }

        public static string GetThreatStageDescription(ThreatType type, int stage)
        {
            bool isTr = CurrentLanguage == GameLanguage.Turkish;
            switch (type)
            {
                case ThreatType.Drought:
                    if (stage == 1) return isTr ? "1. Aşama: Yaprakların sararması" : "Stage 1: Yellowing leaves";
                    if (stage == 2) return isTr ? "2. Aşama: Kuru dallar ve yarıklar" : "Stage 2: Dry branches & soil cracks";
                    return isTr ? "3. Aşama: Kritik kuraklık!" : "Stage 3: Critical drought!";

                case ThreatType.Wildfire:
                    if (stage == 1) return isTr ? "1. Aşama: Ağaç tepelerinde duman" : "Stage 1: Faint smoke above treetops";
                    if (stage == 2) return isTr ? "2. Aşama: Yayılan alev odakları" : "Stage 2: Active open flames spreading";
                    return isTr ? "3. Aşama: Kontrolden çıkan yangın!" : "Stage 3: Uncontrollable wildfire!";

                case ThreatType.Landslide:
                    if (stage == 1) return isTr ? "1. Aşama: Sarsıntılar ve taş düşmesi" : "Stage 1: Tremors & falling rocks";
                    return isTr ? "2. Aşama: Kütle çökmesi! Acil tehlike!" : "Stage 2: Rock mass collapse! Immediate danger!";

                case ThreatType.Tsunami:
                    if (stage == 1) return isTr ? "1. Aşama: Kıyıdan suyun çekilmesi" : "Stage 1: Sharp water recession from shore";
                    return isTr ? "2. Aşama: Ufukta dev tsunami dalgası!" : "Stage 2: Giant tsunami wave on horizon!";

                default:
                    return isTr ? $"{stage}. Aşama" : $"Stage {stage}";
            }
        }
    }
}
