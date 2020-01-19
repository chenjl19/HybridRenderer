using System;

namespace NT
{
    public enum TokenType {
        None = -1,
        Name,
        String,
        Literal,
        Number,
        Punctuation,
    }

    public enum TokenSubType {
        None = 0,
        Integer = 1 << 0,
        Decimal = 1 << 1,
        Hex = 1 << 2,
        Octal = 1 << 3,
        Binary = 1 << 4,
        Long = 1 << 5,
        Float = 1 << 6
    }

    public class Token {
        public TokenType type;
        public TokenSubType subType;
        public int line;
        public int lineCrossed;
        public string lexme;
        public int integerValue;
        public float floatValue;

        public Token() {
            Clear();
        }

        public void GetCopy(ref Token copy) {
            type = copy.type;
            subType = copy.subType;
            line = copy.line;
            lineCrossed = copy.lineCrossed;
            lexme = copy.lexme;
            integerValue = copy.integerValue;
            floatValue = copy.floatValue;
        }

        public void Clear() {
            type = TokenType.None;
            subType = TokenSubType.None;
            line = 0;
            lineCrossed = 0;
            lexme = string.Empty;
            integerValue = 0;
            floatValue = 0f;
        }

        public override int GetHashCode() {
            return base.GetHashCode();
        }

        public override bool Equals(object obj) {
            return base.Equals(obj);
        }

        public static bool operator==(Token a, string b) {
            return a.lexme == b;
        }

        public static bool operator!=(Token a, string b) {
            return a.lexme != b;
        }

        public static bool operator==(string a, Token b) {
            return b.lexme == b;
        }

        public static bool operator!=(string a, Token b) {
            return b.lexme != b;
        }
    }
}