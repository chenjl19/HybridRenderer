using System;
using System.IO;
using System.Text;

namespace NT
{
    public class Lexer {
        [Flags]
        public enum Flags {
            None = 0,
            NoStringConcat = 1 << 0
        }

        public enum PunctuationType {
            BackSlash,
            Comma,

            MulAssign,
            DivAssign,
            AddAssign,
            SubAssign,
            Inc,
            Dec,

            Mul,
            Div,
            Add,
            Sub,
            Assign,

            ParenthesesOpen,
            ParenthesesClose,
            BraceOpen,
            BraceClose,
            SqbracketOpen,
            SqbracketClose,
        }

        public struct  Punctuation {
            public string p;				
            public PunctuationType n;
            public Punctuation(string name, PunctuationType type) {
                p = name;
                n = type;
            }
        };

        static readonly Punctuation[] defaultPunctuations = new Punctuation[] {
            new Punctuation("*=", PunctuationType.MulAssign),
            new Punctuation("/=", PunctuationType.DivAssign),
            new Punctuation("+", PunctuationType.Add),
            new Punctuation("-", PunctuationType.Sub),
            new Punctuation("*", PunctuationType.Mul),
            new Punctuation("/", PunctuationType.Div),
            new Punctuation("(", PunctuationType.ParenthesesOpen),
            new Punctuation(")", PunctuationType.ParenthesesClose),
            new Punctuation("{", PunctuationType.BraceOpen),
            new Punctuation("}", PunctuationType.BraceClose),
            new Punctuation("[", PunctuationType.SqbracketOpen),
            new Punctuation("]", PunctuationType.SqbracketClose),
            new Punctuation(",", PunctuationType.Comma),
            new Punctuation("\\", PunctuationType.BackSlash)
        };

        readonly char[] text;
        int parseIndex;
        int lastParseIndex;
        int line;
        int lastLine;
        bool tokenAvailable;
        Token lastToken;
        Punctuation[] punctuations;
        Flags flags;

        public Lexer(string inText, Flags flags = Flags.None) {
            text = inText.ToCharArray();
            punctuations = defaultPunctuations;
            lastToken = new Token();
            this.flags = flags;
        }

        public Lexer(char[] inText, Flags flags = Flags.None) {
            text = inText;
            punctuations = defaultPunctuations;
            lastToken = new Token();
            this.flags = flags;
        }

        public int GetFileOffset() {
            return parseIndex;
        }

        public int GetLineNum() {
            return line;
        }

        public char[] GetText(int start, int num) {
            if(start >= 0 && (start + num) <= text.Length) {
                char[] copy = new char[num];
                Array.Copy(text, start, copy, 0, num);
                return copy;
            }
            return null;
        }

        bool SkipWhitespace() {
_SkipWhiteSpace:
            while(parseIndex < text.Length && text[parseIndex] <= ' ') {
                if(text[parseIndex] == '\n') {
                    line++;
                }
                parseIndex++;
            }

            if(parseIndex < text.Length && text[parseIndex] == '/' && (parseIndex + 1) < text.Length) {
                // comments //
                if(text[parseIndex + 1] == '/') {
                    parseIndex += 2;
                    while(parseIndex < text.Length && text[parseIndex] != '\n') {
                        parseIndex++;
                    }
                    if(parseIndex < text.Length) {
                        parseIndex++;
                        line++;
                    }
                    goto _SkipWhiteSpace;
                } else if(text[parseIndex + 1] == '*') {
                    // comments /* */
                    parseIndex += 2;
                    while(parseIndex < text.Length) {
                        if(text[parseIndex] == '\n') {
                            parseIndex++;
                            line++;
                        } else if(text[parseIndex] == '/') {
                            if(text[parseIndex - 1] == '*') {
                                parseIndex++;
                                break;
                            }
                            if(text[parseIndex + 1] == '*') {
                                Console.WriteLine("nested comment.");
                                return false;
                            }
                        }
                        parseIndex++;
                    }
                    goto _SkipWhiteSpace;
                }
            } 
            
            return parseIndex < text.Length;
        }

        bool ReadString(ref Token token, char quote) {
            if(quote == '\"') {
                token.type = TokenType.String;
            } else {
                token.type = TokenType.Literal;
            }

            parseIndex++;

            StringBuilder builder = new StringBuilder();

            while(true) {
                if(text[parseIndex] == quote) {
                    parseIndex++;
                    if(flags.HasFlag(Flags.NoStringConcat)) {
                        break;
                    }

                    int tmpIndex = parseIndex;
                    int tmpLine = line;
                    if(!SkipWhitespace()) {
                        parseIndex = tmpIndex;
                        line = tmpLine;
                    }
                    if(parseIndex >= text.Length) {
                        break;
                    }
                    if(text[parseIndex] != quote) {
                        parseIndex = tmpIndex;
                        line = tmpLine;
                        break;
                    }
                    parseIndex++;
                } else {
                    if(parseIndex == text.Length) {
                        throw new InvalidDataException("missing trailing quote.");
                    }
                    if(text[parseIndex] == '\n') {
                        throw new InvalidDataException("newline inside string.");
                    }
                    builder.Append(text[parseIndex++]);
                }
            }
            token.line = line;
            token.lexme = builder.ToString();
            return true;
        }

        bool ReadName(ref Token token) {
            StringBuilder builder = new StringBuilder();
            token.type = TokenType.Name;

            do {
                builder.Append(text[parseIndex++]);
            } while(parseIndex < text.Length && (char.IsLetter(text[parseIndex]) || char.IsDigit(text[parseIndex]) || text[parseIndex] == '_'));

            token.line = line;
            token.lexme = builder.ToString();
            return true;
        }

        bool ReadNumber(ref Token token) {
            StringBuilder builder = new StringBuilder();
            token.type = TokenType.Number;

            char c = text[parseIndex];
            char c2 = text[parseIndex + 1];

            if(c == '0' && c2 != '.') {
                // check hex
                if(c2 == 'x' || c2 == 'X') {
                    builder.Append(text[parseIndex++]);
                    builder.Append(text[parseIndex++]);
                    c = text[parseIndex];
                    while((c >= '0' && c <= '9') || (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'F')) {
                        builder.Append(c);
                        c = text[++parseIndex];
                        if(parseIndex == text.Length) {
                            break;
                        }
                    }
                    token.subType = TokenSubType.Hex | TokenSubType.Integer;
                    token.lexme = builder.ToString();
                } else {
                    builder.Append(c);
                    c = text[++parseIndex];
                    while(parseIndex < text.Length && (c >= '0' && c <= '7')) {
                        builder.Append(c);
                        c = text[++parseIndex];
                    }
                    string s = builder.ToString();
                    if(int.TryParse(s, out token.integerValue)) {
                        token.subType = TokenSubType.Octal | TokenSubType.Integer;
                        token.lexme = s;
                    }
                }
            } else {
                int dot = 0;
                while(true) {
                    if(c == '.') {
                        dot++;
                    } else if(!(c >= '0' && c <= '9')) {
                        break;
                    }
                    builder.Append(c);
                    c = text[++parseIndex];
                    if(parseIndex == text.Length) {
                        break;
                    }
                }
                if(c == 'e' && dot == 0) {
                    dot++;
                }
                if(dot == 1) {
                    string s = builder.ToString();
                    if(!float.TryParse(s, out token.floatValue)) {
                        throw new InvalidDataException("invalid float value.");
                    }
                    token.lexme = s;
                    token.subType = TokenSubType.Decimal | TokenSubType.Float;
                } else if(dot > 1) {
                    throw new InvalidDataException("invalid float value.");
                } else {
                    string s = builder.ToString();
                    if(!int.TryParse(s, out token.integerValue)) {
                        throw new InvalidDataException("invalid integer value.");
                    }
                    token.lexme = s;
                    token.floatValue = token.integerValue;
                    token.subType = TokenSubType.Decimal | TokenSubType.Integer;
                }
            }
            
            token.line = line;
            return true;
        }

        bool ReadPunctuation(ref Token token) {
            int l = 0;
            string p = null;
            for(int i = 0; i < punctuations.Length; i++) {
                p = punctuations[i].p;
                for(; l < p.Length && parseIndex != text.Length; l++, parseIndex++) {
                    if(p[l] != text[parseIndex]) {
                        break;
                    }
                }
                if(l == p.Length) {
                    token.line = line;
                    token.lexme = p;
                    token.type = TokenType.Punctuation;
                    return true;
                }
            }
            return false;
        }

        public bool ReadToken(ref Token token) {
            if(text == null || parseIndex >= text.Length) {
                return false;
            }

            if(tokenAvailable) {
                tokenAvailable = false;
                token.GetCopy(ref lastToken);
                return true;
            }

            lastParseIndex = parseIndex;
            lastLine = line;

            token.Clear();

            if(!SkipWhitespace()) {
                return false;
            }

            token.line = line;
            token.lineCrossed = line - lastLine;

            StringBuilder builder = new StringBuilder();
            char c = text[parseIndex];
            if(c == '\"' || c == '\'') {
                if(!ReadString(ref token, c)) {
                    return false;
                }
            } else if(char.IsDigit(c) || c == '.') {
                if(!ReadNumber(ref token)) {
                    return false;
                }
            } else if(c == '_' || char.IsLetter(c)) {
                if(!ReadName(ref token)) {
                    return false;
                }
            } else {
                if(!ReadPunctuation(ref token)) {
                    return false;
                }
            }
            
            return true;
        }

        public bool ExpectTokenType(TokenType type, TokenSubType subType, ref Token token) {
            if(!ReadToken(ref token)) {
                Console.WriteLine("Couldn't read expected token.");
                return false;
            }

            string str = string.Empty;

            if(token.type != type) {
                str = token.type.ToString();
                Console.WriteLine($"Expected a {type.ToString()} but found {str}");
                return false;
            }

            if(token.type == TokenType.Number) {
                if(!token.subType.HasFlag(subType)) {
                    str = token.subType.ToString();
                    Console.WriteLine($"Expected a {type.ToString()} but found {str}");
                    return false;
                }
            } 
            
            return true;
        }        

        public bool ExpectTokenTypeOnLine(TokenType type, TokenSubType subType, ref Token token) {
            if(!ReadTokenOnLine(ref token)) {
                Console.WriteLine("Couldn't read expected token.");
                return false;
            }

            string str = string.Empty;

            if(token.type != type) {
                str = token.type.ToString();
                Console.WriteLine($"Expected a {type.ToString()} but found {str}");
                return false;
            }

            if(token.type == TokenType.Number) {
                if(!((token.subType & subType) != 0)) {
                    str = token.subType.ToString();
                    Console.WriteLine($"Expected a {type.ToString()} but found {str}");
                    return false;
                }
            } 
            
            return true;
        }

        public void UnreadToken(ref Token token) {
            if(!tokenAvailable) {
                this.lastToken.GetCopy(ref token);
                this.tokenAvailable = true;
            }
        }

        public bool SkipBracedSection(bool parseFirstBrace = true) {
            Token token = new Token();
            int depth = parseFirstBrace ? 0 : 1;
            do {
                if(!ReadToken(ref token)) {
                    return false;
                }
                if(token.type == TokenType.Punctuation) {
                    if(token.lexme == "{") {
                        depth++;
                    } else if(token.lexme == "}") {
                        depth--;
                    }
                }
            } while(depth != 0);
            return true;
        }

        public bool ReadTokenOnLine(ref Token token) {
            Token t = new Token();
            
            if(!ReadToken(ref t)) {
                parseIndex = lastParseIndex;
                line = lastLine;
                return false;
            }

            if(t.lineCrossed == 0) {
                token = t;
                return true;
            }

            parseIndex = lastParseIndex;
            line = lastLine;
            token.Clear();
            return false;
        }

        public bool CheckTokenString(string s, ref Token token) {
            if(!ReadToken(ref token)) {
                return false;
            }
            if(token.lexme == s) {
                return true;
            }
            parseIndex = lastParseIndex;
            line = lastLine;
            return false;
        }

        public bool ExpectTokenString(string s) {
            Token token = new Token();

            if(!ReadToken(ref token)) {
                throw new InvalidDataException($"Couldn't find expected {s}");
            }
            if(token.lexme != s) {
                throw new InvalidDataException($"Expected {s} but found {token.lexme}");
            }
            
            return true;
        }

        public int ParseInteger() {
            Token token = new Token();
            if(!ReadToken(ref token)) {
                throw new InvalidDataException("Couldn't read expected integer.");
            }
            if(token.type == TokenType.Punctuation && token.lexme == "-") {
                ExpectTokenTypeOnLine(TokenType.Number, TokenSubType.Integer, ref token);
                return -token.integerValue;
            } else if(token.type != TokenType.Number || token.subType == TokenSubType.Float) {
                throw new InvalidDataException($"Expected integer value, but found {token.lexme}");
            }

            return token.integerValue;
        }

        public float ParseFloat() {
            Token token = new Token();
            if(!ReadToken(ref token)) {
                throw new InvalidDataException("Couldn't read expected floating point number.");
            }
            if(token.type == TokenType.Punctuation && token.lexme == "-") {
                ExpectTokenTypeOnLine(TokenType.Number, TokenSubType.Float, ref token);
                return -token.floatValue;
            }
            if(token.type != TokenType.Number) {
                throw new InvalidDataException($"Expected float value, but found {token.lexme}");
            }
            return token.floatValue;
        }

        public bool ParseBool() {
            Token token = new Token();
            if(!ReadToken(ref token)) {
                throw new InvalidDataException("Couldn't read expected floating point number.");
            }
            if(token.lexme.ToUpper() == "TRUE") {
                return true;
            } else {
                return false;
            }
        }

        public bool ParseCSharpVector(ref SharpDX.Vector2 v) {
            if(!ExpectTokenString("(")) {
                return false;
            }

            float x = ParseFloat();
            ExpectTokenString(",");
            float y = ParseFloat();
            v = new SharpDX.Vector2(x, y);
            return ExpectTokenString(")");
        }

        public bool ParseCSharpVector(ref SharpDX.Vector3 v) {
            if(!ExpectTokenString("(")) {
                return false;
            }

            float x = ParseFloat();
            ExpectTokenString(",");
            float y = ParseFloat();
            ExpectTokenString(",");
            float z = ParseFloat();
            v = new SharpDX.Vector3(x, y, z);
            return ExpectTokenString(")");
        }

        public bool ParseCSharpVector(ref SharpDX.Vector4 v) {
            if(!ExpectTokenString("(")) {
                return false;
            }

            float x = ParseFloat();
            ExpectTokenString(",");
            float y = ParseFloat();
            ExpectTokenString(",");
            float z = ParseFloat();
            ExpectTokenString(",");
            float w = ParseFloat();
            v = new SharpDX.Vector4(x, y, z, w);
            return ExpectTokenString(")");
        }

        public bool ParseCSharpVector(ref SharpDX.Quaternion v) {
            if(!ExpectTokenString("(")) {
                return false;
            }

            float x = ParseFloat();
            ExpectTokenString(",");
            float y = ParseFloat();
            ExpectTokenString(",");
            float z = ParseFloat();
            ExpectTokenString(",");
            float w = ParseFloat();
            v = new SharpDX.Quaternion(x, y, z, w);
            return ExpectTokenString(")");
        }

        public bool ParseCSharpColor(ref SharpDX.Color color) {
            if(!ExpectTokenString("RGBA")) {
                return false;
            }
            if(!ExpectTokenString("(")) {
                return false;
            }

            float r = ParseFloat();
            ExpectTokenString(",");
            float g = ParseFloat();
            ExpectTokenString(",");
            float b = ParseFloat();
            ExpectTokenString(",");
            float a = ParseFloat();
            color = new SharpDX.Color(r, g, b, a);
            return ExpectTokenString(")");            
        }

        public bool ParseVector(ref ushort[] v, int start, int n) {
            if(!ExpectTokenString("(")) {
                return false;
            }

            for(int i = 0; i < n; i++) {
                v[start + i] = (ushort)ParseInteger();
            }
            return ExpectTokenString(")");
        }

        public bool ParseVector(ref float[] v, int start, int n) {
            if(!ExpectTokenString("(")) {
                return false;
            }

            for(int i = 0; i < n; i++) {
                v[start + i] = ParseFloat();
            }
            return ExpectTokenString(")");
        }

        public bool ParseVector(ref SharpDX.Vector2 v) {
            if(!ExpectTokenString("(")) {
                return false;
            }

            v = new SharpDX.Vector2(ParseFloat(), ParseFloat());
            return ExpectTokenString(")");
        }

        public bool ParseVector(ref SharpDX.Vector3 v) {
            if(!ExpectTokenString("(")) {
                return false;
            }

            v = new SharpDX.Vector3(ParseFloat(), ParseFloat(), ParseFloat());
            return ExpectTokenString(")");
        }

        public bool ParseVector(ref SharpDX.Vector4 v) {
            if(!ExpectTokenString("(")) {
                return false;
            }

            v = new SharpDX.Vector4(ParseFloat(), ParseFloat(), ParseFloat(), ParseFloat());
            return ExpectTokenString(")");
        }

        public bool ParseVector(ref SharpDX.Quaternion v) {
            if(!ExpectTokenString("(")) {
                return false;
            }

            v = new SharpDX.Quaternion(ParseFloat(), ParseFloat(), ParseFloat(), ParseFloat());
            return ExpectTokenString(")");
        }
    }
}