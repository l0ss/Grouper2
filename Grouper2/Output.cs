using System;
using System.Collections.Generic;
using System.DirectoryServices;
using System.Linq;
using System.Text;
using Alba.CsConsoleFormat;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Grouper2
{
    class Output
    {
        static public void GetAssessedGPOOutput (KeyValuePair<string, JToken> inputKvp)
        {
            // munge it to a JObject
            JToken gpo = inputKvp.Value;
            
            JToken gpoProps = gpo["GPOProps"];
            // title it with either the display name or the uid, depending on what we have
            string gpoTitle = "";
            if (gpo["GPOProps"]["Display Name"] != null)
            {
                gpoTitle = gpoProps["Display Name"].ToString();
            }
            else
            {
                gpoTitle = gpoProps["UID"].ToString();
            }


            Document outputDocument = new Document();
            ////////////////////////////////////////
            ///  Title and Properties
            ////////////////////////////////////////
            
            outputDocument.Children.Add(
                // nice Title
                new Span(gpoTitle) {Color = ConsoleColor.Green}, "\n",
                new Span("--------------------------------------") { Color = ConsoleColor.Green}, "\n", "\n",
                // nice Section header
                new Span("GPO Properties") {Color = ConsoleColor.Yellow}, "\n",
                new Span("##############") { Color = ConsoleColor.Yellow }
            );

            Grid gpoPropsGrid = JsonToGrid(gpoProps, 0);
            outputDocument.Children.Add(gpoPropsGrid);

            // grab all our findings
            JToken uPolFindings = inputKvp.Value["Findings"]["User Policy"];
            JToken mPolFindings = inputKvp.Value["Findings"]["Machine Policy"];
            
            // create a document for each to go in
            Document userPolFindingsDoc = new Document();
            Document machinePolFindingsDoc = new Document();

            // send the json off to get turned into nice output
            if (uPolFindings != null)
            {
                userPolFindingsDoc = GetFindingsDocument(uPolFindings, "user");
            }

            // and again
            if (mPolFindings != null)
            {
                machinePolFindingsDoc = GetFindingsDocument(mPolFindings, "machine");
            }
            
            // add our findings docs to our final doc
            outputDocument.Children.Add(
                userPolFindingsDoc,
                machinePolFindingsDoc
                );

            ConsoleRenderer.RenderDocument(outputDocument);
        }

        private static Document GetFindingsDocument(JToken polFindings, string polType)
        {
            Document findingsDocument = new Document();

            if (polType == "user")
            {
                findingsDocument.Children.Add(
                    new Span("\nFindings in User Policy") { Color = ConsoleColor.Yellow }, "\n",
                    new Span("#######################") { Color = ConsoleColor.Yellow }, "\n"
                    );
            }
            else if (polType == "machine")
            {
                findingsDocument.Children.Add(
                    new Span("\nFindings in Machine Policy") { Color = ConsoleColor.Yellow }, "\n",
                    new Span("##########################") { Color = ConsoleColor.Yellow }, "\n"
                    );
            }
            else
            {
                Utility.DebugWrite("THERE IS A THIRD WAY.");
            }

            foreach (JObject polFindingCat in polFindings)
            {
                foreach (KeyValuePair<string, JToken> cat in polFindingCat)
                {
                    if (cat.Key == "Drives")
                    {
                        findingsDocument.Children.Add(
                            new Span("GPP Drive Mappings") { Color = ConsoleColor.Magenta }, "\n",
                            new Span("~~~~~~~~~~~~~~~~~~") { Color = ConsoleColor.Magenta }
                        );
                        foreach (JToken findings in cat.Value)
                        {
                            foreach (JToken finding in findings)
                            {
                                findingsDocument.Children.Add(JsonToGrid(finding, 0));
                            }
                        }
                        continue;
                    }
                    if (cat.Key == "Privilege Rights")
                    {
                        findingsDocument.Children.Add(
                            new Span("OS Privileges") { Color = ConsoleColor.Magenta }, "\n",
                            new Span("~~~~~~~~~~~~~") { Color = ConsoleColor.Magenta }
                        );
                        findingsDocument.Children.Add(JsonToGrid(cat.Value, 0));
                        continue;
                    }
                    if (cat.Key == "Group Membership")
                    {
                        findingsDocument.Children.Add(
                            new Span("Group Membership") { Color = ConsoleColor.Magenta }, "\n",
                            new Span("~~~~~~~~~~~~~~~~") { Color = ConsoleColor.Magenta }
                        );
                        findingsDocument.Children.Add(JsonToGrid(cat.Value, 0));
                        continue;
                    }
                    if (cat.Key == "Groups")
                    {
                        findingsDocument.Children.Add(
                            new Span("GPP Users and Groups") { Color = ConsoleColor.Magenta }, "\n",
                            new Span("~~~~~~~~~~~~~~~~~~~~") { Color = ConsoleColor.Magenta }
                        );
                        JToken gppUsers = cat.Value["GPP Users"];
                        JToken gppGroups = cat.Value["GPP Groups"];

                        if (gppGroups != null)
                        {
                            findingsDocument.Children.Add(new Span("\nGPP Groups\n"), new Span("---------"));
                            foreach (JProperty groupFinding in gppGroups)
                            {
                                findingsDocument.Children.Add(JsonToGrid(groupFinding.Value, 0));
                            }
                        }

                        if (gppUsers != null)
                        {
                            findingsDocument.Children.Add(new Span("\nGPP Users\n"), new Span("---------"));
                            foreach (JProperty userFinding in gppUsers)
                            {
                                findingsDocument.Children.Add(JsonToGrid(userFinding.Value, 0));
                            }
                        }
                        continue;
                    }
                    if (cat.Key == "DataSources")
                    {
                        findingsDocument.Children.Add(
                            new Span("GPP Data Sources") { Color = ConsoleColor.Magenta }, "\n",
                            new Span("~~~~~~~~~~~~~~~~") { Color = ConsoleColor.Magenta }
                        );
                        foreach (JProperty datasourceFinding in cat.Value)
                        {
                            findingsDocument.Children.Add(JsonToGrid(datasourceFinding.Value, 0));
                        }
                        continue;
                    }
                    if (cat.Key == "Printers")
                    {
                        findingsDocument.Children.Add(
                            new Span("GPP Printer Mappings") { Color = ConsoleColor.Magenta }, "\n",
                            new Span("~~~~~~~~~~~~~~~~~~~~") { Color = ConsoleColor.Magenta }
                        );
                        //TODO validate this once my azure thing is back online
                        foreach (JProperty printerFinding in cat.Value)
                        {
                            findingsDocument.Children.Add(JsonToGrid(printerFinding.Value, 0));
                        }
                        continue;
                    }
                    if (cat.Key == "Files")
                    {
                        findingsDocument.Children.Add(
                            new Span("GPP Files") { Color = ConsoleColor.Magenta }, "\n",
                            new Span("~~~~~~~~~") { Color = ConsoleColor.Magenta }
                        );
                        foreach (JProperty fileFinding in cat.Value)
                        {
                            findingsDocument.Children.Add(JsonToGrid(fileFinding.Value, 0));
                        }
                        continue;
                    }
                    if (cat.Key == "ScheduledTasks")
                    {
                        findingsDocument.Children.Add(
                            new Span("GPP Scheduled Tasks") { Color = ConsoleColor.Magenta }, "\n",
                            new Span("~~~~~~~~~~~~~~~~~~~") { Color = ConsoleColor.Magenta }
                        );
                        foreach (JProperty schedtaskFinding in cat.Value)
                        {
                            findingsDocument.Children.Add(JsonToGrid(schedtaskFinding.Value, 0));
                        }
                        continue;
                    }
                    if (cat.Key == "Assigned Applications")
                    {
                        findingsDocument.Children.Add(
                            new Span("Assigned Applications") { Color = ConsoleColor.Magenta }, "\n",
                            new Span("~~~~~~~~~~~~~~~~~~~~~") { Color = ConsoleColor.Magenta }
                        );
                        foreach (JProperty aasFinding in cat.Value)
                        {
                            findingsDocument.Children.Add(JsonToGrid(aasFinding.Value, 0));
                        }
                        continue;
                    }
                    if (cat.Key == "Service General Setting")
                    {
                        //TODO double check this in Online mode
                        findingsDocument.Children.Add(
                            new Span("Windows Services") { Color = ConsoleColor.Magenta }, "\n",
                            new Span("~~~~~~~~~~~~~~~~") { Color = ConsoleColor.Magenta }
                        );
                        foreach (JProperty sgsFinding in cat.Value)
                        {
                            findingsDocument.Children.Add(JsonToGrid(sgsFinding.Value, 0));
                        }
                        continue;
                    }
                    if (cat.Key == "NTServices")
                    {
                        findingsDocument.Children.Add(
                            new Span("GPP NT Services") { Color = ConsoleColor.Magenta }, "\n",
                            new Span("~~~~~~~~~~~~~~~") { Color = ConsoleColor.Magenta }
                        );
                        foreach (JProperty ntserviceFinding in cat.Value)
                        {
                            findingsDocument.Children.Add(JsonToGrid(ntserviceFinding.Value, 0));
                        }
                        continue;
                    }
                    if (cat.Key == "Shortcuts")
                    {
                        findingsDocument.Children.Add(
                            new Span("GPP Shortcuts") { Color = ConsoleColor.Magenta }, "\n",
                            new Span("~~~~~~~~~~~~~") { Color = ConsoleColor.Magenta }
                        );
                        foreach (JProperty shortcutFinding in cat.Value)
                        {
                            findingsDocument.Children.Add(JsonToGrid(shortcutFinding.Value, 0));
                        }
                        continue;
                    }
                    if (cat.Key == "System Access")
                    {
                        findingsDocument.Children.Add(
                            new Span("System Access") { Color = ConsoleColor.Magenta }, "\n",
                            new Span("~~~~~~~~~~~~~") { Color = ConsoleColor.Magenta }
                        );
                            findingsDocument.Children.Add(JsonToGrid(cat.Value, 0));
                        continue;
                    }
                    if (cat.Key == "Kerberos Policy")
                    {
                        findingsDocument.Children.Add(
                            new Span("Kerberos Policy") { Color = ConsoleColor.Magenta }, "\n",
                            new Span("~~~~~~~~~~~~~~~") { Color = ConsoleColor.Magenta }
                        );
                        findingsDocument.Children.Add(JsonToGrid(cat.Value, 0));
                        
                        continue;
                    }
                    if (cat.Key == "Registry Values")
                    {
                        // TODO patched out until registry values thing is done
                        /*
                        findingsDocument.Children.Add(
                            new Span("Registry Values") { Color = ConsoleColor.Magenta }, "\n",
                            new Span("~~~~~~~~~~~~~~~") { Color = ConsoleColor.Magenta }
                        );
                        foreach (JToken regvalFinding in cat.Value)
                        {
                            findingsDocument.Children.Add(JsonToGrid(regvalFinding));
                        }
                        */
                        continue;
                    }
                    if (cat.Key == "RegistrySettings")
                    {
                        findingsDocument.Children.Add(
                            new Span("GPP Registry Settings") { Color = ConsoleColor.Magenta }, "\n",
                            new Span("~~~~~~~~~~~~~~~~~~~~~") { Color = ConsoleColor.Magenta }
                        );

                        // TODO patched out until azure back online
                        
                        /*
                        foreach (JProperty regsetFindings in cat.Value)
                        {
                            Console.WriteLine("1");
                            Utility.DebugWrite(regsetFindings.ToString());
                            foreach (JObject regsetFinding in regsetFindings)
                            {
                                Console.WriteLine("2");
                                Utility.DebugWrite(regsetFinding.ToString());
                                foreach (JProperty thing in regsetFinding.Values())
                                {
                                    Utility.DebugWrite(thing.ToString());
                                }
                                //JProperty findingVal = regsetFinding.Value;
                                //findingsDocument.Children.Add(JsonToGrid(findingVal, 0));
                            }
                        }
                        */
                        continue;
                    }
                    if (cat.Key == "Registry Keys")
                    {
                        findingsDocument.Children.Add(
                            new Span("GPP Registry Keys") { Color = ConsoleColor.Magenta }, "\n",
                            new Span("~~~~~~~~~~~~~~~~~") { Color = ConsoleColor.Magenta }
                        );
                        foreach (JProperty regkeyFinding in cat.Value)
                        {
                            findingsDocument.Children.Add(JsonToGrid(regkeyFinding.Value, 0));
                        }
                        continue; ;
                    }
                    if (cat.Key == "EnvironmentVariables")
                    {
                        findingsDocument.Children.Add(
                            new Span("GPP Env Vars") { Color = ConsoleColor.Magenta }, "\n",
                            new Span("~~~~~~~~~~~~") { Color = ConsoleColor.Magenta }
                        );
                        foreach (JProperty envvarFinding in cat.Value)
                        {
                            findingsDocument.Children.Add(JsonToGrid(envvarFinding.Value, 0));
                        }
                        continue;
                    }
                    if (cat.Key == "IniFiles")
                    {
                        findingsDocument.Children.Add(
                            new Span("GPP Ini Files") { Color = ConsoleColor.Magenta }, "\n",
                            new Span("~~~~~~~~~~~~~") { Color = ConsoleColor.Magenta }
                        );
                        foreach (JProperty iniFileFinding in cat.Value)
                        {
                            findingsDocument.Children.Add(JsonToGrid(iniFileFinding.Value, 0));
                        }
                        continue;
                    }
                    if (cat.Key == "Scripts")
                    {
                        findingsDocument.Children.Add(
                            new Span("Scripts") { Color = ConsoleColor.Magenta }, "\n",
                            new Span("~~~~~~~") { Color = ConsoleColor.Magenta }
                        );
                        foreach (JProperty scriptFindings in cat.Value)
                        {
                            foreach (JProperty scriptFinding in scriptFindings.Value)
                            {
                                findingsDocument.Children.Add(JsonToGrid(scriptFinding.Value, 0));
                            }
                        }
                        continue;
                    }
                    else
                    {
                        findingsDocument.Children.Add(new Span(cat.Key + " wasn't properly prettified for output.\n") {Color= ConsoleColor.Red});
                    }
                }
            }

            return findingsDocument;
        }

        private static Grid JsonToGrid(JToken jprops, int iteration)
        {
            iteration++;

            ConsoleColor gridColor = ConsoleColor.White;

            switch (iteration)
            {
                case 1:
                    gridColor = ConsoleColor.Gray;
                    break;
                case 2:
                    gridColor = ConsoleColor.DarkGray;
                    break;
              
                default:
                    gridColor = ConsoleColor.Black;
                    break;
            }

            Grid grid = new Grid
            {
                Color = ConsoleColor.White,
                Columns = {GridLength.Auto, GridLength.Auto},
                Stroke = LineThickness.Single,
                StrokeColor = gridColor
            };

            foreach (JProperty jprop in jprops)
            {
                string name = jprop.Name;
                JToken value = jprop.Value;
                if ((value.Count() == 1) || (value.Count() == 0))
                {
                    if (jprop.Value is JArray)
                    {
                        foreach (JToken arrayItem in jprop.Value)
                        {
                            grid.Children.Add(new Cell(name), new Cell(JsonToGrid(arrayItem, iteration)));
                        }
                    }
                    else if (jprop.Value is JObject)
                    {
                        grid.Children.Add(new Cell(jprop.Name), new Cell(JsonToGrid(jprop.Value, iteration)));
                    }
                    else
                    {
                        grid.Children.Add(new Cell(jprop.Name), new Cell(jprop.Value.ToString()));
                    }
                }
                else if (value.Count() > 1)
                {
                    if (value is JArray)
                    {
                        Grid subGrid = new Grid
                        {
                            StrokeColor = gridColor,
                            Columns = {GridLength.Auto},
                            Stroke = LineThickness.None
                        };
                        foreach (JToken arrayItem in value)
                        {
                            subGrid.Children.Add(new Cell(JsonToGrid(arrayItem, iteration)));
                        }
                        grid.Children.Add(new Cell(name), new Cell(subGrid));
                    }
                    else
                    {
                        Grid subGrid = JsonToGrid(value, iteration);
                        grid.Children.Add(new Cell(name), new Cell(subGrid));
                    }
                }
            }
            return grid;
        }
    }
}
