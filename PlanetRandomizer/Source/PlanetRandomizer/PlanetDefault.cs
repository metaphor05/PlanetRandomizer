using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PlanetRandomizer
{
    public class PlanetDefault
    {
        private static DefSettings mInstance;
        public static DefSettings Instance
        {
            get
            {
                return mInstance = mInstance ?? DefSettings.Load(KSPUtil.ApplicationRootPath + "/GameData/PlanetRandomizer/Resources/PlanetRandomizerDefault.cfg");
            }
        }

        public static void Load(string file)
        {
            mInstance = DefSettings.Load(file);
        }
    }

    public class DefSettings
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
                return KSPUtil.ApplicationRootPath + "/GameData/PlanetRandomizer/Resources/PlanetRandomizerDefault.cfg";
            }
        }

        public void Save(string file)
        {
            try
            {
                ConfigNode save = new ConfigNode();
                ConfigNode.CreateConfigFromObject(PlanetDefault.Instance, save);
                save.Save(file);
            }
            catch
            {

            }
        }

        public static DefSettings Load()
        {
            ConfigNode load = ConfigNode.Load(File);
            DefSettings settings = new DefSettings();
            if (load == null)
            {
                settings.Save(File);
                return settings;
            }
            ConfigNode.LoadObjectFromConfig(settings, load);

            return settings;
        }

        public static DefSettings Load(string file)
        {
            ConfigNode load = ConfigNode.Load(file);
            DefSettings settings = new DefSettings();
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
