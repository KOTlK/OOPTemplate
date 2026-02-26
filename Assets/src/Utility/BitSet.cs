using System.Text;

public struct BitSet {
    private const int BitsPerSlot = sizeof(ulong) * 8;

    private readonly ulong[] _bits;
    private readonly uint    _bitCount;
    private readonly uint    _slotCount;

    public BitSet(uint bitCount) {
        _bitCount  = bitCount;
        _slotCount = _bitCount / BitsPerSlot;
        if (_slotCount == 0) {
            _slotCount = 1;
        }
        _bits      = new ulong[_slotCount];
    }

    public BitSet Copy() {
        var copy = new BitSet(_bitCount);

        for(var i = 0; i < _slotCount; i++) {
            copy._bits[i] = _bits[i];
        }

        return copy;
    }

    public bool Allocated => _bits != null;

    public override int GetHashCode() {
        ulong total = 0;
        for (uint i = 0; i < _slotCount; i++) {
            total += _bits[i];
        }
        return (int)total;
    }

    public override bool Equals(object o) {
        return this == (BitSet)o;
    }

    public static bool operator ==(BitSet lhs, BitSet rhs) {
        if (lhs._bitCount != rhs._bitCount) return false;

        for (uint i = 0; i < lhs._slotCount; i++) {
            if (lhs._bits[i] != rhs._bits[i]) {
                return false;
            }
        }
        return true;
    }

    public static bool operator !=(BitSet lhs, BitSet rhs) {
        return !(lhs == rhs);
    }

    public void SetBit(uint bit) {
        uint index    = bit / BitsPerSlot;
        uint localBit = bit % BitsPerSlot;
        _bits[index] |= (1UL << (int)localBit);
    }

    public void ClearBit(uint bit) {
        uint index    = bit / BitsPerSlot;
        uint localBit = bit % BitsPerSlot;
        _bits[index] &= ~(1UL << (int)localBit);
    }

    public void ToggleBit(uint bit) {
        uint index    = bit / BitsPerSlot;
        uint localBit = bit % BitsPerSlot;
        _bits[index] ^= (1UL << (int)localBit);
    }

    public bool TestBit(uint bit) {
        uint index    = bit / BitsPerSlot;
        uint localBit = bit % BitsPerSlot;

        return (_bits[index] & (1UL << (int)localBit)) != 0;
    }

    public void ClearAll() {
        for (uint i = 0; i < _slotCount; i++) {
            _bits[i] = 0;
        }
    }

    public void SetAll() {
        for (uint i = 0; i < _slotCount; i++) {
            _bits[i] = ulong.MaxValue;
        }
    }

    public BitSet And(BitSet other) {
        BitSet res = new BitSet(_bitCount);
        for (uint i = 0; i < _slotCount; i++) {
            res._bits[i] = _bits[i] & other._bits[i];
        }
        return res;
    }

    private static readonly StringBuilder _sb = new();
    public override string ToString() {
        _sb.Clear();

        _sb.Append('|');
        for (uint i = 0; i < _bitCount; i++) {
            _sb.Append(TestBit(i) ? '1' : '0');
            _sb.Append('|');
        }

        var str = _sb.ToString();

        _sb.Clear();

        return str;
    }
}