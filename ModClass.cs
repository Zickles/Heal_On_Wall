using Modding;
using System;
using System.IO;
using System.Reflection;
using Satchel;
using Heal_On_Wall.Consts;
using HutongGames.PlayMaker;
using UnityEngine;
using HutongGames.PlayMaker.Actions;
using SFCore;
using GlobalEnums;
using Satchel.Reflected;
using SFCore.Generics;
using System.Collections.Generic;

namespace Heal_On_Wall
{
        class Heal_On_Wall : SaveSettingsMod<HOWSettings>
        {
            new public string GetName() => "Heal On Wall";
            public override string GetVersion() => "1.0.0";
            public TextureStrings Ts { get; private set; }
            public List<int> CharmIDs { get; private set; }

        private bool MyCustomFocus(On.HeroController.orig_CanFocus orig, HeroController self)
        {
            return !GameManager.instance.isPaused && HeroController.instance.hero_state != ActorStates.no_input && !HeroController.instance.cState.dashing && !HeroController.instance.cState.backDashing && (!HeroController.instance.cState.attacking || HeroControllerR.attack_time >= HeroController.instance.ATTACK_RECOVERY_TIME) && !HeroController.instance.cState.recoiling && (HeroController.instance.cState.onGround || (PlayerData.instance.hasWalljump && HeroController.instance.cState.touchingWall && !HeroController.instance.cState.touchingNonSlider)) && !HeroController.instance.cState.transitioning && !HeroController.instance.cState.recoilFrozen && !HeroController.instance.cState.hazardDeath && !HeroController.instance.cState.hazardRespawning && HeroController.instance.CanInput();
        }



        public Heal_On_Wall() : base("Heal On Wall")
        {
            Ts = new TextureStrings();
        }

        private void OnFSM(On.PlayMakerFSM.orig_OnEnable orig, PlayMakerFSM self)
        {
            orig(self);

            if (self.gameObject.name != "Knight" || self.FsmName != "Spell Control") return;
            // Now we are "Spell Control" on "Knight"

            // Add methods to do custom wall focusing stuff
            self.InsertCustomAction("Focus Start", () =>
            {
                if (HeroController.instance.cState.onGround) return;
                    if (!SaveSettings.equippedCharms[0]) return;
                // Charm is equipped

                // Remove left ground transitions while focusing

                self.GetAction<Tk2dPlayAnimation>("Focus Start", 15).Enabled = false;
                self.GetAction<Tk2dPlayAnimation>("Focus Heal", 7).Enabled = false;
                self.GetAction<Tk2dPlayAnimationWithEvents>("Focus Get Finish", 8).Enabled = false;
                self.RemoveTransition("Focus Start", "LEFT GROUND");
                self.RemoveTransition("Focus", "LEFT GROUND");
                self.RemoveTransition("Focus Left", "LEFT GROUND");
                self.RemoveTransition("Focus Right", "LEFT GROUND");
            }, 0);
            self.AddCustomAction("Focus Start", () =>
            {
                if (HeroController.instance.cState.onGround) return;
                if (!SaveSettings.equippedCharms[0]) return;
                // Charm is equipped

                // No gravity while focusing
                self.gameObject.GetComponent<HeroController>().AffectedByGravity(false);
            });

            // Add method to revert custom wall focusing stuff
            self.InsertCustomAction("Regain Control", () =>
            {
                if (HeroController.instance.cState.onGround) return;
                if (!SaveSettings.equippedCharms[0]) return;
                // Charm is equipped

                // Re-add left ground transitions while focusing
                
                self.AddTransition("Focus Start", "LEFT GROUND", "Focus Cancel");
                self.AddTransition("Focus", "LEFT GROUND", "Grace Check");
                self.AddTransition("Focus Left", "LEFT GROUND", "Grace Check 2");
                self.AddTransition("Focus Right", "LEFT GROUND", "Grace Check 2");
                self.GetAction<Tk2dPlayAnimation>("Focus Heal", 7).Enabled = true;
                self.GetAction<Tk2dPlayAnimationWithEvents>("Focus Get Finish", 8).Enabled = true;
                self.GetAction<Tk2dPlayAnimation>("Focus Start", 15).Enabled = true;
                
                // Yes gravity after focusing
                self.gameObject.GetComponent<HeroController>().AffectedByGravity(true);
            }, 0);
        }
        public override void Initialize()
        {
            Log("Initializing");
            On.PlayMakerFSM.OnEnable += OnFSM;
            On.HeroController.CanFocus += MyCustomFocus;

            CharmIDs = CharmHelper.AddSprites(Ts.Get(TextureStrings.HOWKey));

            InitCallbacks();

            Log("Initialized");
        }



        private void InitCallbacks()
        {
            ModHooks.GetPlayerBoolHook += OnGetPlayerBoolHook;
            ModHooks.SetPlayerBoolHook += OnSetPlayerBoolHook;
            ModHooks.GetPlayerIntHook += OnGetPlayerIntHook;
            ModHooks.AfterSavegameLoadHook += InitSaveSettings;
            ModHooks.LanguageGetHook += OnLanguageGetHook;
            //ModHooks.CharmUpdateHook += OnCharmUpdateHook;

            On.GameManager.Update += GameManagerUpdate;
        }

        #region Mod Reload

        private void GameManagerUpdate(On.GameManager.orig_Update orig, GameManager self)
        {
            orig(self);
        }

        #endregion

        private bool _changed = false;
        private void InitSaveSettings(SaveGameData data)
        {
            // Found in a project, might help saving, don't know, but who cares
            // Charms
            SaveSettings.gotCharms = SaveSettings.gotCharms;
            SaveSettings.newCharms = SaveSettings.newCharms;
            SaveSettings.equippedCharms = SaveSettings.equippedCharms;
            SaveSettings.charmCosts = SaveSettings.charmCosts;

            _changed = false;
        }



        #region ModHooks

        private string[] _charmNames =
        {
            "Wall Heal",
        };
        private string[] _charmDescriptions =
        {
            "Lets you Heal on a Wall",
        };
        private string OnLanguageGetHook(string key, string sheet, string orig)
        {
            if (key.StartsWith("CHARM_NAME_"))
            {
                int charmNum = int.Parse(key.Split('_')[2]);
                if (CharmIDs.Contains(charmNum))
                {
                    return _charmNames[CharmIDs.IndexOf(charmNum)];
                }
            }
            else if (key.StartsWith("CHARM_DESC_"))
            {
                int charmNum = int.Parse(key.Split('_')[2]);
                if (CharmIDs.Contains(charmNum))
                {
                    return _charmDescriptions[CharmIDs.IndexOf(charmNum)];
                }
            }
            return orig;
        }
        private bool OnGetPlayerBoolHook(string target, bool orig)
        {
            if (target.StartsWith("gotCharm_"))
            {
                int charmNum = int.Parse(target.Split('_')[1]);
                if (CharmIDs.Contains(charmNum))
                {
                    return SaveSettings.gotCharms[CharmIDs.IndexOf(charmNum)];
                }
            }
            if (target.StartsWith("newCharm_"))
            {
                int charmNum = int.Parse(target.Split('_')[1]);
                if (CharmIDs.Contains(charmNum))
                {
                    return SaveSettings.newCharms[CharmIDs.IndexOf(charmNum)];
                }
            }
            if (target.StartsWith("equippedCharm_"))
            {
                int charmNum = int.Parse(target.Split('_')[1]);
                if (CharmIDs.Contains(charmNum))
                {
                    return SaveSettings.equippedCharms[CharmIDs.IndexOf(charmNum)];
                }
            }
            return orig;
        }
        private bool OnSetPlayerBoolHook(string target, bool orig)
        {
            if (target.StartsWith("gotCharm_"))
            {
                int charmNum = int.Parse(target.Split('_')[1]);
                if (CharmIDs.Contains(charmNum))
                {
                    SaveSettings.gotCharms[CharmIDs.IndexOf(charmNum)] = orig;
                    return orig;
                }
            }
            if (target.StartsWith("newCharm_"))
            {
                int charmNum = int.Parse(target.Split('_')[1]);
                if (CharmIDs.Contains(charmNum))
                {
                    SaveSettings.newCharms[CharmIDs.IndexOf(charmNum)] = orig;
                    return orig;
                }
            }
            if (target.StartsWith("equippedCharm_"))
            {
                int charmNum = int.Parse(target.Split('_')[1]);
                if (CharmIDs.Contains(charmNum))
                {
                    SaveSettings.equippedCharms[CharmIDs.IndexOf(charmNum)] = orig;
                    return orig;
                }
            }
            return orig;
        }
        private int OnGetPlayerIntHook(string target, int orig)
        {
            if (target.StartsWith("charmCost_"))
            {
                int charmNum = int.Parse(target.Split('_')[1]);
                if (CharmIDs.Contains(charmNum))
                {
                    return SaveSettings.charmCosts[CharmIDs.IndexOf(charmNum)];
                }
            }
            return orig;
        }

        #endregion

        

        





    }


}
