using System;
using System.Collections.Generic;

namespace TextMateSharp.Themes
{
    public class ColorMap
    {

        private int lastColorId;
        private Dictionary<string /* color */, int? /* ID color */ > color2id;

        public ColorMap()
        {
            this.lastColorId = 0;
            this.color2id = new Dictionary<string, int?>();
        }

        public int GetId(string color)
        {
            if (color == null)
            {
                return 0;
            }
            color = color.ToUpper();
            int? value;
            this.color2id.TryGetValue(color, out value);
            if (value != null)
            {
                return value.Value;
            }
            value = ++this.lastColorId;
            this.color2id[color] = value;
            return value.Value;
        }

        public string GetColor(int id)
        {
            foreach (string color in color2id.Keys)
            {
                if (color2id[color].Value == id)
                {
                    return color;
                }
            }
            return null;
        }

        public ICollection<string> GetColorMap()
        {
            return this.color2id.Keys;
        }

        public override int GetHashCode()
        {
            return color2id.GetHashCode() + lastColorId.GetHashCode();
        }

        public bool equals(object obj)
        {
            if (this == obj)
            {
                return true;
            }
            if (obj == null)
            {
                return false;
            }
            if (GetType() != obj.GetType())
            {
                return false;
            }
            ColorMap other = (ColorMap)obj;
            return Object.Equals(color2id, other.color2id) && lastColorId == other.lastColorId;
        }
    }
}
