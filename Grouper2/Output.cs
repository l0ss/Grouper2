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

            ////////////////////////////////////////
            ///  Title and Properties
            ////////////////////////////////////////
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

            Grid gpoPropsGrid = JPropsToGrid(gpoProps);
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

            if (polType == "machine")
            {
                findingsDocument.Children.Add(
                    new Span("\nFindings in Machine Policy") { Color = ConsoleColor.Yellow }, "\n",
                    new Span("##########################") { Color = ConsoleColor.Yellow }, "\n"
                    );
            }

            foreach (JObject polFindingCat in polFindings)
            {
                foreach (KeyValuePair<string, JToken> cat in polFindingCat)
                {
                    if (cat.Key == "Drives")
                    {
                        findingsDocument.Children.Add(
                            new Span("GPP Drive Mappings") { Color = ConsoleColor.Magenta }, "\n",
                            new Span("~~~~~~~~~~~~~~~~~~") { Color = ConsoleColor.Magenta }, "\n"
                        );
                        foreach (JToken driveFindings in cat.Value)
                        {
                            foreach (JToken driveFinding in driveFindings)
                            {
                                findingsDocument.Children.Add(JPropsToGrid(driveFinding));
                            }
                        }
                        continue;
                    }
                    if (cat.Key == "Privilege Rights")
                    {
                        findingsDocument.Children.Add(
                            new Span("OS Privileges") { Color = ConsoleColor.Magenta }, "\n",
                            new Span("~~~~~~~~~~~~~") { Color = ConsoleColor.Magenta }, "\n"
                        );
                        foreach (JToken privFinding in cat.Value)
                        {
                            //findingsDocument.Children.Add(JPropsToGrid(privFinding));
                        }
                        continue;
                    }
                    if (cat.Key == "Group Membership")
                    {
                        findingsDocument.Children.Add(
                            new Span("Group Membership") { Color = ConsoleColor.Magenta }, "\n",
                            new Span("~~~~~~~~~~~~~~~~") { Color = ConsoleColor.Magenta }, "\n"
                        );
                        foreach (JToken groupFinding in cat.Value)
                        {
                            //findingsDocument.Children.Add(JPropsToGrid(groupFinding));
                        }
                        continue;
                    }
                    if (cat.Key == "Groups")
                    {
                        findingsDocument.Children.Add(
                            new Span("GPP Users and Groups") { Color = ConsoleColor.Magenta }, "\n",
                            new Span("~~~~~~~~~~~~~~~~~~~~") { Color = ConsoleColor.Magenta }, "\n"
                        );
                        foreach (JToken ungFinding in cat.Value)
                        {
                            //findingsDocument.Children.Add(JPropsToGrid(ungFinding));
                        }
                        continue;
                    }
                    if (cat.Key == "DataSources")
                    {
                        findingsDocument.Children.Add(
                            new Span("GPP Data Sources") { Color = ConsoleColor.Magenta }, "\n",
                            new Span("~~~~~~~~~~~~~~~~") { Color = ConsoleColor.Magenta }, "\n"
                        );
                        foreach (JToken datasourceFinding in cat.Value)
                        {
                            //findingsDocument.Children.Add(JPropsToGrid(datasourceFinding));
                        }
                        continue;
                    }
                    if (cat.Key == "Printers")
                    {
                        findingsDocument.Children.Add(
                            new Span("GPP Printer Mappings") { Color = ConsoleColor.Magenta }, "\n",
                            new Span("~~~~~~~~~~~~~~~~~~~~") { Color = ConsoleColor.Magenta }, "\n"
                        );
                        foreach (JToken printerFinding in cat.Value)
                        {
                            //findingsDocument.Children.Add(JPropsToGrid(printerFinding));
                        }
                        continue;
                    }
                    if (cat.Key == "Files")
                    {
                        findingsDocument.Children.Add(
                            new Span("GPP Files") { Color = ConsoleColor.Magenta }, "\n",
                            new Span("~~~~~~~~~") { Color = ConsoleColor.Magenta }, "\n"
                        );
                        foreach (JToken fileFinding in cat.Value)
                        {
                            //findingsDocument.Children.Add(JPropsToGrid(fileFinding));
                        }
                        continue;
                    }
                    if (cat.Key == "ScheduledTasks")
                    {
                        findingsDocument.Children.Add(
                            new Span("GPP Scheduled Tasks") { Color = ConsoleColor.Magenta }, "\n",
                            new Span("~~~~~~~~~~~~~~~~~~~") { Color = ConsoleColor.Magenta }, "\n"
                        );
                        foreach (JToken schedtaskFinding in cat.Value)
                        {
                            //findingsDocument.Children.Add(JPropsToGrid(schedtaskFinding));
                        }
                        continue;
                    }
                    if (cat.Key == "Assigned Applications")
                    {
                        findingsDocument.Children.Add(
                            new Span("Assigned Applications") { Color = ConsoleColor.Magenta }, "\n",
                            new Span("~~~~~~~~~~~~~~~~~~~~~") { Color = ConsoleColor.Magenta }, "\n"
                        );
                        foreach (JToken aasFinding in cat.Value)
                        {
                            //findingsDocument.Children.Add(JPropsToGrid(aasFinding));
                        }
                        continue;
                    }
                    if (cat.Key == "Service General Setting")
                    {
                        findingsDocument.Children.Add(
                            new Span("Windows Services") { Color = ConsoleColor.Magenta }, "\n",
                            new Span("~~~~~~~~~~~~~~~~") { Color = ConsoleColor.Magenta }, "\n"
                        );
                        foreach (JToken sgsFinding in cat.Value)
                        {
                            //findingsDocument.Children.Add(JPropsToGrid(sgsFinding));
                        }
                        continue;
                    }
                    if (cat.Key == "NTServices")
                    {
                        findingsDocument.Children.Add(
                            new Span("GPP NT Services") { Color = ConsoleColor.Magenta }, "\n",
                            new Span("~~~~~~~~~~~~~~~") { Color = ConsoleColor.Magenta }, "\n"
                        );
                        foreach (JToken ntserviceFinding in cat.Value)
                        {
                            //findingsDocument.Children.Add(JPropsToGrid(ntserviceFinding));
                        }
                        continue;
                    }
                    if (cat.Key == "Shortcuts")
                    {
                        findingsDocument.Children.Add(
                            new Span("GPP Shortcuts") { Color = ConsoleColor.Magenta }, "\n",
                            new Span("~~~~~~~~~~~~~") { Color = ConsoleColor.Magenta }, "\n"
                        );
                        foreach (JToken shortcutFinding in cat.Value)
                        {
                            //findingsDocument.Children.Add(JPropsToGrid(shortcutFinding));
                        }
                        continue;
                    }
                    if (cat.Key == "System Access")
                    {
                        findingsDocument.Children.Add(
                            new Span("System Access") { Color = ConsoleColor.Magenta }, "\n",
                            new Span("~~~~~~~~~~~~~") { Color = ConsoleColor.Magenta }, "\n"
                        );
                        foreach (JToken sysaccFinding in cat.Value)
                        {
                            //findingsDocument.Children.Add(JPropsToGrid(sysaccFinding));
                        }
                        continue;
                    }
                    if (cat.Key == "Kerberos Policy")
                    {
                        findingsDocument.Children.Add(
                            new Span("Kerberos Policy") { Color = ConsoleColor.Magenta }, "\n",
                            new Span("~~~~~~~~~~~~~~~") { Color = ConsoleColor.Magenta }, "\n"
                        );
                        foreach (JToken krbpolFinding in cat.Value)
                        {
                            //findingsDocument.Children.Add(JPropsToGrid(krbpolFinding));
                        }
                        continue;
                    }
                    if (cat.Key == "Registry Values")
                    {
                        // TODO patched out until registry values thing is done
                        /*
                        findingsDocument.Children.Add(
                            new Span("Registry Values") { Color = ConsoleColor.Magenta }, "\n",
                            new Span("~~~~~~~~~~~~~~~") { Color = ConsoleColor.Magenta }, "\n"
                        );
                        foreach (JToken regvalFinding in cat.Value)
                        {
                            findingsDocument.Children.Add(JPropsToGrid(regvalFinding));
                        }
                        */
                        continue;
                    }
                    if (cat.Key == "RegistrySettings")
                    {
                        findingsDocument.Children.Add(
                            new Span("GPP Registry Settings") { Color = ConsoleColor.Magenta }, "\n",
                            new Span("~~~~~~~~~~~~~~~~~~~~~") { Color = ConsoleColor.Magenta }, "\n"
                        );
                        foreach (JToken regsetFinding in cat.Value)
                        {
                            //findingsDocument.Children.Add(JPropsToGrid(regsetFinding));
                        }
                        continue;
                    }
                    if (cat.Key == "Registry Keys")
                    {
                        findingsDocument.Children.Add(
                            new Span("GPP Registry Keys") { Color = ConsoleColor.Magenta }, "\n",
                            new Span("~~~~~~~~~~~~~~~~~") { Color = ConsoleColor.Magenta }, "\n"
                        );
                        foreach (JToken regkeyFinding in cat.Value)
                        {
                            //findingsDocument.Children.Add(JPropsToGrid(regkeyFinding));
                        }
                        continue; ;
                    }
                    if (cat.Key == "EnvironmentVariables")
                    {
                        findingsDocument.Children.Add(
                            new Span("GPP Env Vars") { Color = ConsoleColor.Magenta }, "\n",
                            new Span("~~~~~~~~~~~~") { Color = ConsoleColor.Magenta }, "\n"
                        );
                        foreach (JToken envvarFinding in cat.Value)
                        {
                            //findingsDocument.Children.Add(JPropsToGrid(envvarFinding));
                        }
                        continue;
                    }
                    if (cat.Key == "IniFiles")
                    {
                        findingsDocument.Children.Add(
                            new Span("GPP Ini Files") { Color = ConsoleColor.Magenta }, "\n",
                            new Span("~~~~~~~~~~~~~") { Color = ConsoleColor.Magenta }, "\n"
                        );
                        foreach (JToken iniFileFinding in cat.Value)
                        {
                            //findingsDocument.Children.Add(JPropsToGrid(iniFileFinding));
                        }
                        continue;
                    }
                    if (cat.Key == "Scripts")
                    {
                        findingsDocument.Children.Add(
                            new Span("Scripts") { Color = ConsoleColor.Magenta }, "\n",
                            new Span("~~~~~~~") { Color = ConsoleColor.Magenta }, "\n"
                        );
                        foreach (JToken scriptFinding in cat.Value)
                        {
                            //findingsDocument.Children.Add(JPropsToGrid(scriptFinding));
                        }
                        continue;
                    }
                    else
                    {
                        findingsDocument.Children.Add(new Span(cat.Key + " wasn't properly prettified for output.\n") {Color= ConsoleColor.Red});
                    }
                }
            }
            findingsDocument.Children.Add(new Span("\n\n"));

            return findingsDocument;
        }

        private static Grid JPropsToGrid(JToken jprops)
        {
            Grid grid = new Grid
            {
                Color = ConsoleColor.Gray,
                Columns = {GridLength.Auto, GridLength.Auto},
                Children = { }
            };

            foreach (JProperty jprop in jprops)
            {
                JToken value = jprop.Value;
                if ((value.Count() == 1) || (value.Count() == 0))
                {
                    grid.Children.Add(new Cell(jprop.Name), new Cell(jprop.Value.ToString()));
                }
                else if (value.Count() > 1)
                {
                    Grid subGrid = new Grid
                    {
                        Color = ConsoleColor.Gray,
                        Columns = {GridLength.Auto, GridLength.Auto},
                        Children = { },

                    };

                    foreach (JProperty child in value)
                    {
                        subGrid.Children.Add(new Cell(child.Name), new Cell(child.Value.ToString()));
                    }
                    grid.Children.Add(new Cell(jprop.Name), new Cell(subGrid));
                }
            } 
            
            return grid;
        }
    }
}
