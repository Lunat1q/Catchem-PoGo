using System;


namespace PoGoLibrary.Providers
{
    public static class HashBuilder
    {
        /* IOS 1.15.0 */
        static ulong[] magic_table = {
        0x2dd7caaefcf073eb, 0xa9209937349cfe9c,
        0xb84bfc934b0e60ef, 0xff709c157b26e477,
        0x3936fd8735455112, 0xca141bf22338d331,
        0xdd40e749cb64fd02, 0x5e268f564b0deb26,
        0x658239596bdea9ec, 0x31cedf33ac38c624,
        0x12f56816481b0cfd, 0x94e9de155f40f095,
        0x5089c907844c6325, 0xdf887e97d73c50e3,
        0xae8870787ce3c11d, 0xa6767d18c58d2117,
    };

        static UInt128 ROUND_MAGIC = new UInt128(0xe3f0d44988bcdfab, 0x081570afdd535ec3);
        static ulong FINAL_MAGIC0 = 0xce7c4801d683e824;
        static ulong FINAL_MAGIC1 = 0x6823775b1daad522;
        static UInt32 HASH_SEED = 0x46e945f8;

        private static ulong read_int64(byte[] p, int offset) { return BitConverter.ToUInt64(p, offset); }

        public static UInt32 Hash32(byte[] buffer)
        {
            return Hash32Salt(buffer, HASH_SEED);
        }

        public static UInt32 Hash32Salt(byte[] buffer, UInt32 salt)
        {
            var ret = Hash64Salt(buffer, salt);
            return (UInt32)ret ^ (UInt32)(ret >> 32);
        }

        public static UInt64 Hash64(byte[] buffer)
        {
            return Hash64Salt(buffer, HASH_SEED);
        }

        public static UInt64 Hash64Salt(byte[] buffer, UInt32 salt)
        {
            byte[] newBuffer = new byte[buffer.Length + 4];
            byte[] saltBytes = BitConverter.GetBytes(salt);
            Array.Reverse(saltBytes);
            Array.Copy(saltBytes, 0, newBuffer, 0, saltBytes.Length);
            Array.Copy(buffer, 0, newBuffer, saltBytes.Length, buffer.Length);

            return ComputeHash(newBuffer);
        }

        public static UInt64 Hash64Salt64(byte[] buffer, UInt64 salt)
        {
            byte[] newBuffer = new byte[buffer.Length + 8];
            byte[] saltBytes = BitConverter.GetBytes(salt);
            Array.Reverse(saltBytes);
            Array.Copy(saltBytes, 0, newBuffer, 0, saltBytes.Length);
            Array.Copy(buffer, 0, newBuffer, saltBytes.Length, buffer.Length);

            return ComputeHash(newBuffer);
        }

        public static ulong ComputeHash(byte[] input)
        {
            uint len = (uint)input.Length;
            uint num_chunks = len / 128;

            // copy tail, pad with zeroes
            byte[] tail = new byte[128];
            uint tail_size = len % 128;
            Array.Copy(input, len - tail_size, tail, 0, tail_size);

            UInt128 hash;

            if (num_chunks != 0) hash = HashChunk(input, 128, 0);
            else hash = HashChunk(tail, tail_size, 0);

            hash += ROUND_MAGIC;

            int offset = 0;

            if (num_chunks != 0)
            {
                while (--num_chunks > 0)
                {
                    offset += 128;
                    hash = HashMulAdd(hash, ROUND_MAGIC, HashChunk(input, 128, offset));
                }

                if (tail_size > 0)
                {
                    hash = HashMulAdd(hash, ROUND_MAGIC, HashChunk(tail, tail_size, 0));
                }
            }

            hash += new UInt128(tail_size * 8, 0);

            if (hash > new UInt128(0x7fffffffffffffff, 0xffffffffffffffff)) hash++;

            hash = hash << 1 >> 1;

            ulong X = hash.hi + (hash.lo >> 32);
            X = ((X + (X >> 32) + 1) >> 32) + hash.hi;
            ulong Y = (X << 32) + hash.lo;

            ulong A = X + FINAL_MAGIC0;
            if (A < X) A += 0x101;

            ulong B = Y + FINAL_MAGIC1;
            if (B < Y) B += 0x101;

            UInt128 H = new UInt128(A) * B;
            UInt128 mul = new UInt128(0x101);
            H = (mul * H.hi) + H.lo;
            H = (mul * H.hi) + H.lo;

            if (H.hi > 0) H += mul;
            if (H.lo > 0xFFFFFFFFFFFFFEFE) H += mul;
            return H.lo;
        }

        private static UInt128 HashChunk(byte[] chunk, long size, int off)
        {
            UInt128 hash = new UInt128(0);
            for (int i = 0; i < 8; i++)
            {
                int offset = i * 16;
                if (offset >= size) break;
                ulong a = read_int64(chunk, off + offset);
                ulong b = read_int64(chunk, off + offset + 8);
                hash += (new UInt128(a + magic_table[i * 2])) * (new UInt128(b + magic_table[i * 2 + 1]));
            }
            return hash << 2 >> 2;
        }

        private static UInt128 HashMulAdd(UInt128 hash, UInt128 mul, UInt128 add)
        {
            ulong a0 = add.lo & 0xffffffff,
                a1 = add.lo >> 32,
                a23 = add.hi;

            ulong m0 = mul.lo & 0xffffffff,
                m1 = mul.lo >> 32,
                m2 = mul.hi & 0xffffffff,
                m3 = mul.hi >> 32;

            ulong h0 = hash.lo & 0xffffffff,
                h1 = hash.lo >> 32,
                h2 = hash.hi & 0xffffffff,
                h3 = hash.hi >> 32;

            ulong c0 = (h0 * m0),
                c1 = (h0 * m1) + (h1 * m0),
                c2 = (h0 * m2) + (h1 * m1) + (h2 * m0),
                c3 = (h0 * m3) + (h1 * m2) + (h2 * m1) + (h3 * m0),
                c4 = (h1 * m3) + (h2 * m2) + (h3 * m1),
                c5 = (h2 * m3) + (h3 * m2),
                c6 = (h3 * m3);

            ulong r2 = c2 + (c6 << 1) + a23,
                r3 = c3 + (r2 >> 32),
                r0 = c0 + (c4 << 1) + a0 + (r3 >> 31),
                r1 = c1 + (c5 << 1) + a1 + (r0 >> 32);

            ulong res0 = ((r3 << 33 >> 1) | (r2 & 0xffffffff)) + (r1 >> 32);
            return new UInt128(res0, (r1 << 32) | (r0 & 0xffffffff));
        }
    }
    
    struct UInt128
    {
        public ulong hi, lo;

        #region constructors

        public UInt128(ulong high, ulong low)
        {
            hi = high; lo = low;
        }

        public UInt128(ulong low)
        {
            hi = 0; lo = low;
        }

        #endregion
        #region comparators

        public bool Equals(UInt128 other)
        {
            return (hi == other.hi && lo == other.lo);
        }

        public static bool operator >(UInt128 a, UInt128 b)
        {
            if (a.hi == b.hi) return a.lo > b.lo;
            return a.hi > b.hi;
        }

        public static bool operator <(UInt128 a, UInt128 b)
        {
            if (a.hi == b.hi) return a.lo < b.lo;
            return a.hi < b.hi;
        }

        #endregion
        #region arithmetic

        public static UInt128 operator ++(UInt128 a)
        {
            a.lo++;
            if (a.lo == 0) a.hi++;
            return a;
        }

        public static UInt128 operator +(UInt128 a, UInt128 b)
        {
            ulong C = (((a.lo & b.lo) & 1) + (a.lo >> 1) + (b.lo >> 1)) >> 63;
            return new UInt128(a.hi + b.hi + C, a.lo + b.lo);
        }

        public static UInt128 operator +(UInt128 a, ulong b)
        {
            return a + new UInt128(b);
        }

        public static UInt128 operator -(UInt128 a, UInt128 b)
        {
            ulong L = a.lo - b.lo;
            ulong C = (((L & b.lo) & 1) + (b.lo >> 1) + (L >> 1)) >> 63;
            return new UInt128(a.hi - (b.hi + C), L);
        }

        #endregion
        #region bitwise operations

        public static UInt128 operator &(UInt128 a, UInt128 b)
        {
            return new UInt128(a.hi & b.hi, a.lo & b.lo);
        }

        public static UInt128 operator <<(UInt128 a, int b)
        {
            a.hi <<= b;
            a.hi |= (a.lo >> (64 - b));
            a.lo <<= b;
            return a;
        }

        public static UInt128 operator >>(UInt128 a, int b)
        {
            a.lo >>= b;
            a.lo |= (a.hi << (64 - b));
            a.hi >>= b;
            return a;
        }

        #endregion
        #region multiplication

        private static UInt128 m64(ulong a, ulong b)
        {
            ulong a1 = (a & 0xffffffff), b1 = (b & 0xffffffff),
                t = (a1 * b1), w3 = (t & 0xffffffff), k = (t >> 32), w1;

            a >>= 32;
            t = (a * b1) + k;
            k = (t & 0xffffffff);
            w1 = (t >> 32);

            b >>= 32;
            t = (a1 * b) + k;
            k = (t >> 32);

            return new UInt128((a * b) + w1 + k, (t << 32) + w3);
        }

        public static UInt128 operator *(UInt128 a, int b) { return a * (ulong)b; }

        public static UInt128 operator *(UInt128 a, ulong b)
        {
            UInt128 ans = m64(a.lo, b);
            ans.hi += (a.hi * b);
            return ans;
        }

        public static UInt128 operator *(UInt128 a, UInt128 b)
        {
            UInt128 ans = m64(a.lo, b.lo);
            ans.hi += (a.hi * b.lo) + (a.lo * b.hi);
            return ans;
        }

        #endregion
    }
}
