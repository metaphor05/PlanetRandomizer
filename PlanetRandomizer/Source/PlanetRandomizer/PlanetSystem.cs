using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Reflection;
using System.IO;
using UnityEngine;
using KSP;

namespace PlanetRandomizer
{
    [KSPAddon(KSPAddon.Startup.MainMenu, false)]
    public class PlanetSystem : MonoBehaviour
    {

        private System.Random randSeed = new System.Random();
        private bool showGUI = false;

        public static PlanetSystem Instance;

        public void Start()
        {
            if (Instance)
            {
                DestroyImmediate(gameObject);
            }
            else
            {
                DontDestroyOnLoad(gameObject);
                Instance = this;
                GameEvents.onGameSceneLoadRequested.Add(new EventData<GameScenes>.OnEvent(OnGameSceneLoadRequested));
                PlanetSettings.Load(KSPUtil.ApplicationRootPath + "/GameData/PlanetRandomizer/Resources/PlanetRandomizer.cfg");
                PlanetDefault.Load(KSPUtil.ApplicationRootPath + "/GameData/PlanetRandomizer/Resources/PlanetRandomizer.cfg");
            }
        }

        void OnGameSceneLoadRequested(GameScenes scene)
        {
            if (scene == GameScenes.SPACECENTER)
            {
                if (Planetarium.GetUniversalTime() > 300)
                {
                    DefaultSystem();
                    showGUI = false;
                }
                else if (File.Exists(KSPUtil.ApplicationRootPath + "/saves/" + HighLogic.SaveFolder + "/PlanetRandomizer.cfg"))
                {
                    print("Loading System");
                    PlanetSettings.Load(KSPUtil.ApplicationRootPath + "/saves/" + HighLogic.SaveFolder + "/PlanetRandomizer.cfg");
                    RebuildSystem();
                }
                else if (!File.Exists(KSPUtil.ApplicationRootPath + "/saves/" + HighLogic.SaveFolder + "/PlanetRandomizer.cfg"))
                {
                    DefaultSystem();
                    print("Showing GUI");
                    showGUI = true;
                }
            }
            /*if (scene == GameScenes.MAINMENU)
            {
                PlanetSettings.Load(KSPUtil.ApplicationRootPath + "/GameData/PlanetRandomizer/Resources/PlanetRandomizer.cfg");
                DefaultSystem();
            }*/
        }

        Rect windowRect = new Rect(100, 100, 150, 120);
        void OnGUI()
        {
            if (showGUI)
            {
                windowRect = GUI.Window(121, windowRect, WindowFunction, "Planet Randomizer");
            }
        }
        void WindowFunction(int windowID)
        {
            PlanetSettings.Instance.seed = int.Parse(GUI.TextField(new Rect(10, 30, 60, 20), PlanetSettings.Instance.seed.ToString()));
            if (GUI.Button(new Rect(80, 30, 60, 20), "Random"))
            {
                PlanetSettings.Instance.seed = randSeed.Next(999999);
            }
            if (GUI.Button(new Rect(10, 60, 130, 20), "Select"))
            {
                print("Select pressed");
                showGUI = true;
                DefaultSystem();
                ChangeSystem();
            }
            if (GUI.Button(new Rect(10, 90, 130, 20), "Done"))
            {
                print("Done pressed");
                showGUI = false;
            }
            GUI.DragWindow();
        }


        private void ChangeSystem()
        {
            print("Changing System");
            System.Random rand = new System.Random(PlanetSettings.Instance.seed);

            CelestialBody sun = GetSun();
            List<ChangedPlanet> tempPlanet = new List<ChangedPlanet>();

            foreach (CelestialBody body in FlightGlobals.Bodies)
            {
                if (body.gameObject.name != "Sun")
                {
                    ChangedPlanet cp = new ChangedPlanet();

                    cp.Name = body.gameObject.name;
                    print("Changing " + cp.Name);

                    double origRadius = body.Radius;

                    if (body.name != "Kerbin" && body.pqsController != null)
                    {
                        cp.Radius = origRadius * Math.Pow(2, 2.0 * rand.NextDouble() - 1);
                    }
                    else
                    {
                        cp.Radius = origRadius; // Kerbin, gas giants, and stars don't change their radius
                    }

                    if (body.name == "Kerbin")
                    {
                        cp.Mass = body.Mass; // Kerbin doesn't change its mass
                    }
                    else if (body.Mass >= sun.Mass * 0.1)
                    {
                        cp.Mass = sun.Mass * 0.1 * Math.Pow(2, 2.0 * rand.NextDouble() - 1);
                    }
                    else
                    {
                        cp.Mass = body.Mass * Math.Pow(cp.Radius / origRadius, 3) * Math.Pow(2, 2.0 * rand.NextDouble() - 1);
                    }

                    /*foreach (Transform t in ScaledSpace.Instance.scaledSpaceTransforms)
                    {
                        if (t.gameObject.name == body.gameObject.name)
                        {
                            float origLocalScale = t.localScale.x;
                            float scaleFactor = (float)((double)origLocalScale * cp.Radius / origRadius);
                            t.localScale = new Vector3(scaleFactor, scaleFactor, scaleFactor);
                        }
                    }*/

                    cp.RotationPeriod = body.rotationPeriod;
                    cp.SemiMajorAxis = body.orbit.semiMajorAxis;
                    cp.Eccentricity = body.orbit.eccentricity;
                    cp.Inclination = body.orbit.inclination;
                    cp.MeanAnomalyAtEpoch = body.orbit.meanAnomalyAtEpoch;
                    cp.LAN = body.orbit.LAN;
                    cp.ArgumentOfPeriapsis = body.orbit.argumentOfPeriapsis;
                    cp.ReferenceBody = null;

                    tempPlanet.Add(cp);
                }
            }

            // sorting planets by mass
            tempPlanet.Sort(CompareByMass);
            int i = 0;
            foreach (ChangedPlanet cp in tempPlanet)
            {
                i++;
                cp.Rank = i;
            }
            int numberOfPlanets = i;
            print("number of planets = " + numberOfPlanets);

            // generating orbital parameters
            double smaSolarMin = 2000000000; // minimum solar orbit semi-major axis
            double smaSolarMax = 100000000000; // maximum solar orbit semi-major axis
            if (File.Exists(KSPUtil.ApplicationRootPath + "/GameData/RealSolarSystem/RealSolarSystem.cfg")) // checks for RSS
            {
                smaSolarMin = 10 * smaSolarMin;
                smaSolarMax = 30 * smaSolarMax;
            }
            if (File.Exists(KSPUtil.ApplicationRootPath + "/GameData/PlanetFactory/PlanetFactory.dll")) // checks for PlanetFactory
            {
                smaSolarMax = 5 * smaSolarMax;
            }

            // randomizing parameters
            double eccMax = 0.4; // maximum eccentricity
            double incMax = 20; // maximum inclination
            double smaMinRadius = 10.0; // minimum semi-major axis as multiplier of reference body radius
            double smaMaxSOI = 0.4; // maximum semi-major axis as multiplier of reference body SOI radius
            double maxMassRatio = 0.1; // maximum ratio between the masses of a satellite and its primary
            double minTidalLockingMassRatio = 0.02; // minimum ratio between the masses of a satellite and its primary before the primary is tidally locked to the satellite
            double maxTidalLockingRadius = 80; // maximum separation between a primary and its satellite at which the satellite is tidally locked
            double maxRotationRate = 0.2; // maximum rotation speed as a multiple of orbital speed at the surface
            double minRotationFactor = 0.01; // minimum rotation speed factor as a multiple of maximum rotation speed
            double eccIncExponent = 2.0; // controls how circular/equatorial large planet/moon orbits are
            double soiSeparationFactor = 2.0; // controls how far apart planets/moons are in terms of their SOI;
            double sunSeparationFactor = 0.1; // controls how far apart planets orbiting the Sun are in terms of their SMA;

            double sma = 0;
            double ecc = 0;
            int nSun = 5; // inverse of chance that a body orbits the Sun
            for (int j = 1; j <= i; j++)
            {
                foreach (ChangedPlanet cp in tempPlanet)
                {

                    if (cp.Rank == j)
                    {
                        print("Changing " + cp.Name + "'s orbit, rank " + j + ".");

                        // 1/nSun chance that the planet orbits the Sun
                        int r1 = rand.Next(nSun);
                        print("r1 = " + r1);
                        if (r1 == 0)
                        {
                            cp.ReferenceBody = "Sun";

                            // excludes orbits that cross SOI with another body
                            List<double> excludedRegionsLow = new List<double>();
                            List<double> excludedRegionsHigh = new List<double>();
                            foreach (ChangedPlanet cp1 in tempPlanet)
                            {
                                if (cp1.ReferenceBody == "Sun" && cp1.Rank < cp.Rank)
                                {
                                    double cp1SOI = cp1.SemiMajorAxis * Math.Pow(cp1.Mass / sun.Mass, 0.4);
                                    excludedRegionsLow.Add(cp1.SemiMajorAxis * (1 - sunSeparationFactor) * (1 - cp1.Eccentricity) - 2 * cp1SOI * soiSeparationFactor);
                                    excludedRegionsHigh.Add(cp1.SemiMajorAxis * (1 + sunSeparationFactor) * (1 + cp1.Eccentricity) + 2 * cp1SOI * soiSeparationFactor);
                                }
                            }
                            print("Finished excluded zones for " + cp.Name);
                            bool orbitPermitted = false;
                            while (!orbitPermitted)
                            {
                                orbitPermitted = true;
                                sma = smaSolarMin + Math.Pow(rand.NextDouble(), 2) * (smaSolarMax - smaSolarMin);
                                ecc = eccMax * rand.NextDouble() * Math.Pow((double)cp.Rank / numberOfPlanets, eccIncExponent); // more massive planets have lower eccentricities

                                print("Trying out SMA = " + sma);

                                for (int k = 0; k < excludedRegionsLow.Count; k++)
                                {
                                    if (sma * (1 + ecc) > excludedRegionsLow[k] && sma * (1 - ecc) < excludedRegionsHigh[k])
                                    {
                                        orbitPermitted = false;
                                    }
                                }
                            }

                            cp.SemiMajorAxis = sma;
                            cp.Eccentricity = ecc;
                            cp.Inclination = incMax * rand.NextDouble() * Math.Pow((double)cp.Rank / numberOfPlanets, eccIncExponent); // more massive planets have lower inclinations
                            cp.MeanAnomalyAtEpoch = 2 * Math.PI * rand.NextDouble();
                            cp.LAN = 360 * rand.NextDouble();
                            cp.ArgumentOfPeriapsis = 360 * rand.NextDouble();

                            print("Planet " + cp.Name + " is orbiting Sun1.");

                        }

                        // otherwise, chance of orbiting anything bigger than itself (including Sun)
                        else
                        {
                            bool refBodyAllowed = false;
                            int r2 = 0;
                            while (!refBodyAllowed)
                            {
                                r2 = (int)(j - (j * Math.Pow(rand.NextDouble(), 3)));

                                print("r2 = " + r2);

                                if (r2 == 0) // orbiting Sun
                                {
                                    refBodyAllowed = true;
                                    cp.ReferenceBody = "Sun";

                                    // excludes orbits that cross SOI with another body
                                    List<double> excludedRegionsLow = new List<double>();
                                    List<double> excludedRegionsHigh = new List<double>();
                                    foreach (ChangedPlanet cp1 in tempPlanet)
                                    {
                                        if (cp1.ReferenceBody == "Sun" && cp1.Rank < cp.Rank)
                                        {
                                            print("Adding " + cp1.Name + " to excluded SOIs.");
                                            double cp1SOI = cp1.SemiMajorAxis * Math.Pow(cp1.Mass / sun.Mass, 0.4);
                                            excludedRegionsLow.Add(cp1.SemiMajorAxis * (1 - sunSeparationFactor) * (1 - cp1.Eccentricity) - 2 * cp1SOI * soiSeparationFactor);
                                            excludedRegionsHigh.Add(cp1.SemiMajorAxis * (1 + sunSeparationFactor) * (1 + cp1.Eccentricity) + 2 * cp1SOI * soiSeparationFactor);
                                        }
                                    }
                                    print("Finished excluded zones for " + cp.Name);
                                    bool orbitPermitted = false;
                                    int orbitTryCount = 0;
                                    while (!orbitPermitted && orbitTryCount <= 5)
                                    {
                                        orbitPermitted = true;
                                        sma = smaSolarMin + Math.Pow(rand.NextDouble(), 2) * (smaSolarMax - smaSolarMin);
                                        ecc = eccMax * rand.NextDouble() * Math.Pow((double)cp.Rank / numberOfPlanets, eccIncExponent);

                                        print("Trying out SMA = " + sma);

                                        for (int k = 0; k < excludedRegionsLow.Count; k++)
                                        {
                                            print("k = " + k);
                                            if (sma * (1 + ecc) > excludedRegionsLow[k] && sma * (1 - ecc) < excludedRegionsHigh[k])
                                            {
                                                orbitPermitted = false;
                                                print("orbit permitted = " + orbitPermitted);
                                            }
                                        }
                                        orbitTryCount++;
                                        print("orbitTryCount = " + orbitTryCount);
                                        if (orbitTryCount > 5)
                                        {
                                            refBodyAllowed = false;
                                        }
                                    }

                                    cp.SemiMajorAxis = sma;
                                    cp.Eccentricity = ecc;
                                    cp.Inclination = incMax * rand.NextDouble() * Math.Pow((double)cp.Rank / numberOfPlanets, eccIncExponent);
                                    cp.MeanAnomalyAtEpoch = 2 * Math.PI * rand.NextDouble();
                                    cp.LAN = 360 * rand.NextDouble();
                                    cp.ArgumentOfPeriapsis = 360 * rand.NextDouble();

                                    print("Planet " + cp.Name + " is orbiting Sun2.");

                                }

                                else // orbiting another body
                                {
                                    refBodyAllowed = true;
                                    ChangedPlanet cpRef = tempPlanet.Find(cp2 => cp2.Rank == r2);
                                    cp.ReferenceBody = cpRef.Name;

                                    if (cp.Mass >= cpRef.Mass * maxMassRatio)
                                    {
                                        refBodyAllowed = false;
                                    }

                                    double cpRefSOI = 0;
                                    if (cpRef.ReferenceBody == "Sun")
                                    {
                                        cpRefSOI = cpRef.SemiMajorAxis * Math.Pow(cpRef.Mass / (sun.Mass + cpRef.Mass), 0.4);
                                    }
                                    else
                                    {
                                        foreach (ChangedPlanet cp1 in tempPlanet)
                                        {
                                            if (cp1.Name == cpRef.ReferenceBody && cp1.Rank < cpRef.Rank)
                                            {
                                                cpRefSOI = cpRef.SemiMajorAxis * Math.Pow(cpRef.Mass / (cp1.Mass + cpRef.Mass), 0.4);
                                            }
                                        }
                                    }
                                    double smaMin = cpRef.Radius * smaMinRadius + cp.Radius;
                                    double smaMax = cpRefSOI * smaMaxSOI;

                                    if (smaMin > smaMax)
                                    {
                                        refBodyAllowed = false;
                                    }

                                    // excludes orbits that cross SOI with another body
                                    List<double> excludedRegionsLow = new List<double>();
                                    List<double> excludedRegionsHigh = new List<double>();
                                    foreach (ChangedPlanet cp1 in tempPlanet)
                                    {
                                        if (cp1.ReferenceBody == cpRef.Name && cp1.Rank < cp.Rank)
                                        {
                                            double cp1SOI = cp1.SemiMajorAxis * Math.Pow(cp1.Mass / (cpRef.Mass + cp1.Mass), 0.4);
                                            excludedRegionsLow.Add(cp1.SemiMajorAxis * (1 - cp1.Eccentricity) - 2 * cp1SOI * soiSeparationFactor);
                                            excludedRegionsHigh.Add(cp1.SemiMajorAxis * (1 + cp1.Eccentricity) + 2 * cp1SOI * soiSeparationFactor);
                                        }
                                    }
                                    bool orbitPermitted = false;
                                    int orbitTryCount = 0;
                                    while (!orbitPermitted && orbitTryCount < 10)
                                    {
                                        orbitPermitted = true;
                                        sma = smaMin + Math.Pow(rand.NextDouble(), 4) * (smaMax - smaMin);
                                        ecc = eccMax * rand.NextDouble() * Math.Pow((double)cp.Rank / numberOfPlanets, eccIncExponent);

                                        for (int k = 0; k < excludedRegionsLow.Count; k++)
                                        {
                                            if (sma * (1 + ecc) > excludedRegionsLow[k] && sma * (1 - ecc) < excludedRegionsHigh[k])
                                            {
                                                orbitPermitted = false;
                                            }
                                        }
                                        orbitTryCount++;
                                        if (orbitTryCount >= 10)
                                        {
                                            refBodyAllowed = false;
                                        }

                                    }

                                    cp.SemiMajorAxis = sma;
                                    cp.Eccentricity = ecc;
                                    cp.Inclination = incMax * rand.NextDouble() * Math.Pow((double)cp.Rank / numberOfPlanets, eccIncExponent);
                                    cp.MeanAnomalyAtEpoch = 2 * Math.PI * rand.NextDouble();
                                    cp.LAN = 360 * rand.NextDouble();
                                    cp.ArgumentOfPeriapsis = 360 * rand.NextDouble();

                                    // changing rotation period
                                    if (sma <= cpRef.Radius * maxTidalLockingRadius)
                                    {
                                        // tidal locking of satellite to primary if close enough
                                        cp.RotationPeriod = 2 * Math.PI * Math.Sqrt((Math.Pow(sma, 3) / 6.674E-11) / (cp.Mass + cpRef.Mass));
                                        if (cp.Mass >= cpRef.Mass * minTidalLockingMassRatio && sma <= cpRef.Radius * maxTidalLockingRadius / 3 && refBodyAllowed == true)
                                        {
                                            // tidal locking of primary to satellite if close enough and massive enough
                                            cpRef.RotationPeriod = 2 * Math.PI * Math.Sqrt((Math.Pow(sma, 3) / 6.674E-11) / (cp.Mass + cpRef.Mass));
                                        }
                                    }
                                    else
                                    {
                                        // if no tidal locking, rotation speed is inversely distributed between maxRotationRate and maxRotationRate*minRotationFactor
                                        cp.RotationPeriod = (1 / maxRotationRate * 2 * Math.PI * Math.Sqrt((Math.Pow(cp.Radius, 3) / 6.674E-11) / cp.Mass)) / (minRotationFactor + (1 - minRotationFactor) * rand.NextDouble());
                                    }

                                    print("Planet " + cp.Name + " is orbiting " + cpRef.Name + ".");

                                }
                            }

                            print(cp.Name + " orbit changed.");

                        }
                    }
                }
            }


            List<ChangedPlanet> resultPlanet = new List<ChangedPlanet>();

            foreach (ChangedPlanet cp in tempPlanet)
            {
                ChangedPlanet planet = new ChangedPlanet();
                planet = cp;
                resultPlanet.Add(planet);
            }

            PlanetSettings.Instance.Planets = resultPlanet.ToArray();
            PlanetSettings.Instance.Save(KSPUtil.ApplicationRootPath + "/saves/" + HighLogic.SaveFolder + "/PlanetRandomizer.cfg");
            RebuildSystem();


            /*if (HighLogic.LoadedScene == GameScenes.SPACECENTER)
            {
                PQSCity ksc = null;
                foreach (PQSCity city in Resources.FindObjectsOfTypeAll(typeof(PQSCity)))
                {
                    if (city.name == "KSC")
                    {
                        ksc = city;
                        break;
                    }
                }
                if (ksc == null)
                {
                    return;
                }
                ksc.repositionToSphere = true;
                foreach (SpaceCenterCamera2 cam in Resources.FindObjectsOfTypeAll(typeof(SpaceCenterCamera2)))
                {
                    if (ksc.repositionToSphere || ksc.repositionToSphereSurface)
                    {
                        CelestialBody Kerbin = FlightGlobals.Bodies.Find(body => body.name == ksc.sphere.name);
                        if (Kerbin == null)
                        {
                            return;
                        }
                        double nomHeight = Kerbin.pqsController.GetSurfaceHeight((Vector3d)ksc.repositionRadial.normalized) - Kerbin.Radius;
                        if (ksc.repositionToSphereSurface)
                        {
                            nomHeight += ksc.repositionRadiusOffset;
                        }
                        cam.altitudeInitial = 0f - (float)nomHeight;
                    }
                    else
                    {
                        cam.altitudeInitial = 0f - (float)ksc.repositionRadiusOffset;
                    }
                    cam.ResetCamera();
                }
            }*/

        }


        private void RebuildSystem()
        {
            print("Rebuilding System");
            CelestialBody sun = GetSun();
            sun.orbitingBodies.Clear();
            foreach (ChangedPlanet planet in PlanetSettings.Instance.Planets)
            {
                CelestialBody target = (from c in FlightGlobals.Bodies where c.gameObject.name == planet.Name select c).FirstOrDefault();
                if (target != null)
                {
                    print("Changing " + planet.ToString());
                    target.orbitingBodies.Clear();
                    double origRadius = target.Radius;
                    target.Radius = planet.Radius;

                    if (target.pqsController != null)
                    {
                        target.pqsController.radius = planet.Radius;
                        if (target.ocean)
                        {
                            /*print("Changing " + planet.Name + "'s Ocean1.");
                            CelestialBody targetOcean = (from c in FlightGlobals.Bodies where c.gameObject.name == planet.Name + "Ocean" select c).FirstOrDefault();
                            print("Changing " + planet.Name + "'s Ocean3.");
                            targetOcean.pqsController.radius = planet.Radius;
                            print("Changing " + planet.Name + "'s Ocean4.");*/
                        }
                    }

                    foreach (Transform t in ScaledSpace.Instance.scaledSpaceTransforms)
                    {
                        if (t.gameObject.name == target.gameObject.name)
                        {
                            float origLocalScale = t.localScale.x;
                            float scaleFactor = (float)((double)origLocalScale * planet.Radius / origRadius);
                            t.localScale = new Vector3(scaleFactor, scaleFactor, scaleFactor);
                        }
                    }

                    target.Mass = planet.Mass;
                    target.GeeASL = target.Mass * (6.674E-11 / 9.81) / (target.Radius * target.Radius);
                    target.gMagnitudeAtCenter = target.Mass * 6.674E-11;
                    target.gravParameter = target.gMagnitudeAtCenter;

                    target.tidallyLocked = false;
                    target.rotationPeriod = planet.RotationPeriod;

                    target.orbit.semiMajorAxis = planet.SemiMajorAxis;
                    target.orbit.eccentricity = planet.Eccentricity;

                    CelestialBody targetref = (from cRef in FlightGlobals.Bodies where cRef.gameObject.name == planet.ReferenceBody select cRef).FirstOrDefault();
                    targetref.orbitingBodies.Add(target);
                    target.orbit.referenceBody = targetref;

                    target.orbit.inclination = planet.Inclination;
                    target.orbit.meanAnomalyAtEpoch = planet.MeanAnomalyAtEpoch;
                    target.orbit.LAN = planet.LAN;
                    target.orbit.argumentOfPeriapsis = planet.ArgumentOfPeriapsis;

                    target.orbitDriver.QueuedUpdate = true;
                    target.CBUpdate();
                    target.sphereOfInfluence = GetSOI(target);
                    target.hillSphere = GetHillSphere(target);
                    target.orbit.period = GetPeriod(target);

                }

            }

            foreach (CelestialBody body in FlightGlobals.fetch.bodies)
            {
                foreach (AtmosphereFromGround ag in Resources.FindObjectsOfTypeAll(typeof(AtmosphereFromGround)))
                {
                    if (ag != null && ag.planet != null)
                    {
                        // generalized version of Starwaster's code. Thanks Starwaster!
                        if (body.name == ag.planet.name)
                        {
                            print("Found atmo for " + body.name + ": " + ag.name + ", has localScale " + ag.transform.localScale.x);
                            UpdateAFG(body, ag);
                            print("Atmo updated");
                        }
                    }
                }
            }
            print("Done changing planets");

        }


        public void DefaultSystem()
        {
            if (!File.Exists(KSPUtil.ApplicationRootPath + "/GameData/PlanetRandomizer/Resources/PlanetRandomizerDefault.cfg"))
            {
                print("Saving default system");

                List<ChangedPlanet> tempPlanet = new List<ChangedPlanet>();

                foreach (CelestialBody body in FlightGlobals.Bodies)
                {
                    if (body.gameObject.name != "Sun")
                    {
                        ChangedPlanet cp = new ChangedPlanet();

                        cp.Name = body.gameObject.name;
                        cp.Radius = body.Radius;
                        cp.Mass = body.Mass;

                        cp.RotationPeriod = body.rotationPeriod;
                        cp.SemiMajorAxis = body.orbit.semiMajorAxis;
                        cp.Eccentricity = body.orbit.eccentricity;
                        cp.Inclination = body.orbit.inclination;
                        cp.MeanAnomalyAtEpoch = body.orbit.meanAnomalyAtEpoch;
                        cp.LAN = body.orbit.LAN;
                        cp.ArgumentOfPeriapsis = body.orbit.argumentOfPeriapsis;
                        cp.ReferenceBody = body.orbit.referenceBody.name;

                        tempPlanet.Add(cp);
                    }
                }

                PlanetDefault.Instance.Planets = tempPlanet.ToArray();
                PlanetDefault.Instance.Save(KSPUtil.ApplicationRootPath + "/GameData/PlanetRandomizer/Resources/PlanetRandomizerDefault.cfg");
                RebuildSystemDef();
            }
            else
            {
                PlanetDefault.Load(KSPUtil.ApplicationRootPath + "/GameData/PlanetRandomizer/Resources/PlanetRandomizerDefault.cfg");
                RebuildSystemDef();
            }
        }

        private void RebuildSystemDef()
        {
            print("Rebuilding System Default");
            CelestialBody sun = GetSun();
            sun.orbitingBodies.Clear();
            foreach (ChangedPlanet planet in PlanetDefault.Instance.Planets)
            {
                CelestialBody target = (from c in FlightGlobals.Bodies where c.gameObject.name == planet.Name select c).FirstOrDefault();
                if (target != null)
                {
                    print("Changing " + planet.ToString());
                    target.orbitingBodies.Clear();
                    double origRadius = target.Radius;
                    target.Radius = planet.Radius;

                    if (target.pqsController != null)
                    {
                        target.pqsController.radius = planet.Radius;
                    }

                    foreach (Transform t in ScaledSpace.Instance.scaledSpaceTransforms)
                    {
                        if (t.gameObject.name == target.gameObject.name)
                        {
                            float origLocalScale = t.localScale.x;
                            float scaleFactor = (float)((double)origLocalScale * planet.Radius / origRadius);
                            t.localScale = new Vector3(scaleFactor, scaleFactor, scaleFactor);
                        }
                    }

                    target.Mass = planet.Mass;
                    target.GeeASL = target.Mass * (6.674E-11 / 9.81) / (target.Radius * target.Radius);
                    target.gMagnitudeAtCenter = target.Mass * 6.674E-11;
                    target.gravParameter = target.gMagnitudeAtCenter;

                    target.tidallyLocked = false;
                    target.rotationPeriod = planet.RotationPeriod;

                    target.orbit.semiMajorAxis = planet.SemiMajorAxis;
                    target.orbit.eccentricity = planet.Eccentricity;

                    CelestialBody targetref = (from cRef in FlightGlobals.Bodies where cRef.gameObject.name == planet.ReferenceBody select cRef).FirstOrDefault();
                    targetref.orbitingBodies.Add(target);
                    target.orbit.referenceBody = targetref;

                    target.orbit.inclination = planet.Inclination;
                    target.orbit.meanAnomalyAtEpoch = planet.MeanAnomalyAtEpoch;
                    target.orbit.LAN = planet.LAN;
                    target.orbit.argumentOfPeriapsis = planet.ArgumentOfPeriapsis;

                    target.orbitDriver.QueuedUpdate = true;
                    target.CBUpdate();
                    target.sphereOfInfluence = GetSOI(target);
                    target.hillSphere = GetHillSphere(target);
                    target.orbit.period = GetPeriod(target);

                }

            }

            foreach (CelestialBody body in FlightGlobals.fetch.bodies)
            {
                foreach (AtmosphereFromGround ag in Resources.FindObjectsOfTypeAll(typeof(AtmosphereFromGround)))
                {
                    if (ag != null && ag.planet != null)
                    {
                        // generalized version of Starwaster's code. Thanks Starwaster!
                        if (body.name == ag.planet.name)
                        {
                            print("Found atmo for " + body.name + ": " + ag.name + ", has localScale " + ag.transform.localScale.x);
                            UpdateAFG(body, ag);
                            print("Atmo updated");
                        }
                    }
                }
            }
            print("Done changing planets");

        }



        public static void UpdateAFG(CelestialBody body, AtmosphereFromGround afg)
        {
            afg.outerRadius = (float)body.Radius * 1.025f;
            afg.innerRadius = afg.outerRadius * 0.975f;
            afg.KrESun = afg.Kr * afg.ESun;
            afg.KmESun = afg.Km * afg.ESun;
            afg.Kr4PI = afg.Kr * 4f * (float)Math.PI;
            afg.Km4PI = afg.Km * 4f * (float)Math.PI;
            afg.g2 = afg.g * afg.g;
            afg.outerRadius2 = afg.outerRadius * afg.outerRadius;
            afg.innerRadius2 = afg.innerRadius * afg.innerRadius;
            afg.scale = 1f / (afg.outerRadius - afg.innerRadius);
            afg.scaleDepth = -0.25f;
            afg.scaleOverScaleDepth = afg.scale / afg.scaleDepth;

        }

        private CelestialBody GetSun()
        {
            foreach (CelestialBody body in FlightGlobals.Bodies)
            {
                if (body.gameObject.name == "Sun")
                    return body;
            }
            return null;
        }

        double GetHillSphere(CelestialBody body)
        {
            return body.orbit.semiMajorAxis * (1.0 - body.orbit.eccentricity) * Math.Pow(body.Mass / (body.orbit.referenceBody.Mass + body.Mass), 1 / 3);
        }

        double GetSOI(CelestialBody body)
        {
            return body.orbit.semiMajorAxis * Math.Pow(body.Mass / (body.orbit.referenceBody.Mass + body.Mass), 0.4);
        }

        double GetPeriod(CelestialBody body)
        {
            return 2 * Math.PI * Math.Sqrt((Math.Pow(body.orbit.semiMajorAxis, 3) / 6.674E-11) / (body.Mass + body.referenceBody.Mass));
        }

        private static int CompareByMass(ChangedPlanet a, ChangedPlanet b)
        {
            if (a == null)
            {
                if (b == null)
                    return 0;
                else
                    return -1;
            }
            else
            {
                if (b == null)
                    return 1;
                else
                    return b.Mass.CompareTo(a.Mass);
            }
        }

    }
}
