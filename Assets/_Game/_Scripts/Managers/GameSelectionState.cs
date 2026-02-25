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
        #region Fields
        public LevelData SelectedLevel { get; private set; }
        public List<UnitData> SelectedCohort { get; private set; } = new List<UnitData>();
        public List<MaouSamaTD.Skills.SovereignRiteData> SelectedRites { get; private set; } = new List<MaouSamaTD.Skills.SovereignRiteData>();
        #endregion

        #region Public API
        public void SetLevel(LevelData level)
        {
            SelectedLevel = level;
            
            if (level != null && level.PremadeCohort != null && level.PremadeCohort.Count > 0)
            {
                SelectedCohort = new List<UnitData>(level.PremadeCohort);
            }
        }

        public void SetCohort(List<UnitData> cohort)
        {
            SelectedCohort = new List<UnitData>(cohort);
        }

        public void Clear()
        {
            SelectedLevel = null;
            SelectedCohort.Clear();
            SelectedRites.Clear();
        }
        #endregion
    }
}
