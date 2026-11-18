using UnityEditor;
using UnityEngine;

namespace RandomRace.Editor
{
    public static class RunRaceChecks
    {
        [MenuItem("Random Race/Run Rule Checks")]
        public static void Run()
        {
            try { RaceChecks.Run(); Debug.Log("Random Race: all rule checks passed (1,000 seeded games)."); }
            catch (System.Exception e) { Debug.LogException(e); }
        }
    }
}
