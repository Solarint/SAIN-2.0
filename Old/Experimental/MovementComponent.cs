using EFT;
using System.Collections;
using UnityEngine;

namespace SAIN.Components
{
    public class MovementSpeed : MonoBehaviour
    {
        public float MoveSpeed { get; private set; } = 0f;
        public float AimMoveSpeed { get; private set; } = 0f;
        public float PoseLevel { get; private set; } = 0f;

        private BotOwner bot;

        private void Awake()
        {
            bot = GetComponent<BotOwner>();
            StartCoroutine(ContinuousSpeed());
        }

        // Runs continuously in the background to check for suitable times to use lean
        private IEnumerator ContinuousSpeed()
        {
            while (true)
            {
                // Redundant CheckForCalcPath if the BotOwner is alive before continuing
                if (!bot.GetPlayer.HealthController.IsAlive || bot.GetPlayer == null)
                {
                    StopAllCoroutines();
                    yield break;
                }

                if (bot.IsRole(WildSpawnType.marksman))
                {
                    StopAllCoroutines();
                    yield break;
                }

                if (ShouldIChange()) WhatSpeedShouldIPick();

                // Overall CheckForCalcPath Frequency
                yield return new WaitForSeconds(0.2f);
            }
        }

        // Logic checks for when to execute lean or reset
        private bool ShouldIChange()
        {
            // CheckForCalcPath if the BotOwner is alive before continuing, and stop the Coroutine if they are dead.
            if (bot?.GetPlayer?.HealthController?.IsAlive == false) StopAllCoroutines();

            // Makes sure the BotOwner is active before sending lean commands
            if (bot?.BotState != EBotState.Active) return false;

            return true;
        }
        public void WhatSpeedShouldIPick()
        {
            // CheckForCalcPath if the BotOwner is alive before continuing, and stop the Coroutine if they are dead.
            if (!bot.GetPlayer.HealthController.IsAlive) StopAllCoroutines();

            // Makes sure the BotOwner is active before sending lean commands
            if (bot.BotState != EBotState.Active) return;

            if (bot.Memory.IsPeace) // Peace Mode
            {
                MoveSpeed = 0.75f;
                PoseLevel = 1.0f;
                AimMoveSpeed = 0.65f;

                // Slows down Scavs Even More
                if (bot.IsRole(WildSpawnType.assault))
                {
                    MoveSpeed = 0.5f;
                    PoseLevel = 1.0f;
                    AimMoveSpeed = 0.5f;
                }
                ChangeSpeed(MoveSpeed, PoseLevel, AimMoveSpeed);
                return;
            }
            else // Combat Mode
            {
                MoveSpeed = 0.9f;
                PoseLevel = 1.0f;
                AimMoveSpeed = 0.75f;

                // Speed bots up in close quarters
                if (bot.Memory.GoalEnemy.Distance <= 20f)
                {
                    MoveSpeed = 1.0f;
                    PoseLevel = 1.0f;
                    AimMoveSpeed = 0.85f;
                }
                ChangeSpeed(MoveSpeed, PoseLevel, AimMoveSpeed);
                return;
            }
        }
        private void ChangeSpeed(float movespeed, float pose, float aimmovespeed)
        {
            // Bot Commands
            bot.SetTargetMoveSpeed(MoveSpeed);
            bot.Mover.SetTargetMoveSpeed(MoveSpeed);

            bot.GetPlayer.MovementContext.SetAimingSlowdown(false, AimMoveSpeed);

            bot.SetPose(PoseLevel);
            bot.GetPlayer.ChangePose(PoseLevel);
        }
    }
}

