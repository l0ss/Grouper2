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
        static public Document GetG2BannerDocument()
        {
            Document outputDocument = new Document();
            string barf = @"  .,-:::::/  :::::::..       ...      ...    :::::::::::::. .,::::::  :::::::..     .:::.   
,;;-'````'   ;;;;``;;;;   .;;;;;;;.   ;;     ;;; `;;;```.;;;;;;;''''  ;;;;``;;;;   ,;'``;.  
[[[   [[[[[[/ [[[,/[[['  ,[[     \[[,[['     [[[  `]]nnn]]'  [[cccc    [[[,/[[['   ''  ,[[' 
*$$c.    *$$  $$$$$$c    $$$,     $$$$$      $$$   $$$**     $$****    $$$$$$c     .c$$P'   
 `Y8bo,,,o88o 888b *88bo,*888,_ _,88P88    .d888   888o      888oo,__  888b *88bo,d88 _,oo, 
   `'YMUP*YMM MMMM   *W*   *YMMMMMP*  *YmmMMMM**   YMMMb     ****YUMMM MMMM   *W* MMMUP**^^ 
                                                            Now even Grouperer.              
                                                            github.com/mikeloss/Grouper2    
                                                            @mikeloss                          ";
            string[] barfLines = barf.Split(new[] { Environment.NewLine }, StringSplitOptions.None);

            ConsoleColor[] patternOne = { ConsoleColor.White, ConsoleColor.Yellow, ConsoleColor.Red, ConsoleColor.Red, ConsoleColor.DarkRed, ConsoleColor.DarkRed, ConsoleColor.White, ConsoleColor.White, ConsoleColor.White, ConsoleColor.White };
            ConsoleColor[] patternTwo =
            {
                ConsoleColor.White, ConsoleColor.White, ConsoleColor.Cyan, ConsoleColor.Blue, ConsoleColor.DarkBlue,
                ConsoleColor.DarkBlue, ConsoleColor.White, ConsoleColor.White, ConsoleColor.White, ConsoleColor.White,
            };
            int i = 0;
            foreach (string barfLine in barfLines)
            {
                string barfOne = barfLine.Substring(0, 82);
                string barfTwo = barfLine.Substring(82, 9);
                outputDocument.Children.Add(
                    new Span(barfOne) { Color = patternOne[i] }, new Span(barfTwo) { Color = patternTwo[i] }, "\n"
                );
                i += 1;
            }

            return outputDocument;
        }

        static public Document GetAssessedGPOOutput (KeyValuePair<string, JToken> inputKvp)
        {
            JToken gpo = inputKvp.Value;

            // catch that final 'scripts' section when it comes in
            if (inputKvp.Key == "Scripts")
            {
                JToken scripts = inputKvp.Value;
                Document scriptsDoc = new Document();
                scriptsDoc.Children.Add(
                    new Span("Scripts found in SYSVOL") {Color = ConsoleColor.Green}, "\n",
                    new Span("-----------------------") { Color = ConsoleColor.Green}
                );
                foreach (JProperty script in scripts)
                {
                    scriptsDoc.Children.Add(JsonToGrid(script.First, 0));
                }
                return scriptsDoc;
            }

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
            JToken scriptFindings = inputKvp.Value["Scripts"];
            
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

            if (scriptFindings != null)
            {
                Utility.DebugWrite(scriptFindings.ToString());
            }
            
            // add our findings docs to our final doc
            outputDocument.Children.Add(
                userPolFindingsDoc,
                machinePolFindingsDoc
                );

            return outputDocument;
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
                        //foreach (JProperty sgsFinding in cat.Value)
                        //{
                            findingsDocument.Children.Add(JsonToGrid(cat.Value, 0));
                        //}
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
                        findingsDocument.Children.Add(
                            new Span("Registry Values") { Color = ConsoleColor.Magenta }, "\n",
                            new Span("~~~~~~~~~~~~~~~") { Color = ConsoleColor.Magenta }
                        );
                            findingsDocument.Children.Add(JsonToGrid(cat.Value, 0));
                        continue;
                    }
                    if (cat.Key == "RegistrySettings")
                    {
                        findingsDocument.Children.Add(
                            new Span("GPP Registry Settings") { Color = ConsoleColor.Magenta }, "\n",
                            new Span("~~~~~~~~~~~~~~~~~~~~~") { Color = ConsoleColor.Magenta }
                        );
                        
                        foreach (JProperty regsetFindings in cat.Value)
                        {
                            foreach (JObject regsetFinding in regsetFindings)
                            {
                                foreach (JToken thing in regsetFinding.Values())
                                {
                                    findingsDocument.Children.Add(JsonToGrid(thing, 0));
                                }
                            }
                        }
                        continue;
                    }
                    if (cat.Key == "Registry Keys")
                    {
                        findingsDocument.Children.Add(
                            new Span("GPP Registry Keys") { Color = ConsoleColor.Magenta }, "\n",
                            new Span("~~~~~~~~~~~~~~~~~") { Color = ConsoleColor.Magenta }
                        );
                        foreach (JProperty regKeyFinding in cat.Value)
                        {
                            findingsDocument.Children.Add(JsonToGrid(regKeyFinding.Value, 0));
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
            // HERE BE DRAGONS
            iteration++;

            ConsoleColor gridColor = ConsoleColor.White;
            GridLength col1Width = GridLength.Auto;
            GridLength col2Width = GridLength.Auto;

            switch (iteration)
            {
                case 1:
                    gridColor = ConsoleColor.Gray;
                    //col1Width = GridLength.Char(20);
                    //col2Width = GridLength.Char(80);
                    break;
                case 2:
                    gridColor = ConsoleColor.Gray;
                    //col1Width = GridLength.Char(20);
                    //col2Width = GridLength.Auto;
                    break;
              
                default:
                    gridColor = ConsoleColor.DarkGray;
                    //col1Width = GridLength.Auto;
                    //col2Width = GridLength.Auto;
                    break;
            }

            Grid grid = new Grid
            {
                Color = ConsoleColor.White,
                Columns = {col1Width, col2Width},
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
                            Color = ConsoleColor.White,
                            Columns = { GridLength.Auto},
                            Stroke = LineThickness.Single,
                            StrokeColor = gridColor
                        };
                        foreach (JToken arrayItem in value)
                        {
                            subGrid.Children.Add(new Cell(arrayItem.ToString()));
                        }
                        grid.Children.Add(new Cell(name), new Cell(subGrid));
                    }
                    else
                    {
                        Grid subGrid = JsonToGrid(value, iteration);
                        grid.Children.Add(new Cell(name), subGrid);
                    }
                }
            }
            return grid;
        }
    }
}
