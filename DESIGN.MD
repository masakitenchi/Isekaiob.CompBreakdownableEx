# Design.MD
---
## Goal
Make breakable buildings in HSK requires matched comps from their technology ages || based on building designation to fix when breakdown

### 1. Code Diging:
There is a hidden MTBF value which is unused in original `Rimworld.CompBreakdownable`
not sure if its eaten by compiler or intentional.

The Repairing job `JobDefoF.FixBrokenDownBuilding` is fired by a predefined WorkGiver `WorkGiver_FixBrokenDownBuilding`
By Searching  MapComp `Rimworld.BreakdownManager`.

It then calls `JobDriver_FixBrokenDownBuilding`.

Everything related to this logic in vanilla rimworld is hardcoded.
So we have to write an independant system.

## 2. Code Designing:
Namespace = isekaiob
CompBreakdownableEx
CompProperties_BreakdownableEx
BreakdownManagerEx
JobDriver_FixBrokenDownBuildingEx
WorkGiver_FixBrokenDownBuildingEx

### 3. XML Part.

Register the WorkGiver and Job.
### 4. Test
Write a test building and test.

## Comps defName Fast Lookup:

**Based on Hardcore SK modpack, differs from Vanilla Rimworld**
`C#

        public static ThingDef ComponentIndustrial;
        public static ThingDef ComponentSpacer;
        public static ThingDef ComponentUltra;
        public static ThingDef ElectronicComponents;
        public static ThingDef Weapon_Parts;
        public static ThingDef Mechanism;
        public static ThingDef AdvMechanism;
`