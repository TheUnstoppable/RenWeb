using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RenWeb
{
    public class RenderableEmbedClass
    {
        [JsonProperty(Required = Required.Always)]
        public string Name = null;

        [JsonProperty(Required = Required.Always)]
        public int Width = 0;

        [JsonProperty(Required = Required.Always)]
        public int Height = 0;

        [JsonProperty(Required = Required.Always)]
        public string BackgroundFile = null;

        [JsonProperty(Required = Required.AllowNull)]
        public RenderableEmbedTextClass[] Texts = new RenderableEmbedTextClass[0];

        [JsonProperty(Required = Required.AllowNull)]
        public RenderableEmbedImageClass[] Images = new RenderableEmbedImageClass[0];
    }

    public class RenderableEmbedImageClass
    {
        [JsonProperty(Required = Required.Always)]
        public int X = 0;

        [JsonProperty(Required = Required.Always)]
        public int Y = 0;

        [JsonProperty(Required = Required.Always)]
        public int Width = 0;

        [JsonProperty(Required = Required.Always)]
        public int Height = 0;

        [JsonProperty(Required = Required.Always)]
        public string Source = null;
    }

    public class RenderableEmbedTextClass
    {
        [JsonProperty(Required = Required.Always)]
        public string FontFamily = "Arial";

        [JsonProperty(Required = Required.Always)]
        public int X = 0;

        [JsonProperty(Required = Required.Always)]
        public int Y = 0;

        [JsonProperty(Required = Required.Always)]
        public int Size = 0;

        [JsonProperty(Required = Required.Always)]
        public int MaxCharacters = 0;

        [JsonProperty(Required = Required.Always)]
        public string Color = null;

        [JsonProperty(Required = Required.AllowNull)]
        public bool Bold = false;

        [JsonProperty(Required = Required.AllowNull)]
        public bool Italic = false;

        [JsonProperty(Required = Required.Always)]
        public string Text = null;
    }
}
