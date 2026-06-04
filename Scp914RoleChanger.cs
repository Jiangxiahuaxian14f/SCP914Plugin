using System;
using System.Collections.Generic;
using Exiled.API.Features;
using Exiled.Events.EventArgs.Scp914;
using PlayerRoles;
using MEC;

namespace Scp914RoleChanger
{
    public class Plugin : Plugin<Config>
    {
        public override string Name => "Scp914RoleChanger";
        public override string Author => "YourName";
        public override Version Version => new Version(2, 2, 0);
        private static readonly Random _random = new Random();
        private HashSet<RoleTypeId> humans = new HashSet<RoleTypeId>()
        {
            RoleTypeId.ClassD, RoleTypeId.Scientist, RoleTypeId.FacilityGuard,
            RoleTypeId.NtfCadet, RoleTypeId.NtfPrivate, RoleTypeId.NtfSergeant,
            RoleTypeId.NtfSpecialist, RoleTypeId.NtfCaptain,
            RoleTypeId.ChaosConscript, RoleTypeId.ChaosMarauder,
            RoleTypeId.ChaosRepressor, RoleTypeId.ChaosRifleman
        };

        public override void OnEnabled()
        {
            Exiled.Events.Handlers.Scp914.UpgradingPlayer += OnScp914Upgrading;
            base.OnEnabled();
        }

        public override void OnDisabled()
        {
            Exiled.Events.Handlers.Scp914.UpgradingPlayer -= OnScp914Upgrading;
            base.OnDisabled();
        }

        private void OnScp914Upgrading(UpgradingPlayerEventArgs ev)
        {
            RoleTypeId role = ev.Player.Role.Type;

            // SCP 和教程角色不受任何影响
            if (role == RoleTypeId.Tutorial || role.ToString().StartsWith("Scp"))
                return;

            string knockdown = ev.KnobSetting.ToString();

            // 超粗加工 (Coarse) -> 50%死亡 50%小僵尸
            if (knockdown.Equals("Coarse"))
            {
                if (_random.Next(2) == 0)
                    Kill(ev, "SCP-914 的超粗加工将你碾碎了");
                else
                    BecomeZombie(ev);
                return;
            }

            // 超精加工 (VeryFine) -> 30%死亡 30%小僵 40%任意人类
            if (knockdown.Equals("VeryFine"))
            {
                int chance = _random.Next(1, 101);
                if (chance <= 30)
                    Kill(ev, "SCP-914 的超精加工将你粉碎了");
                else if (chance <= 60)
                    BecomeZombie(ev);
                else
                    BecomeRandomHuman(ev, role);
                return;
            }

            // 其他档位：先 25%小僵 25%死亡，剩余50%执行角色特定规则
            int special = _random.Next(1, 101);
            if (special <= 25)
            {
                BecomeZombie(ev);
                return;
            }
            if (special <= 50)
            {
                Kill(ev, "SCP-914 将你粉碎了");
                return;
            }

            // 剩余 50% 执行特定规则
            if (role == RoleTypeId.ClassD) HandleClassD(ev, knockdown);
            else if (role == RoleTypeId.Scientist) HandleScientist(ev, knockdown);
            else if (role == RoleTypeId.FacilityGuard) HandleGuard(ev, knockdown);
            else if (IsChaosOrNtf(role)) HandleChaosNtf(ev, role, knockdown);
            else Kill(ev, "SCP-914 将你碾碎了");
        }

        private bool IsChaosOrNtf(RoleTypeId role) => role.ToString().Contains("Chaos") || role.ToString().Contains("Ntf");

        private void HandleClassD(UpgradingPlayerEventArgs ev, string knockdown)
        {
            if (knockdown.Equals("OneToOne"))
                ChangeRole(ev, RoleTypeId.Scientist, "科学家");
            else if (knockdown.Equals("Fine"))
                ChangeRole(ev, RoleTypeId.FacilityGuard, "保安");
            else
                Kill(ev, "SCP-914 将你粉碎了");
        }

        private void HandleScientist(UpgradingPlayerEventArgs ev, string knockdown)
        {
            if (knockdown.Equals("OneToOne"))
                ChangeRole(ev, RoleTypeId.ClassD, "D级人员");
            else if (knockdown.Equals("Fine"))
                ChangeRole(ev, RoleTypeId.FacilityGuard, "保安");
            else
                Kill(ev, "SCP-914 将你粉碎了");
        }

        private void HandleGuard(UpgradingPlayerEventArgs ev, string knockdown)
        {
            if (knockdown.Equals("Rough")) // 半粗加工：50% D级，50% 科学家
            {
                if (_random.Next(2) == 0)
                    ChangeRole(ev, RoleTypeId.ClassD, "D级人员");
                else
                    ChangeRole(ev, RoleTypeId.Scientist, "科学家");
            }
            else if (knockdown.Equals("Fine")) // 精加工：50% 混沌，50% 九尾狐
            {
                if (_random.Next(2) == 0)
                    ChangeRole(ev, GetRandomChaosRole(), "混沌");
                else
                    ChangeRole(ev, GetRandomNtfRole(), "九尾狐");
            }
            else if (knockdown.Equals("OneToOne"))
            {
                ChangeRole(ev, RoleTypeId.Scientist, "科学家");
            }
            // 其他档位不处理（已经在上层概率判定中可能死亡或变僵）
        }

        private void HandleChaosNtf(UpgradingPlayerEventArgs ev, RoleTypeId role, string knockdown)
        {
            if (knockdown.Equals("OneToOne") || knockdown.Equals("Fine"))
            {
                if (role.ToString().Contains("Chaos"))
                    ChangeRole(ev, GetRandomNtfRole(), "九尾狐");
                else
                    ChangeRole(ev, GetRandomChaosRole(), "混沌");
            }
            else if (knockdown.Equals("Rough") || knockdown.Equals("SlightlyRough"))
            {
                ChangeRole(ev, RoleTypeId.FacilityGuard, "保安");
            }
            else
            {
                Kill(ev, "SCP-914 将你碾碎了");
            }
        }

        private RoleTypeId GetRandomNtfRole()
        {
            RoleTypeId[] ntf = { RoleTypeId.NtfCadet, RoleTypeId.NtfPrivate, RoleTypeId.NtfSergeant, RoleTypeId.NtfSpecialist, RoleTypeId.NtfCaptain };
            return ntf[_random.Next(ntf.Length)];
        }

        private RoleTypeId GetRandomChaosRole()
        {
            RoleTypeId[] chaos = { RoleTypeId.ChaosConscript, RoleTypeId.ChaosMarauder, RoleTypeId.ChaosRepressor, RoleTypeId.ChaosRifleman };
            return chaos[_random.Next(chaos.Length)];
        }

        private void ChangeRole(UpgradingPlayerEventArgs ev, RoleTypeId newRole, string roleName)
        {
            ev.Player.Role.Set(newRole);
            ev.Player.ShowHint($"SCP-914 将你变成了{roleName}！", 5);
            Map.Broadcast(5, $"{ev.Player.Nickname} 被914变成了{roleName}。");
        }

        private void BecomeZombie(UpgradingPlayerEventArgs ev)
        {
            ev.Player.Role.Set(RoleTypeId.Scp0492);
            ev.Player.ShowHint("SCP-914 将你变成了小僵尸！", 5);
            Map.Broadcast(5, $"{ev.Player.Nickname} 被914变成了小僵尸！");
        }

        private void Kill(UpgradingPlayerEventArgs ev, string message)
        {
            ev.Player.Kill(message);
            ev.Player.ShowHint(message, 5);
            Map.Broadcast(5, $"{ev.Player.Nickname} 被914杀死了！");
        }

        private void BecomeRandomHuman(UpgradingPlayerEventArgs ev, RoleTypeId currentRole)
        {
            List<RoleTypeId> candidates = new List<RoleTypeId>(humans);
            candidates.Remove(currentRole);
            if (candidates.Count == 0) candidates = new List<RoleTypeId>(humans);
            RoleTypeId newRole = candidates[_random.Next(candidates.Count)];
            ev.Player.Role.Set(newRole);
            ev.Player.ShowHint($"SCP-914 的超精加工将你变成了{newRole}！", 5);
            Map.Broadcast(5, $"{ev.Player.Nickname} 通过914的超精加工变成了{newRole}。");
        }
    }

    public class Config : IConfig
    {
        public bool IsEnabled { get; set; } = true;
        public bool Debug { get; set; } = false;
    }
}
