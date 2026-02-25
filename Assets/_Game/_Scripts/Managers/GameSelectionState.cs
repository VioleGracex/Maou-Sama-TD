using System.Collections.Generic;
using MaouSamaTD.Levels;
using MaouSamaTD.Units;

namespace MaouSamaTD.Managers
{
    /// <summary>
    /// Holds the state for the upcoming game session (Level, Selected Cohort, etc).
    /// Should be bound as a Singleton in ProjectContext.
    /// </summary>
    public class GameSelectionState
    {
        public LevelData SelectedLevel { get; private set; }
        public List<UnitData> SelectedCohort { get; private set; } = new List<UnitData>();
        public List<MaouSamaTD.Skills.SovereignRiteData> SelectedRites { get; private set; } = new List<MaouSamaTD.Skills.SovereignRiteData>();

        // Optional: Difficulty, Modifiers, etc.

        public void SetLevel(LevelData level)
        {
            SelectedLevel = level;
            
            // If level has premade cohort, auto-assign
            if (level != null && level.PremadeCohort != null && level.PremadeCohort.Count > 0)
            {
                SelectedCohort = new List<UnitData>(level.PremadeCohort);
            }
            else
            {
                // Otherwise keep existing or clear? 
                // Usually we keep the player's last selected cohort unless forced.
                // If null, we might want to clear or let CohortManager handle it.
            }
        }

        public void SetCohort(List<UnitData> cohort)
        {
            SelectedCohort = new List<UnitData>(cohort);
        }
    }
}
