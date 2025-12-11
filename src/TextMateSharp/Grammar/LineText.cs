using System;

namespace TextMateSharp.Grammars
{
    public readonly struct LineText
    {
        private readonly ReadOnlyMemory<char> _memory;

        public LineText(ReadOnlyMemory<char> memory)
        {
            _memory = memory;
        }

        public LineText(string text)
        {
            _memory = text?.AsMemory() ?? ReadOnlyMemory<char>.Empty;
        }

        public ReadOnlyMemory<char> Memory => _memory;

        public int Length => _memory.Length;

        public bool IsEmpty => _memory.IsEmpty;

        public static implicit operator LineText(string text) => new LineText(text);

        public static implicit operator LineText(ReadOnlyMemory<char> memory) => new LineText(memory);

        public static implicit operator ReadOnlyMemory<char>(LineText lineText) => lineText._memory;

        public override string ToString() => _memory.Span.ToString();
    }
}
