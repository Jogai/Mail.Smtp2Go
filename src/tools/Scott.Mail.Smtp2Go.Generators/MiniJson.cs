using System.Globalization;
using System.Text;

namespace Scott.Mail.Smtp2Go.Generators;

/// <summary>
/// A small JSON reader for the endpoint snapshot. Generators run inside the compiler, where System.Text.Json is not guaranteed to be
/// loadable, so the few constructs the snapshot uses are parsed here: objects become <see cref="Dictionary{TKey, TValue}"/>, arrays
/// <see cref="List{T}"/>, numbers <see cref="double"/>, and strings, booleans and null themselves.
/// </summary>
internal static class MiniJson
{
    public static object? Parse(string text)
    {
        if (text is null)
        {
            throw new ArgumentNullException(nameof(text));
        }

        Reader reader = new(text);
        reader.SkipWhitespace();
        object? value = reader.ReadValue();
        reader.SkipWhitespace();
        if (!reader.AtEnd)
        {
            throw reader.Error("unexpected trailing content");
        }

        return value;
    }

    private struct Reader(string text)
    {
        private readonly string _text = text;
        private int _position;

        public readonly bool AtEnd => _position >= _text.Length;

        public object? ReadValue()
        {
            if (AtEnd)
            {
                throw Error("unexpected end of input");
            }

            char c = _text[_position];
            switch (c)
            {
                case '{':
                    return ReadObject();
                case '[':
                    return ReadArray();
                case '"':
                    return ReadString();
                case 't':
                    Expect("true");
                    return true;
                case 'f':
                    Expect("false");
                    return false;
                case 'n':
                    Expect("null");
                    return null;
                default:
                    if (c == '-' || (c >= '0' && c <= '9'))
                    {
                        return ReadNumber();
                    }

                    throw Error($"unexpected character '{c}'");
            }
        }

        public void SkipWhitespace()
        {
            while (!AtEnd && (_text[_position] is ' ' or '\t' or '\r' or '\n'))
            {
                _position++;
            }
        }

        public readonly FormatException Error(string message)
        {
            return new FormatException($"Invalid JSON at offset {_position.ToString(CultureInfo.InvariantCulture)}: {message}.");
        }

        private Dictionary<string, object?> ReadObject()
        {
            Dictionary<string, object?> result = new(StringComparer.Ordinal);
            _position++; // {
            SkipWhitespace();
            if (!AtEnd && _text[_position] == '}')
            {
                _position++;
                return result;
            }

            while (true)
            {
                SkipWhitespace();
                if (AtEnd || _text[_position] != '"')
                {
                    throw Error("expected a property name");
                }

                string name = ReadString();
                SkipWhitespace();
                if (AtEnd || _text[_position] != ':')
                {
                    throw Error("expected ':'");
                }

                _position++;
                SkipWhitespace();
                result[name] = ReadValue();
                SkipWhitespace();
                if (AtEnd)
                {
                    throw Error("unterminated object");
                }

                char c = _text[_position++];
                if (c == '}')
                {
                    return result;
                }

                if (c != ',')
                {
                    throw Error("expected ',' or '}'");
                }
            }
        }

        private List<object?> ReadArray()
        {
            List<object?> result = [];
            _position++; // [
            SkipWhitespace();
            if (!AtEnd && _text[_position] == ']')
            {
                _position++;
                return result;
            }

            while (true)
            {
                SkipWhitespace();
                result.Add(ReadValue());
                SkipWhitespace();
                if (AtEnd)
                {
                    throw Error("unterminated array");
                }

                char c = _text[_position++];
                if (c == ']')
                {
                    return result;
                }

                if (c != ',')
                {
                    throw Error("expected ',' or ']'");
                }
            }
        }

        private string ReadString()
        {
            _position++; // opening quote
            StringBuilder builder = new();
            while (true)
            {
                if (AtEnd)
                {
                    throw Error("unterminated string");
                }

                char c = _text[_position++];
                if (c == '"')
                {
                    return builder.ToString();
                }

                if (c != '\\')
                {
                    builder.Append(c);
                    continue;
                }

                if (AtEnd)
                {
                    throw Error("unterminated escape sequence");
                }

                char escaped = _text[_position++];
                switch (escaped)
                {
                    case '"':
                    case '\\':
                    case '/':
                        builder.Append(escaped);
                        break;
                    case 'b':
                        builder.Append('\b');
                        break;
                    case 'f':
                        builder.Append('\f');
                        break;
                    case 'n':
                        builder.Append('\n');
                        break;
                    case 'r':
                        builder.Append('\r');
                        break;
                    case 't':
                        builder.Append('\t');
                        break;
                    case 'u':
                        if (_position + 4 > _text.Length || !ushort.TryParse(_text.Substring(_position, 4), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out ushort code))
                        {
                            throw Error("invalid \\u escape");
                        }

                        builder.Append((char)code);
                        _position += 4;
                        break;
                    default:
                        throw Error($"invalid escape '\\{escaped}'");
                }
            }
        }

        private double ReadNumber()
        {
            int start = _position;
            while (!AtEnd && (_text[_position] is '-' or '+' or '.' or 'e' or 'E' or (>= '0' and <= '9')))
            {
                _position++;
            }

            string slice = _text.Substring(start, _position - start);
            if (!double.TryParse(slice, NumberStyles.Float, CultureInfo.InvariantCulture, out double value))
            {
                throw Error($"invalid number '{slice}'");
            }

            return value;
        }

        private void Expect(string literal)
        {
            if (string.CompareOrdinal(_text, _position, literal, 0, literal.Length) != 0)
            {
                throw Error($"expected '{literal}'");
            }

            _position += literal.Length;
        }
    }
}
