using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace RenWeb
{
    public class Embedding
    {
        public static MemoryStream RenderImage(ImageFormat Format, RenderableEmbedClass EmbedData)
        {
            Bitmap bmp = new Bitmap(EmbedData.Width, EmbedData.Height);
            Graphics g = Graphics.FromImage(bmp);

            //Drawing background first.
            Image Background = Image.FromFile(Main.RootHTTPFolder + "\\" + EmbedData.BackgroundFile);
            g.DrawImage(Background, 0, 0);
            Background.Dispose();

            //Iterating images
            if (EmbedData.Images != null)
            {
                foreach (RenderableEmbedImageClass Img in EmbedData.Images)
                {
                    Image File = Image.FromFile(Main.RootHTTPFolder + "\\" + Img.Source);
                    g.DrawImage(File, Img.X, Img.Y, Img.Width, Img.Height);
                    File.Dispose();
                }
            }

            if (EmbedData.Texts != null)
            {
                foreach (RenderableEmbedTextClass Text in EmbedData.Texts)
                {
                    FontStyle Style = FontStyle.Regular;
                    if (Text.Bold && Text.Italic)
                    {
                        Style = FontStyle.Bold | FontStyle.Italic;
                    }
                    else if (Text.Bold)
                    {
                        Style = FontStyle.Bold;
                    }
                    else if (Text.Italic)
                    {
                        Style = FontStyle.Italic;
                    }

                    StringFormat SFormat = new StringFormat();
                    switch (Text.LineAlign.ToLower())
                    {
                        case "left":
                            SFormat.LineAlignment = StringAlignment.Near;
                            break;
                        case "center":
                            SFormat.LineAlignment = StringAlignment.Center;
                            break;
                        case "right":
                            SFormat.LineAlignment = StringAlignment.Far;
                            break;
                        default:
                            SFormat = StringFormat.GenericDefault;
                            break;
                    }

                    FontFamily Family = new FontFamily(Text.FontFamily);
                    Font Font = new Font(Family, (float)Text.Size, Style, GraphicsUnit.Pixel);
                    Rectangle Bound = new Rectangle(Text.X, Text.Y, EmbedData.Width - Text.X, Font.Height + 10 /* To fix underscores, etc. */);
                    string Formatted = WebServer.ProcessHTML(Text.Text);
                    if (Formatted.Length > Text.MaxCharacters)
                        Formatted = Formatted.Substring(0, Text.MaxCharacters) + "...";
                    g.DrawString(Formatted, Font, new SolidBrush(HexToColor(Text.Color)), Bound, SFormat);
                    g.DrawRectangle(Pens.Transparent, Bound);
                }
            }
            MemoryStream ms = new MemoryStream();
            bmp.Save(ms, Format);
            return ms;
        }

        public static Color HexToColor(string Hex)
        {
            if(Hex[0] == '#')
                Hex.Remove(0, 1);

            if(Hex.Length == 6)
            {
                int R = Convert.ToInt32(Hex.Substring(0, 2), 16);
                int G = Convert.ToInt32(Hex.Substring(2, 2), 16);
                int B = Convert.ToInt32(Hex.Substring(4, 2), 16);
                return Color.FromArgb(255, R, G, B);
            }
            return Color.White;
        }
    }
}
