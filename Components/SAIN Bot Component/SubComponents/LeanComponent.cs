using BepInEx.Logging;
using EFT;
using SAIN.Helpers;
using SAIN.Layers.Logic;
using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;
using static SAIN.UserSettings.DebugConfig;

namespace SAIN.Components
{
    public class LeanComponent : MonoBehaviour
    {
        public List<SAINLogicDecision> DontLeanDecisions = new List<SAINLogicDecision> { SAINLogicDecision.Surgery, SAINLogicDecision.None, SAINLogicDecision.Reload, SAINLogicDecision.RunForCover, SAINLogicDecision.RunAway, SAINLogicDecision.FirstAid, SAINLogicDecision.Stims, SAINLogicDecision.RunAwayGrenade };

        private void Awake()
        {
            SAIN = GetComponent<SAINComponent>();

            Lean = new BotLean(BotOwner);
            SideStep = new BotSideStep(BotOwner);
            BlindFire = new BotBlindFire(BotOwner);

            Logger = BepInEx.Logging.Logger.CreateLogSource(GetType().Name);
        }

        private void Update()
        {
            if (SAIN.BotActive && TargetPosition != null && !DontLeanDecisions.Contains(SAIN.CurrentDecision))
            {
                if (LeanCoroutine == null)
                {
                    LeanCoroutine = StartCoroutine(BotLeanLoop());

                    SideStepCoroutine = StartCoroutine(BotSideStepLoop());

                    BlindFireCoroutine = StartCoroutine(BotBlindFireLoop());
                }
            }
            else
            {
                if (LeanCoroutine != null)
                {
                    Lean.SetLean(0f);
                    BlindFire.SetBlindFire(0);
                    SideStep.SetSideStep(0f);

                    StopCoroutine(LeanCoroutine);
                    LeanCoroutine = null;

                    StopCoroutine(SideStepCoroutine);
                    SideStepCoroutine = null;

                    StopCoroutine(BlindFireCoroutine);
                    BlindFireCoroutine = null;
                }
            }
        }

        private IEnumerator BotLeanLoop()
        {
            var wait = new WaitForSeconds(0.33f);

            while (true)
            {
                Lean.FindLeanDirectionRayCast(TargetPosition.Value);

                if (!DontLeanDecisions.Contains(SAIN.CurrentDecision))
                {
                    Lean.SetLean(Lean.Angle);
                }

                yield return wait;
            }
        }

        private IEnumerator BotSideStepLoop()
        {
            var wait = new WaitForSeconds(0.66f);

            while (true)
            {
                if (SAIN.CurrentDecision == SAINLogicDecision.HoldInCover)
                {
                    SideStep.Update();
                }

                yield return wait;
            }
        }

        private IEnumerator BotBlindFireLoop()
        {
            var wait = new WaitForSeconds(0.66f);

            while (true)
            {
                BlindFire.Update(TargetPosition.Value);

                yield return wait;
            }
        }

        public Vector3? CheckLeanPositions
        {
            get
            {
                var lean = Lean.RayCast;

                Vector3? MoveLeanPos = null;

                if (lean.LeftLosPos != null)
                {
                    MoveLeanPos = lean.LeftLosPos;
                }
                else if (lean.RightLosPos != null)
                {
                    MoveLeanPos = lean.RightLosPos;
                }

                return MoveLeanPos;
            }
        }

        public Vector3? TargetPosition
        {
            get
            {
                if (BotOwner.Memory.GoalEnemy != null)
                {
                    BotOwner.Memory.GoalEnemy.Person.MainParts.TryGetValue(BodyPartType.body, out BodyPartClass body);
                    return body.Position;
                }
                else if (BotOwner.Memory.GoalTarget?.GoalTarget?.Position != null)
                {
                    return BotOwner.Memory.GoalTarget.GoalTarget.Position;
                }
                else
                {
                    return null;
                }
            }
        }

        public LeanSetting LeanDirection => Lean.LeanDirection;

        public bool BotIsLeaning => LeanDirection == LeanSetting.Left || LeanDirection == LeanSetting.Right;

        public BotLean Lean { get; private set; }

        public BotSideStep SideStep { get; private set; }

        public BotBlindFire BlindFire { get; private set; }

        public void Dispose()
        {
            StopAllCoroutines();
            Destroy(this);
        }

        private Coroutine LeanCoroutine;

        private Coroutine SideStepCoroutine;

        private Coroutine BlindFireCoroutine;

        private ManualLogSource Logger;

        private BotOwner BotOwner => SAIN.BotOwner;

        private SAINComponent SAIN;

        public class BotLean : SAINBot
        {
            public LeanSetting LeanDirection => RayCast.LeanDirection;
            public LeanRayCast RayCast { get; private set; }

            public BotLean(BotOwner bot) : base(bot)
            {
                RayCast = new LeanRayCast(bot);
                Logger = BepInEx.Logging.Logger.CreateLogSource(GetType().Name);
            }

            public void SetLean(float num)
            {
                BotOwner.GetPlayer.MovementContext.SetTilt(num);
            }

            public float Angle
            {
                get
                {
                    float angle;
                    switch (LeanDirection)
                    {
                        case LeanSetting.Left:
                            angle = -5f;
                            break;

                        case LeanSetting.Right:
                            angle = 5f;
                            break;

                        default:
                            angle = 0f;
                            break;
                    }
                    return angle;
                }
            }

            public LeanSetting FindLeanDirectionRayCast(Vector3 targetPos)
            {
                return RayCast.FindLeanDirectionRayCast(targetPos);
            }

            private LeanSetting FindLeanDirection(Vector3 targetPos)
            {
                NavMeshPath path = new NavMeshPath();
                if (NavMesh.CalculatePath(BotOwner.Transform.position, targetPos, -1, path))
                {
                    // Corner 0 is at BotOwner position. So we need corner 1 and 2 to check lean angle.
                    if (path.corners.Length > 2)
                    {
                        Vector3 cornerADirection = (path.corners[1] - BotOwner.Transform.position).normalized;

                        var dirToEnemy = (targetPos - BotOwner.Transform.position).normalized;
                        Quaternion rotation = Quaternion.Euler(0, 90, 0);

                        var rightOfEnemy = rotation * dirToEnemy;

                        if (Vector3.Dot(cornerADirection, rightOfEnemy) > 0f)
                        {
                            return LeanSetting.Right;
                        }
                        else
                        {
                            return LeanSetting.Left;
                        }
                    }
                }

                return LeanSetting.None;
            }

            private readonly ManualLogSource Logger;

            public class LeanRayCast : SAIN.SAINBot
            {
                public LeanSetting LeanDirection { get; set; }

                public LeanRayCast(BotOwner bot) : base(bot)
                {
                    Logger = BepInEx.Logging.Logger.CreateLogSource(this.GetType().Name + $": {bot.name}: ");
                }

                public LeanSetting FindLeanDirectionRayCast(Vector3 targetPos)
                {
                    if (RayTimer < Time.time)
                    {
                        RayTimer = Time.time + 0.25f;

                        DirectLineOfSight = CheckOffSetRay(targetPos, 0f, 0f, out var direct);

                        RightLos = CheckOffSetRay(targetPos, 90f, 0.66f, out var rightOffset);
                        if (!RightLos)
                        {
                            RightLosPos = rightOffset;

                            rightOffset.y = BotOwner.Position.y;
                            float halfDist1 = (rightOffset - BotOwner.Position).magnitude / 2f;

                            RightHalfLos = CheckOffSetRay(targetPos, 90f, halfDist1, out var rightHalfOffset);
                            if (!RightHalfLos)
                            {
                                RightHalfLosPos = rightHalfOffset;
                            }
                            else
                            {
                                RightHalfLosPos = null;
                            }
                        }
                        else
                        {
                            RightLosPos = null;
                            RightHalfLosPos = null;
                        }

                        LeftLos = CheckOffSetRay(targetPos, -90f, 0.66f, out var leftOffset);
                        if (!LeftLos)
                        {
                            LeftLosPos = leftOffset;

                            leftOffset.y = BotOwner.Position.y;
                            float halfDist2 = (leftOffset - BotOwner.Position).magnitude / 2f;

                            LeftHalfLos = CheckOffSetRay(targetPos, -90f, halfDist2, out var leftHalfOffset);

                            if (!LeftHalfLos)
                            {
                                LeftHalfLosPos = leftHalfOffset;
                            }
                            else
                            {
                                LeftHalfLosPos = null;
                            }
                        }
                        else
                        {
                            LeftLosPos = null;
                            LeftHalfLosPos = null;
                        }
                    }

                    var setting = GetSettingFromResults();
                    LeanDirection = setting;
                    return setting;
                }

                private float RayTimer = 0f;

                public LeanSetting GetSettingFromResults()
                {
                    LeanSetting setting;

                    if (SAIN.Lean.DontLeanDecisions.Contains(SAIN.CurrentDecision))
                    {
                        return LeanSetting.None;
                    }

                    if (BotOwner.Memory.GoalEnemy != null && DirectLineOfSight)
                    {
                        //return LeanSetting.None;
                    }

                    if ((LeftLos || LeftHalfLos) && !RightLos)
                    {
                        setting = LeanSetting.Left;
                    }
                    else if (!LeftLos && (RightLos || RightHalfLos))
                    {
                        setting = LeanSetting.Right;
                    }
                    else
                    {
                        setting = LeanSetting.None;
                    }

                    return setting;
                }

                private bool CheckOffSetRay(Vector3 targetPos, float angle, float dist, out Vector3 Point)
                {
                    Vector3 startPos = BotOwner.Position;
                    startPos.y = SAIN.HeadPosition.y;

                    if (dist > 0f)
                    {
                        var dirToEnemy = (targetPos - BotOwner.Position).normalized;

                        Quaternion rotation = Quaternion.Euler(0, angle, 0);

                        Vector3 direction = rotation * dirToEnemy;

                        Point = FindOffset(startPos, direction, dist);

                        if ((Point - startPos).magnitude < dist / 3f)
                        {
                            return true;
                        }
                    }
                    else
                    {
                        Point = startPos;
                    }

                    bool LOS = LineOfSight(Point, targetPos);

                    Point.y = BotOwner.Position.y;

                    return LOS;
                }

                private bool LineOfSight(Vector3 start, Vector3 target)
                {
                    var direction = target - start;
                    float distance = Mathf.Clamp(direction.magnitude, 0f, 8f);
                    //DebugGizmos.SingleObjects.Ray(start, direction, Color.yellow, distance, 0.01f, true, 0.25f, true);

                    if (!Physics.Raycast(start, direction, distance, LayerMaskClass.HighPolyWithTerrainMask))
                    {
                        return true;
                    }
                    return false;
                }

                private Vector3 FindOffset(Vector3 start, Vector3 direction, float distance)
                {
                    if (Physics.Raycast(start, direction, out var hit, distance, LayerMaskClass.HighPolyWithTerrainMask))
                    {
                        return hit.point;
                    }
                    else
                    {
                        return start + direction.normalized * distance;
                    }
                }

                public bool DirectLineOfSight { get; set; }

                public bool LeftLos { get; set; }
                public Vector3? LeftLosPos { get; set; }

                public bool LeftHalfLos { get; set; }
                public Vector3? LeftHalfLosPos { get; set; }

                public bool RightLos { get; set; }
                public Vector3? RightLosPos { get; set; }

                public bool RightHalfLos { get; set; }
                public Vector3? RightHalfLosPos { get; set; }

                protected ManualLogSource Logger;
            }
        }

        public class BotSideStep : SAINBot
        {
            public SideStepSetting CurrentSideStep { get; private set; }

            public BotSideStep(BotOwner bot) : base(bot)
            {
                Logger = BepInEx.Logging.Logger.CreateLogSource(GetType().Name);
            }

            public void Update()
            {
                if (!SAIN.BotActive)
                {
                    return;
                }

                if (BotOwner.Memory.GoalEnemy == null)
                {
                    return;
                }

                var lean = SAIN.Lean.LeanDirection;
                var move = BotOwner.GetPlayer.MovementContext;
                var enemy = BotOwner.Memory.GoalEnemy;

                float value = 0f;
                SideStepSetting setting = SideStepSetting.None;

                if (!enemy.CanShoot && !enemy.IsVisible)
                {
                    switch (lean)
                    {
                        case LeanSetting.Left:
                            value = -1f;
                            setting = SideStepSetting.Left;
                            break;

                        case LeanSetting.Right:
                            value = 1f;
                            setting = SideStepSetting.Right;
                            break;

                        default:
                            break;
                    }
                }

                SetSideStep(value);

                CurrentSideStep = setting;
            }

            public void SetSideStep(float value)
            {
                BotOwner.GetPlayer.MovementContext.SetSidestep(value);
            }

            private readonly ManualLogSource Logger;
        }

        public class BotBlindFire : SAINBot
        {
            public BotBlindFire(BotOwner bot) : base(bot)
            {
                Logger = BepInEx.Logging.Logger.CreateLogSource(this.GetType().Name);
            }

            public void Update(Vector3 targetPos)
            {
                if (BotOwner.Memory.GoalEnemy == null)
                {
                    return;
                }

                if (BlindFireTimer > Time.time)
                {
                    return;
                }

                var enemy = BotOwner.Memory.GoalEnemy;
                int blindfire = 0;

                if (!enemy.CanShoot)
                {
                    if (RayCastCheck(BotOwner.WeaponRoot.position, targetPos))
                    {
                        Vector3 rayPoint = BotOwner.LookSensor._headPoint;
                        rayPoint.y += 0.1f;

                        if (!RayCastCheck(rayPoint, targetPos))
                        {
                            blindfire = 1;
                        }
                    }
                }

                if (blindfire == 1 && BotOwner.WeaponManager.IsReady && BotOwner.WeaponManager.HaveBullets)
                {
                    SetBlindFire(blindfire);
                    BotOwner.Steering.LookToPoint(targetPos);
                    //BotOwner.ShootData.Shoot();
                }
                else
                {
                    SetBlindFire(0);
                }
            }

            private float BlindFireTimer = 0f;

            private bool RayCastCheck(Vector3 start, Vector3 targetPos)
            {
                Vector3 direction = targetPos - start;
                float magnitude = (targetPos - start).magnitude;
                return Physics.Raycast(start, direction, magnitude, LayerMaskClass.HighPolyWithTerrainMask);
            }

            public void SetBlindFire(int value)
            {
                BotOwner.GetPlayer.MovementContext.SetBlindFire(value);
            }

            private int GetBlindFire => BotOwner.GetPlayer.MovementContext.BlindFire;

            private readonly ManualLogSource Logger;
        }
    }
}