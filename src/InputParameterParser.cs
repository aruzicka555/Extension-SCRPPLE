//  Authors:  Robert M. Scheller, Vincent Schuster, Alec Kretchun

using Edu.Wisc.Forest.Flel.Util;
using Landis.Core;
using System.Collections.Generic;

namespace Landis.Extension.Scrapple
{
    /// <summary>
    /// A parser that reads the plug-in's parameters from text input.
    /// </summary>
    public class InputParameterParser
        : TextParser<IInputParameters>
    {
        private InputVar<string> speciesName;
        private ISpeciesDataset speciesDataset;
        private Dictionary<string, int> speciesLineNums;
        //---------------------------------------------------------------------
        public override string LandisDataValue
        {
            get
            {
                return PlugIn.ExtensionName;
            }
        }
        //---------------------------------------------------------------------

        public InputParameterParser()
        {
            //Edu.Wisc.Forest.Flel.Util.Percentage p = new Edu.Wisc.Forest.Flel.Util.Percentage();
            // RegisterForInputValues();
            this.speciesDataset = PlugIn.ModelCore.Species;
            this.speciesLineNums = new Dictionary<string, int>();
            this.speciesName = new InputVar<string>("Species");
        }

        //---------------------------------------------------------------------

        protected override IInputParameters Parse()
        {
            const string MapNames = "MapNames";
            ReadLandisDataVar();

            InputParameters parameters = new InputParameters();

            InputVar<int> timestep = new InputVar<int>("Timestep");
            ReadVar(timestep);
            parameters.Timestep = timestep.Value;
            
            //InputVar<double> rha = new InputVar<double>("RelativeHumiditySlopeAdjust");
            //if (ReadOptionalVar(rha))
            //    parameters.RelativeHumiditySlopeAdjustment = rha.Value;
            //else
            //    parameters.RelativeHumiditySlopeAdjustment = 1.0;

            // VS: Needed to spin up climate file
            //InputVar<int> duration = new InputVar<int>("Duration");
            //ReadVar(duration);
            //parameters.Duration = duration.Value;


            //                   --------- Input files ---------
            //--------------------------------------------------------------------------
            //InputVar<string> climateConfigFile = new InputVar<string>("ClimateConfigFile");
            //ReadVar(climateConfigFile);
            //parameters.ClimateConfigFile = climateConfigFile.Value;

            InputVar<string> humanIgnitionsMapFile = new InputVar<string>("AccidentalIgnitionsMap");
            ReadVar(humanIgnitionsMapFile);
            parameters.AccidentalFireMap = humanIgnitionsMapFile.Value;

            InputVar<string> lighteningIgnitionsMapFile = new InputVar<string>("LightningIgnitionsMap");
            ReadVar(lighteningIgnitionsMapFile);
            parameters.LighteningFireMap = lighteningIgnitionsMapFile.Value;

            InputVar<string> rxIgnitionsMapFile = new InputVar<string>("RxIgnitionsMap");
            ReadVar(rxIgnitionsMapFile);
            parameters.RxFireMap = rxIgnitionsMapFile.Value;

            InputVar<string> humanSuppressionMapFile = new InputVar<string>("AccidentalSuppressionMap");
            ReadVar(humanSuppressionMapFile);
            parameters.AccidentalFireMap = humanSuppressionMapFile.Value;

            InputVar<string> lighteningSuppressionMapFile = new InputVar<string>("LightningSuppressionMap");
            ReadVar(lighteningSuppressionMapFile);
            parameters.LighteningFireMap = lighteningSuppressionMapFile.Value;

            InputVar<string> rxSuppressionMapFile = new InputVar<string>("RxSuppressionMap");
            ReadVar(rxSuppressionMapFile);
            parameters.RxFireMap = rxSuppressionMapFile.Value;
            

            // Load Ground Slope Data
            //const string GroundSlopeFile = "GroundSlopeFile";
            InputVar<string> groundSlopeFile = new InputVar<string>("GroundSlopeMap");
            ReadVar(groundSlopeFile);

            PlugIn.ModelCore.UI.WriteLine("   Loading Slope data...");
            Topography.ReadGroundSlopeMap(groundSlopeFile.Value);

            // Load Uphill Slope Azimuth Data
            InputVar<string> uphillSlopeMap = new InputVar<string>("UphillSlopeAzimuthMap");
            ReadVar(uphillSlopeMap);

            PlugIn.ModelCore.UI.WriteLine("   Loading Azimuth data...");
            Topography.ReadUphillSlopeAzimuthMap(uphillSlopeMap.Value);

            InputVar<double> lightningB0 = new InputVar<double>("LightningIgnitionsB0");
            ReadVar(lightningB0);
            parameters.LightningIgnitionB0 = lightningB0.Value;

            InputVar<double> lightningB1 = new InputVar<double>("LightningIgnitionsB1");
            ReadVar(lightningB1);
            parameters.LightningIgnitionB1 = lightningB1.Value;

            InputVar<double> rxB0 = new InputVar<double>("RxFireIgnitionsB0");
            ReadVar(rxB0);
            parameters.LightningIgnitionB0 = rxB0.Value;

            InputVar<double> rxB1 = new InputVar<double>("RxFireIgnitionsB1");
            ReadVar(rxB1);
            parameters.LightningIgnitionB1 = rxB1.Value;

            InputVar<double> accidentalB0 = new InputVar<double>("AccidentalIgnitionsB0");
            ReadVar(accidentalB0);
            parameters.LightningIgnitionB0 = accidentalB0.Value;

            InputVar<double> accidentalB1 = new InputVar<double>("AccidentalIgnitionsB1");
            ReadVar(accidentalB1);
            parameters.LightningIgnitionB1 = accidentalB1.Value;

            InputVar<double> maxFF = new InputVar<double>("MaximumFineFuels");
            ReadVar(maxFF);
            parameters.MaxFineFuels = maxFF.Value;

            //-------------------------------------------------------------------
            //  Read table of Fire Damage classes.
            //  Damages are in increasing order.
            PlugIn.ModelCore.UI.WriteLine("   Loading Fire mortality data...");

            const string FireIntensityClass_1_DamageTable = "FireIntensityClass_1_DamageTable";
            const string FireIntensityClass_2_DamageTable = "FireIntensityClass_2_DamageTable";
            const string FireIntensityClass_3_DamageTable = "FireIntensityClass_3_DamageTable";


            InputVar<string> spp = new InputVar<string>("Species Name");
            InputVar<int> maxAge = new InputVar<int>("Max Interval Age");
            InputVar<int> minAge = new InputVar<int>("Min Interval Age");
            InputVar<double> probMortality = new InputVar<double>("Probability of Mortality");

            ReadName(FireIntensityClass_1_DamageTable);
            while (!AtEndOfInput && CurrentName != FireIntensityClass_2_DamageTable)
            {
                //int previousMaxAge = 0;

                StringReader currentLine = new StringReader(CurrentLine);

                //ISpecies species = ReadSpecies(currentLine);
                ReadValue(speciesName, currentLine);
                ISpecies species = PlugIn.ModelCore.Species[speciesName.Value.Actual];
                if (species == null)
                    throw new InputValueException(speciesName.Value.String,
                                                  "{0} is not a species name.",
                                                  speciesName.Value.String);
                //TextReader.SkipWhitespace(currentLine);
                //while (currentLine.Peek() != -1)
                //{

                IFireDamage damage = new FireDamage();
                parameters.FireDamages_Severity1.Add(damage);
                damage.DamageSpecies = species;

                ReadValue(minAge, currentLine);
                damage.MinAge = minAge.Value;

                ReadValue(maxAge, currentLine);
                damage.MaxAge = maxAge.Value;

                ////  Maximum age for every damage must be > 
                ////  maximum age of previous damage.
                //if (maxAge.Value.Actual <= previousMaxAge)
                //{
                //    throw new InputValueException(maxAge.Value.String,
                //        "MaxAge must > the maximum age ({0}) of the preceeding damage class",
                //        previousMaxAge);
                //}

                //previousMaxAge = maxAge.Value.Actual;

                ReadValue(probMortality, currentLine);
                damage.ProbablityMortality = probMortality.Value;

                //TextReader.SkipWhitespace(currentLine);
                //}

                GetNextLine();
            }

            ReadName(FireIntensityClass_2_DamageTable);
            while (!AtEndOfInput && CurrentName != FireIntensityClass_3_DamageTable)
            {
                //int previousMaxAge = 0;

                StringReader currentLine = new StringReader(CurrentLine);

                //ISpecies species = ReadSpecies(currentLine);
                ReadValue(speciesName, currentLine);
                ISpecies species = PlugIn.ModelCore.Species[speciesName.Value.Actual];
                if (species == null)
                    throw new InputValueException(speciesName.Value.String,
                                                  "{0} is not a species name.",
                                                  speciesName.Value.String);

                IFireDamage damage = new FireDamage();
                parameters.FireDamages_Severity2.Add(damage);
                damage.DamageSpecies = species;

                ReadValue(minAge, currentLine);
                damage.MinAge = minAge.Value;

                ReadValue(maxAge, currentLine);
                damage.MaxAge = maxAge.Value;

                ReadValue(probMortality, currentLine);
                damage.ProbablityMortality = probMortality.Value;

                GetNextLine();
            }

            ReadName(FireIntensityClass_3_DamageTable);
            while (!AtEndOfInput && CurrentName != MapNames) 
            {

                StringReader currentLine = new StringReader(CurrentLine);

                //ISpecies species = ReadSpecies(currentLine);
                ReadValue(speciesName, currentLine);
                ISpecies species = PlugIn.ModelCore.Species[speciesName.Value.Actual];
                if (species == null)
                    throw new InputValueException(speciesName.Value.String,
                                                  "{0} is not a species name.",
                                                  speciesName.Value.String);
                IFireDamage damage = new FireDamage();
                parameters.FireDamages_Severity3.Add(damage);
                damage.DamageSpecies = species;

                ReadValue(minAge, currentLine);
                damage.MinAge = minAge.Value;

                ReadValue(maxAge, currentLine);
                damage.MaxAge = maxAge.Value;

                ReadValue(probMortality, currentLine);
                damage.ProbablityMortality = probMortality.Value;

                GetNextLine();
            }

            // Next, read out the data to verify:
            PlugIn.ModelCore.UI.WriteLine("   Fire mortality data for severity class 1:");
            foreach (FireDamage damage in parameters.FireDamages_Severity1)
                PlugIn.ModelCore.UI.WriteLine("      {0} : {1} : {2}", damage.DamageSpecies.Name, damage.MaxAge, damage.ProbablityMortality);

            PlugIn.ModelCore.UI.WriteLine("   Fire mortality data for severity class 2:");
            foreach (FireDamage damage in parameters.FireDamages_Severity2)
                PlugIn.ModelCore.UI.WriteLine("      {0} : {1} : {2}", damage.DamageSpecies.Name, damage.MaxAge, damage.ProbablityMortality);

            PlugIn.ModelCore.UI.WriteLine("   Fire mortality data for severity class 3:");
            foreach (FireDamage damage in parameters.FireDamages_Severity3)
                PlugIn.ModelCore.UI.WriteLine("      {0} : {1} : {2}", damage.DamageSpecies.Name, damage.MaxAge, damage.ProbablityMortality);

            InputVar<string> mapNames = new InputVar<string>(MapNames);
            ReadVar(mapNames);
            parameters.MapNamesTemplate = mapNames.Value;

            return parameters; 
        }
        //---------------------------------------------------------------------

        //public static Distribution DistParse(string word)
        //{
        //    switch (word)
        //    {
        //        case "gamma":     return Distribution.gamma;
        //        case "lognormal": return Distribution.lognormal;
        //        case "normal":    return Distribution.normal;
        //        case "Weibull":   return Distribution.Weibull;
        //        default: throw new System.FormatException("Valid Distributions: gamma, lognormal, normal, Weibull");
        //    }
        //}


        //---------------------------------------------------------------------

        /// <summary>
        /// Registers the appropriate method for reading input values.
        /// </summary>
        //public static void RegisterForInputValues()
        //{

        //    Edu.Wisc.Forest.Flel.Util.Type.SetDescription<Distribution>("Random Number Distribution");
        //    InputValues.Register<Distribution>(DistParse);
        //}
        //---------------------------------------------------------------------

        private void ValidatePath(InputValue<string> path)
        {
            if (path.Actual.Trim(null) == "")
                throw new InputValueException(path.String,
                                              "Invalid file path: {0}",
                                              path.String);
        }

        //---------------------------------------------------------------------

        /// <summary>
        /// Reads a species name from the current line, and verifies the name.
        /// </summary>
        //private ISpecies ReadSpecies(StringReader currentLine)
        //{
            
        //    //int lineNumber;
        //    //if (speciesLineNums.TryGetValue(species.Name, out lineNumber))
        //    //    throw new InputValueException(speciesName.Value.String,
        //    //                                  "The species {0} was previously used on line {1}",
        //    //                                  speciesName.Value.String, lineNumber);
        //    //else
        //    //    speciesLineNums[species.Name] = LineNumber;
        //    return species;
        //}
    }
}
