using System;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace Catchem.Extensions
{
    public static class RichTextBoxExtensions
    {
        private static readonly Regex UrlRegex = new Regex(@"(?#Protocol)(?:(?:ht|f)tp(?:s?)\:\/\/|~/|/)?(?#Username:Password)(?:\w+:\w+@)?(?#Subdomains)(?:(?:[-\w]+\.)+(?#TopLevel Domains)(?:com|org|net|gov|mil|biz|info|mobi|name|aero|jobs|museum|travel|[a-z]{2}))(?#Port)(?::[\d]{1,5})?(?#Directories)(?:(?:(?:/(?:[-\w~!$+|.,=]|%[a-f\d]{2})+)+|/)+|\?|#)?(?#Query)(?:(?:\?(?:[-\w~!$+|.,*:]|%[a-f\d{2}])+=(?:[-\w~!$+|.,*:=]|%[a-f\d]{2})*)(?:&amp;(?:[-\w~!$+|.,*:]|%[a-f\d{2}])+=(?:[-\w~!$+|.,*:=]|%[a-f\d]{2})*)*)*(?#Anchor)(?:#(?:[-\w~!$+|.,*:=]|%[a-f\d]{2})*)?");

 
        public static void AppendText(this RichTextBox box, string text, Color color)
        {
            var tr = new TextRange(box.Document.ContentEnd, box.Document.ContentEnd) { Text = text };
            try
            {
                tr.ApplyPropertyValue(TextElement.ForegroundProperty, new SolidColorBrush(color));
            }
            catch (FormatException) { }
        }

        public static void AppendParagraph(this RichTextBox box, string text, Color color)
        {
            var paragraph = new Paragraph();
            paragraph.Inlines.Add(text);
            paragraph.Foreground = new SolidColorBrush(color);

            paragraph.DetectUrLs();

            box.Document.Blocks.Add(paragraph);

            box.ScrollToEnd();
        }

        public static bool IsHyperlink(string word)
        {
            // First check to make sure the word has at least one of the characters we need to make a hyperlink
            if (UrlRegex.IsMatch(word))
            {
                if (Uri.IsWellFormedUriString(word, UriKind.Absolute))
                {
                    // The string is an Absolute URI
                    return true;
                }
                else if (UrlRegex.IsMatch(word))
                {
                    Uri uri = new Uri(word, UriKind.RelativeOrAbsolute);

                    if (!uri.IsAbsoluteUri)
                    {
                        // rebuild it it with http to turn it into an Absolute URI
                        uri = new Uri(@"http://" + word, UriKind.Absolute);
                    }

                    if (uri.IsAbsoluteUri)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public static void DetectUrLs(this Paragraph par)
        {
            try
            {


                string paragraphText = new TextRange(par.ContentStart, par.ContentEnd).Text;

                int count = 0;
                foreach (Match match in UrlRegex.Matches(paragraphText))
                {
                    var position = par.ContentStart;
                    var p1 = position.GetPositionAtOffset(match.Index + 1 + count*6);
                    var p2 = p1?.GetPositionAtOffset(match.Length);
                    if (p1 == null || p2 == null)
                    {
                        //Donothing
                    }
                    else
                    {
                        var link = new Hyperlink(p1, p2) {NavigateUri = new Uri(match.Value)};
                        link.Click += Hyperlink_Click;
                        count++;
                    }
                }
            }
            catch (Exception)
            {
                //ignore
            }
        }

        public static void Hyperlink_Click(object sender, EventArgs e)
        {
            try
            {
                var hyperlink = sender as Hyperlink;
                if (hyperlink?.NavigateUri != null) Process.Start(hyperlink.NavigateUri.AbsoluteUri);
            }
            catch (Exception)
            {
                //ignore
            }
        }
    }
}
