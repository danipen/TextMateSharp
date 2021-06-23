using System;
using System.Text;

namespace TextMateSharp.Internal.Oniguruma
{
    public class OnigString
    {
        public string _string;
        public byte[] utf8_value;

        private int[] charsPosFromBytePos;
        private bool computedOffsets;


        public OnigString(string str)
        {
            this._string = str;
            this.utf8_value = Encoding.UTF8.GetBytes(str);
        }

        public int ConvertUtf16OffsetToUtf8(int posInChars)
        {
            if (!computedOffsets)
            {
                ComputeOffsets();
            }
            if (charsPosFromBytePos == null)
            {
                // Same conditions as code below, but taking into account that the
                // bytes and chars len are the same.
                if (posInChars < 0 || this.utf8_value.Length == 0 || posInChars > this.utf8_value.Length)
                {
                    throw new IndexOutOfRangeException("Position " + posInChars.ToString() + " is out of the bounds of the UTF8 array");
                }
                return posInChars;
            }

            int[] charsLenInBytes = charsPosFromBytePos;
            if (posInChars < 0 || charsLenInBytes.Length == 0)
            {
                throw new IndexOutOfRangeException("Position " + posInChars.ToString() + " is out of the bounds of the UTF8 array");
            }
            if (posInChars == 0)
            {
                return 0;
            }

            int last = charsLenInBytes[charsLenInBytes.Length - 1];
            if (last < posInChars)
            {
                if (last == posInChars - 1)
                {
                    return charsLenInBytes.Length;
                }
                else
                {
                    throw new IndexOutOfRangeException("Position " + posInChars.ToString() + " is out of the bounds of the UTF8 array");
                }
            }

            int index = Array.BinarySearch(charsLenInBytes, posInChars);
            while (index > 0)
            {
                if (charsLenInBytes[index - 1] == posInChars)
                {
                    index--;
                }
                else
                {
                    break;
                }
            }
            return index;
        }

        public int ConvertUtf8OffsetToUtf16(int posInBytes)
        {
            if (!computedOffsets)
            {
                ComputeOffsets();
            }
            if (charsPosFromBytePos == null)
            {
                return posInBytes;
            }
            if (posInBytes < 0)
            {
                return posInBytes;
            }
            if (posInBytes >= charsPosFromBytePos.Length)
            {
                //One off can happen when finding the end of a regexp (it's the right boundary).
                return charsPosFromBytePos[posInBytes - 1] + 1;
            }
            return charsPosFromBytePos[posInBytes];
        }

        private void ComputeOffsets()
        {
            if (this.utf8_value.Length != this._string.Length)
            {
                charsPosFromBytePos = new int[this.utf8_value.Length];
                int bytesLen = 0; ;
                int charsLen = 0;
                int length = this.utf8_value.Length;
                for (int i = 0; i < length;)
                {
                    int codeLen = GetByteCount(this.utf8_value, i, length);
                    for (int i1 = 0; i1 < codeLen; i1++)
                    {
                        charsPosFromBytePos[bytesLen + i1] = charsLen;
                    }
                    bytesLen += codeLen;
                    i += codeLen;
                    charsLen += 1;
                }
                if (bytesLen != this.utf8_value.Length)
                {
                    throw new Exception(bytesLen + " != " + this.utf8_value.Length);
                }
            }
            computedOffsets = true;
        }

        private int GetByteCount(byte[] utf8_value, int ini, int lenght)
        {
            return Encoding.UTF8.GetByteCount(Encoding.UTF8.GetString(utf8_value, ini, lenght));
        }
    }
}
