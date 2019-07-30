using System;
using System.Collections.Generic;
using System.Linq;
using Alba.CsConsoleFormat;
using Grouper2.Auditor;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Grouper2.Utility
{
    class Output
    {
        public static Document GetG2BannerDocument()
        {
            Document outputDocument = new Document();
            string[] barfLines = new string[] {
                @"  .,-:::::/::::::..      ..     ...   ::::::::::::..,::::::::::::..  ,;'``;. ",
                @",;;-'````' ;;;``;;;;  .;;;;;;.  ;;    ;;;`;;;```.;;;;;;'''';;;``;;;; ''  ,[[ ",
                @"[[[   [[[[[[[[,/[[[' ,[[    \[[[['    [[[ `]]nnn]]' [[cccc  [[,/[[['  .c$P'  ",
                @"'$$c.    '$$$$$$$c   $$$,    $$$$     $$$  $$$''    $$''''  $$$$$c   d8MMMUP*",
                @" `Y8bo,,,o8888b '88bo'888,__,8888   .d888  888o     888oo,__88b '88bo        ",
                @"   `'YMUP'YMMMM   'W'  'YMMMMP' 'YmMMMM''  YMMMb    ''''YUMMMMM   'W'        ",
                @"                                                    Now even Grouperer.      ",
                @"                                                    github.com/l0ss/Grouper2 ",
                @"                                                    @mikeloss                "
            };

            ConsoleColor[] patternOne = { ConsoleColor.White, ConsoleColor.Yellow, ConsoleColor.Red, ConsoleColor.Red, ConsoleColor.DarkRed, ConsoleColor.DarkRed, ConsoleColor.White, ConsoleColor.White, ConsoleColor.White, ConsoleColor.White };
            ConsoleColor[] patternTwo =
            {
                ConsoleColor.White, ConsoleColor.Cyan, ConsoleColor.Blue, ConsoleColor.DarkBlue, ConsoleColor.White,
                ConsoleColor.White, ConsoleColor.White, ConsoleColor.White, ConsoleColor.White, ConsoleColor.White
            };
            int i = 0;
            foreach (string barfLine in barfLines)
            {
                string barfOne = barfLine.Substring(0, 69);
                string barfTwo = barfLine.Substring(69, 8);
                outputDocument.Children.Add(
                    new Span(barfOne) { Color = patternOne[i] }, new Span(barfTwo) { Color = patternTwo[i] }, "\n"
                );
                i += 1;
            }

            return outputDocument;
        }

        public static void OutputAuditReport(AuditReport report, GrouperPlan plan)
        {
            // Final output is finally happening finally here:

            // dump the json if we are in a basic output mode
            if (!plan.PrettyOutput && !plan.HtmlOut)
            {
                string jsonReport = JsonConvert.SerializeObject(report, Formatting.Indented);
                Console.WriteLine(jsonReport);
                Console.Error.WriteLine(
                    "If you find yourself thinking 'wtf this is very ugly and hard to read', consider trying the -g argument.");
                return;
            }
            
            
            // gotta add a line feed to make sure we're clear to write the nice output.
            Console.Error.WriteLine("\n");
            /*
            if (this.HtmlOut)
            {
                try
                {
                    // gotta add a line feed to make sure we're clear to write the nice output.

                    Document htmlDoc = new Document();

                    htmlDoc.Children.Add(Output.GetG2BannerDocument());

                    foreach (KeyValuePair<string, JToken> gpo in this.Results)
                    {
                        htmlDoc.Children.Add(Output.GetAssessedGpoOutput(gpo));
                    }

                    ConsoleRenderer.RenderDocument(htmlDoc,
                        new HtmlRenderTarget( System.IO.File.Create(this.HtmlOutPath), new UTF8Encoding(false)));
                }
                catch (UnauthorizedAccessException)
                {
                    Console.Error.WriteLine("Tried to write html output file but I'm not allowed.");
                }
            }

            if (this.PrettyOutput)
            {
                Document prettyDoc = new Document();

                prettyDoc.Children.Add(Output.GetG2BannerDocument());

                foreach (KeyValuePair<string, JToken> gpo in this.Results)
                {
                    prettyDoc.Children.Add(Output.GetAssessedGpoOutput(gpo));
                }

                ConsoleRenderer.RenderDocument(prettyDoc);
            }
             */
        }

        public static Document GetAssessedGpoOutput (KeyValuePair<string, JToken> inputKvp)
        {
            JToken gpo = inputKvp.Value;

            ConsoleColor displayNameColor = ConsoleColor.Green;
            ConsoleColor sectionColor = ConsoleColor.Yellow;

            // catch that final 'scripts' section when it comes in
            if (inputKvp.Key == "Scripts")
            {
                JToken scripts = inputKvp.Value;
                Document scriptsDoc = new Document();
                scriptsDoc.Children.Add(
                    new Span("Scripts found in SYSVOL") {Color = sectionColor}, "\n",
                    new Span("-----------------------") { Color = sectionColor}
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
                new Span("~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~") { Color = displayNameColor}, "\n",
                new Span(gpoTitle) {Color = displayNameColor}, "\n",
                new Span("~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~") { Color = displayNameColor}, "\n",
                // nice Section header
                new Span("GPO Properties") {Color = sectionColor}, "\n",
                new Span("##############") { Color = sectionColor }
            );

            Grid gpoPropsGrid = JsonToGrid(gpoProps, 0);
            outputDocument.Children.Add(gpoPropsGrid);

            // grab all our findings
            JToken uPolFindings = inputKvp.Value["Findings"]["User Policy"];
            JToken mPolFindings = inputKvp.Value["Findings"]["Machine Policy"];
            JToken packageFindings = inputKvp.Value["Findings"]["Packages"];
            
            // create a document for each to go in
            Document userPolFindingsDoc = new Document();
            Document machinePolFindingsDoc = new Document();
            Document packageFindingsDoc = new Document();


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
            
            
            if (packageFindings != null)
            {
                packageFindingsDoc.Children.Add(
                    new Span("MSI Packages") { Color = ConsoleColor.Yellow }, "\n",
                    new Span("############") { Color = ConsoleColor.Yellow }
                );
                foreach (JProperty package in packageFindings)
                {
                    packageFindingsDoc.Children.Add(JsonToGrid(package.Value, 0));
                }
            }
            
            // add our findings docs to our final doc
            outputDocument.Children.Add(
                userPolFindingsDoc,
                machinePolFindingsDoc,
                packageFindingsDoc
                );

            return outputDocument;
        }

        private static Document GetFindingsDocument(JToken polFindings, string polType)
        {
            Document findingsDocument = new Document();
            
            ConsoleColor sectionColor = ConsoleColor.Yellow;
            ConsoleColor findingColor = ConsoleColor.Cyan;

            if (polType == "user")
            {
                findingsDocument.Children.Add(
                    new Span("\nFindings in User Policy") { Color = sectionColor }, "\n",
                    new Span("#######################") { Color = sectionColor }, "\n"
                    );
            }
            else if (polType == "machine")
            {
                findingsDocument.Children.Add(
                    new Span("\nFindings in Machine Policy") { Color = sectionColor }, "\n",
                    new Span("##########################") { Color = sectionColor }, "\n"
                    );
            }
            else
            {
                DebugWrite("THERE IS A THIRD WAY.");
            }

            foreach (JObject polFindingCat in polFindings)
            {
                foreach (KeyValuePair<string, JToken> cat in polFindingCat)
                {
                    if (cat.Key == "Drives")
                    {
                        findingsDocument.Children.Add(
                            new Span("GPP Drive Mappings") { Color = findingColor }, "\n",
                            new Span("~~~~~~~~~~~~~~~~~~") { Color = findingColor }
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
                            new Span("OS Privileges") { Color = findingColor }, "\n",
                            new Span("~~~~~~~~~~~~~") { Color = findingColor }
                        );
                        findingsDocument.Children.Add(JsonToGrid(cat.Value, 0));
                        continue;
                    }
                    if (cat.Key == "Group Membership")
                    {
                        findingsDocument.Children.Add(
                            new Span("Group Membership") { Color = findingColor }, "\n",
                            new Span("~~~~~~~~~~~~~~~~") { Color = findingColor }
                        );
                        findingsDocument.Children.Add(JsonToGrid(cat.Value, 0));
                        continue;
                    }
                    if (cat.Key == "Groups")
                    {
                        findingsDocument.Children.Add(
                            new Span("GPP Users and Groups") { Color = findingColor }, "\n",
                            new Span("~~~~~~~~~~~~~~~~~~~~") { Color = findingColor }
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
                            new Span("GPP Data Sources") { Color = findingColor }, "\n",
                            new Span("~~~~~~~~~~~~~~~~") { Color = findingColor }
                        );
                        foreach (JProperty datasourceFinding in cat.Value)
                        {
                            findingsDocument.Children.Add(JsonToGrid(datasourceFinding.Value, 0));
                        }
                        continue;
                    }
                    if (cat.Key == "NetworkShareSettings")
                    {
                        findingsDocument.Children.Add(
                            new Span("GPP Network Shares") { Color = findingColor }, "\n",
                            new Span("~~~~~~~~~~~~~~~~") { Color = findingColor }
                        );
                        foreach (JProperty netshareFinding in cat.Value)
                        {
                            findingsDocument.Children.Add(JsonToGrid(netshareFinding.Value, 0));
                        }
                        continue;
                    }
                    if (cat.Key == "Printers")
                    {
                        findingsDocument.Children.Add(
                            new Span("GPP Printer Mappings") { Color = findingColor }, "\n",
                            new Span("~~~~~~~~~~~~~~~~~~~~") { Color = findingColor }
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
                            new Span("GPP Files") { Color = findingColor }, "\n",
                            new Span("~~~~~~~~~") { Color = findingColor }
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
                            new Span("GPP Scheduled Tasks") { Color = findingColor }, "\n",
                            new Span("~~~~~~~~~~~~~~~~~~~") { Color = findingColor }
                        );
                        foreach (JProperty schedtaskFinding in cat.Value)
                        {
                            findingsDocument.Children.Add(JsonToGrid(schedtaskFinding.Value, 0));
                        }
                        continue;
                    }
                    if (cat.Key == "Service General Setting")
                    {
                        findingsDocument.Children.Add(
                            new Span("Windows Services") { Color = findingColor }, "\n",
                            new Span("~~~~~~~~~~~~~~~~") { Color = findingColor }
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
                            new Span("GPP NT Services") { Color = findingColor }, "\n",
                            new Span("~~~~~~~~~~~~~~~") { Color = findingColor }
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
                            new Span("GPP Shortcuts") { Color = findingColor }, "\n",
                            new Span("~~~~~~~~~~~~~") { Color = findingColor }
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
                            new Span("System Access") { Color = findingColor }, "\n",
                            new Span("~~~~~~~~~~~~~") { Color = findingColor }
                        );
                        Grid grid = new Grid
                        {
                            Color = ConsoleColor.White,
                            Columns =
                            {
                                new Column { Width = GridLength.Auto, MinWidth = 25, MaxWidth = 40},
                                new Column { Width = GridLength.Auto, MinWidth = 5, MaxWidth = 40}
                            },
                            Stroke = LineThickness.Single,
                            StrokeColor = ConsoleColor.Gray
                        };
                        foreach (JProperty jprop in cat.Value)
                        {
                            string name = jprop.Name;
                            JToken value = jprop.Value;
                            grid.Children.Add(new Cell(jprop.Name), new Cell(jprop.Value.ToString()));
                        }

                        findingsDocument.Children.Add(grid);
                        continue;
                    }
                    if (cat.Key == "Kerberos Policy")
                    {
                        findingsDocument.Children.Add(
                            new Span("Kerberos Policy") { Color = findingColor }, "\n",
                            new Span("~~~~~~~~~~~~~~~") { Color = findingColor }
                        );
                        Grid grid = new Grid
                        {
                            Color = ConsoleColor.White,
                            Columns =
                            {
                                new Column { Width = GridLength.Auto, MinWidth = 25, MaxWidth = 40},
                                new Column { Width = GridLength.Auto, MinWidth = 5, MaxWidth = 40}
                            },
                            Stroke = LineThickness.Single,
                            StrokeColor = ConsoleColor.Gray
                        };
                        foreach (JProperty jprop in cat.Value)
                        {
                            string name = jprop.Name;
                            JToken value = jprop.Value;
                            grid.Children.Add(new Cell(jprop.Name), new Cell(jprop.Value.ToString()));
                        }

                        findingsDocument.Children.Add(grid);
                        
                        continue;
                    }
                    if (cat.Key == "Registry Values")
                    {
                        findingsDocument.Children.Add(
                            new Span("Registry Values") { Color = findingColor }, "\n",
                            new Span("~~~~~~~~~~~~~~~") { Color = findingColor }
                        );
                        Grid grid = new Grid
                        {
                            Color = ConsoleColor.White,
                            Columns =
                            {
                                new Column { Width = GridLength.Auto, MinWidth = 25, MaxWidth = 60},
                                new Column { Width = GridLength.Auto, MinWidth = 5, MaxWidth = 40}
                            },
                            Stroke = LineThickness.Single,
                            StrokeColor = ConsoleColor.Gray
                        };
                        grid.Children.Add(new Cell("Key"), new Cell("Value"));
                        foreach (JProperty jprop in cat.Value)
                        {
                            string name = jprop.Name;
                            JToken value = jprop.Value;
                            grid.Children.Add(new Cell(jprop.Name), new Cell(jprop.Value.ToString().Trim('[',']').Trim()));
                        }

                        findingsDocument.Children.Add(grid);
                        continue;
                    }
                    if (cat.Key == "RegistrySettings")
                    {
                        findingsDocument.Children.Add(
                            new Span("GPP Registry Settings") { Color = findingColor }, "\n",
                            new Span("~~~~~~~~~~~~~~~~~~~~~") { Color = findingColor }
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
                            new Span("GPP Registry Keys") { Color = findingColor }, "\n",
                            new Span("~~~~~~~~~~~~~~~~~") { Color = findingColor }
                        );
                        foreach (JProperty regKeyFinding in cat.Value)
                        {
                            findingsDocument.Children.Add(JsonToGrid(regKeyFinding.Value, 0));
                        }
                        continue;
                    }
                    if (cat.Key == "EnvironmentVariables")
                    {
                        findingsDocument.Children.Add(
                            new Span("GPP Env Vars") { Color = findingColor }, "\n",
                            new Span("~~~~~~~~~~~~") { Color = findingColor }
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
                            new Span("GPP Ini Files") { Color = findingColor }, "\n",
                            new Span("~~~~~~~~~~~~~") { Color = findingColor }
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
                            new Span("Scripts") { Color = findingColor }, "\n",
                            new Span("~~~~~~~") { Color = findingColor }
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
                    break;
                case 2:
                    gridColor = ConsoleColor.Gray;
                    break;
                default:
                    gridColor = ConsoleColor.DarkGray;
                    break;
            }

            Grid grid = new Grid
            {
                Color = ConsoleColor.White,
                Columns =
                {
                    new Column { Width = col1Width, MinWidth = 5, MaxWidth = 20},
                    new Column { Width = col2Width, MinWidth = 20}
                },
                Stroke = LineThickness.Single,
                StrokeColor = gridColor
            };

            foreach (JProperty jprop in jprops)
            {
                string name = jprop.Name;
                JToken value = jprop.Value;
                if (value.Count() == 1 || !value.Any())
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

        public static void DebugWrite(string textToWrite)
        {
            if (!JankyDb.DebugMode) return;
            
            Console.BackgroundColor = ConsoleColor.Yellow;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("\n" + textToWrite + "\n");
            Console.ResetColor();
        }

        public static void WriteColor(string textToWrite, ConsoleColor fgColor)
        {
            Console.ForegroundColor = fgColor;
            Console.Write(textToWrite);
            Console.ResetColor();
        }

        public static void WriteColorLine(string textToWrite, ConsoleColor fgColor)
        {
            Console.ForegroundColor = fgColor;
            Console.WriteLine(textToWrite);
            Console.ResetColor();
        }

        public static void PrintBanner()
        {
            string[] barfLines = new string[] {
                @"  .,-:::::/::::::..      ..     ...   ::::::::::::..,::::::::::::..  ,;'``;. ",
                @",;;-'````' ;;;``;;;;  .;;;;;;.  ;;    ;;;`;;;```.;;;;;;'''';;;``;;;; ''  ,[[ ",
                @"[[[   [[[[[[[[,/[[[' ,[[    \[[[['    [[[ `]]nnn]]' [[cccc  [[,/[[['  .c$P'  ",
                @"'$$c.    '$$$$$$$c   $$$,    $$$$     $$$  $$$''    $$''''  $$$$$c   d8MMMUP*",
                @" `Y8bo,,,o8888b '88bo'888,__,8888   .d888  888o     888oo,__88b '88bo        ",
                @"   `'YMUP'YMMMM   'W'  'YMMMMP' 'YmMMMM''  YMMMb    ''''YUMMMMM   'W'        ",
                @"                                                    Now even Grouperer.      ",
                @"                                                    github.com/l0ss/Grouper2 ",
                @"                                                    @mikeloss                "
            };

            ConsoleColor[] patternOne = { ConsoleColor.White, ConsoleColor.Yellow, ConsoleColor.Red, ConsoleColor.Red, ConsoleColor.DarkRed, ConsoleColor.DarkRed, ConsoleColor.White, ConsoleColor.White, ConsoleColor.White, ConsoleColor.White };
            ConsoleColor[] patternTwo =
            {
                ConsoleColor.White, ConsoleColor.Cyan, ConsoleColor.Blue, ConsoleColor.DarkBlue, ConsoleColor.White,
                ConsoleColor.White, ConsoleColor.White, ConsoleColor.White, ConsoleColor.White, ConsoleColor.White
            };
            int i = 0;
            foreach (string barfLine in barfLines)
            {
                string barfOne = barfLine.Substring(0, 69);
                string barfTwo = barfLine.Substring(69, 8);
                WriteColor(barfOne, patternOne[i]);
                WriteColorLine(barfTwo, patternTwo[i]);
                i += 1;
            }
        }
    }
}
