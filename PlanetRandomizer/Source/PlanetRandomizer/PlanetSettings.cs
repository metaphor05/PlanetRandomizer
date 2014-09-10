using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.IO;
using UnityEngine;
using KSP;

namespace PlanetRandomizer
{
    public class PlanetSettings
    {
        private static Settings mInstance;
        public static Settings Instance
        {
            get
            {
                return mInstance = mInstance ?? Settings.Load();
            }
        }

        public static void Load(string file)
        {
            mInstance = Settings.Load(file);
        }
    }

    public class Settings
    {
        [Persistent]
        public int seed = 1;
        public EditableInt Seed
        {
            get { return new EditableInt(seed); }
            set { seed = value.val; }
        }

        [Persistent(collectionIndex = "PLANET")]
        public ChangedPlanet[] Planets = new ChangedPlanet[] { };


        private static String File
        {
            get
            {
                return KSPUtil.ApplicationRootPath + "/saves/" + HighLogic.SaveFolder + "/PlanetRandomizer.cfg";
            }
        }

        public void Save(string file)
        {
            try
            {
                ConfigNode save = new ConfigNode();
                ConfigNode.CreateConfigFromObject(PlanetSettings.Instance, save);
                save.Save(file);
            }
            catch
            {

            }
        }

        public static Settings Load()
        {
            ConfigNode load = ConfigNode.Load(File);
            Settings settings = new Settings();
            if (load == null)
            {
                settings.Save(File);
                return settings;
            }
            ConfigNode.LoadObjectFromConfig(settings, load);

            return settings;
        }

        public static Settings Load(string file)
        {
            ConfigNode load = ConfigNode.Load(file);
            Settings settings = new Settings();
            if (load == null)
            {
                settings.Save(file);
                return settings;
            }
            ConfigNode.LoadObjectFromConfig(settings, load);

            return settings;
        }
    }
}
