using System;
using System.Collections.Generic;

namespace TextMateSharp.Themes
{
    public class ColorMap
    {

        private int _lastColorId;
        private Dictionary<string /* color */, int? /* ID color */ > _color2id;

        public ColorMap()
        {
            this._lastColorId = 0;
            this._color2id = new Dictionary<string, int?>();
        }

        public int GetId(string color)
        {
            if (color == null)
            {
                return 0;
            }
            color = color.ToUpper();
            int? value;
            this._color2id.TryGetValue(color, out value);
            if (value != null)
            {
                return value.Value;
            }
            value = ++this._lastColorId;
            this._color2id[color] = value;
            return value.Value;
        }

        public string GetColor(int id)
        {
            foreach (string color in _color2id.Keys)
            {
                if (_color2id[color].Value == id)
                {
                    return color;
                }
            }
            return null;
        }

        public ICollection<string> GetColorMap()
        {
            return this._color2id.Keys;
        }

        public override int GetHashCode()
        {
            return _color2id.GetHashCode() + _lastColorId.GetHashCode();
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
            return Object.Equals(_color2id, other._color2id) && _lastColorId == other._lastColorId;
        }
    }
}
