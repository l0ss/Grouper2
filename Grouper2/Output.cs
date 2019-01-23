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
            
            StringBuilder sb = new StringBuilder();
            //sb.Append("GPO ah ah ah");

            // munge it to a JObject
            JToken gpo = inputKvp.Value;

            // title it with either the display name or the uid, depending on what we have
            string gpoTitle = "";
            if (gpo["GPOProps"]["Display Name"] != null)
            {
                gpoTitle = gpo["GPOProps"]["Display Name"].ToString();
            }
            else
            {
                gpoTitle = "Unknown GPO";
            }

            JToken gpoProps = gpo["GPOProps"];
            
            //////////////////////////////////////
            
            LineThickness headerThickness = new LineThickness(LineWidth.Double, LineWidth.Single);
            Document outputDocument = new Document(
                // nice Title
                new Span (gpoTitle) { Color = ConsoleColor.Yellow }, "\n",
                new Span { Color = ConsoleColor.White}, "GPO Properties",
                new Grid
                {
                    Color = ConsoleColor.White,
                    Columns = { GridLength.Auto, GridLength.Auto},
                    Children =
                    {
                        new Cell("UID"), new Cell(gpoProps["UID"].ToString()),
                        new Cell("Path"), new Cell(gpoProps["gpoPath"].ToString())
                        //new Cell("Created"), new Cell(gpoProps["Created"].ToString()),
                        //new Cell("Status"), new Cell(Utility.GetSafeString(gpoProps, "GPO Status"))
                    }
                }
                );



            ConsoleRenderer.RenderDocument(outputDocument);
        }

    }
}
