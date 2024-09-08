using System.Collections.Generic;
using Verse;
namespace Isekaiob
{
    /// <summary>
    /// This is a mapbased comp which holds a simple timer to check for breakdown and a list for WorkGiver to use with.
    /// </summary>
    public class BreakdownManagerEx :MapComponent
    {
        private List<Isekaiob.CompBreakdownableEx> comps = new List<Isekaiob.CompBreakdownableEx>();
        public HashSet<Thing> brokenDownThings = new HashSet<Thing>();

        public BreakdownManagerEx(Map map)
            : base(map)
        {
#if DEBUG
            Verse.Log.Message(string.Format("[IKOB]CBEX BreakdownManagerEx ctor is running on Map Index {0}",map.Index));
#endif
        }
        /// <summary>
        /// Todo: I Wonder If we can cache repair cost here to reduce further value looking up/
        /// </summary>
        /// <param name="c"></param>
        public void Register(Isekaiob.CompBreakdownableEx c)
        {
#if DEBUG
            Verse.Log.Message("[IKOB]CBEX Registering C");
#endif
            this.comps.Add(c);
            if (!c.BrokenDown)
                return;
            this.brokenDownThings.Add((Thing) c.parent);
        }

        public void Deregister(Isekaiob.CompBreakdownableEx c)
        {
            this.comps.Remove(c);
            this.brokenDownThings.Remove((Thing) c.parent);
        }

        public override void MapComponentTick()
        {
            if (Find.TickManager.TicksGame % CompBreakdownableExtended_Mod.settings.iThrottleCompBreakdownableEx != 0)
                return;
            //Can we Parallax this job?
            for (int index = 0; index < this.comps.Count; ++index)
                this.comps[index].CheckForBreakdown();
#if DEBUG
            Verse.Log.Message("[IKOB]BreakdownManagerSK is Ticking!");
#endif
        }
        public void Notify_BrokenDown(Thing thing) => this.brokenDownThings.Add(thing);

        public void Notify_Repaired(Thing thing) => this.brokenDownThings.Remove(thing);
    }
}