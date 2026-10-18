using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
namespace Labyrinth
{
    [DefaultExecutionOrder(-10)]
    // Dibuja los menús, la interfaz y los diálogos.
    public sealed class GameUI:MonoBehaviour
    {
        public bool defeat;
        public int InputConsumedFrame { get; private set; } = -1;
        public bool ShowingCredits=>credits;
        public string notice = "";
        bool settings, credits, styled, activate;
        int selected, line;
        string screenKey = "";
        string[] lines;
        Action onDone;
        float typed, toastUntil, shakeUntil, creditsStart;
        string toast = "";
        Vector2 historyScroll;
        GUIStyle title, label, small, center, cardTitle, levelNumber, bossStatus;
        Texture2D logo, border, card;
        MenuBackdrop backdrop;
        readonly Dictionary<string, float> hover = new Dictionary<string, float>();
        readonly HashSet<string> pointerInside = new HashSet<string>();
        float Scale=>Mathf.Min(Screen.width/1280f, Screen.height/720f);
        Vector2 MousePosition
        {
            get
            {
                if (Mouse.current == null)return new Vector2(-1000, -1000);
                var p = Mouse.current.position.ReadValue();
                float s = Scale;
                return new Vector2((p.x-(Screen.width-1280*s)*.5f)/s, (Screen.height-p.y-(Screen.height-720*s)*.5f)/s);
            }
        }
        public void Toast(string text)
        {
            toast = text;
            toastUntil = Time.unscaledTime+3.5f;
        }
        public void ShakeHUD()
        {
            shakeUntil = Time.unscaledTime+.32f;
        }

        // Abre una conversación y pausa el combate.
        public void ShowDialogue(string[] text, Action done)
        {
            if (text == null || text.Length == 0)
            {
                if (done != null)done();
                return;
            }
            lines = text;
            line = 0;
            typed = 0;
            onDone = done;
            var g = GameDirector.I;
            if (g.player)
            {
                g.player.CancelAim();
                g.player.ClearQueuedActions();
            }
            g.dialogue = true;
            g.SetTime();
        }
        string Body(string raw)
        {
            if (raw == null)raw = "";
            int p = raw.IndexOf('|');
            return p<0?raw:raw.Substring(p+1);
        }

        // Revela o avanza el texto del diálogo.
        void Advance()
        {
            if (lines == null)return;
            InputConsumedFrame = Time.frameCount;
            GameDirector.I.sound.Play(SoundCue.DialogueAdvance);
            if (typed<Body(lines[line]).Length)
            {
                typed = Body(lines[line]).Length;
                return;
            }
            if (++line<lines.Length)
            {
                typed = 0;
                return;
            }
            lines = null;
            var g = GameDirector.I;
            g.dialogue = false;
            g.SetTime();
            var callback = onDone;
            onDone = null;
            if (callback != null)callback();
        }

        // Lee los controles de menús y diálogos.
        void Update()
        {
            var g = GameDirector.I;
            if (!g || g.gameObject != gameObject)return;
            if (!backdrop)backdrop = GetComponent<MenuBackdrop>();
            var k = Keyboard.current;
            if (k == null)return;
            if (lines != null)
            {
                typed+=Time.unscaledDeltaTime*45;
                if (k.enterKey.wasPressedThisFrame || k.spaceKey.wasPressedThisFrame || k.zKey.wasPressedThisFrame)Advance();
                return;
            }
            if (g.oldFriend && g.oldFriend.Choosing)
            {
                if (k.digit1Key.wasPressedThisFrame)g.oldFriend.Choose(0);
                else if(k.digit2Key.wasPressedThisFrame)g.oldFriend.Choose(1);
                else if(k.digit3Key.wasPressedThisFrame)g.oldFriend.Choose(2);
                return;
            }
            if (g.rewards && g.rewards.Active)
            {
                if (k.digit1Key.wasPressedThisFrame)g.rewards.Choose(0);
                else if(k.digit2Key.wasPressedThisFrame)g.rewards.Choose(1);
                else if(k.digit3Key.wasPressedThisFrame)g.rewards.Choose(2);
                return;
            }
            if (k.escapeKey.wasPressedThisFrame)
            {
                if (settings)
                {
                    settings = false;
                    PlayerPrefs.Save();
                    return;
                }
                if (credits)
                {
                    credits = false;
                    return;
                }
                if ((Campaign.IsCombat(g.stage) || g.stage == Campaign.OldFriend) && !g.finished)
                {
                    if (g.player && g.player.Aiming)return;
                    g.paused = !g.paused;
                    if (g.player)g.player.ClearQueuedActions();
                    g.SetTime();
                }
            }
            string key = defeat?"defeat":g.paused?"pause":g.stage<0?"menu":"other";
            if (screenKey != key)
            {
                screenKey = key;
                selected = 0;
                activate = false;
            }
            int count = defeat?2:g.paused?4:g.stage<0?5:0;
            if (!settings && !credits && count>0)
            {
                if (k.downArrowKey.wasPressedThisFrame || k.sKey.wasPressedThisFrame)
                {
                    selected = (selected+1)%count;
                    g.sound.Play(SoundCue.ButtonHover);
                }
                if (k.upArrowKey.wasPressedThisFrame || k.wKey.wasPressedThisFrame)
                {
                    selected = (selected+count-1)%count;
                    g.sound.Play(SoundCue.ButtonHover);
                }
                if (k.enterKey.wasPressedThisFrame || k.spaceKey.wasPressedThisFrame)activate = true;
            }
        }

        // Configura los estilos de la interfaz.
        void Styles()
        {
            if (styled)return;
            styled = true;
            title = new GUIStyle(GUI.skin.label)
            {
                fontSize = 34, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter, wordWrap = true
            };
            title.normal.textColor = new Color(.92f, .84f, 1);
            label = new GUIStyle(GUI.skin.label)
            {
                fontSize = 20, wordWrap = true
            };
            label.normal.textColor = Color.white;
            small = new GUIStyle(label)
            {
                fontSize = 16
            };
            center = new GUIStyle(label)
            {
                alignment = TextAnchor.MiddleCenter
            };
            cardTitle = new GUIStyle(title)
            {
                fontSize = 26
            };
            bossStatus = new GUIStyle(small)
            {
                wordWrap = false
            };
            levelNumber = new GUIStyle(small)
            {
                alignment = TextAnchor.MiddleRight,
                fontSize = 19,
                fontStyle = FontStyle.Bold,
                wordWrap = false
            };
            levelNumber.normal.textColor = new Color(.90f, .78f, 1f);
            logo = Resources.Load<Texture2D>("HexenknallUI/Titulo");
            border = Resources.Load<Texture2D>("HexenknallUI/BordesHUD");
            card = Resources.Load<Texture2D>("HexenknallUI/Cards");
        }
        void Fill(Rect r, Color c)
        {
            var old = GUI.color;
            GUI.color = c;
            GUI.DrawTexture(r, Texture2D.whiteTexture);
            GUI.color = old;
        }
        void Art(Texture t, Rect r)
        {
            if (t)GUI.DrawTexture(r, t, ScaleMode.StretchToFill, true);
        }
        void Background()
        {
            Fill(new Rect(0, 0, 1280, 720), new Color(.018f, .009f, .03f));
            if (backdrop && backdrop.Frame)Art(backdrop.Frame, new Rect(0, 0, 1280, 720));
        }
        bool Fancy(Rect rect, string text, string id, int nav = -1, bool enabled = true)
        {
            bool inside = rect.Contains(MousePosition) && enabled;
            if (inside && pointerInside.Add(id))
            {
                GameDirector.I.sound.Play(id.StartsWith("card.") || id.StartsWith("exchange.")?SoundCue.CardHover:SoundCue.ButtonHover);
                if (nav>=0)selected = nav;
            }
            if (!inside)pointerInside.Remove(id);
            bool active = enabled && (inside || (nav>=0 && selected == nav));
            if (!hover.ContainsKey(id))hover[id] = 0;
            if (Event.current.type == EventType.Repaint)hover[id] = Mathf.MoveTowards(hover[id], active?1:0, Time.unscaledDeltaTime*7);
            float h = hover[id], pulse = .8f+.2f*Mathf.Sin(Time.unscaledTime*4);
            var r = rect;
            r.x-=h*3;
            r.y-=h*2;
            r.width+=h*6;
            r.height+=h*4;
            if (h>0)for(int i = 7;i>0;i--) Fill(new Rect(r.x-i, r.y-i, r.width+i*2, r.height+i*2), new Color(.55f, .13f, 1, .018f*h*pulse));
            Fill(r, enabled?new Color(.035f+h*.09f, .012f, .06f+h*.11f, .99f):new Color(.025f, .02f, .03f, .97f));
            Color edge = Color.Lerp(new Color(.44f, .39f, .5f), new Color(.86f, .48f, 1), h);
            if (!enabled)edge*=.5f;
            Fill(new Rect(r.x+9, r.y, r.width-18, 2), edge);
            Fill(new Rect(r.x+9, r.yMax-2, r.width-18, 2), edge);
            Fill(new Rect(r.x, r.y+9, 2, r.height-18), edge);
            Fill(new Rect(r.xMax-2, r.y+9, 2, r.height-18), edge);
            if (h>.1f)
            {
                float x = r.x+8+Mathf.Repeat(Time.unscaledTime*110, r.width-16);
                Fill(new Rect(x, r.y-2, 6, 5), new Color(1, .7f, 1, h));
                GUI.Label(new Rect(r.x-22, r.y, 24, r.height), "✦", center);
                GUI.Label(new Rect(r.xMax-2, r.y, 24, r.height), "✦", center);
            }
            var old = GUI.color;
            GUI.color = enabled?Color.white:new Color(.48f, .45f, .5f);
            GUI.Label(r, text, center);
            GUI.color = old;
            bool clicked = GUI.Button(rect, GUIContent.none, GUIStyle.none) && enabled;
            if (nav>=0 && selected == nav && activate)
            {
                activate = false;
                clicked = enabled;
            }
            if (clicked)GameDirector.I.sound.Play(SoundCue.ButtonClick);
            return clicked;
        }

        // Dibuja la pantalla que esté activa.
        void OnGUI()
        {
            var g = GameDirector.I;
            if (!g || g.gameObject != gameObject)return;
            Styles();
            GUI.matrix = Matrix4x4.identity;
            if (g.stage<0 || defeat || g.stage == Campaign.Ending)Fill(new Rect(0, 0, Screen.width, Screen.height), Color.black);
            float s = Scale;
            GUI.matrix = Matrix4x4.TRS(new Vector3((Screen.width-1280*s)*.5f, (Screen.height-720*s)*.5f, 0), Quaternion.identity, Vector3.one*s);
            if (settings)
            {
                Background();
                Settings();
                return;
            }
            if (credits || g.stage == Campaign.Ending)
            {
                Background();
                Credits(g);
                return;
            }
            if (g.rewards && g.rewards.Active)
            {
                Background();
                Cards();
                return;
            }
            if (g.oldFriend && g.oldFriend.Choosing)
            {
                Background();
                Exchange();
                return;
            }
            if (g.stage<0)MainMenu(g);
            else if(defeat)
            {
                Background();
                Defeat(g);
            }
            else
            {
                Hud(g);
                if (g.paused)Pause(g);
                if (lines != null)Dialogue();
            }
            if (Time.unscaledTime<toastUntil)GUI.Label(new Rect(220, 598, 840, 74), toast, center);
        }

        // Muestra las opciones del menú principal.
        void MainMenu(GameDirector g)
        {
            Background();
            if (!backdrop || !backdrop.Frame)
            {
                Art(logo, new Rect(310, 90, 660, 210));
                GUI.Label(new Rect(350, 140, 580, 75), "HEXENKNALL", title);
            }
            GUI.Label(new Rect(400, 333, 480, 32), "DIFICULTAD: "+PlayerController.DifficultyName, center);
            if (Fancy(new Rect(497, 375, 286, 53), "Jugar", "menu.play", 0))g.NewGame();
            if (Fancy(new Rect(497, 440, 286, 53), "Continuar", "menu.continue", 1, RunProgress.HasSave))g.Continue();
            if (Fancy(new Rect(497, 505, 286, 53), "Ajustes", "menu.settings", 2))settings = true;
            if (Fancy(new Rect(497, 570, 286, 53), "Salir", "menu.quit", 3))Application.Quit();
            if (Fancy(new Rect(520, 644, 240, 38), "Créditos", "menu.credits", 4))
            {
                credits = true;
                creditsStart = Time.unscaledTime;
            }
            if (RunProgress.HasSave)GUI.Label(new Rect(820, 444, 320, 45), "Continuar: "+Campaign.Scene(RunProgress.ResumeStage(RunProgress.Data)), small);
        }

        // Dibuja la información durante el combate.
        void Hud(GameDirector g)
        {
            var matrix = GUI.matrix;
            float remaining = Mathf.Clamp01((shakeUntil-Time.unscaledTime)/.32f);
            float amount = PresentationCatalog.Current?PresentationCatalog.Current.hudShake:4;
            if (remaining>0)GUI.matrix*=Matrix4x4.Translate(new Vector3(Mathf.Sin(Time.unscaledTime*87), Mathf.Cos(Time.unscaledTime*101), 0)*remaining*amount);
            Fill(new Rect(24, 20, 1232, 95), new Color(.025f, .012f, .045f, .91f));
            var p = g.player;
            GUI.Label(new Rect(45, 25, 780, 35), g.levelTitle+"    ·    Vida "+(p?p.health:0)+" / "+(p?p.maxHealth:3), label);
            if (g.stage == Campaign.OldFriend)GUI.Label(new Rect(45, 63, 1140, 35), "Z: hablar con Old Friend  ·  Cruza el portal después del intercambio  ·  Esc: pausa", small);
            else if(p)GUI.Label(new Rect(45, 63, 1140, 35), "Z: círculo arcano"+(p.BombQueued?" [pendiente]":"")+"  |  X: rayo "+(p.RayUnlocked?Status(p.RayRemaining):"BLOQUEADO")+"  |  C: invocación "+(p.SpecialUnlocked?Status(p.SpecialRemaining):"BLOQUEADA")+"  |  Esc: pausa", small);
            foreach (var a in g.board.actors)
            {
                if (a && a.boss && a.health > 0)
                    GUI.Label(new Rect(840, 25, 262, 35), "GUARDIÁN  " + a.health + " / 4    FASE " + (a.health <= 2 ? 2 : 1), bossStatus);
            }

            // Indica la etapa actual en la parte superior derecha.
            string nivel = "NIVEL " + g.stage.ToString("00");
            if (g.stage == 0) nivel = "PRÓLOGO";
            else if (g.stage == Campaign.FinalBoss) nivel = "JEFE FINAL";
            else if (g.stage == Campaign.OldFriend) nivel = "REFUGIO";
            GUI.Label(new Rect(1108, 25, 116, 35), nivel, levelNumber);

            Art(border, new Rect(0, 0, 1280, 720));
            GUI.matrix = matrix;
            if (!string.IsNullOrEmpty(notice))GUI.Label(new Rect(180, 132, 920, 70), notice, title);
            if (p && p.Aiming)GUI.Label(new Rect(250, 570, 780, 78), "WASD / flechas: apuntar  ·  Enter: invocar  ·  Esc: cancelar\nCírculo violeta: válido. Gris: casilla bloqueada.", center);
            if (g.oldFriend && g.oldFriend.CanTalk)GUI.Label(new Rect(330, 520, 620, 45), "[Z] Hablar con Old Friend", center);
        }
        string Status(float n)=>n<=0?"LISTO":n.ToString("0.0")+" s";

        // Muestra las opciones después de perder.
        void Defeat(GameDirector g)
        {
            if (!backdrop || !backdrop.Frame)GUI.Label(new Rect(300, 80, 680, 160), "GAME OVER\nHas caído en el laberinto", title);
            if (Fancy(new Rect(444, 325, 390, 78), "Reiniciar", "defeat.retry", 0))g.Load(Campaign.Scene(g.stage));
            if (Fancy(new Rect(444, 430, 390, 78), "Salir al menú", "defeat.menu", 1))g.Load("MainMenu");
        }

        // Dibuja el menú de pausa y reinicio.
        void Pause(GameDirector g)
        {
            Fill(new Rect(0, 0, 1280, 720), new Color(.008f, .003f, .02f, .88f));
            GUI.Label(new Rect(380, 42, 520, 60), "PAUSA", title);
            GUI.Label(new Rect(883, 54, 290, 35), PlayerController.DifficultyName, small);
            if (Fancy(new Rect(455, 110, 370, 48), "Continuar", "pause.resume", 0))
            {
                g.paused = false;
                g.SetTime();
            }
            if (Fancy(new Rect(455, 170, 370, 48), "Reiniciar nivel", "pause.restart", 1))g.Load(Campaign.Scene(g.stage));
            if (Fancy(new Rect(455, 230, 370, 48), "Ajustes", "pause.settings", 2))settings = true;
            if (Fancy(new Rect(455, 290, 370, 48), "Guardar y salir al menú", "pause.menu", 3))g.Load("MainMenu");
            GUI.Label(new Rect(220, 353, 840, 50), "BENDICIONES Y MALDICIONES ACTIVAS", cardTitle);
            var d = RunProgress.Data;
            historyScroll = GUI.BeginScrollView(new Rect(260, 410, 760, 225), historyScroll, new Rect(0, 0, 730, Mathf.Max(225, d.history.Count*29+20)));
            for (int i = 0;i<d.history.Count;i++)GUI.Label(new Rect(15, 10+i*29, 700, 27), d.history[i].Replace("Ventaja:", "Bendición:"), small);
            if (d.history.Count == 0)GUI.Label(new Rect(15, 10, 700, 40), "Todavía no tienes pactos.", small);
            GUI.EndScrollView();
        }

        // Muestra el texto y el personaje que habla.
        void Dialogue()
        {
            Fill(new Rect(100, 454, 1080, 245), new Color(.025f, .009f, .045f, .98f));
            string raw = lines[line];
            if (raw == null)raw = "";
            int sep = raw.IndexOf('|');
            string body = Body(raw);
            GUI.Label(new Rect(125, 464, 1030, 40), sep<0?"":raw.Substring(0, sep), cardTitle);
            GUI.Label(new Rect(140, 510, 1000, 123), body.Substring(0, Mathf.Min(body.Length, (int)typed)), label);
            GUI.Label(new Rect(140, 649, 200, 30), (line+1)+" / "+lines.Length, small);
            if (Fancy(new Rect(805, 643, 340, 40), "Continuar  [Z / Enter]", "dialogue.next"))Advance();
        }

        // Presenta las cartas disponibles.
        void Cards()
        {
            var g = GameDirector.I;
            var d = RunProgress.Data;
            bool curse = g.rewards.ChoosingCurse;
            GUI.Label(new Rect(90, 35, 1100, 55), curse?"TODO PODER EXIGE UN PRECIO":"ELIGE TU BENDICIÓN", title);
            GUI.Label(new Rect(120, 92, 1040, 65), curse?"Tres maldiciones de rareza "+CardRules.RarityName((Rarity)d.chosenRarity):"Nivel "+d.rewardStage+" completado. Después elegirás una maldición de la misma rareza.", center);
            int choice = -1;
            for (int i = 0;i<d.offers.Count;i++)
            {
                var c = d.offers[i];
                float x = 50+i*403;
                CardPanel(x, CardRules.RarityName(c.rarity), CardRules.Title(c.effect), CardRules.Description(c), CardRules.Tint(c.rarity));
                if (Fancy(new Rect(x+30, 528, 310, 45), "Elegir ["+(i+1)+"]", "card."+i))choice = i;
            }
            if (Fancy(new Rect(430, 651, 420, 43), "Guardar y volver al menú", "cards.menu"))g.Load("MainMenu");
            if (choice>=0)g.rewards.Choose(choice);
        }
        void CardPanel(float x, string top, string name, string description, Color tint)
        {
            Fill(new Rect(x, 175, 370, 425), new Color(.035f, .014f, .06f, .98f));
            Art(card, new Rect(x, 167, 370, 445));
            GUI.Label(new Rect(x+24, 202, 322, 40), top, center);
            Fill(new Rect(x+28, 249, 314, 2), tint);
            GUI.Label(new Rect(x+27, 264, 316, 93), name, cardTitle);
            GUI.Label(new Rect(x+34, 362, 302, 152), description, label);
        }
        void Exchange()
        {
            var g = GameDirector.I;
            var d = RunProgress.Data;
            GUI.Label(new Rect(100, 32, 1080, 60), "DEJA ATRÁS UN PESO", title);
            GUI.Label(new Rect(100, 98, 1080, 60), "Tienes "+CardLedger.Count(d, false)+" bendiciones y "+CardLedger.Count(d, true)+" maldiciones. Los pactos retirados se eligen al azar.", center);
            int choice = -1;
            for (int i = 0;i<3;i++)
            {
                int option = d.oldFriendOrder[i];
                float x = 50+i*403;
                bool valid = CardLedger.CanExchange(d, option);
                string description = option == 0?"Retira 1 maldición. Conservas todas tus bendiciones.":"Retira "+option+" bendición"+(option == 1?"":"es")+" y "+(option+1)+" maldiciones.";
                CardPanel(x, "INTERCAMBIO", new[]
                {
                    "Un respiro", "Un precio justo", "Empezar de nuevo"
                }
                [option], description+(valid?"\nSolo puedes realizar un intercambio.":"\nNo tienes suficientes pactos."), new Color(.75f, .4f, 1));
                if (Fancy(new Rect(x+30, 528, 310, 45), "Elegir ["+(i+1)+"]", "exchange."+i, -1, valid))choice = i;
            }
            if (Fancy(new Rect(430, 651, 420, 43), "Guardar y volver al menú", "exchange.menu"))g.Load("MainMenu");
            if (choice>=0)g.oldFriend.Choose(choice);
        }

        // Muestra los créditos del juego.
        void Credits(GameDirector g)
        {
            Fill(new Rect(195, 36, 890, 640), new Color(.02f, .008f, .035f, .92f));
            GUI.Label(new Rect(300, 64, 680, 65), "HEXENKNALL", title);
            GUI.Label(new Rect(280, 145, 720, 55), "ClubPanitas Studios", cardTitle);
            var names = new[]
            {
                "Fabrizio Mendoza", "Santiago Soria", "Jairo Marquez", "Rodolfo Jimenez"
            };
            for (int i = 0;i<names.Length;i++)GUI.Label(new Rect(340, 240+i*58, 600, 45), names[i], center);
            GUI.Label(new Rect(290, 493, 700, 60), g.stage == Campaign.Ending?"Has roto el último sello. Gracias por jugar.":"Diseño, programación y producción", center);
            if (Fancy(new Rect(445, 590, 390, 52), "Volver al menú", "credits.menu"))
            {
                if (g.stage == Campaign.Ending)g.Load("MainMenu");
                else credits = false;
            }
        }
        void Slider(string text, string key, float fallback, float min = 0, float max = 1)
        {
            GUILayout.Label(text, label);
            float old = PlayerPrefs.GetFloat(key, fallback), value = GUILayout.HorizontalSlider(old, min, max);
            if (!Mathf.Approximately(old, value))
            {
                PlayerPrefs.SetFloat(key, value);
                GameDirector.I.sound.Apply();
            }
            GUILayout.Space(12);
        }

        // Dibuja los ajustes de sonido y dificultad.
        void Settings()
        {
            Fill(new Rect(310, 45, 660, 630), new Color(.025f, .008f, .045f, .99f));
            GUILayout.BeginArea(new Rect(350, 65, 580, 540));
            GUILayout.Label("HEXENKNALL · AJUSTES", title);
            GUILayout.Space(20);
            Slider("Volumen general", "Master", .65f);
            Slider("Música", "Music", .45f);
            Slider("Efectos", "SFX", .7f);
            Slider("Brillo", "Brightness", 0, -1, 1.5f);
            GUILayout.Label("Dificultad", label);
            int difficulty = PlayerController.Difficulty;
            int chosen = GUILayout.SelectionGrid(difficulty, new[]
            {
                "Difícil", "Muy difícil"
            }
            , 2, GUILayout.Height(34));
            if (chosen != difficulty)PlayerPrefs.SetInt(PlayerController.DifficultyPrefKey, chosen);
            GUILayout.Label(chosen == 0?"Controles siguen el giro de la cámara.":"Controles fijos aunque gire la cámara.", small);
            GUILayout.Space(10);
            int old = Mathf.Clamp(PlayerPrefs.GetInt("Resolution", 1), 0, 2);
            int next = GUILayout.SelectionGrid(old, new[]
            {
                "1280 × 720", "1600 × 900", "1920 × 1080"
            }
            , 3);
            bool full = PlayerPrefs.GetInt("Fullscreen", 0) == 1, value = GUILayout.Toggle(full, "Pantalla completa");
            if (next != old || full != value)
            {
                PlayerPrefs.SetInt("Resolution", next);
                PlayerPrefs.SetInt("Fullscreen", value?1:0);
                Screen.SetResolution(new[]
                {
                    1280, 1600, 1920
                }
                [next], new[]
                {
                    720, 900, 1080
                }
                [next], value);
            }
            GUILayout.EndArea();
            if (Fancy(new Rect(445, 602, 390, 48), "Guardar y volver", "settings.back"))
            {
                PlayerPrefs.Save();
                settings = false;
            }
        }
        void OnApplicationQuit()
        {
            PlayerPrefs.Save();
        }
    }
}
