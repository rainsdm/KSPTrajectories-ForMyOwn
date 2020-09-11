﻿/*
  Copyright© (c) 2017-2020 S.Gray, (aka PiezPiedPy).

  This file is part of Trajectories.
  Trajectories is available under the terms of GPL-3.0-or-later.
  See the LICENSE.md file for more details.

  Trajectories is free software: you can redistribute it and/or modify
  it under the terms of the GNU General Public License as published by
  the Free Software Foundation, either version 3 of the License, or
  (at your option) any later version.

  Trajectories is distributed in the hope that it will be useful,
  but WITHOUT ANY WARRANTY; without even the implied warranty of
  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.

  You should have received a copy of the GNU General Public License
  along with Trajectories.  If not, see <http://www.gnu.org/licenses/>.
*/

using System.Collections.Generic;
using UnityEngine;

namespace Trajectories
{
    /// <summary> Contains copied game data that can be used outside the Unity main thread. All needed data must be copied and not referenced. </summary>
    internal static class GameDataCache
    {
        internal static double UniversalTime { get; set; }
        internal static double WarpDeltaTime { get; set; }

        internal static Vessel AttachedVessel { get; private set; }
        internal static List<Part> VesselParts { get; private set; }
        internal static List<Quaternion> PartRotations { get; private set; }
        internal static List<Vector3d> PartTransformsUp { get; private set; }
        internal static List<Vector3d> PartTransformsForward { get; private set; }
        internal static List<Vector3d> PartTransformsRight { get; private set; }
        internal static double VesselMass { get; private set; }
        internal static Vector3d VesselWorldPos { get; private set; }
        internal static Vector3d VesselOrbitVelocity { get; private set; }
        internal static Vector3d VesselTransformUp { get; private set; }
        internal static Vector3d VesselTransformForward { get; private set; }

        internal static CelestialBody Body { get; private set; }
        internal static Vector3d BodyWorldPos { get; private set; }
        internal static bool BodyHasAtmosphere { get; private set; }
        internal static bool BodyHasOcean { get; set; }
        internal static double BodyAtmosphereDepth { get; set; }
        internal static double BodyMaxGroundHeight { get; set; }
        internal static double BodyRadius { get; set; }
        internal static Vector3d BodyAngularVelocity { get; set; }
        internal static double BodyGravityParameter { get; set; }

        internal static List<ManeuverNode> ManeuverNodes { get; private set; }
        internal static Orbit Orbit { get; private set; }
        internal static List<Orbit> FlightPlan { get; private set; }

        /// <summary> Updates entire cache </summary>
        internal static bool Update()
        {
            Profiler.Start("GameDataCache.Update");

            UniversalTime = Planetarium.GetUniversalTime();
            WarpDeltaTime = TimeWarp.fixedDeltaTime;

            if (AttachedVessel != Trajectories.AttachedVessel || Body != Trajectories.AttachedVessel.mainBody || VesselParts.Count != Trajectories.AttachedVessel.Parts.Count)
            {
                Util.DebugLog("GameDataCache updated due to vessel, body, or parts count changed");

                AttachedVessel = Trajectories.AttachedVessel;
                VesselParts = new List<Part>(AttachedVessel.Parts);

                Body = FlightGlobals.Bodies.Find((CelestialBody b) => { return b.name == AttachedVessel.mainBody.name; });
                UpdateBodyCache();

                Trajectories.AerodynamicModel.Init();
            }

            if (AttachedVessel.patchedConicSolver == null)
            {
                Util.DebugLogWarning("PatchedConicsSolver is null, Skipping.");
                return false;
            }

            UpdateVesselCache();

            BodyWorldPos = Body.position;

            ManeuverNodes = new List<ManeuverNode>(AttachedVessel.patchedConicSolver.maneuverNodes);
            Orbit = new Orbit(AttachedVessel.orbit);
            FlightPlan = new List<Orbit>(AttachedVessel.patchedConicSolver.flightPlan);

            Profiler.Stop("GameDataCache.Update");
            return true;
        }

        private static void UpdateVesselCache()
        {
            UpdateVesselMass();

            VesselWorldPos = AttachedVessel.GetWorldPos3D();
            VesselOrbitVelocity = AttachedVessel.obt_velocity;
            VesselTransformUp = AttachedVessel.ReferenceTransform.up;
            VesselTransformForward = AttachedVessel.ReferenceTransform.forward;

            if (PartRotations == null || PartRotations.Count != VesselParts.Count)
            {
                CreatePartTransforms();
            }
            else
            {
                UpdatePartTransforms();
            }

        }

        private static void CreatePartTransforms()
        {
            PartRotations ??= new List<Quaternion>(VesselParts.Count);
            PartTransformsUp ??= new List<Vector3d>(VesselParts.Count);
            PartTransformsForward ??= new List<Vector3d>(VesselParts.Count);
            PartTransformsRight ??= new List<Vector3d>(VesselParts.Count);
            PartRotations.Clear();
            PartTransformsUp.Clear();
            PartTransformsForward.Clear();
            PartTransformsRight.Clear();

            foreach (Part part in VesselParts)
            {
                PartRotations.Add(part.transform.rotation);
                PartTransformsUp.Add(part.transform.up);
                PartTransformsForward.Add(part.transform.forward);
                PartTransformsRight.Add(part.transform.right);
            }
        }

        private static void UpdatePartTransforms()
        {
            int part_index = 0;
            foreach (Part part in VesselParts)
            {
                PartRotations[part_index] = part.transform.rotation;
                PartTransformsUp[part_index] = part.transform.up;
                PartTransformsForward[part_index] = part.transform.forward;
                PartTransformsRight[part_index] = part.transform.right;
                part_index++;
            }
        }

        private static void UpdateVesselMass()
        {
            foreach (Part part in VesselParts)
            {
                if (part.physicalSignificance == Part.PhysicalSignificance.NONE)
                    continue;

                float partMass = part.mass + part.GetResourceMass() + part.GetPhysicslessChildMass();
                VesselMass += partMass;
            }
        }

        private static void UpdateBodyCache()
        {
            BodyHasAtmosphere = Body.atmosphere;
            BodyHasOcean = Body.ocean;
            BodyAtmosphereDepth = Body.atmosphereDepth;
            BodyMaxGroundHeight = Body.pqsController.mapMaxHeight;
            BodyRadius = Body.Radius;
            BodyAngularVelocity = Body.angularVelocity;
            BodyGravityParameter = Body.gravParameter;
        }
    }
}
