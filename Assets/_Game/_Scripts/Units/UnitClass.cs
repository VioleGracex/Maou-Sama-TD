namespace MaouSamaTD.Units
{
    /// <summary>
    /// Vassals are divided into ten distinct tactical roles, plus standard Enemies.
    /// </summary>
    public enum UnitClass
    {
        // Vassal Tactical Roles
        Bastion,        // Heavy Tank: High HP/DEF. Block Count: 3-4.
        Vanguard,       // Melee DPS: Balanced stats. Block Count: 2.
        Executioner,    // Burst Assassin: High ATK/ASPD, low HP.
        Ranger,         // Physical Ranged: High Ground. Prioritizes Flying.
        Warlock,        // Magical Ranged: High Ground. AoE magic/CC.
        Sage,           // Healer: Restores HP to allies.
        Architect,      // Fortifier: Deploys Traps/Towers.
        Necromancer,    // Summoner: Spawns "Fodder" units.
        Support,        // Buffer: Passive auras.
        Gunner,         // Rapid Fire: Extreme ASPD, True DMG.
        Assassin,       // Infiltrator: Ignores 1 enemy to strike backline.
        
        // Miscellaneous / Enemy
        EnemyMelee,
        EnemyRanged,
        EnemyBoss
    }
}
