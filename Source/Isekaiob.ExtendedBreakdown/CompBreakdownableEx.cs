using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace Isekaiob
{
    /// <summary>
    /// Added A Place to specify which thingdef to use when repairing this, Meant to be gathered by WorkGiver And Reserve.
    /// Also Exposed The MTB And Make it live in logic. I wonder why tynan wrote this value and end up not used anymore.
    /// </summary>
    public class CompBreakdownableEx : ThingComp
    {
        //Begin Mod Here
        public CompProperties_BreakdownableEx Props => (CompProperties_BreakdownableEx)this.props;
        private int BreakdownMTBTicks => this.Props.BreakdownMTBTicks;
        public ThingDef RepairCost => this.Props.RepairCost;
        public int RepairTime => this.Props.RepairTime;

        private bool brokenDownInt;
        private CompPowerTrader powerComp;
        public const string BreakdownSignal = "Breakdown";
        private OverlayHandle? overlayBrokenDown;
        /// <summary>
        /// This string is pregenerated at runtime so no inspection IMGUI string roaming GC
        /// </summary>
        private string INSPECT_Repair;
        public bool BrokenDown => this.brokenDownInt;

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look<bool>(ref this.brokenDownInt, "brokenDown");
        }
        /// <summary>
        /// Can we replace it with our own overlay?
        /// </summary>
        private void UpdateOverlays()
        {
            if (!this.parent.Spawned)
                return;
            this.parent.Map.overlayDrawer.Disable((Thing)this.parent, ref this.overlayBrokenDown);
            if (!this.brokenDownInt)
                return;
            this.overlayBrokenDown =
                new OverlayHandle?(this.parent.Map.overlayDrawer.Enable((Thing)this.parent, OverlayTypes.BrokenDown));
        }
        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
#if DEBUG
            Verse.Log.Message(string.Format("[IKOB]CompBreakdownableEx now PostSpawnSetup on Thing {0} with cost {3} at {1} on Map Index {2}",this.parent.Label,this.parent.Position,this.parent.Map.Index,this.RepairCost.defName));
#endif
            if (this.parent.GetComp<CompBreakdownable>() != null)
            {
                Verse.Log.Error(string.Format("[IKOB]CBEX Detected both CBEX And Vanilla CompBreakdownAble coexists on defName \n {0},Check the def.",this.parent.def.defName));
            }
            base.PostSpawnSetup(respawningAfterLoad);
            this.powerComp = this.parent.GetComp<CompPowerTrader>();
            this.parent.Map.GetComponent<BreakdownManagerEx>().Register(this);
            this.UpdateOverlays();

            var tmpsb = new StringBuilder();
            tmpsb.AppendLine("BrokenDown".Translate());
            tmpsb.Append("IKOB_CBEX_RepairCostRequirements".Translate());
            tmpsb.Append(RepairCost.label);
            INSPECT_Repair = tmpsb.ToString();
        }

        /// <summary>
        /// Parent is destroyed before its comp. so you cannot print debug logs from here.
        /// Thats why the #if block below is commented out.
        /// </summary>
        /// <param name="map"></param>
        public override void PostDeSpawn(Map map)
        {
            /*
#if DEBUG
                Verse.Log.Message(string.Format("[IKOB]CompBreakdownableEx now PostDeSpawn on Thing {0} at {1} on Map Index {2}",this.parent.Label,this.parent.Position,this.parent.Map.Index));
#endif
            */
            base.PostDeSpawn(map);
            map.GetComponent<BreakdownManagerEx>().Deregister(this);
        }

        public void CheckForBreakdown()
        {
            if (!this.CanBreakdownNow() || !Rand.MTBEventOccurs(BreakdownMTBTicks, 1f, (float)CompBreakdownableExtended_Mod.settings.iThrottleCompBreakdownableEx))
                return;
            this.DoBreakdown();
        }
        protected bool CanBreakdownNow()
        {
            if (this.BrokenDown)
                return false;
            //Todo: modsetting.should powerless machine breakdown switch here.
            return this.powerComp == null || this.powerComp.PowerOn;
        }

        public void Notify_Repaired()
        {
            this.brokenDownInt = false;
            this.parent.Map.GetComponent<BreakdownManagerEx>().Notify_Repaired((Thing)this.parent);
            if (this.parent is Building_PowerSwitch)
                this.parent.Map.powerNetManager.Notfiy_TransmitterTransmitsPowerNowChanged(
                    this.parent.GetComp<CompPower>());
            this.UpdateOverlays();
        }

        public void DoBreakdown()
        {
            this.brokenDownInt = true;
            //Todo: This signal needs further path tracing to see how it affect other things.
            this.parent.BroadcastCompSignal("Breakdown");
#if DEBUG
            Verse.Log.Message( string.Format("[IKOB]CBEX Brokendown on Thing{0} @ {1} on Map {2}",this.parent.Label,this.parent.Position,this.parent.Map.Index));
#endif
            this.parent.Map.GetComponent<BreakdownManagerEx>().Notify_BrokenDown((Thing)this.parent);
            this.UpdateOverlays();
            //Todo: Additional events After Breakdown like explosion,make fleck,make filth 
        }

        public override string CompInspectStringExtra()
        {
            return this.BrokenDown ? INSPECT_Repair : (string)null;
        }
    }
}