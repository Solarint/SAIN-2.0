﻿using BepInEx.Logging;
using EFT;
using SAIN.Components;

namespace SAIN.Classes
{
    public class MovementClass : SAINBot
    {
        public MovementClass(BotOwner bot) : base(bot)
        {
            Logger = BepInEx.Logging.Logger.CreateLogSource(this.GetType().Name);
        }

        public void DecideMovementSpeed()
        {
        }

        public void SetSprint(bool value)
        {
            if (value)
            {
                BotOwner.SetPose(1f);
                BotOwner.SetTargetMoveSpeed(1f);
            }
            BotOwner.GetPlayer.EnableSprint(value);
            BotOwner.Sprint(value);
        }

        private void FullSpeed()
        {
            BotOwner.SetTargetMoveSpeed(1f);
        }

        private void Sneak()
        {
            BotOwner.SetPose(0f);
            BotOwner.SetTargetMoveSpeed(0f);
        }

        private void SlowWalk()
        {
            BotOwner.SetTargetMoveSpeed(0.45f);
        }

        private void NormalSpeed()
        {
            BotOwner.SetTargetMoveSpeed(0.85f);
        }

        private bool ShouldBotSneak
        {
            get
            {
                if (BotOwner.Memory.GoalEnemy == null)
                {
                    return true;
                }
                else
                {
                    return !SAIN.HasEnemyAndCanShoot;
                }
            }
        }

        private readonly ManualLogSource Logger;
    }
}