using BepInEx;
using BepInEx.Configuration;
using RoR2;
using UnityEngine;
using EntityStates.Merc;
using System.Linq;
using UnityEngine.Networking;
using EntityStates;
using System.Collections.Generic;





namespace MercenaryTweaks
{
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    public class MercenaryTweaks : BaseUnityPlugin
    {
        public const string PluginGUID = PluginAuthor + "." + PluginName;
        public const string PluginAuthor = "codedly";
        public const string PluginName = "MercenaryTweaks";
        public const string PluginVersion = "1.3.0";

        public static ConfigEntry<bool> M1AttackSpeedFix { get; set; }
        public static ConfigEntry<bool> M2AttackSpeedIgnore { get; set; }
        public static ConfigEntry<bool> FullJumpReset { get; set; }
        public static ConfigEntry<bool> ExtraJumpOnKill { get; set; }
        public static ConfigEntry<int> JumpAmount { get; set; }
        public static ConfigEntry<bool> SingleTargetEvis { get; set; }
        public static ConfigEntry<bool> EvisMassacre { get; set; }
        public static ConfigEntry<float> EvisDamage { get; set; }
        public static ConfigEntry<bool> EvisSlayer { get; set; }

        // JumpReset, checks to see if a kill belonged to mercenary, if it did, reset his jumps
        public void FullJumpResetMethod(DamageReport report)
        {
            if (report.attackerBody && report.attackerBody.bodyIndex == BodyCatalog.FindBodyIndex("MercBody"))
            {
                report.attackerBody.characterMotor.jumpCount = 0;
            }
        }
        public void ExtraJumpOnKillMethod(DamageReport report)
        {
            if (report.attackerBody && report.attackerBody.bodyIndex == BodyCatalog.FindBodyIndex("MercBody")
                && report.attackerBody.characterMotor.jumpCount > 0)
            {
                report.attackerBody.characterMotor.jumpCount -= JumpAmount.Value;
            }
        }

        // MassacreCode, when eviscerate kills an enemy, resets the duration, unless you're holding the ability button
        // Thank you to the creators of the StandaloneAncientScepter mod for allowing me to use this code!
        // And thanks to Withor for making it work with SingleTargetEvis!
        public void MassacreCode(DamageReport rep)
        {
            CharacterBody attackerBody = rep.attackerBody;
            EntityState entityState;
            if (attackerBody == null)
            {
                entityState = null;
            }
            else
            {
                EntityStateMachine component = attackerBody.GetComponent<EntityStateMachine>();
                entityState = ((component != null) ? component.state : null);
            }
            EntityState entityState2 = entityState;
            Evis evis = entityState2 as Evis;
            bool flag = evis != null && rep.attackerBody && Vector3.Distance(rep.attackerBody.transform.position, rep.victim.transform.position) < Evis.maxRadius;
            if (flag)
            {
                Evis nextState = new Evis();
                attackerBody.GetComponent<EntityStateMachine>().SetNextState(nextState);
            }
        }
        public HurtBox target;
        public void Awake()
        {   // Config file entries for all of the tweaks

            // M1AttackSpeedFix, implemented, fully functional
            M1AttackSpeedFix = Config.Bind(

                "Merc tweaks",

                "M1 attack speed fix",

                true,

                "When enabled, gives Merc's 3rd m1 a fixed duration, allowing for consistent m1 extends, breaks at 18.0 attack speed, not sure why"
            );

            // M2AttackSpeedIgnore, implemented, fully functional
            M2AttackSpeedIgnore = Config.Bind(

                "Merc tweaks",

                "M2 Attack Speed Ignore",

                true,

                "When enabled, Whirlwind and Rising Thunder will ignore attack speed, making their utility consistent throughout the run"
            );

            // ExtraJumpOnKill, implemented, fully functional
            ExtraJumpOnKill = Config.Bind(

                "Merc tweaks",

                "Extra Jump On Kill",

                true,

                "When enabled, killing an enemy will give the amount of jumps specified in Jump Amount"
            );

            // JumpAmount, implemented, fully functional
            JumpAmount = Config.Bind(

                "Merc tweaks",

                "Jump Amount",

                1,

                "Amount of jumps given by Extra Jump On Kill, note: numbers higher than 1 can give you more jumps than you started with"
            );

            // FullJumpReset, implemented, fully functional
            FullJumpReset = Config.Bind(

                "Merc tweaks",

                "Reset ALL jumps on kill",

                false,

                "When enabled, killing an enemy resets all of your jumps, including ground jump, very cursed, pretty funny"
            );

            // SingleTargetEvis, implemented, fully functional
            SingleTargetEvis = Config.Bind(

                "Merc tweaks",

                "Single target Eviscerate",

                true,

                "When enabled, eviscerate will only attack one enemy at a time, instead of spreading its hits across all nearby targets, targets the lowest health enemy first"
            );

            // EvisMassacre, implemented, fully functional
            EvisMassacre = Config.Bind(

                "Merc tweaks",

                "Eviscerate Masacre upgrade",

                true,

                "When enabled, Eviscerate will work like its upgrade in ror1, resetting its duration and switching targets upon killing an enemy"
            );

            // EvisDamage, implemented, fully functional
            EvisDamage = Config.Bind(

                "Merc tweaks",

                "Eviscerate Damage Coefficient",

                1.1f,

                "This number represents how much of your base damage each hit deals, i.e, 1.1 = 110%, 2.0 = 200%, etc"
            );

            // EvisSlayer, implemented, fully functional
            EvisSlayer = Config.Bind(

                "Merc tweaks",

                "Eviscerate Slayer Upgrade",

                true,

                "When enabled, Eviscerate will gain slayer, making it deal more damage to low health targets"
            );

            // this code prevents eviscerate from targeting allies
            On.EntityStates.Merc.EvisDash.FixedUpdate += (orig, self) =>
            {
                self.stopwatch += Time.fixedDeltaTime;
                if (self.stopwatch > EvisDash.dashPrepDuration && !self.isDashing)
                {
                    self.isDashing = true;
                    self.dashVector = self.inputBank.aimDirection;
                    self.CreateBlinkEffect(Util.GetCorePosition(self.gameObject));
                    self.PlayCrossfade("FullBody, Override", "EvisLoop", 0.1f);
                    if (self.modelTransform)
                    {
                        TemporaryOverlayInstance temporaryOverlay = TemporaryOverlayManager.AddOverlay(self.modelTransform.gameObject);
                        temporaryOverlay.duration = 0.6f;
                        temporaryOverlay.animateShaderAlpha = true;
                        temporaryOverlay.alphaCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);
                        temporaryOverlay.destroyComponentOnEnd = true;
                        temporaryOverlay.originalMaterial = Resources.Load<Material>("Materials/matHuntressFlashBright");
                        temporaryOverlay.AddToCharacterModel(self.modelTransform.GetComponent<CharacterModel>());
                        TemporaryOverlayInstance temporaryOverlay2 = TemporaryOverlayManager.AddOverlay(self.modelTransform.gameObject);
                        temporaryOverlay2.duration = 0.7f;
                        temporaryOverlay2.animateShaderAlpha = true;
                        temporaryOverlay2.alphaCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);
                        temporaryOverlay2.destroyComponentOnEnd = true;
                        temporaryOverlay2.originalMaterial = Resources.Load<Material>("Materials/matHuntressFlashExpanded");
                        temporaryOverlay2.AddToCharacterModel(self.modelTransform.GetComponent<CharacterModel>());
                    }
                }
                bool flag = self.stopwatch >= EvisDash.dashDuration + EvisDash.dashPrepDuration;
                if (self.isDashing)
                {
                    if (self.characterMotor && self.characterDirection)
                    {
                        self.characterMotor.rootMotion += self.dashVector * (self.moveSpeedStat * EvisDash.speedCoefficient * Time.fixedDeltaTime);
                    }
                    if (self.isAuthority)
                    {
                        Collider[] array = Physics.OverlapSphere(self.transform.position, self.characterBody.radius + EvisDash.overlapSphereRadius * (flag ? EvisDash.lollypopFactor : 1f), LayerIndex.entityPrecise.mask);
                        for (int i = 0; i < array.Length; i++)
                        {
                            HurtBox component = array[i].GetComponent<HurtBox>();
                            if (component && component.healthComponent != self.healthComponent && !(component.teamIndex == self.teamComponent.teamIndex))
                            {
                                Evis nextState = new Evis();
                                self.outer.SetNextState(nextState);
                                return;
                            }
                        }
                    }
                }
                if (flag && self.isAuthority)
                {
                    self.outer.SetNextStateToMain();
                }
            };


            // SingleTargetEvis, makes eviscerate only target one enemy at a time, and target the lowest health enemy
            if (SingleTargetEvis.Value)
            {
                On.EntityStates.Merc.Evis.OnEnter += (orig, self) =>
                {
                    orig(self);
                    BullseyeSearch bullseyeSearch = new BullseyeSearch();
                    bullseyeSearch.searchOrigin = self.transform.position;
                    bullseyeSearch.searchDirection = UnityEngine.Random.onUnitSphere;
                    bullseyeSearch.maxDistanceFilter = Evis.maxRadius;
                    bullseyeSearch.teamMaskFilter = TeamMask.GetUnprotectedTeams(TeamComponent.GetObjectTeam(self.gameObject));
                    bullseyeSearch.sortMode = BullseyeSearch.SortMode.Distance;
                    bullseyeSearch.RefreshCandidates();
                    bullseyeSearch.FilterOutGameObject(self.gameObject);
                    IEnumerable<HurtBox> a = bullseyeSearch.GetResults();
                    target = a.FirstOrDefault<HurtBox>();
                    for (int i = 0; i < a.Count<HurtBox>(); i++)
                    {
                        float health = a.ElementAt(i).healthComponent.health;
                        if (health < target.healthComponent.health)
                        {
                            target = a.ElementAt<HurtBox>(i);
                        }
                    }
                };
                On.EntityStates.Merc.Evis.FixedUpdate += (orig, self) =>
                {
                    self.stopwatch += Time.fixedDeltaTime;
                    self.attackStopwatch += Time.fixedDeltaTime;
                    float num = 1f / Evis.damageFrequency / self.attackSpeedStat;
                    if (self.attackStopwatch >= num)
                    {
                        self.attackStopwatch -= num;
                        HurtBox hurtBox = target;
                        if (hurtBox)
                        {
                            Util.PlayAttackSpeedSound(Evis.slashSoundString, base.gameObject, Evis.slashPitch);
                            Util.PlaySound(Evis.dashSoundString, base.gameObject);
                            Util.PlaySound(Evis.impactSoundString, base.gameObject);
                            HurtBoxGroup hurtBoxGroup = hurtBox.hurtBoxGroup;
                            HurtBox hurtBox2 = hurtBoxGroup.hurtBoxes[UnityEngine.Random.Range(0, hurtBoxGroup.hurtBoxes.Length - 1)];
                            if (hurtBox2)
                            {
                                Vector3 position = hurtBox2.transform.position;
                                Vector2 normalized = UnityEngine.Random.insideUnitCircle.normalized;
                                Vector3 normal = new Vector3(normalized.x, 0f, normalized.y);
                                EffectManager.SimpleImpactEffect(Evis.hitEffectPrefab, position, normal, false);
                                Transform transform = hurtBox.hurtBoxGroup.transform;
                                if (NetworkServer.active)
                                {
                                    DamageInfo damageInfo = new DamageInfo();
                                    damageInfo.damage = Evis.damageCoefficient * self.damageStat;
                                    damageInfo.attacker = self.gameObject;
                                    damageInfo.procCoefficient = Evis.procCoefficient;
                                    damageInfo.position = hurtBox2.transform.position;
                                    damageInfo.crit = self.crit;

                                    // EvisSlayer, when enabled, Eviscerate deals more damage the lower an enemy's health is
                                    if (EvisSlayer.Value)
                                    {
                                        damageInfo.damageType.damageType = DamageType.BonusToLowHealth;
                                    }

                                    hurtBox2.healthComponent.TakeDamage(damageInfo);
                                    GlobalEventManager.instance.OnHitEnemy(damageInfo, hurtBox2.healthComponent.gameObject);
                                    GlobalEventManager.instance.OnHitAll(damageInfo, hurtBox2.healthComponent.gameObject);
                                }
                            }
                        }
                        else if (self.isAuthority && self.stopwatch > Evis.minimumDuration)
                        {
                            self.outer.SetNextStateToMain();
                        }
                    }
                    if (self.characterMotor)
                    {
                        self.characterMotor.velocity = Vector3.zero;
                    }
                    if (self.stopwatch >= Evis.duration && self.isAuthority)
                    {
                        self.outer.SetNextStateToMain();
                    }
                };
            }
            else
            {
                On.EntityStates.Merc.Evis.FixedUpdate += (orig, self) =>
                {
                    self.stopwatch += Time.fixedDeltaTime;
                    self.attackStopwatch += Time.fixedDeltaTime;
                    float num = 1f / Evis.damageFrequency / self.attackSpeedStat;
                    if (self.attackStopwatch >= num)
                    {
                        self.attackStopwatch -= num;
                        HurtBox hurtBox = self.SearchForTarget();
                        if (hurtBox)
                        {
                            Util.PlayAttackSpeedSound(Evis.slashSoundString, base.gameObject, Evis.slashPitch);
                            Util.PlaySound(Evis.dashSoundString, base.gameObject);
                            Util.PlaySound(Evis.impactSoundString, base.gameObject);
                            HurtBoxGroup hurtBoxGroup = hurtBox.hurtBoxGroup;
                            HurtBox hurtBox2 = hurtBoxGroup.hurtBoxes[UnityEngine.Random.Range(0, hurtBoxGroup.hurtBoxes.Length - 1)];
                            if (hurtBox2)
                            {
                                Vector3 position = hurtBox2.transform.position;
                                Vector2 normalized = UnityEngine.Random.insideUnitCircle.normalized;
                                Vector3 normal = new Vector3(normalized.x, 0f, normalized.y);
                                EffectManager.SimpleImpactEffect(Evis.hitEffectPrefab, position, normal, false);
                                Transform transform = hurtBox.hurtBoxGroup.transform;
                                if (NetworkServer.active)
                                {
                                    DamageInfo damageInfo = new DamageInfo();
                                    damageInfo.damage = Evis.damageCoefficient * self.damageStat;
                                    damageInfo.attacker = base.gameObject;
                                    damageInfo.procCoefficient = Evis.procCoefficient;
                                    damageInfo.position = hurtBox2.transform.position;
                                    damageInfo.crit = self.crit;

                                    // EvisSlayer, when enabled, Eviscerate deals more damage the lower an enemy's health is
                                    if (EvisSlayer.Value)
                                    {
                                        damageInfo.damageType.damageType = DamageType.BonusToLowHealth;
                                    }

                                    hurtBox2.healthComponent.TakeDamage(damageInfo);
                                    GlobalEventManager.instance.OnHitEnemy(damageInfo, hurtBox2.healthComponent.gameObject);
                                    GlobalEventManager.instance.OnHitAll(damageInfo, hurtBox2.healthComponent.gameObject);
                                }
                            }
                        }
                        else if (self.isAuthority && self.stopwatch > Evis.minimumDuration)
                        {
                            self.outer.SetNextStateToMain();
                        }
                    }
                    if (self.characterMotor)
                    {
                        self.characterMotor.velocity = Vector3.zero;
                    }
                    if (self.stopwatch >= Evis.duration && self.isAuthority)
                    {
                        self.outer.SetNextStateToMain();
                    }

                };
            }

            // M1AttackSpeedFix, when enabled, the 3rd basic attack will ignore attack speed
            if (M1AttackSpeedFix.Value)
            {
                On.EntityStates.Merc.Weapon.GroundLight2.OnEnter += (orig, self) =>
                {
                    if (self.step == 2)
                    {
                        self.ignoreAttackSpeed = true;
                    }
                    orig(self);
                    if (self.ignoreAttackSpeed && self.isComboFinisher)
                    {
                        self.durationBeforeInterruptable = EntityStates.Merc.Weapon.GroundLight2.comboFinisherBaseDurationBeforeInterruptable;
                    }
                };
            }

            // M2AttackSpeedIgnore, when enabled, whirlwind and rising thunder will be unaffected by attack speed
            if (M2AttackSpeedIgnore.Value)
            {
                On.EntityStates.Merc.Uppercut.PlayAnim += (orig, self) =>
                {
                    self.duration = Uppercut.baseDuration;
                    orig(self);
                };
                On.EntityStates.Merc.WhirlwindAir.PlayAnim += (orig, self) =>
                {
                    self.duration = self.baseDuration;
                    orig(self);
                };
                On.EntityStates.Merc.WhirlwindGround.PlayAnim += (orig, self) =>
                {
                    self.duration = self.baseDuration;
                    orig(self);
                };
            }

            // FullJumpReset, when enabled, runs the FullJumpResetMethod when a character dies
            if (FullJumpReset.Value)
            {
                GlobalEventManager.onCharacterDeathGlobal += FullJumpResetMethod;
            }

            // ExtraJumpOnKill, when enabled, runs the ExtraJumpOnKillMethod when a character dies
            if (ExtraJumpOnKill.Value)
            {
                GlobalEventManager.onCharacterDeathGlobal += ExtraJumpOnKillMethod;
            }

            // EvisMassacre, when enabled, runs the MassacreCode method when a character dies
            if (EvisMassacre.Value)
            {
                GlobalEventManager.onCharacterDeathGlobal += MassacreCode;
            }

            //EvisDamage, sets eviscerate's damage coefficient to the config value
            On.EntityStates.Merc.Evis.OnEnter += (orig, self) =>
            {
                Evis.damageCoefficient = EvisDamage.Value;

                orig(self);
            };
        }
    }
}