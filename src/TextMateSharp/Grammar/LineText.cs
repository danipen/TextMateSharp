using System;
using System.Runtime.InteropServices;

namespace TextMateSharp.Grammars
{
    public readonly struct LineText : IEquatable<LineText>
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

        public bool Equals(LineText other)
        {
            // Fast path: check length first
            if (_memory.Length != other._memory.Length)
                return false;

            // Empty memories are equal
            if (_memory.Length == 0)
                return true;

            // Try to check if they reference the same memory region
            if (MemoryMarshal.TryGetArray(_memory, out ArraySegment<char> thisSegment) &&
                MemoryMarshal.TryGetArray(other._memory, out ArraySegment<char> otherSegment))
            {
                // If same array and same offset, they're definitely equal (length already checked)
                if (ReferenceEquals(thisSegment.Array, otherSegment.Array) &&
                    thisSegment.Offset == otherSegment.Offset)
                {
                    return true;
                }
            }

            // Fall back to content comparison
            return _memory.Span.SequenceEqual(other._memory.Span);
        }

        public override bool Equals(object obj)
        {
            return obj is LineText other && Equals(other);
        }

        public override int GetHashCode()
        {
            ReadOnlySpan<char> span = _memory.Span;

            if (span.IsEmpty)
                return 0;

            // DJB2 hash algorithm - fast and good distribution, no allocations
            unchecked
            {
                int hash = 5381;
                for (int i = 0; i < span.Length; i++)
                {
                    hash = ((hash << 5) + hash) ^ span[i];
                }
                return hash;
            }
        }

        public static bool operator ==(LineText left, LineText right) => left.Equals(right);

        public static bool operator !=(LineText left, LineText right) => !left.Equals(right);

        public override string ToString() => _memory.Span.ToString();
    }
}
