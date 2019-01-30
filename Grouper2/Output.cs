using System;
using System.Collections.Generic;
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

            ////////////////////////////////////////
            ///  Title and Properties
            ////////////////////////////////////////
            LineThickness headerThickness = new LineThickness(LineWidth.Double, LineWidth.Single);
            Document outputDocument = new Document();

            
            outputDocument.Children.Add(
                // nice Title
                new Span(gpoTitle) {Color = ConsoleColor.Green}, "\n",
                new Span("--------------------------------------") { Color = ConsoleColor.Green}, "\n", "\n",
                // nice Section header
                new Span("GPO Properties") {Color = ConsoleColor.Yellow}, "\n",
                new Span("##############") { Color = ConsoleColor.Yellow }
            );
            
            if (GlobalVar.OnlineChecks)
                // we get a different set of properties if we're online so handle that
            {
                outputDocument.Children.Add(
                    new Grid
                    {
                        Color = ConsoleColor.DarkGray,
                        Columns = {GridLength.Auto, GridLength.Auto},
                        Children =
                        {
                            new Cell("UID"), new Cell(gpoProps["UID"].ToString()),
                            new Cell("Path"), new Cell(gpoProps["gpoPath"].ToString()),
                            new Cell("Created"), new Cell(gpoProps["Created"].ToString()),
                            new Cell("Status"), new Cell(Utility.GetSafeString(gpoProps, "GPO Status"))
                        }
                    }
                );
            }
            else
            {
                outputDocument.Children.Add(
                    new Grid
                    {
                        Color = ConsoleColor.White,
                        Columns = { GridLength.Auto, GridLength.Auto },
                        Children =
                        {
                            new Cell("UID"), new Cell(gpoProps["UID"].ToString()),
                            new Cell("Path"), new Cell(gpoProps["gpoPath"].ToString())
                        }
                    }
                );
            }

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
                        foreach (JToken driveFinding in cat.Value)
                        {
                            findingsDocument.Children.Add(GetDrivesDocument(driveFinding));
                        }
                    }
                    if (cat.Key == "Scripts")
                    {
                        /*
                         findingsDocument.Children.Add(
                             new Span("Scripts")
                         );
                        foreach (JToken scriptFinding in cat.Value)
                        {
                            findingsDocument.Children.Add(GetScriptsDocument(scriptFinding));
                        }
                        */
                    }
                    else
                    {
                        findingsDocument.Children.Add(cat.Key + " wasn't properly prettified for output.\n");
                    }
                }
            }
            findingsDocument.Children.Add(new Span("\n\n"));

            return findingsDocument;
        }

        private static Document GetDrivesDocument(JToken drivesJToken)
        {
            Document outDoc = new Document();
            
            foreach (JToken driveJToken in drivesJToken)
            {
                outDoc.Children.Add(new Span(driveJToken["Name"].ToString()));
            }
            return outDoc;
        }

        private static Document GetScriptsDocument(JToken scriptsJToken)
        {
            Document outDoc = new Document();
            
            foreach (JToken scriptJToken in scriptsJToken)
            {
                //outDoc.Children.Add(new Span(scriptJToken["Name"].ToString()));
            }
            return outDoc;
        }
    }
}
