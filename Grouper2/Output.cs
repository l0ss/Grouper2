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
                new Span(gpoTitle) {Color = ConsoleColor.Yellow}, "\n",
                // nice Section header
                new Span {Color = ConsoleColor.White}, "GPO Properties"
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

            if (uPolFindings != null)
            {
                Utility.DebugWrite(uPolFindings.ToString());
            }

            // add our findings docs to our final doc
            outputDocument.Children.Add(
                new Span("Findings"),
                userPolFindingsDoc,
                machinePolFindingsDoc
                );

            ConsoleRenderer.RenderDocument(outputDocument);
        }

    }
}
